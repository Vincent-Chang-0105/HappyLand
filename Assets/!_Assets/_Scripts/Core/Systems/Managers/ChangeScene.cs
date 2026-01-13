using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public void ChangeSceneFunction(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
