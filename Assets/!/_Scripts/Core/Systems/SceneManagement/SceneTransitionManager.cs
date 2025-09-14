using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : PersistentSingleton<SceneTransitionManager>
{
    [Header("Transition Settings")]
    [SerializeField] private GameObject transitionCanvas;
    [SerializeField] private Animator transitionAnimator;
    [SerializeField] private float transitionTime = 1f;
    
    [Header("Player Spawn Settings")]
    [SerializeField] private string playerSpawnPointTag = "PlayerSpawn";

    public void TransitionToScene(string sceneName, string spawnPointName = "")
    {
        StartCoroutine(TransitionCoroutine(sceneName, spawnPointName));
    }
    
    private IEnumerator TransitionCoroutine(string sceneName, string spawnPointName)
    {
        // Start transition animation (fade out)
        if (transitionAnimator != null)
        {
            transitionCanvas.SetActive(true);
            transitionAnimator.SetTrigger("FadeOut");
        }
        
        yield return new WaitForSeconds(transitionTime / 2f);
        
        // Load new scene
        SceneManager.LoadScene(sceneName);
        
        yield return new WaitForSeconds(0.1f); // Small delay for scene to load
        
        // Position player at spawn point
        PositionPlayerAtSpawnPoint(spawnPointName);
        
        yield return new WaitForSeconds(transitionTime / 2f);
        
        // End transition animation (fade in)
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("FadeIn");
        }
        
        yield return new WaitForSeconds(transitionTime / 2f);
        
        if (transitionCanvas != null)
            transitionCanvas.SetActive(false);
    }
    
    private void PositionPlayerAtSpawnPoint(string spawnPointName)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        
        GameObject spawnPoint = null;
        
        if (!string.IsNullOrEmpty(spawnPointName))
        {
            spawnPoint = GameObject.Find(spawnPointName);
        }
        
        if (spawnPoint == null)
        {
            spawnPoint = GameObject.FindGameObjectWithTag(playerSpawnPointTag);
        }
        
        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.transform.position;
            player.transform.rotation = spawnPoint.transform.rotation;
        }
    }
}
