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
    const int playerNumber = 4; //仮置き。偶数でなければならない。エントリー画面で設定する予定。
    public List<PlayerStatus> playerStatuses = new List<PlayerStatus>();
    void Start()
    {
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

}
