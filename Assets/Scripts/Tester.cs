using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Tester : MonoBehaviour
{
    [SerializeField] PlayerEntryManager entryManager;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    readonly List<InputDevice> simulatedDevices = new List<InputDevice>();
    PlayerInputManager inputManager;
#endif

    void Awake()
    {
        if (entryManager == null)
            entryManager = FindAnyObjectByType<PlayerEntryManager>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (entryManager != null)
            inputManager = entryManager.GetComponent<PlayerInputManager>();
#endif
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

        if (!Input.GetKeyDown(KeyCode.Space)) return;
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
            Debug.LogWarning("Tester: チーム確定後は疑似プレイヤーを追加できません");
            return false;
        }

        var device = InputSystem.AddDevice<Gamepad>($"Entry Test Gamepad {simulatedDevices.Count + 1}");
        var player = inputManager.JoinPlayer(pairWithDevice: device);
        if (player == null)
        {
            InputSystem.RemoveDevice(device);
            Debug.LogWarning("Tester: 疑似プレイヤーの参加に失敗しました");
            return false;
        }

        simulatedDevices.Add(device);
        Debug.Log($"Tester: 疑似プレイヤーを追加しました ({PlayerInput.all.Count}/8)");
        return true;
    }

    void OnDestroy()
    {
        foreach (var device in simulatedDevices)
        {
            if (device != null && device.added)
                InputSystem.RemoveDevice(device);
        }
    }
#endif
}
