using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerEntryStageLabel : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] Sprite backgroundSprite;
    [SerializeField] TMP_FontAsset fontAsset;
    [SerializeField] Color textColor = new Color(0.23f, 0.12f, 0.02f, 1f);

    const float CanvasWidth = 1000f;
    const float StageCoverage = 0.9f;
    const float StageGap = 0.02f;
    const float FrontOffset = 0.001f;

    TextMeshProUGUI playerNumber;
    int number = 1;

    void Awake()
    {
        BuildLabel();
        RefreshText();
    }

    public void SetPlayerNumber(int value)
    {
        number = Mathf.Max(1, value);
        if (playerNumber == null) BuildLabel();
        RefreshText();
    }

    void BuildLabel()
    {
        var existing = transform.Find("playernumberBG");
        if (existing != null)
        {
            playerNumber = existing.GetComponentInChildren<TextMeshProUGUI>(true);
            if (playerNumber != null) return;
        }

        var backgroundObject = new GameObject("playernumberBG", typeof(RectTransform));
        backgroundObject.layer = gameObject.layer;

        var backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.SetParent(transform, false);
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        CalculateLayout(
            out var canvasSize,
            out var canvasScale,
            out var canvasPosition);
        backgroundRect.sizeDelta = canvasSize;
        backgroundRect.localPosition = canvasPosition;
        backgroundRect.localRotation = Quaternion.identity;
        backgroundRect.localScale = canvasScale;

        var canvas = backgroundObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10;

        var scaler = backgroundObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        var background = backgroundObject.AddComponent<Image>();
        background.sprite = backgroundSprite;
        background.color = Color.white;
        background.preserveAspect = false;
        background.raycastTarget = false;

        var textObject = new GameObject("playerNumber", typeof(RectTransform));
        textObject.layer = gameObject.layer;

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(backgroundRect, false);
        textRect.anchorMin = new Vector2(0.08f, 0.12f);
        textRect.anchorMax = new Vector2(0.92f, 0.88f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.localScale = Vector3.one;

        playerNumber = textObject.AddComponent<TextMeshProUGUI>();
        playerNumber.font = fontAsset;
        playerNumber.color = textColor;
        playerNumber.fontStyle = FontStyles.Bold;
        playerNumber.alignment = TextAlignmentOptions.Center;
        playerNumber.enableAutoSizing = true;
        // Canvasの縦解像度に比例させ、補正前と同じワールド上の文字サイズを保つ。
        playerNumber.fontSizeMin = canvasSize.y * 0.18f;
        playerNumber.fontSizeMax = canvasSize.y * 0.72f;
        playerNumber.textWrappingMode = TextWrappingModes.NoWrap;
        playerNumber.raycastTarget = false;
    }

    void CalculateLayout(
        out Vector2 canvasSize,
        out Vector3 canvasScale,
        out Vector3 canvasPosition)
    {
        var box = GetComponent<BoxCollider>();
        var boxSize = box != null ? box.size : Vector3.one;
        var boxCenter = box != null ? box.center : Vector3.zero;

        var targetLocalWidth = boxSize.x * StageCoverage;
        var targetLocalHeight = boxSize.y * StageCoverage;
        var stageScaleX = Mathf.Max(Mathf.Abs(transform.localScale.x), 0.0001f);
        var stageScaleY = Mathf.Max(Mathf.Abs(transform.localScale.y), 0.0001f);

        // 親のX/Yスケール比をCanvasの縦横比で相殺する。
        // 現在は (15 / 5) = 3 なので1000 x 333.33pxとなり、
        // 1pxあたりのワールド寸法がX/Yとも同じになる。
        var worldAspect = targetLocalWidth * stageScaleX
            / (targetLocalHeight * stageScaleY);
        var canvasHeight = CanvasWidth / worldAspect;
        canvasSize = new Vector2(CanvasWidth, canvasHeight);

        var scaleX = targetLocalWidth / canvasSize.x;
        var scaleY = targetLocalHeight / canvasSize.y;
        canvasScale = new Vector3(scaleX, scaleY, Mathf.Min(scaleX, scaleY));

        var stageBottom = boxCenter.y - boxSize.y * 0.5f;
        var stageFront = boxCenter.z - boxSize.z * 0.5f;
        canvasPosition = new Vector3(
            boxCenter.x,
            stageBottom - boxSize.y * StageGap - targetLocalHeight * 0.5f,
            stageFront - FrontOffset);
    }

    void RefreshText()
    {
        if (playerNumber != null) playerNumber.text = $"P{number}";
    }
}
