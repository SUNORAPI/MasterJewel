using UnityEngine;

// 自身のPlayerStatusをGridSysに登録する
public class PlayerRegistrar : MonoBehaviour
{
    [SerializeField] int playerId = -1; // エントリー時に決まる（-1なら未設定のまま）

    bool registered; // GridSysへの二重登録防止フラグ

    // PAC/PHPMが自分のplayerIdを参照するための公開プロパティ
    public int PlayerId => playerId;

    // エントリー時にPlayerEntryManagerから呼ぶ
    public void SetPlayerId(int id) => playerId = id;

    void Start()
    {
        // ゲームシーンへ移ったらGridSys.StartからRegisterToGridが呼ばれる。
        if (GridSys.Instance != null) RegisterToGrid();
    }

    // GridSysへ自身を登録
    public void RegisterToGrid()
    {
        if (registered) return;
        if (playerId < 0)
        {
            Debug.LogError("PlayerRegistrar: playerIdが未設定のまま登録されようとしました");
            return;
        }
        var status = PlayerStatusManager.Instance.GetStatus(playerId);
        if (status == null) return;
        GridSys.Instance.Register(playerId, transform, status);
        registered = true;
    }

    void OnDestroy()
    {
        // シーン終了時のエラー防止nullチェック
        if (registered && GridSys.Instance != null) GridSys.Instance.Unregister(playerId);
    }
}
