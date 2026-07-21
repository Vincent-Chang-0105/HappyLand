using UnityEngine;

public class ChangeScene : MonoBehaviour
{
    public void ChangeSceneFunction(string sceneName)
    {
        SceneTransitionManager.Instance.LoadScene(sceneName);
    }
}
