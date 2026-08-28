using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class TitleScreenController : MonoBehaviour
{
    [Header("Blink (Visible : Hidden = 3 : 1)")]
    [SerializeField, Min(0.01f)] float visibleDuration = 0.9f;
    [SerializeField, Min(0.01f)] float hiddenDuration = 0.3f;

    [Header("Scene Transition")]
    [SerializeField] string entrySceneName = "PlayerEntry";

    TextMeshProUGUI promptText;
    float nextBlinkTime;
    bool isTransitioning;

    void Awake()
    {
        promptText = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        SetPromptVisible(true);
        nextBlinkTime = Time.unscaledTime + visibleDuration;
    }

    void Update()
    {
        UpdateBlink();

        if (isTransitioning
            || SceneTransitionController.IsTransitioning
            || !WasStartPressed()) return;

        if (SceneTransitionController.LoadScene(entrySceneName))
        {
            isTransitioning = true;
            SetPromptVisible(true);
        }
    }

    void UpdateBlink()
    {
        if (Time.unscaledTime < nextBlinkTime) return;

        SetPromptVisible(!promptText.enabled);
        nextBlinkTime = Time.unscaledTime
            + (promptText.enabled ? visibleDuration : hiddenDuration);
    }

    void SetPromptVisible(bool isVisible)
    {
        promptText.enabled = isVisible;
    }

    static bool WasStartPressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        foreach (var gamepad in Gamepad.all)
        {
            foreach (var control in gamepad.allControls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame)
                    return true;
            }
        }

        return false;
    }
}
