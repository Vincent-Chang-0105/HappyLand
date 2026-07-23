using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using AudioSystem;

/// <summary>
/// Generic pan cooking station. All mode-specific logic lives in CookingModeSO subclasses.
/// Pan detects which mode is active by checking which mode's UnlockRoles are all present,
/// then delegates CanCook / StartCooking / Stop / Complete to that mode.
/// </summary>
public class Pan : CookingStation
{
    [Header("Cooking Modes (priority order — most specific first)")]
    [SerializeField] private List<CookingModeSO> modes;

    [Header("Gesture")]
    [SerializeField] private GestureStateMachine gestureMachine;

    [Header("Visuals")]
    [SerializeField] private PanVisualController visuals;

    [Header("Audio")]
    [SerializeField] private SoundData sizzleSound;
    [SerializeField] private SoundData panImpactSound;
    [SerializeField] private SoundData pourSound;
    [SerializeField] private SoundData ingredientAddSound;

    [Header("Pan Bob Animation")]
    [SerializeField] private float panBobHeightMin = 0.08f;
    [SerializeField] private float panBobHeightMax = 0.25f;
    [SerializeField] private float panBobDurationMin = 0.4f;
    [SerializeField] private float panBobDurationMax = 0.8f;

    [Header("Idle Sizzle Animation")]
    [SerializeField] private float sizzleShakeStrength = 0.03f;
    [SerializeField] private float sizzleShakeFrequency = 20f;

    [Header("Pour Settings")]
    [SerializeField] private float pourTiltAngle = 45f;
    [SerializeField] private float pourTiltSmoothing = 8f;
    [SerializeField] private float pourDetectRadius = 1.5f;
    [SerializeField] private float pourPickupRadius = 1.5f;
    [SerializeField] private float pourDuration = 0.4f;

    // --- Ingredient state ---
    private readonly HashSet<IngredientRole> addedRoles = new HashSet<IngredientRole>();
    private CookingModeSO activeMode;
    // Noodle object is a special GameObject that travels with the pour
    private GameObject noodleObject;

    // --- Pour state ---
    private bool isPourable;
    private bool isPouring;
    private bool isDraggingPan;
    private Vector3 dragOffset;
    private Camera mainCamera;
    private PlatingStation nearbyPlatingStation;

    // --- Animation state ---
    private Vector3 originalPanPosition;
    private Quaternion originalPanRotation;
    private SoundEmitter currentSizzleEmitter;
    private Sequence panBobSequence;
    private bool isSizzleAnimating;

    #region CookingStation abstract overrides

    protected override bool CanCookIngredient(GameObject ingredient)
        => activeMode?.CanCook(ingredient) ?? false;

    protected override void StartCookingIngredient(GameObject ingredient)
        => activeMode?.StartCooking(ingredient);

    protected override void StopCookingIngredient(GameObject ingredient)
        => activeMode?.StopCooking(ingredient);

    protected override void CompleteCookingIngredient(GameObject ingredient)
        => activeMode?.CompleteCooking(ingredient);

    protected override bool AdditionalBowlAcceptanceCheck()
    {
        if (isPouring || isPourable) return false;
        // Need at least one mode unlocked
        return activeMode != null;
    }

    protected override string GetCookingProcessName()
        => activeMode != null ? activeMode.outputDish.ToString() : "cooking";

    #endregion

    #region Unity Lifecycle

