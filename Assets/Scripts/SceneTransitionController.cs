using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-10000)]
public sealed class SceneTransitionController : MonoBehaviour
{
    const string AssetsResourceName = "SceneTransitionAssets";
    const float DefaultDuration = 0.75f;

    static SceneTransitionController instance;

    RectTransform leftPanel;
    RectTransform rightPanel;
    CanvasGroup canvasGroup;
    bool isTransitioning;
    float timeScaleBeforeTransition = 1f;

    public static bool IsTransitioning => instance != null && instance.isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;

        var assets = Resources.Load<SceneTransitionAssets>(AssetsResourceName);
        if (assets == null
            || assets.LeftPanelSprite == null
            || assets.RightPanelSprite == null)
        {
            Debug.LogError("SceneTransitionController: transition assets are missing.");
            return;
        }

        var root = new GameObject("Scene Transition");
        root.layer = LayerMask.NameToLayer("UI");

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var group = root.AddComponent<CanvasGroup>();
        var controller = root.AddComponent<SceneTransitionController>();
        controller.Initialize(
            CreatePanel(root.transform, "FadeL", assets.LeftPanelSprite),
            CreatePanel(root.transform, "FadeR", assets.RightPanelSprite),
            group);
    }

    static RectTransform CreatePanel(Transform parent, string name, Sprite sprite)
    {
        var panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.layer = parent.gameObject.layer;

        var rect = panelObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panelObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = true;
        image.preserveAspect = false;
        return rect;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Initialize(RectTransform left, RectTransform right, CanvasGroup group)
    {
        leftPanel = left;
        rightPanel = right;
        canvasGroup = group;
        SetPanelX(0f, 0f);
        canvasGroup.blocksRaycasts = true;
    }

    IEnumerator Start()
    {
        isTransitioning = true;
        PauseGameplay();
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return AnimatePanels(GetOpenLeftX(), GetOpenRightX());
        canvasGroup.blocksRaycasts = false;
        isTransitioning = false;
        ResumeGameplay();
    }

    public static bool LoadScene(string sceneName, Action beforeLoad = null)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return false;
        if (instance == null) Bootstrap();

        if (instance == null)
        {
            beforeLoad?.Invoke();
            SceneManager.LoadScene(sceneName);
            return true;
        }

        if (instance.isTransitioning) return false;
        instance.StartCoroutine(instance.LoadSceneRoutine(sceneName, beforeLoad));
        return true;
    }

    IEnumerator LoadSceneRoutine(string sceneName, Action beforeLoad)
    {
        isTransitioning = true;
        canvasGroup.blocksRaycasts = true;
        PauseGameplay();
        Canvas.ForceUpdateCanvases();

        yield return AnimatePanels(0f, 0f);

        beforeLoad?.Invoke();
        Time.timeScale = 0f;
        SceneManager.LoadScene(sceneName);

        // 切替後の画面を1フレーム閉じたままにしてからシャッターを開く。
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return AnimatePanels(GetOpenLeftX(), GetOpenRightX());

        canvasGroup.blocksRaycasts = false;
        isTransitioning = false;
        ResumeGameplay();
    }

    IEnumerator AnimatePanels(float targetLeftX, float targetRightX)
    {
        var startLeftX = leftPanel.anchoredPosition.x;
        var startRightX = rightPanel.anchoredPosition.x;
        var elapsed = 0f;

        while (elapsed < DefaultDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var normalizedTime = Mathf.Clamp01(elapsed / DefaultDuration);
            var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            SetPanelX(
                Mathf.LerpUnclamped(startLeftX, targetLeftX, easedTime),
                Mathf.LerpUnclamped(startRightX, targetRightX, easedTime));
            yield return null;
        }

        SetPanelX(targetLeftX, targetRightX);
    }

    float GetOpenLeftX()
    {
        return -leftPanel.rect.width;
    }

    float GetOpenRightX()
    {
        return rightPanel.rect.width;
    }

    void SetPanelX(float leftX, float rightX)
    {
        leftPanel.anchoredPosition = new Vector2(leftX, 0f);
        rightPanel.anchoredPosition = new Vector2(rightX, 0f);
    }

    void PauseGameplay()
    {
        timeScaleBeforeTransition = Time.timeScale;
        Time.timeScale = 0f;
    }

    void ResumeGameplay()
    {
        Time.timeScale = timeScaleBeforeTransition;
    }
}
