using UnityEngine;

// プレイヤーGameObjectに付け、自分のPlayerStatusをGridSysに登録する
public class PlayerRegistrar : MonoBehaviour
{
    [SerializeField] int playerId; // playerStatuses の添字。当面はInspectorで手動設定（後でエントリー画面が設定）

    void Start()
    {
        var status = PlayerStatusManager.Instance.GetStatus(playerId);
        if (status == null) return;
        GridSys.Instance.Register(playerId, transform, status);
    }

    void OnDestroy()
    {
        // シーン終了時のnull参照を避けるため null チェック
        if (GridSys.Instance != null) GridSys.Instance.Unregister(playerId);
    }
}
