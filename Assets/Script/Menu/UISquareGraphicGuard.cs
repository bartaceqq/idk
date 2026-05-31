using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UISquareGraphicGuard
{
    private const float DefaultCircleSize = 34f;

    public static void ApplyToOpenScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) { continue; }
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                ApplyToHierarchy(roots[rootIndex]);
            }
        }
    }

    public static void ApplyToHierarchy(GameObject root)
    {
        if (root == null) { return; }
        Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < sliders.Length; i++) { FixSliderHandle(sliders[i]); }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++) { FixCircleImage(images[i]); }
    }

    public static void FixSliderHandle(Slider slider)
    {
        if (slider == null || slider.handleRect == null) { return; }
        RectTransform handle = slider.handleRect;
        float size = ResolveSquareSize(handle);
        Vector2 anchorMin = handle.anchorMin;
        Vector2 anchorMax = handle.anchorMax;

        if (slider.direction == Slider.Direction.LeftToRight ||
            slider.direction == Slider.Direction.RightToLeft)
        {
            float centerY = (anchorMin.y + anchorMax.y) * 0.5f;
            anchorMin.y = centerY; anchorMax.y = centerY;
        }
        else
        {
            float centerX = (anchorMin.x + anchorMax.x) * 0.5f;
            anchorMin.x = centerX; anchorMax.x = centerX;
        }

        handle.anchorMin = anchorMin; handle.anchorMax = anchorMax;
        handle.sizeDelta = new Vector2(size, size);
        Image image = handle.GetComponent<Image>();
        if (image != null) { image.preserveAspect = true; }
    }

    private static void FixCircleImage(Image image)
    {
        if (image == null || image.rectTransform == null || !LooksLikeCircleImage(image)) { return; }
        RectTransform rect = image.rectTransform;
        if (rect.anchorMin != rect.anchorMax) { return; }
        float size = ResolveSquareSize(rect);
        rect.sizeDelta = new Vector2(size, size);
        image.preserveAspect = true;
    }

    private static bool LooksLikeCircleImage(Image image)
    {
        string objectName = image.gameObject.name;
        string spriteName = image.sprite != null ? image.sprite.name : string.Empty;
        return ContainsCircleKeyword(objectName) || ContainsCircleKeyword(spriteName);
    }

    private static bool ContainsCircleKeyword(string value)
    {
        if (string.IsNullOrEmpty(value)) { return false; }
        return value.IndexOf("circle", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            value.IndexOf("spinner", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            value.IndexOf("knob", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            value.IndexOf("handle", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static float ResolveSquareSize(RectTransform rect)
    {
        float width = Mathf.Abs(rect.sizeDelta.x);
        float height = Mathf.Abs(rect.sizeDelta.y);
        if (width > 0.01f && height > 0.01f) { return Mathf.Min(width, height); }
        if (width > 0.01f) { return width; }
        if (height > 0.01f) { return height; }
        float rectWidth = Mathf.Abs(rect.rect.width);
        float rectHeight = Mathf.Abs(rect.rect.height);
        if (rectWidth > 0.01f && rectHeight > 0.01f) { return Mathf.Min(rectWidth, rectHeight); }
        return DefaultCircleSize;
    }
}
