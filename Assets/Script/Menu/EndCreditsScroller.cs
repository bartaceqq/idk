using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class EndCreditsScroller : MonoBehaviour
{
    [SerializeField] private RectTransform creditsText;
    [SerializeField] private Text creditsTextGraphic;
    [SerializeField] private float startY = 420f;
    [SerializeField] private float endY = 2600f;
    [SerializeField, Min(0.1f)] private float durationSeconds = 47.5f;
    [SerializeField, Min(0f)] private float startDelaySeconds = 0.5f;
    [SerializeField, Min(0f)] private float finishPadding = 80f;
    [SerializeField] private bool loop;
    [SerializeField] private bool returnOnEscape = true;
    [SerializeField] private bool returnWhenFinished = true;
    [SerializeField] private string returnSceneName = "MainMenu";
    private float elapsedSeconds;
    private float resolvedEndY;
    private bool isReturning;

    private void Awake()
    {
        ResolveReferences();
        ResetScrollPosition();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ResetScrollPosition();
    }

    private void Update()
    {
        if (creditsText == null) { return; }
        if (returnOnEscape && Input.GetKeyDown(KeyCode.Escape)) { ReturnToMenu(); return; }
        elapsedSeconds += Time.unscaledDeltaTime;
        if (elapsedSeconds < 0f) { return; }

        float t = Mathf.Clamp01(elapsedSeconds / durationSeconds);
        SetCreditsY(Mathf.Lerp(startY, resolvedEndY, Mathf.SmoothStep(0f, 1f, t)));

        bool finished = t >= 1f || CreditsHaveLeftScreen();
        if (loop && finished) { ResetScrollPosition(); }
        else if (returnWhenFinished && finished) { ReturnToMenu(); }
    }

    private void ResolveReferences()
    {
        if (creditsText == null) { creditsText = transform as RectTransform; }
        if (creditsTextGraphic == null && creditsText != null)
        {
            creditsTextGraphic = creditsText.GetComponent<Text>();
        }
    }

    private void ResetScrollPosition()
    {
        elapsedSeconds = -startDelaySeconds;
        resolvedEndY = ResolveEndY();
        SetCreditsY(startY);
    }

    private float ResolveEndY()
    {
        if (creditsTextGraphic == null) { return endY; }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(creditsText);
        float textHeight = Mathf.Max(creditsTextGraphic.preferredHeight, 1f);
        return textHeight + GetViewportHeight() * 0.5f + finishPadding;
    }

    private bool CreditsHaveLeftScreen()
    {
        if (creditsTextGraphic == null) { return false; }

        float textHeight = Mathf.Max(creditsTextGraphic.preferredHeight, 1f);
        float creditsBottomY = creditsText.anchoredPosition.y - textHeight;
        return creditsBottomY > GetViewportHeight() * 0.5f + finishPadding;
    }

    private float GetViewportHeight()
    {
        Canvas canvas = creditsText != null ? creditsText.GetComponentInParent<Canvas>() : null;
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        return canvasRect != null && canvasRect.rect.height > 1f ? canvasRect.rect.height : 1080f;
    }

    private void SetCreditsY(float y)
    {
        if (creditsText == null) { return; }
        Vector2 position = creditsText.anchoredPosition;
        position.y = y;
        creditsText.anchoredPosition = position;
    }

    private void ReturnToMenu()
    {
        if (isReturning) { return; }
        isReturning = true;
        Time.timeScale = 1f;
        if (!string.IsNullOrWhiteSpace(returnSceneName) && Application.CanStreamedLevelBeLoaded(returnSceneName))
        {
            SceneManager.LoadScene(returnSceneName);
            return;
        }
        SceneManager.LoadScene(0);
    }
}
