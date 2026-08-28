using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Tester : MonoBehaviour
{
    [SerializeField] PlayerEntryManager entryManager;

    PlayerInputManager inputManager;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    int simulatedDeviceCount;
#endif

    void Awake()
    {
        if (entryManager == null)
            entryManager = FindAnyObjectByType<PlayerEntryManager>();

        if (entryManager != null)
            inputManager = entryManager.GetComponent<PlayerInputManager>();
    }

    void Start()
    {
        AddKeyboardPlayerOne();
    }

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.J))
            AddSimulatedPlayer();

        for (var count = 1; count <= 8; count++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + count)))
                AddSimulatedPlayersUpTo(count);
        }
#endif

        var confirmPressed = Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (!confirmPressed) return;
        if (entryManager != null && entryManager.IsAnimating) return;

        // 1回目でチーム確定、2回目でゲーム開始。
        if (entryManager != null && !entryManager.TeamsConfirmed)
        {
            entryManager.ConfirmTeams();
            return;
        }

        if (entryManager != null) entryManager.PreparePlayersForGame();
        SceneManager.LoadScene("MainGame");
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void AddSimulatedPlayersUpTo(int targetCount)
    {
        while (PlayerInput.all.Count < targetCount)
        {
            if (!AddSimulatedPlayer()) break;
        }
    }

    bool AddSimulatedPlayer()
    {
        if (entryManager == null || inputManager == null) return false;
        if (entryManager.TeamsConfirmed)
        {
            Debug.LogWarning("Tester: チーム確定後はBotを追加できません");
            return false;
        }

        var device = InputSystem.AddDevice<Gamepad>($"Debug Bot Gamepad {simulatedDeviceCount + 1}");
        var player = inputManager.JoinPlayer(pairWithDevice: device);
        if (player == null)
        {
            InputSystem.RemoveDevice(device);
            Debug.LogWarning("Tester: Botの参加に失敗しました");
            return false;
        }

        simulatedDeviceCount++;
        var bot = player.GetComponent<DebugBotController>();
        if (bot == null) bot = player.gameObject.AddComponent<DebugBotController>();
        bot.Initialize(device);

        Debug.Log($"Tester: Botを追加しました ({PlayerInput.all.Count}/8)");
        return true;
    }
#endif

    void AddKeyboardPlayerOne()
    {
        if (entryManager == null || inputManager == null) return;
        if (PlayerInput.all.Count > 0)
        {
            Debug.LogWarning("Tester: 1Pが既に参加しているためキーボード1Pを追加しません");
            return;
        }

        var device = InputSystem.AddDevice<Gamepad>("Keyboard Player 1");
        var player = inputManager.JoinPlayer(pairWithDevice: device);
        if (player == null)
        {
            InputSystem.RemoveDevice(device);
            Debug.LogError("Tester: キーボード1Pの参加に失敗しました");
            return;
        }

        var controller = player.GetComponent<KeyboardVirtualGamepadController>();
        if (controller == null)
            controller = player.gameObject.AddComponent<KeyboardVirtualGamepadController>();
        controller.Initialize(device);

        Debug.Log("Tester: キーボード1Pを追加しました (WASD / YGHB)");
    }
}
