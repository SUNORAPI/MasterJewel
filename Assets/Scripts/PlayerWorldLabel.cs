using TMPro;
using UnityEngine;

// プレイヤーの頭上にP番号を表示し、所属チームの色へ自動更新する。
[DisallowMultipleComponent]
public sealed class PlayerWorldLabel : MonoBehaviour
{
    [SerializeField] Color blueTeamColor = new Color(0.15f, 0.55f, 1f, 1f);
    [SerializeField] Color redTeamColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] Color pendingColor = Color.white;
    [SerializeField, Min(0f)] float heightOffsetInCells = 0.15f;
    [SerializeField, Min(0.001f)] float worldScaleInCells = 0.04f;

    Transform labelTransform;
    TextMeshPro label;
    Camera activeCamera;
    int playerId = -1;
    int displayedTeam = int.MinValue;

    public void Initialize(int id)
    {
        playerId = id;
        EnsureLabel();
        label.text = playerId >= 0 ? $"P{playerId + 1}" : "P?";
        RefreshColor();
    }

    void LateUpdate()
    {
        EnsureLabel();
        RefreshColor();

        if (activeCamera == null) activeCamera = Camera.main;

        float cellSize = GridSys.Instance != null ? GridSys.Instance.CellSize : 4f;
        float top = GetPlayerTop();
        labelTransform.position = new Vector3(
            transform.position.x,
            top + cellSize * heightOffsetInCells,
            transform.position.z);

        // 親Playerのゲーム用縮小率に影響されず、全プレイヤーで同じ表示サイズにする。
        float parentScale = Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z));
        labelTransform.localScale = Vector3.one
            * (cellSize * worldScaleInCells / Mathf.Max(parentScale, 0.0001f));

        if (activeCamera != null)
        {
            labelTransform.rotation = Quaternion.LookRotation(
                labelTransform.position - activeCamera.transform.position,
                activeCamera.transform.up);
        }
    }

    void EnsureLabel()
    {
        if (label != null) return;

        var labelObject = new GameObject("Player Number Label");
        labelTransform = labelObject.transform;
        labelTransform.SetParent(transform, false);

        label = labelObject.AddComponent<TextMeshPro>();
        label.text = playerId >= 0 ? $"P{playerId + 1}" : "P?";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 6f;
        label.fontStyle = FontStyles.Bold;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.outlineWidth = 0.2f;
        label.outlineColor = Color.black;

        var meshRenderer = labelObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null) meshRenderer.sortingOrder = 100;
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

    float GetPlayerTop()
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

        if (!float.IsNegativeInfinity(top)) return top;

        var playerCollider = GetComponent<Collider>();
        return playerCollider != null ? playerCollider.bounds.max.y : transform.position.y;
    }
}
