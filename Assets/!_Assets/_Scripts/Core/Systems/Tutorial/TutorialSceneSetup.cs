using UnityEngine;
using UnityEngine.UI;

public class TutorialSceneSetup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private CustomerGenerator customerGenerator;
    [SerializeField] private TutorialUIPanel uiPanel;
    [SerializeField] private TutorialArrowIndicator arrowIndicator;
    [SerializeField] private ScreenTransitionManager screenTransitionManager;
    [SerializeField] private ParticleSystem taskCompletionVFX;

    [Header("Settings")]
    [SerializeField] private bool autoStartTutorial = true;
    [SerializeField] private float startDelay = 0.5f;

    [Header("Skip")]
    [SerializeField] private Button skipButton;

    private void Start()
    {
        // Stop auto customer generation for tutorial (StopCustomerGeneration stops the coroutine)
        if (customerGenerator != null)
        {
            customerGenerator.StopCustomerGeneration();
            //Debug.Log("TutorialSceneSetup: Stopped customer auto-generation for tutorial.");
        }

        // Refresh scene references on the persistent TutorialManager so it uses this scene's objects
        TutorialManager tm = TutorialManager.HasInstance ? TutorialManager.Instance : tutorialManager;
        if (tm != null)
            tm.RefreshSceneReferences(uiPanel, arrowIndicator, screenTransitionManager, taskCompletionVFX);

        // Subscribe to tutorial completion to auto-load Level1
        if (tutorialManager != null)
        {
            tutorialManager.OnTutorialCompleted += EndTutorialAndLoadGame;
        }
        else if (TutorialManager.HasInstance)
        {
            TutorialManager.Instance.OnTutorialCompleted += EndTutorialAndLoadGame;
        }

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipTutorial);

        if (autoStartTutorial)
        {
            Invoke(nameof(StartTutorial), startDelay);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (tutorialManager != null)
        {
            tutorialManager.OnTutorialCompleted -= EndTutorialAndLoadGame;
        }
        else if (TutorialManager.HasInstance)
        {
            TutorialManager.Instance.OnTutorialCompleted -= EndTutorialAndLoadGame;
        }
    }

    public void StartTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.StartTutorial();
        }
        else if (TutorialManager.HasInstance)
        {
            TutorialManager.Instance.StartTutorial();
        }
        else
        {
            Debug.LogError("TutorialSceneSetup: No TutorialManager found!");
        }
    }

    public void SpawnTutorialCustomer(string orderName = "")
    {
        if (customerGenerator != null)
        {
            if (!string.IsNullOrEmpty(orderName))
            {
                customerGenerator.SpawnCustomerWithSpecificOrder(orderName);
            }
            else
            {
                customerGenerator.ForceSpawnCustomer();
            }
        }
    }

    public void SkipTutorial()
    {
        TutorialManager tm = tutorialManager != null ? tutorialManager :
                             TutorialManager.HasInstance ? TutorialManager.Instance : null;
        if (tm != null)
            tm.StopTutorial(); // fires OnTutorialCompleted → EndTutorialAndLoadGame
        else
            EndTutorialAndLoadGame(); // fallback: load directly
    }

    public void EndTutorialAndLoadGame()
    {
        // Load the main game scene when tutorial is complete
        SceneTransitionManager.Instance.LoadScene("Level1");
    }
}
