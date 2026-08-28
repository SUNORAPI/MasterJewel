using UnityEngine;

// 複数ポイントを1つにまとめて保持する取得用宝石。
public class DroppedCrystal : MonoBehaviour
{
    public int Points { get; private set; }

    bool collected;

    public void Initialize(int points)
    {
        Points = Mathf.Max(0, points);
    }

    void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    void TryCollect(Collider other)
    {
        if (collected || Points <= 0 || PlayerStatusManager.Instance == null) return;

        var registrar = other.GetComponentInParent<PlayerRegistrar>();
        if (registrar == null) return;

        var status = PlayerStatusManager.Instance.GetStatus(registrar.PlayerId);
        if (status == null) return;

        collected = true;
        status.Crystals += Points;
        Debug.Log($"Player {registrar.PlayerId} が宝石 {Points}P を取得した");
        Destroy(gameObject);
    }
}
