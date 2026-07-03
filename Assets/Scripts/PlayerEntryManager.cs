using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputManager))]
public class PlayerEntryManager : MonoBehaviour
{
    [SerializeField] Transform[] spawnPoints;

    PlayerInputManager manager;

    void Awake()
    {
        manager = GetComponent<PlayerInputManager>();
    }

    void OnEnable()
    {
        manager.onPlayerJoined += OnPlayerJoined;
        manager.onPlayerLeft += OnPlayerLeft;
    }

    void OnDisable()
    {
        if (manager == null) return;
        manager.onPlayerJoined -= OnPlayerJoined;
        manager.onPlayerLeft -= OnPlayerLeft;
    }

    void OnPlayerJoined(PlayerInput input)
    {
        int id = PlayerStatusManager.Instance.AddPlayer();

        var registrar = input.GetComponent<PlayerRegistrar>();
        if (registrar != null) registrar.SetPlayerId(id);

        // 参加プレイヤーをMainGameシーンへ持ち越す（GridSys登録はMainGame側で行われる）
        DontDestroyOnLoad(input.gameObject);

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var p = spawnPoints[input.playerIndex % spawnPoints.Length];
            input.transform.SetPositionAndRotation(p.position, p.rotation);
        }

        var deviceName = input.devices.Count > 0 ? input.devices[0].displayName : "(no device)";
        Debug.Log($"Player joined: id={id}, team={PlayerStatusManager.Instance.GetStatus(id).teamNumber}, device={deviceName}");
    }

    void OnPlayerLeft(PlayerInput input)
    {
        Debug.Log($"Player left: playerIndex={input.playerIndex}");
    }
}
