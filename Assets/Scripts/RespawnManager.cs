using System.Collections;
using UnityEditor.UI;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    PlayerHPManager PlayerHPManager;
    [SerializeField] float respawnDelay = 2f;
    [SerializeField] Transform respawnPoint;
    PlayerStatus status;
    public bool isDead;
    public void Respawn(int PlayerId)
    {
        StartCoroutine(Die(PlayerId));
    }

    IEnumerator Die(int PlayerId)
    {
        isDead = true;
        Debug.Log($"Player {PlayerId} は倒れた");

        // TODO: 所持宝石(status.Crystals)のドロップ処理は今回未実装

        // プレイヤーを一時的に無効化（操作・表示・当たり判定を止める）
        PlayerHPManager.SetPlayerEnabled(false);

        yield return new WaitForSeconds(respawnDelay);

        // ステータスと位置を初期化して復帰
        status.health = 100;
        Vector3 pos = respawnPoint != null ? respawnPoint.position : Vector3.zero;
        transform.position = pos;

        PlayerHPManager.SetPlayerEnabled(true);
        isDead = false;
        Debug.Log($"Player {PlayerId} がリスポーンした");
    }
}
