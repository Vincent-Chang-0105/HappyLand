using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to any Button to trigger tutorial events when clicked.
/// Configure the event type in the Inspector.
/// </summary>
public class TutorialEventTrigger : MonoBehaviour
{
    public enum TutorialEventType
    {
        None,
        WashScreenEntered,
        FaucetOpened,
        CookScreenEntered,
        ServeScreenEntered,
        BoilScreenEntered,
        CutScreenEntered
    }

    [SerializeField] private TutorialEventType eventToTrigger;
    [SerializeField] private bool triggerOnStart = false; // Trigger when object becomes active

    private void Start()
    {
        var button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(TriggerEvent);
        }

        if (triggerOnStart)
        {
            TriggerEvent();
        }
    }

    public void TriggerEvent()
    {
        Debug.Log($"TutorialEventTrigger: TriggerEvent called for {eventToTrigger}");
        Debug.Log($"  HasInstance: {TutorialManager.HasInstance}");

        if (!TutorialManager.HasInstance)
        {
            Debug.LogWarning("TutorialEventTrigger: No TutorialManager instance!");
            return;
        }

        Debug.Log($"  IsTutorialActive: {TutorialManager.Instance.IsTutorialActive}");
        Debug.Log($"  CurrentStep: {TutorialManager.Instance.CurrentStep?.stepName ?? "null"}");
        Debug.Log($"  CurrentStep CompletionType: {TutorialManager.Instance.CurrentStep?.completionType}");

        if (!TutorialManager.Instance.IsTutorialActive)
        {
            Debug.LogWarning("TutorialEventTrigger: Tutorial is not active!");
            return;
        }

        Debug.Log($"TutorialEventTrigger: Firing event {eventToTrigger}");

        switch (eventToTrigger)
        {
            case TutorialEventType.WashScreenEntered:
                TutorialEvents.WashScreenEntered();
                break;
            case TutorialEventType.FaucetOpened:
                TutorialEvents.FaucetOpened();
                break;
            case TutorialEventType.CookScreenEntered:
                TutorialEvents.CookScreenEntered();
                break;
            case TutorialEventType.ServeScreenEntered:
                TutorialEvents.ServeScreenEntered();
                break;
            case TutorialEventType.BoilScreenEntered:
                TutorialEvents.BoilScreenEntered();
                break;
            case TutorialEventType.CutScreenEntered:
                TutorialEvents.CutScreenEntered();
                break;
        }
    }
}