    protected override void Start()
    {
        base.Start();

        originalPanPosition = transform.localPosition;
        originalPanRotation = transform.localRotation;

        if (gestureMachine != null)
        {
            gestureMachine.OnGestureComplete    += OnGestureComplete;
            gestureMachine.OnAllGesturesComplete += OnAllGesturesComplete;
            gestureMachine.OnGestureProgress    += OnGestureProgress;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopSizzleSound();

        if (gestureMachine != null)
        {
            gestureMachine.OnGestureComplete    -= OnGestureComplete;
            gestureMachine.OnAllGesturesComplete -= OnAllGesturesComplete;
            gestureMachine.OnGestureProgress    -= OnGestureProgress;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (isPourable && !isPouring)
            HandlePourInput();
    }

    #endregion

    #region Ingredient Role API (called by DraggableIngredient)

    /// <summary>Returns true if this role can currently be added to the pan.</summary>
    public bool CanAcceptIngredient(IngredientRole role)
    {
        if (isPourable || isPouring) return false;
        if (addedRoles.Contains(role)) return false;

        // After chicken arrives we're in "waiting for seasoning" — only accept post-bowl roles
        if (isCooking)
        {
            if (!currentTossStateIsWaitingForSeasoning) return false;
            return activeMode != null && activeMode.PostBowlRoles.Contains(role);
        }

        return modes != null && modes.Any(m => m.AcceptsRole(role, addedRoles));
    }

    /// <summary>Add an ingredient role; refreshes active mode and updates visuals.</summary>
    public void AddIngredient(IngredientRole role, Ingredient ingredientData = null)
    {
        if (!CanAcceptIngredient(role)) return;

        addedRoles.Add(role);
        PlayIngredientAddSound();

        // Special tutorial hooks
        if (role == IngredientRole.FryingOil)  TutorialEvents.OilAdded();
        if (role == IngredientRole.SinigangMix) TutorialEvents.SinigangMixAdded();
        if (role == IngredientRole.FryingSalt)  TutorialEvents.SaltAdded();

        // Oil triggers sizzle immediately
        if (role == IngredientRole.FryingOil)
            PlaySizzleSound();

        RefreshActiveMode();
        visuals?.OnRoleAdded(role, activeMode);

        // If chicken is already in the pan and we just completed seasoning, auto-start
        if (isCooking && activeMode != null && currentTossStateIsWaitingForSeasoning)
        {
            if (activeMode.IsFullySeasoned(addedRoles))
                StartCooking();
        }
    }

    // Noodle is special: a full GameObject is handed into the pan
    public bool CanAcceptNoodles()
        => CanAcceptIngredient(IngredientRole.NoodleIngredient);

    public void AddNoodles(GameObject noodleObj)
    {
        if (!CanAcceptNoodles() || noodleObj == null) return;

        noodleObject = noodleObj;
        addedRoles.Add(IngredientRole.NoodleIngredient);
        PlayIngredientAddSound();
        RefreshActiveMode();
        visuals?.OnRoleAdded(IngredientRole.NoodleIngredient, activeMode);

        // Animate noodle into pan
        Transform container = ingredientContainer != null ? ingredientContainer : transform;
        noodleObject.transform.SetParent(container);
        noodleObject.transform.DOLocalMove(new Vector3(0f, 0.1f, 0f), 0.35f).SetEase(Ease.OutBack);
        noodleObject.transform.DOScale(0.8f, 0.35f).SetEase(Ease.OutBack);
    }

    private bool currentTossStateIsWaitingForSeasoning = false;

    private void RefreshActiveMode()
    {
        activeMode = modes?.FirstOrDefault(m =>
            !m.IsBlockedBy(addedRoles) && m.IsUnlockedBy(addedRoles));
    }

    #endregion

    #region Cooking

    protected override void StartCooking()
    {
        // Allow re-entry when seasoning just completed; block all other duplicate calls
        if (isCooking && !currentTossStateIsWaitingForSeasoning) return;
        if (ingredientsInStation.Count == 0) return;
        if (activeMode == null) return;

        // Frying/Adobo need post-bowl seasonings before tossing begins
        if (!activeMode.IsFullySeasoned(addedRoles))
        {
            currentTossStateIsWaitingForSeasoning = true;
            isCooking = true;
            NotificationManager.Instance.SetNewNotification(activeMode.SeasoningNotification);
            return;
        }

        currentTossStateIsWaitingForSeasoning = false;
        isCooking = true;

        foreach (var ingredient in ingredientsInStation)
            if (ingredient != null) StartCookingIngredient(ingredient);

        PlaySizzleSound();
        StartSizzleAnimation();
        StartPanBobAnimation();

        if (cookingEffect != null) cookingEffect.Play();

        gestureMachine?.Begin(
            activeMode.requiredGestures,
            activeMode.gestureCookInterval,
            transform,
            originalPanPosition,
            originalPanRotation,
            ingredientsInStation);

        if (cookingMeterUI != null) cookingMeterUI.SetActive(true);
    }

    protected override void UpdateCooking()
    {
        // GestureStateMachine drives all timing; only update UI position here
        if (cookingMeterUI != null && cookingMeterUI.activeInHierarchy)
            cookingMeterUI.transform.position =
                Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
    }

    protected override void CompleteCooking()
    {
        foreach (var ingredient in ingredientsInStation)
            if (ingredient != null) CompleteCookingIngredient(ingredient);

        isCooking = false;
        isPourable = true;
        currentTossStateIsWaitingForSeasoning = false;

        TutorialEvents.AllTossesCompleted();

        if (activeMode?.allGesturesCompleteSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().Play(activeMode.allGesturesCompleteSound);

        if (cookingMeterUI != null) cookingMeterUI.SetActive(false);
        if (cookingEffect  != null) cookingEffect.Stop();

        StopSizzleSound();
        StopSizzleAnimation();
        StopPanBobAnimation();

        transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 4, 0.5f);
    }

    #endregion

    #region Gesture Events

    private void OnGestureComplete()
    {
        StopPanBobAnimation();
        StopSizzleAnimation();
        StopSizzleSound();
        UpdateProgressBar(0f);

        if (activeMode?.gestureCompleteSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().WithRandomPitch()
                .Play(activeMode.gestureCompleteSound);
    }

    private void OnAllGesturesComplete()
    {
        CompleteCooking();
    }

    private void OnGestureProgress(float velocityProgress)
    {
        // Resume sizzle between tosses
        if (isCooking && !isSizzleAnimating)
        {
            StartSizzleAnimation();
            PlaySizzleSound();
            StartPanBobAnimation();
        }

        UpdateProgressBar(velocityProgress);
    }

    private void UpdateProgressBar(float progress)
    {
        if (cookingProgressBar != null)
        {
            cookingProgressBar.fillAmount = progress;
            cookingProgressBar.color = Color.Lerp(Color.yellow, Color.red, progress);
        }
    }

    #endregion

    #region Pour Mechanic

    private void HandlePourInput()
    {
        if (Input.GetMouseButtonDown(0) && !isDraggingPan)
        {
            mainCamera = Camera.main;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = transform.position.z;
            if (Vector2.Distance(mouseWorldPos, transform.position) <= pourPickupRadius)
            {
                isDraggingPan = true;
                dragOffset = transform.position - mouseWorldPos;
            }
        }

        if (isDraggingPan && Input.GetMouseButton(0))
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = transform.position.z;
            transform.position = mouseWorldPos + dragOffset;

            Vector3 delta = transform.position - originalPanPosition;
            float tiltTarget = Mathf.Clamp(delta.x * -(pourTiltAngle / 3f), -pourTiltAngle, pourTiltAngle);
            float currentZ = transform.eulerAngles.z;
            if (currentZ > 180f) currentZ -= 360f;
            float newZ = Mathf.LerpAngle(currentZ, tiltTarget, Time.unscaledDeltaTime * pourTiltSmoothing);
            transform.rotation = Quaternion.Euler(0, 0, newZ);

            UpdatePlatingStationHighlight();
        }

        if (isDraggingPan && Input.GetMouseButtonUp(0))
        {
            isDraggingPan = false;
            if (nearbyPlatingStation != null) nearbyPlatingStation.SetHighlight(false);

            PlatingStation station = FindNearbyPlatingStation();
            if (station != null)
            {
                StartPourToPlate(station);
                return;
            }
            SnapBackToOriginal();
        }
    }

    private PlatingStation FindNearbyPlatingStation()
    {
        foreach (var col in Physics2D.OverlapCircleAll(transform.position, pourDetectRadius))
        {
            var station = col.GetComponent<PlatingStation>();
            if (station != null) return station;
        }
        return null;
    }

    private void UpdatePlatingStationHighlight()
    {
        PlatingStation newNearby = FindNearbyPlatingStation();
        if (newNearby == nearbyPlatingStation) return;

        nearbyPlatingStation?.SetHighlight(false);
        nearbyPlatingStation = newNearby;
        nearbyPlatingStation?.SetHighlight(true);
    }

    private void StartPourToPlate(PlatingStation station)
    {
        isPouring = true;

        if (pourSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().Play(pourSound);

        var ingredientsCopy = new List<GameObject>(ingredientsInStation);
        if (activeMode is NoodleModeSO && noodleObject != null)
            ingredientsCopy.Add(noodleObject);

        DishType dishType = activeMode != null ? activeMode.outputDish : DishType.Unknown;

        station.AcceptPour(ingredientsCopy, dishType);

        float totalDelay = ingredientsCopy.Count * 0.1f + pourDuration;
        DOVirtual.DelayedCall(totalDelay, CompletePanPour);
    }

    private void CompletePanPour()
    {
        ingredientsInStation.Clear();

        isCooking  = false;
        isPourable = false;
        isPouring  = false;
        isDraggingPan = false;
        currentTossStateIsWaitingForSeasoning = false;

        noodleObject = null;
        addedRoles.Clear();
        activeMode = null;

        gestureMachine?.Reset();
        visuals?.Reset();

        if (cookingMeterUI != null) cookingMeterUI.SetActive(false);
        if (cookingEffect  != null) cookingEffect.Stop();

        transform.DOKill();
        SnapBackToOriginal();
    }

    private void SnapBackToOriginal()
    {
        transform.DOLocalMove(originalPanPosition, 0.3f).SetEase(Ease.OutQuad);
        transform.DOLocalRotateQuaternion(originalPanRotation, 0.3f).SetEase(Ease.OutQuad);
    }

    public void Reset()
    {
        var toDestroy = new List<GameObject>(ingredientsInStation);
        CompletePanPour();
        foreach (var ingredient in toDestroy)
            if (ingredient != null) Destroy(ingredient);
    }

    #endregion

    #region Audio

    private void PlaySizzleSound()
    {
        StopSizzleSound();
        if (SoundManager.Instance != null && sizzleSound != null)
        {
            currentSizzleEmitter = SoundManager.Instance
                .CreateSoundBuilder()
                .WithPosition(transform.position)
                .Play(sizzleSound);
        }
    }

    private void StopSizzleSound()
    {
        currentSizzleEmitter?.Stop();
        currentSizzleEmitter = null;
    }

    private void PlayIngredientAddSound()
    {
        if (ingredientAddSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().WithRandomPitch().Play(ingredientAddSound);
    }

    #endregion

    #region Idle Animation

    private void StartPanBobAnimation()
    {
        StopPanBobAnimation();
        float bobHeight   = Random.Range(panBobHeightMin, panBobHeightMax);
        float bobDuration = Random.Range(panBobDurationMin, panBobDurationMax);

        panBobSequence = DOTween.Sequence();
        panBobSequence.Append(
            transform.DOLocalMoveY(originalPanPosition.y + bobHeight, bobDuration)
                .SetEase(Ease.InOutSine));
        panBobSequence.Append(
            transform.DOLocalMoveY(originalPanPosition.y, bobDuration)
                .SetEase(Ease.InOutSine));
        panBobSequence.SetLoops(-1);
    }

    private void StopPanBobAnimation()
    {
        panBobSequence?.Kill();
        panBobSequence = null;
        transform.DOLocalMoveY(originalPanPosition.y, 0.1f);
    }

    private void StartSizzleAnimation()
    {
        if (isSizzleAnimating) return;
        isSizzleAnimating = true;
        AnimateSizzleLoop();
    }

    private void AnimateSizzleLoop()
    {
        if (!isSizzleAnimating) return;
        transform.DOShakePosition(1f / sizzleShakeFrequency,
            new Vector3(sizzleShakeStrength, 0, 0), 1, 0f, false)
            .OnComplete(() => { if (isSizzleAnimating) AnimateSizzleLoop(); });
    }

    private void StopSizzleAnimation()
    {
        isSizzleAnimating = false;
    }

    #endregion
}
