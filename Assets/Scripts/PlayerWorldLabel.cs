using TMPro;
using UnityEngine;

// プレイヤーの頭上にP番号を表示し、所属チームの色へ自動更新する。
[DisallowMultipleComponent]
public sealed class PlayerWorldLabel : MonoBehaviour
{
    [SerializeField] Color blueTeamColor = new Color(0.15f, 0.55f, 1f, 1f);
    [SerializeField] Color redTeamColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] Color pendingColor = Color.white;
    [SerializeField, Min(0f)] float heightOffsetInCells = 0.1f;
    [SerializeField, Min(0.001f)] float worldScaleInCells = 0.08f;

    Transform labelTransform;
    TextMeshPro label;
    Camera activeCamera;
    int playerId = -1;
    int displayedTeam = int.MinValue;

    public void Initialize(int id)
    {
        playerId = id;
        if (!EnsureLabel()) return;
        label.text = playerId >= 0 ? $"P{playerId + 1}" : "P?";
        RefreshColor();
    }

    void LateUpdate()
    {
        if (!EnsureLabel()) return;
        RefreshColor();

        // シーン切替中に子オブジェクトが破棄されても、このフレームの処理を継続しない。
        var currentLabelTransform = labelTransform;
        if (currentLabelTransform == null) return;

        if (activeCamera == null) activeCamera = Camera.main;

        float cellSize = GridSys.Instance != null ? GridSys.Instance.CellSize : 4f;
        float top = GetPlayerTop(cellSize);
        currentLabelTransform.position = new Vector3(
            transform.position.x,
            top + cellSize * heightOffsetInCells,
            transform.position.z);

        // 親Playerのゲーム用縮小率に影響されず、全プレイヤーで同じ表示サイズにする。
        float parentScale = Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z));
        currentLabelTransform.localScale = Vector3.one
            * (cellSize * worldScaleInCells / Mathf.Max(parentScale, 0.0001f));

        if (activeCamera != null)
        {
            currentLabelTransform.rotation = activeCamera.transform.rotation;
        }
    }

    bool EnsureLabel()
    {
        if (label != null && labelTransform != null) return true;

        // Unityでは破棄済みObjectのC#参照が次フレームまで残ることがあるため、
        // 片方でも失われていれば古い参照を捨ててラベル全体を作り直す。
        label = null;
        labelTransform = null;
        if (!isActiveAndEnabled) return false;

        var labelObject = new GameObject("Player Number Label");
        labelTransform = labelObject.transform;
        labelTransform.SetParent(transform, false);

        label = labelObject.AddComponent<TextMeshPro>();
        displayedTeam = int.MinValue;
        label.text = playerId >= 0 ? $"P{playerId + 1}" : "P?";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 6f;
        label.fontStyle = FontStyles.Bold;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.outlineWidth = 0.2f;
        label.outlineColor = Color.black;

        var meshRenderer = labelObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 1000;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.allowOcclusionWhenDynamic = false;
        }
        return label != null && labelTransform != null;
    }

    void RefreshColor()
    {
        if (label == null) return;

        int team = -1;
        var manager = PlayerStatusManager.Instance;
        if (manager != null && playerId >= 0 && playerId < manager.Count)
        {
            var status = manager.GetStatus(playerId);
            if (status != null) team = status.teamNumber;
        }

        if (team == displayedTeam) return;
        displayedTeam = team;
        label.color = team == 0
            ? blueTeamColor
            : team == 1
                ? redTeamColor
                : pendingColor;
    }

    float GetPlayerTop(float cellSize)
    {
        float top = float.NegativeInfinity;
        foreach (var playerRenderer in GetComponentsInChildren<Renderer>())
        {
            if (labelTransform != null
                && (playerRenderer.transform == labelTransform
                    || playerRenderer.transform.IsChildOf(labelTransform)))
            {
                continue;
            }

            top = Mathf.Max(top, playerRenderer.bounds.max.y);
        }

        var playerCollider = GetComponent<Collider>();
        float colliderTop = playerCollider != null
            ? playerCollider.bounds.max.y
            : transform.position.y;
        if (float.IsNegativeInfinity(top)) return colliderTop;

        // 異常に大きいSkinnedMeshのBoundsで画面外へ飛ばないよう、最大高さを制限する。
        return Mathf.Clamp(top, colliderTop, transform.position.y + cellSize * 2f);
    }
}
