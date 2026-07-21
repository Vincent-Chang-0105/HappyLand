using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class SceneTransitionManager : PersistentSingleton<SceneTransitionManager>
{
    [Header("Loading Screen UI")]
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private Image progressBar;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float minLoadTime = 0.8f;

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadAsync(() => SceneManager.LoadSceneAsync(sceneName)));
    }

    public void LoadScene(int buildIndex)
    {
        StartCoroutine(LoadAsync(() => SceneManager.LoadSceneAsync(buildIndex)));
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator LoadAsync(System.Func<AsyncOperation> loadOperation)
    {
        if (progressBar != null) progressBar.fillAmount = 0f;

        loadingCanvasGroup.gameObject.SetActive(true);
        loadingCanvasGroup.alpha = 0f;
        yield return loadingCanvasGroup.DOFade(1f, fadeDuration)
            .SetUpdate(true)
            .WaitForCompletion();

        AsyncOperation op = loadOperation();
        op.allowSceneActivation = false;

        float elapsed = 0f;
        float displayProgress = 0f;
        while (!op.isDone)
        {
            elapsed += Time.unscaledDeltaTime;

            float targetProgress = Mathf.Clamp01(op.progress / 0.9f);
            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.unscaledDeltaTime * 0.8f);
            if (progressBar != null)
                progressBar.fillAmount = displayProgress;

            if (op.progress >= 0.9f && elapsed >= minLoadTime)
                op.allowSceneActivation = true;

            yield return null;
        }

        yield return loadingCanvasGroup.DOFade(0f, fadeDuration)
            .SetUpdate(true)
            .WaitForCompletion();
        loadingCanvasGroup.gameObject.SetActive(false);
    }
}
