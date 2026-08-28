using System.Collections.Generic;
using UnityEngine;

// 1つのマスの情報
class GridStatus
{
    // このマスに今いるプレイヤーの番号
    public List<int> players = new List<int>();
}

// プレイヤー1人あたりの情報
class Player
{
    public int id;
    public Transform transform;
    public PlayerStatus status;
}

public class GridSys : MonoBehaviour
{
    const float GameplayPlayerScale = 0.3f;

    // Inspectorで設定
    // Gridのスケールとは別物
    //グリッドは左下起点でワールド座標のXZがグリッドのXYに対応する点に注意
    [SerializeField] float originX = 0f;    // Colliderがない場合のグリッド左下ワールド座標X
    [SerializeField] float originZ = 0f;    // Colliderがない場合のグリッド左下ワールド座標Z
    [SerializeField] float cellSize = 4f;   // 1マスのサイズ
    [SerializeField] int width = 16;    // 横のマス数
    [SerializeField] int height = 16;   // 縦のマス数

    [Header("Respawn")]
    [SerializeField, Min(0f)] float respawnClearance = 0.05f;

    public static GridSys Instance;
    public float CellSize => cellSize;
    public int Width => width;
    public int Height => height;

    GridStatus[,] grid;
    List<Player> players = new List<Player>();

    // AwakeでInstanceを設定
    void Awake()
    {
        Instance = this;
        FitBoardToGrid();
    }

    // マス数・マスサイズの変更に合わせて板(このオブジェクトのメッシュ)のスケールを自動調整する
    void OnValidate()
    {
        FitBoardToGrid();
    }

    void FitBoardToGrid()
    {
        var meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
        if (mesh == null) return;

        Vector3 baseSize = mesh.bounds.size;
        if (baseSize.x <= 0f || baseSize.z <= 0f) return;

        Vector3 scale = transform.localScale;
        scale.x = (width * cellSize) / baseSize.x;
        scale.z = (height * cellSize) / baseSize.z;
        transform.localScale = scale;
    }

    void Start()
    {
        // セル作成
        grid = new GridStatus[width, height];
        for (int x = 0; x < width; x++){
            for (int y = 0; y < height; y++){
                grid[x, y] = new GridStatus();
            }
        }

        // エントリー画面から持ち越した既存プレイヤーをまとめて登録する
        foreach (var reg in FindObjectsByType<PlayerRegistrar>())
        {
            reg.RegisterToGrid();
        }
    }

    void Update()
    {
        // プレイヤーリストをリセット
        for (int x = 0; x < width; x++){
            for (int y = 0; y < height; y++){
                grid[x, y].players.Clear();
            }
        }

        // リスポーン配置と同じ盤面境界を、グリッド座標の原点として使う。
        GetFieldBounds(
            out var gridMinX,
            out _,
            out var gridMinZ,
            out _,
            out _);

        // プレイヤーのマス目判定
        foreach (Player p in players)
        {
            Vector3 pos = p.transform.position;

            // ワールド座標をグリッドに変換
            int cellX = Mathf.FloorToInt((pos.x - gridMinX) / cellSize);
            int cellY = Mathf.FloorToInt((pos.z - gridMinZ) / cellSize);

            // はみ出たときの対処
            cellX = Mathf.Clamp(cellX, 0, width - 1);
            cellY = Mathf.Clamp(cellY, 0, height - 1);

            // プレイヤーにグリッド位置を記録
            p.status.positionX = cellX;
            p.status.positionY = cellY;

            // グリッドにプレイヤーを記録
            grid[cellX, cellY].players.Add(p.id);
        }
    }

    // グリッドのプレイヤー登録
    public void Register(int id, Transform transform, PlayerStatus status)
    {
        Player p = new Player();
        p.id = id;
        p.transform = transform;
        p.status = status;
        players.Add(p);

        // エントリー画面用の大きさから、ゲームプレー用の大きさへ切り替える。
        transform.localScale = Vector3.one * GameplayPlayerScale;

        // エントリー画面から持ち越された座標を、ゲーム開始時のチーム配置に置き換える。
        if (TryGetRespawnPose(id, transform, out var spawnPosition, out var spawnRotation))
        {
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            StopPlayerMotion(transform);
        }
    }

