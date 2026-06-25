using UnityEngine;
using UnityEngine.InputSystem;

// Startボタンが押された未参加のゲームパッドを1人ずつエントリーさせる。
// PlayerInputManagerの「Joinaction = Start」「Notification Behavior = Invoke C Sharp Events」を前提とする。
[RequireComponent(typeof(PlayerInputManager))]
public class PlayerEntryManager : MonoBehaviour
{
    [SerializeField] Transform[] spawnPoints; // 任意：参加位置（playerIndexで選択）

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

    // Startが押されてプレイヤーPrefabが生成された直後に呼ばれる
    // （生成オブジェクトのStartより前なので、ここでplayerIdを渡せる）
    void OnPlayerJoined(PlayerInput input)
    {
        // ステータスを1人ぶん作成し、idを生成プレハブのPlayerRegistrarに渡す
        int id = PlayerStatusManager.Instance.AddPlayer();

        var registrar = input.GetComponent<PlayerRegistrar>();
        if (registrar != null) registrar.SetPlayerId(id);

        // 参加プレイヤーをMainGameシーンへ持ち越す（GridSys登録はMainGame側で行われる）
        DontDestroyOnLoad(input.gameObject);

        // 任意：参加位置へ配置
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
        // idとステータスの対応がずれるため、ステータスは残す（プロトタイプ方針）。
        // グリッドからの登録解除はPlayerRegistrar.OnDestroyが行う。
        Debug.Log($"Player left: playerIndex={input.playerIndex}");
    }
}
