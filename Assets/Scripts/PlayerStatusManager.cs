using UnityEngine;
using System.Collections.Generic;
public class PlayerStatus
{
    //プレーヤーのステータスを管理するクラス
    public int health;
    public int positionX;
    public int positionY;
    public int Crystals;
    public int teamNumber;
    public float attackDeley;
}

public class PlayerStatusManager : MonoBehaviour
{
    public static PlayerStatusManager Instance;

    public List<PlayerStatus> playerStatuses = new List<PlayerStatus>();

    public int Count => playerStatuses.Count;
    public bool TeamsConfirmed { get; private set; }

    // AwakeでInstanceを設定
    void Awake()
    {
        // 既にインスタンスがあれば自分を破棄(重複防止)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // エントリーで1人ぶん追加しidを返す関数
    public int AddPlayer()
    {
        PlayerStatus playerStatus = new PlayerStatus();
        playerStatus.health = 100;
        playerStatus.positionX = 0;
        playerStatus.positionY = 0;
        playerStatus.Crystals = 0;
        playerStatus.teamNumber = -1; // チーム未割り当て
        playerStatus.attackDeley = 0.0f;
        playerStatuses.Add(playerStatus);

        return playerStatuses.Count - 1;
    }

    // エントリー確定時にだけチームを振り分ける。
    public void ConfirmTeamsByHalf()
    {
        var half = (playerStatuses.Count + 1) / 2;
        for (int i = 0; i < playerStatuses.Count; i++)
        {
            playerStatuses[i].teamNumber = (i < half) ? 0 : 1;
        }

        TeamsConfirmed = true;
    }

    // playerIdに対応するステータスを返す関数
    public PlayerStatus GetStatus(int id)
    {
        // 無効なidではエラー
        if (id < 0 || id >= playerStatuses.Count)
        {
            Debug.LogError($"PlayerStatusManager: 無効なplayerId {id} (件数 {playerStatuses.Count})");
            return null;
        }
        return playerStatuses[id];
    }
}
