using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ResultScreenController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] Camera resultCamera;
    [SerializeField] TMP_Text resultTitle;
    [SerializeField] TMP_Text blueWinText;
    [SerializeField] TMP_Text redWinText;
    [SerializeField] string titleSceneName = "Title";

    [Header("Avatar Layout")]
    [SerializeField, Range(0.05f, 0.45f)] float blueCenterX = 0.25f;
    [SerializeField, Range(0.55f, 0.95f)] float redCenterX = 0.75f;
    [SerializeField, Range(0.1f, 0.9f)] float avatarCenterY = 0.42f;
    [SerializeField, Range(0.1f, 0.8f)] float avatarViewportHeight = 0.38f;
    [SerializeField, Min(0.01f)] float avatarSpacing = 0.13f;
    [SerializeField, Min(0.31f)] float cameraDistance = 8f;

    readonly List<GameObject> avatarPreviews = new List<GameObject>();
    bool returnRequested;

    void Start()
    {
        var statusManager = PlayerStatusManager.Instance;
        if (statusManager == null || statusManager.Count == 0)
        {
            SetWinnerText(-1);
            return;
        }

        var blueScore = 0;
        var redScore = 0;
        var bluePlayers = 0;
        var redPlayers = 0;
        for (var id = 0; id < statusManager.Count; id++)
        {
            var status = statusManager.GetStatus(id);
            if (status == null) continue;

            if (status.teamNumber == 0)
            {
                blueScore += status.Crystals;
                bluePlayers++;
            }
            else if (status.teamNumber == 1)
            {
                redScore += status.Crystals;
                redPlayers++;
            }
        }

        var winningTeam = statusManager.ForcedWinningTeam >= 0
            ? statusManager.ForcedWinningTeam
            : DetermineWinningTeam(
                blueScore,
                redScore,
                bluePlayers,
                redPlayers);
        SetWinnerText(winningTeam);
        ShowWinningAvatars(winningTeam);
    }

    void Update()
    {
        if (returnRequested || SceneTransitionController.IsTransitioning) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        var returnPressed = keyboard.spaceKey.wasPressedThisFrame
            || keyboard.enterKey.wasPressedThisFrame
            || keyboard.numpadEnterKey.wasPressedThisFrame;
        if (!returnPressed) return;

        if (SceneTransitionController.LoadScene(titleSceneName, CleanupSession))
            returnRequested = true;
    }

    static void CleanupSession()
    {
        var registrars = FindObjectsByType<PlayerRegistrar>(FindObjectsInactive.Include);
        foreach (var registrar in registrars)
        {
            if (registrar != null) Destroy(registrar.gameObject);
        }

        if (PlayerStatusManager.Instance != null)
            Destroy(PlayerStatusManager.Instance.gameObject);
        PlayerStatusManager.Instance = null;
        Time.timeScale = 1f;
    }

    static int DetermineWinningTeam(
        int blueScore,
        int redScore,
        int bluePlayers,
        int redPlayers)
    {
        if (bluePlayers == 0 && redPlayers == 0) return -1;
        if (bluePlayers > 0 && redPlayers == 0) return 0;
        if (redPlayers > 0 && bluePlayers == 0) return 1;
        if (blueScore == redScore) return -1;
        return blueScore > redScore ? 0 : 1;
    }

    void SetWinnerText(int winningTeam)
    {
        if (blueWinText != null) blueWinText.gameObject.SetActive(winningTeam == 0);
        if (redWinText != null) redWinText.gameObject.SetActive(winningTeam == 1);

        if (resultTitle != null)
            resultTitle.text = winningTeam < 0 ? "Draw" : "Result";
    }

    void ShowWinningAvatars(int winningTeam)
    {
        var registrars = FindObjectsByType<PlayerRegistrar>(FindObjectsInactive.Exclude);
        System.Array.Sort(registrars, (a, b) => a.PlayerId.CompareTo(b.PlayerId));

        var winners = new List<PlayerRegistrar>();
        foreach (var registrar in registrars)
        {
            var status = PlayerStatusManager.Instance.GetStatus(registrar.PlayerId);
            if (status != null && status.teamNumber == winningTeam)
                winners.Add(registrar);
        }

        foreach (var registrar in winners)
        {
            var preview = registrar.CreateAvatarPreview();
            if (preview != null) avatarPreviews.Add(preview);
        }

        // DontDestroyOnLoadされた操作用Playerは、表示用コピーを作ってから隠す。
        foreach (var registrar in registrars)
            registrar.gameObject.SetActive(false);

        if (winningTeam < 0 || avatarPreviews.Count == 0) return;
        if (resultCamera == null) resultCamera = Camera.main;
        if (resultCamera == null) return;

        EnsureResultLight();
        LayoutAvatars(winningTeam);
    }

    void LayoutAvatars(int winningTeam)
    {
        var centerX = winningTeam == 0 ? blueCenterX : redCenterX;
        var gapCount = avatarPreviews.Count - 1;
        var totalWidth = Mathf.Min(avatarSpacing * gapCount, 0.28f);
        var spacing = gapCount > 0 ? totalWidth / gapCount : 0f;
        var maxAvatarWidth = Mathf.Min(0.18f, 0.38f / avatarPreviews.Count);

        for (var i = 0; i < avatarPreviews.Count; i++)
        {
            var viewportX = centerX - totalWidth * 0.5f + spacing * i;
            FitAvatarToViewport(
                avatarPreviews[i],
                new Vector2(viewportX, avatarCenterY),
                maxAvatarWidth);
        }
    }

    void FitAvatarToViewport(
        GameObject avatar,
        Vector2 viewportCenter,
        float maxViewportWidth)
    {
        if (!TryGetRendererBounds(avatar, out var bounds)) return;

        var viewportBottom = resultCamera.ViewportToWorldPoint(new Vector3(
            viewportCenter.x,
            viewportCenter.y - avatarViewportHeight * 0.5f,
            cameraDistance));
        var viewportTop = resultCamera.ViewportToWorldPoint(new Vector3(
            viewportCenter.x,
            viewportCenter.y + avatarViewportHeight * 0.5f,
            cameraDistance));
        var targetHeight = Vector3.Distance(viewportBottom, viewportTop);
        var viewportLeft = resultCamera.ViewportToWorldPoint(new Vector3(
            viewportCenter.x - maxViewportWidth * 0.5f,
            viewportCenter.y,
            cameraDistance));
        var viewportRight = resultCamera.ViewportToWorldPoint(new Vector3(
            viewportCenter.x + maxViewportWidth * 0.5f,
            viewportCenter.y,
            cameraDistance));
        var targetWidth = Vector3.Distance(viewportLeft, viewportRight);
        if (bounds.size.y <= Mathf.Epsilon || bounds.size.x <= Mathf.Epsilon) return;

        var heightScale = targetHeight / bounds.size.y;
        var widthScale = targetWidth / bounds.size.x;
        avatar.transform.localScale *= Mathf.Min(heightScale, widthScale);
        if (!TryGetRendererBounds(avatar, out bounds)) return;

        var targetCenter = resultCamera.ViewportToWorldPoint(new Vector3(
            viewportCenter.x,
            viewportCenter.y,
            cameraDistance));
        avatar.transform.position += targetCenter - bounds.center;
    }

    static bool TryGetRendererBounds(GameObject avatar, out Bounds bounds)
    {
        var renderers = avatar.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    void EnsureResultLight()
    {
        if (FindAnyObjectByType<Light>() != null) return;

        var lightObject = new GameObject("Result Avatar Light");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        var resultLight = lightObject.AddComponent<Light>();
        resultLight.type = LightType.Directional;
        resultLight.color = new Color(1f, 0.96f, 0.84f);
        resultLight.intensity = 1f;
        resultLight.shadows = LightShadows.Soft;
    }

    void OnDestroy()
    {
        foreach (var preview in avatarPreviews)
        {
            if (preview != null) Destroy(preview);
        }
    }
}
