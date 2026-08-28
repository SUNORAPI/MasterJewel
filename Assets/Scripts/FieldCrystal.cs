using UnityEngine;

// フィールド上の採掘対象となる宝石。
public class FieldCrystal : MonoBehaviour
{
    FieldObjectSpawner spawner;

    public void Initialize(FieldObjectSpawner fieldObjectSpawner)
    {
        spawner = fieldObjectSpawner;
    }

    public void Hit(int attackPower, Vector3 hitPosition)
    {
        if (attackPower <= 0) return;

        var activeSpawner = spawner != null ? spawner : FieldObjectSpawner.Instance;
        if (activeSpawner == null)
        {
            Debug.LogWarning("Field crystal could not drop points because no FieldObjectSpawner exists.", this);
            return;
        }

        // ポイントは整数管理のため、小数部分は切り捨てる。
        int droppedPoints = Mathf.FloorToInt(attackPower * 0.2f);
        activeSpawner.SpawnDroppedCrystal(hitPosition, droppedPoints);
    }
}