    // チーム0は左端列、チーム1は右端列に配置する。
    // 縦方向は均等に割り振り、左側は右向き、右側は左向きにする。
    public bool TryGetRespawnPose(
        int playerId,
        Transform playerTransform,
        out Vector3 position,
        out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;

        var statusManager = PlayerStatusManager.Instance;
        if (statusManager == null
            || !statusManager.TryGetTeamSlot(
                playerId,
                out var teamNumber,
                out var slotIndex,
                out var teamSize))
        {
            return false;
        }

        if (teamNumber != 0 && teamNumber != 1) return false;

        GetFieldBounds(out var minX, out var maxX, out var minZ, out var maxZ, out var surfaceY);

        var halfCell = cellSize * 0.5f;
        var leftColumnX = Mathf.Min(minX + halfCell, (minX + maxX) * 0.5f);
        var rightColumnX = Mathf.Max(maxX - halfCell, (minX + maxX) * 0.5f);
        var firstRowZ = Mathf.Min(minZ + halfCell, (minZ + maxZ) * 0.5f);
        var lastRowZ = Mathf.Max(maxZ - halfCell, (minZ + maxZ) * 0.5f);

        // (index + 1) / (count + 1) により、1人なら中央、複数人なら両端との間も等間隔になる。
        var rowT = (slotIndex + 1f) / (teamSize + 1f);
        var x = teamNumber == 0 ? leftColumnX : rightColumnX;
        var z = Mathf.Lerp(firstRowZ, lastRowZ, rowT);
        var y = surfaceY + GetPlayerBottomOffset(playerTransform) + respawnClearance;
        position = new Vector3(x, y, z);
        // Avatar Prefabのローカル正面補正を考慮し、ルートは見た目と逆方向へ向ける。
        rotation = Quaternion.LookRotation(
            teamNumber == 0 ? Vector3.left : Vector3.right,
            Vector3.up);
        return true;
    }

    // 指定マスの中央を盤面表面のワールド座標で返す。
    public Vector3 GetCellCenter(int cellX, int cellY)
    {
        GetFieldBounds(out var minX, out _, out var minZ, out _, out var surfaceY);
        return new Vector3(
            minX + (Mathf.Clamp(cellX, 0, width - 1) + 0.5f) * cellSize,
            surfaceY,
            minZ + (Mathf.Clamp(cellY, 0, height - 1) + 0.5f) * cellSize);
    }

    void GetFieldBounds(
        out float minX,
        out float maxX,
        out float minZ,
        out float maxZ,
        out float surfaceY)
    {
        var fieldCollider = GetComponent<Collider>();
        if (fieldCollider != null && fieldCollider.bounds.size.x > 0f && fieldCollider.bounds.size.z > 0f)
        {
            var bounds = fieldCollider.bounds;
            minX = bounds.min.x;
            maxX = bounds.max.x;
            minZ = bounds.min.z;
            maxZ = bounds.max.z;
            surfaceY = bounds.max.y;
            return;
        }

        minX = originX;
        maxX = originX + width * cellSize;
        minZ = originZ;
        maxZ = originZ + height * cellSize;
        surfaceY = transform.position.y;
    }

    static float GetPlayerBottomOffset(Transform playerTransform)
    {
        if (playerTransform == null) return 0f;

        var playerCollider = playerTransform.GetComponent<Collider>();
        if (playerCollider is BoxCollider box)
        {
            var scaleY = Mathf.Abs(box.transform.lossyScale.y);
            return Mathf.Max(0f, (box.size.y * 0.5f - box.center.y) * scaleY);
        }

        if (playerCollider != null && playerCollider.enabled)
        {
            var offset = playerTransform.position.y - playerCollider.bounds.min.y;
            if (!float.IsNaN(offset) && !float.IsInfinity(offset)) return Mathf.Max(0f, offset);
        }

        return 0.5f;
    }

    static void StopPlayerMotion(Transform playerTransform)
    {
        var body = playerTransform != null ? playerTransform.GetComponent<Rigidbody>() : null;
        if (body == null) return;

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    // プレイヤー登録解除
    public void Unregister(int id)
    {
        players.RemoveAll(p => p.id == id);
    }
}
