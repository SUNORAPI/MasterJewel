using UnityEngine;

// 自身のPlayerStatusをGridSysに登録する
public class PlayerRegistrar : MonoBehaviour
{
    [SerializeField] int playerId = -1;

    [Header("Avatar")]
    [SerializeField] GameObject[] avatarPrefabs;
    [SerializeField] Vector3 avatarLocalPosition = Vector3.zero;
    [SerializeField] Vector3 avatarLocalEulerAngles = Vector3.zero;
    [SerializeField] Vector3 avatarLocalScale = Vector3.one;

    bool registered;
    GameObject avatarInstance;
    Vector3 facingDirection = Vector3.forward;

    public int PlayerId => playerId;
    public Vector3 FacingDirection => facingDirection;

    public void SetPlayerId(int id)
    {
        playerId = id;
        ApplyAvatar();
        EnsureWorldLabel();
    }

    void Start()
    {
        // シーン上でplayerIdを直接指定した場合にもアバターを反映する。
        if (avatarInstance == null) ApplyAvatar();
        EnsureWorldLabel();

        // ゲームシーンへ移ったらGridSys.StartからRegisterToGridが呼ばれる。
        if (GridSys.Instance != null) RegisterToGrid();
    }

    void ApplyAvatar()
    {
        if (playerId < 0 || avatarPrefabs == null || playerId >= avatarPrefabs.Length)
        {
            Debug.LogWarning($"PlayerRegistrar: playerId {playerId} に対応するアバターがありません", this);
            return;
        }

        var avatarPrefab = avatarPrefabs[playerId];
        if (avatarPrefab == null)
        {
            Debug.LogWarning($"PlayerRegistrar: アバター {playerId + 1} が未設定です", this);
            return;
        }

        if (avatarInstance != null)
        {
            Destroy(avatarInstance);
        }

        avatarInstance = Instantiate(avatarPrefab, transform, false);
        avatarInstance.name = $"PlayerAvatar_{playerId + 1}";
        avatarInstance.transform.SetLocalPositionAndRotation(
            avatarLocalPosition,
            Quaternion.Euler(avatarLocalEulerAngles));
        avatarInstance.transform.localScale = avatarLocalScale;
        PrepareAvatarRendering();
        ApplyFacingDirection();
    }

    // 大きく拡大したスキンメッシュが初期Boundsでカリングされるのを防ぐ。
    void PrepareAvatarRendering()
    {
        foreach (var renderer in avatarInstance.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
            if (renderer is SkinnedMeshRenderer skinnedRenderer)
            {
                skinnedRenderer.updateWhenOffscreen = true;
            }
        }

        foreach (var animator in avatarInstance.GetComponentsInChildren<Animator>(true))
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    void EnsureWorldLabel()
    {
        var worldLabel = GetComponent<PlayerWorldLabel>();
        if (worldLabel == null) worldLabel = gameObject.AddComponent<PlayerWorldLabel>();
        worldLabel.Initialize(playerId);
    }

    public void SetFacingDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return;

        facingDirection = direction.normalized;
        ApplyFacingDirection();
    }

    void ApplyFacingDirection()
    {
        if (avatarInstance == null) return;

        // FBXごとの正面補正を保ったまま、見た目だけを入力方向へ向ける。
        avatarInstance.transform.rotation = Quaternion.LookRotation(facingDirection, Vector3.up)
            * Quaternion.Euler(avatarLocalEulerAngles);
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
        SetFacingDirection(status.teamNumber == 0 ? Vector3.right : Vector3.left);
        registered = true;
    }

    void OnDestroy()
    {
        // シーン終了時のエラー防止nullチェック
        if (registered && GridSys.Instance != null) GridSys.Instance.Unregister(playerId);
    }
}
