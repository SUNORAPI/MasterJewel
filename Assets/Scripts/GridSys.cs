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
    // Inspectorで設定
    // Gridのスケールとは別物
    //グリッドは左下起点でワールド座標のXZがグリッドのXYに対応する点に注意
    [SerializeField] float originX = 0f;    // グリッドのワールド座標X
    [SerializeField] float originZ = 0f;    // グリッドのワールド座標Z
    [SerializeField] float cellSize = 2f;   // 1マスのサイズ
    [SerializeField] int width = 16;    // 横のマス数
    [SerializeField] int height = 16;   // 縦のマス数

    public static GridSys Instance;

    GridStatus[,] grid;
    List<Player> players = new List<Player>();

    // AwakeでInstanceを設定
    void Awake()
    {
        Instance = this;
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

        // プレイヤーのマス目判定
        foreach (Player p in players)
        {
            Vector3 pos = p.transform.position;

            // ワールド座標をグリッドに変換
            int cellX = Mathf.FloorToInt((pos.x - originX) / cellSize);
            int cellY = Mathf.FloorToInt((pos.z - originZ) / cellSize);

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
    }

    // プレイヤー登録解除
    public void Unregister(int id)
    {
        players.RemoveAll(p => p.id == id);
    }
}
