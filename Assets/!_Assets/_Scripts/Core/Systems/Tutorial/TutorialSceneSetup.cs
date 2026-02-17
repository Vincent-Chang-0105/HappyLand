using UnityEngine;

public class TutorialSceneSetup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private CustomerGenerator customerGenerator;

    [Header("Settings")]
    [SerializeField] private bool autoStartTutorial = true;
    [SerializeField] private float startDelay = 0.5f;

    private void Start()
    {
        // Stop auto customer generation for tutorial (StopCustomerGeneration stops the coroutine)
        if (customerGenerator != null)
        {
            customerGenerator.StopCustomerGeneration();
            //Debug.Log("TutorialSceneSetup: Stopped customer auto-generation for tutorial.");
        }

        // Subscribe to tutorial completion to auto-load Level1
        if (tutorialManager != null)
        {
            tutorialManager.OnTutorialCompleted += EndTutorialAndLoadGame;
        }
        else if (TutorialManager.HasInstance)
        {
            TutorialManager.Instance.OnTutorialCompleted += EndTutorialAndLoadGame;
        }

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

    public void EndTutorialAndLoadGame()
    {
        // Load the main game scene when tutorial is complete
        UnityEngine.SceneManagement.SceneManager.LoadScene("Level1");
    }
}
