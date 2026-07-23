using System.Collections;
using TMPro;
using UnityEngine;

public class NotificationManager : ImpersistentSingleton<NotificationManager>
{
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float fadeTime;

    private Coroutine activeCoroutine;
    private Color baseColor;

    protected override void Awake()
    {
        base.Awake();
        notificationText.text = "";
        if (notificationText != null) baseColor = notificationText.color;
    }

    public void SetNewNotification(string message)
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(FadeOutNotification(message));
    }

    private IEnumerator FadeOutNotification(string message)
    {
        notificationText.text = message;
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            baseColor.a = Mathf.Lerp(1f, 0f, t / fadeTime);
            notificationText.color = baseColor;
            yield return null;
        }
    }
}