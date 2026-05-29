using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class InfoHandler : MonoBehaviour
{
    public TMP_Text text; public Image image;
    public Image background; public string texttoshow; public Sprite toshowimage;
    public float fadeDuration = 0.25f; public float showDuration = 2f;
    private Coroutine showInfoCoroutine;
    private readonly Queue<InfoPayload> queuedInfos = new Queue<InfoPayload>();
    private struct InfoPayload { public string message; public Sprite icon; }
    public void ShowInfoNow(string message, Sprite icon)
    {
        texttoshow = message;
        toshowimage = icon; queuedInfos.Clear(); if (showInfoCoroutine != null)
        {
            StopCoroutine(showInfoCoroutine); showInfoCoroutine = null;
        }
        showInfoCoroutine = StartCoroutine(ShowImmediateRoutine(texttoshow, toshowimage));
    }
    public void QueueInfo(string message, Sprite icon)
    {
        if (string.IsNullOrWhiteSpace(message) && icon == null) { return; }
        queuedInfos.Enqueue(new InfoPayload { message = message, icon = icon });
        if (showInfoCoroutine == null) { showInfoCoroutine = StartCoroutine(ShowQueueRoutine()); }
    }
    private IEnumerator ShowImmediateRoutine(string message, Sprite icon)
    {
        yield return ShowInfoRoutine(message, icon); showInfoCoroutine = null;
    }
    private IEnumerator ShowQueueRoutine()
    {
        while (queuedInfos.Count > 0)
        {
            InfoPayload payload = queuedInfos.Dequeue();
            yield return ShowInfoRoutine(payload.message, payload.icon);
        }
        showInfoCoroutine = null;
    }
    private IEnumerator ShowInfoRoutine(string message, Sprite icon)
    {
        if (text != null) { text.text = message; }
        if (image != null) { image.sprite = icon; }
        SetAlpha(0f); SetEnabled(true); float t = 0f; while (t < fadeDuration)
        {
            t += Time.deltaTime; float a = Mathf.Clamp01(t / fadeDuration); SetAlpha(a);
            yield return null;
        }
        yield return new WaitForSeconds(showDuration); t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / fadeDuration); SetAlpha(a); yield return null;
        }
        SetEnabled(false);
    }
    private void SetAlpha(float a)
    {
        if (text != null)
        {
            Color c = text.color; c.a = a; text.color = c;
        }
        if (image != null)
        {
            Color c = image.color; c.a = a; image.color = c;
        }
        if (background != null)
        {
            Color c = background.color; c.a = a; background.color = c;
        }
    }
    private void SetEnabled(bool enabled)
    {
        if (text != null) { text.enabled = enabled; }
        if (image != null) { image.enabled = enabled; }
        if (background != null) { background.enabled = enabled; }
    }
}
