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

    const int playerNumber = 4; //仮置き。偶数でなければならない。エントリー画面で設定する予定。
    public List<PlayerStatus> playerStatuses = new List<PlayerStatus>();

    public int Count => playerStatuses.Count;

    // AwakeでInstanceを設定
    void Awake()
    {
        Instance = this;

        var half = playerNumber / 2;
        //プレイヤーのステータスを初期化する
        for (int i = 0; i < playerNumber; i++)
        {
            PlayerStatus playerStatus = new PlayerStatus();
            playerStatus.health = 100;
            playerStatus.positionX = 0;
            playerStatus.positionY = 0;
            playerStatus.Crystals = 0;
            playerStatus.teamNumber = (i < half) ? 0 : 1;
            playerStatus.attackDeley = 0.0f;
            playerStatuses.Add(playerStatus);
        }
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
