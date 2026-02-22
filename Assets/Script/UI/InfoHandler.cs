using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoHandler : MonoBehaviour
{
    public TMP_Text text;
    public Image image;
    public Image background;
    public string texttoshow;
    public Sprite toshowimage;
    public float fadeDuration = 0.25f;
    public float showDuration = 2f;
   

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public IEnumerator showinfo()
    {
        if (text != null) text.text = texttoshow;
        if (image != null && toshowimage != null) image.sprite = toshowimage;

        SetAlpha(0f);
        SetEnabled(true);

        // Fade in
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / fadeDuration);
            SetAlpha(a);
            yield return null;
        }

        // Hold
        yield return new WaitForSeconds(showDuration);

        // Fade out
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / fadeDuration);
            SetAlpha(a);
            yield return null;
        }

        SetEnabled(false);
    }

    private void SetAlpha(float a)
    {
        if (text != null)
        {
            Color c = text.color;
            c.a = a;
            text.color = c;
        }
        if (image != null)
        {
            Color c = image.color;
            c.a = a;
            image.color = c;
        }
        if (background != null)
        {
            Color c = background.color;
            c.a = a;
            background.color = c;
        }
    }

    private void SetEnabled(bool enabled)
    {
        if (text != null) text.enabled = enabled;
        if (image != null) image.enabled = enabled;
        if (background != null) background.enabled = enabled;
    }
}
