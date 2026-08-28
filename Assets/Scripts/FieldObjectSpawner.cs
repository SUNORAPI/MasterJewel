using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridSys))]
public class FieldObjectSpawner : MonoBehaviour
{
    public static FieldObjectSpawner Instance { get; private set; }

    [Header("Field Models")]
    [SerializeField] GameObject largeRockPrefab;
    [SerializeField] GameObject[] rockPrefabs;
    [SerializeField] GameObject redCrystalPrefab;
    [SerializeField] Material redCrystalMaterial;

    [Header("Layout")]
    [SerializeField, Min(0)] int mirroredPairCount = 14;
    [SerializeField, Min(0)] int safeEdgeColumns = 2;
    [SerializeField, Min(0)] int safeEdgeRows = 1;
    [SerializeField] int randomSeed = 20260828;
    [SerializeField, Range(0.1f, 1f)] float cellFootprint = 0.82f;
    [SerializeField, Min(0f)] float surfaceClearance = 0.02f;
    [SerializeField] bool addBoxColliders = true;

    [Header("Dropped Crystal")]
    [SerializeField, Range(0.05f, 1f)] float droppedCrystalFootprint = 0.65f;
    [SerializeField, Min(0f)] float droppedCrystalClearance = 0.05f;

    readonly List<GameObject> fieldPrefabs = new List<GameObject>();
    GridSys grid;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        grid = GetComponent<GridSys>();
        BuildPrefabList();
        SpawnMirroredLayout();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void BuildPrefabList()
    {
        fieldPrefabs.Clear();
        AddPrefab(largeRockPrefab);

        if (rockPrefabs != null)
        {
            foreach (var rockPrefab in rockPrefabs) AddPrefab(rockPrefab);
        }

        AddPrefab(redCrystalPrefab);
    }

    void AddPrefab(GameObject prefab)
    {
        if (prefab != null && !fieldPrefabs.Contains(prefab)) fieldPrefabs.Add(prefab);
    }

    void SpawnMirroredLayout()
    {
        if (fieldPrefabs.Count == 0 || mirroredPairCount <= 0) return;

        var candidates = CreateLeftHalfCandidates();
        var random = new System.Random(randomSeed);
        Shuffle(candidates, random);

        var container = new GameObject("Field Objects");
        if (transform.parent != null) container.transform.SetParent(transform.parent, false);

        int pairTotal = Mathf.Min(mirroredPairCount, candidates.Count);
        int prefabOffset = random.Next(fieldPrefabs.Count);

        for (int i = 0; i < pairTotal; i++)
        {
            Vector2Int leftCell = candidates[i];
            Vector2Int rightCell = new Vector2Int(grid.Width - 1 - leftCell.x, leftCell.y);
            GameObject prefab = fieldPrefabs[(prefabOffset + i) % fieldPrefabs.Count];
            float leftRotation = random.Next(4) * 90f;
            float rightRotation = 180f - leftRotation;

            SpawnAtCell(prefab, leftCell, leftRotation, container.transform);
            SpawnAtCell(prefab, rightCell, rightRotation, container.transform);
        }
    }

    List<Vector2Int> CreateLeftHalfCandidates()
    {
        var candidates = new List<Vector2Int>();
        int firstX = Mathf.Clamp(safeEdgeColumns, 0, grid.Width / 2);
        int endX = Mathf.Max(firstX, grid.Width / 2);
        int firstY = Mathf.Clamp(safeEdgeRows, 0, grid.Height);
        int endY = Mathf.Max(firstY, grid.Height - safeEdgeRows);

        for (int x = firstX; x < endX; x++)
        {
            for (int y = firstY; y < endY; y++) candidates.Add(new Vector2Int(x, y));
        }

        return candidates;
    }

    void SpawnAtCell(GameObject prefab, Vector2Int cell, float yRotation, Transform parent)
    {
        // Model FBXのメインPrefab以外が誤って参照されても、generic Instantiateの
        // InvalidCastExceptionでゲーム開始処理全体を止めない。
        var clonedObject = Instantiate(
            (UnityEngine.Object)prefab,
            Vector3.zero,
            Quaternion.Euler(0f, yRotation, 0f),
            parent);
        var instance = clonedObject as GameObject;
        if (instance == null)
        {
            Debug.LogError($"{prefab.name} is not a GameObject prefab and could not be placed.", prefab);
            if (clonedObject != null) Destroy(clonedObject);
            return;
        }

        instance.name = $"{prefab.name} [{cell.x}, {cell.y}]";
        RemoveImportedCamerasAndLights(instance);

        if (prefab == redCrystalPrefab) ApplyRedCrystalMaterial(instance);

        if (!TryGetRendererBounds(instance, out var bounds))
        {
            Debug.LogWarning($"{prefab.name} has no renderer and could not be placed.", prefab);
            Destroy(instance);
            return;
        }

        float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
        if (footprint <= Mathf.Epsilon)
        {
            Debug.LogWarning($"{prefab.name} has invalid renderer bounds and could not be placed.", prefab);
            Destroy(instance);
            return;
        }

        float scale = grid.CellSize * cellFootprint / footprint;
        instance.transform.localScale *= scale;

        TryGetRendererBounds(instance, out bounds);
        Vector3 cellCenter = grid.GetCellCenter(cell.x, cell.y);
        instance.transform.position += new Vector3(
            cellCenter.x - bounds.center.x,
            cellCenter.y + surfaceClearance - bounds.min.y,
            cellCenter.z - bounds.center.z);

        if (addBoxColliders) AddBoundsCollider(instance);

        if (prefab == redCrystalPrefab)
        {
            instance.AddComponent<FieldCrystal>().Initialize(this);
        }
    }

    // ポイント数にかかわらず、取得可能な宝石を1オブジェクトだけ生成する。
    public GameObject SpawnDroppedCrystal(Vector3 position, int points)
    {
        if (points <= 0 || redCrystalPrefab == null) return null;

        var clonedObject = Instantiate(
            (UnityEngine.Object)redCrystalPrefab,
            Vector3.zero,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        var instance = clonedObject as GameObject;
        if (instance == null)
        {
            Debug.LogError("Dropped crystal prefab is not a GameObject.", redCrystalPrefab);
            if (clonedObject != null) Destroy(clonedObject);
            return null;
        }

        instance.name = $"Dropped Crystal ({points}P)";
        RemoveImportedCamerasAndLights(instance);
        ApplyRedCrystalMaterial(instance);

        if (!TryGetRendererBounds(instance, out var bounds))
        {
            Debug.LogWarning("Dropped crystal has no renderer and could not be spawned.", redCrystalPrefab);
            Destroy(instance);
            return null;
        }

        float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
        if (footprint <= Mathf.Epsilon)
        {
            Debug.LogWarning("Dropped crystal has invalid renderer bounds and could not be spawned.", redCrystalPrefab);
            Destroy(instance);
            return null;
        }

        float baseSize = grid != null ? grid.CellSize : 1f;
        instance.transform.localScale *= baseSize * droppedCrystalFootprint / footprint;

        TryGetRendererBounds(instance, out bounds);
        float surfaceY = grid != null ? grid.GetCellCenter(0, 0).y : position.y;
        instance.transform.position += new Vector3(
            position.x - bounds.center.x,
            surfaceY + droppedCrystalClearance - bounds.min.y,
            position.z - bounds.center.z);

        var collider = AddBoundsCollider(instance);
        if (collider == null)
        {
            Destroy(instance);
            return null;
        }
        collider.isTrigger = true;

        var body = instance.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        instance.AddComponent<DroppedCrystal>().Initialize(points);
        return instance;
    }

    static void RemoveImportedCamerasAndLights(GameObject instance)
    {
        foreach (var importedCamera in instance.GetComponentsInChildren<Camera>(true))
        {
            importedCamera.enabled = false;
            Destroy(importedCamera);
        }

        foreach (var importedLight in instance.GetComponentsInChildren<Light>(true))
        {
            importedLight.enabled = false;
            Destroy(importedLight);
        }
    }

    void ApplyRedCrystalMaterial(GameObject instance)
    {
        if (redCrystalMaterial == null) return;

        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = redCrystalMaterial;
            renderer.sharedMaterials = materials;
        }
    }

    static bool TryGetRendererBounds(GameObject instance, out Bounds bounds)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    static BoxCollider AddBoundsCollider(GameObject instance)
    {
        Bounds? localBounds = null;
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            Bounds rendererBounds = renderer.localBounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        var localCorner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 rootCorner = instance.transform.InverseTransformPoint(
                            renderer.transform.TransformPoint(localCorner));

                        if (localBounds.HasValue)
                        {
                            Bounds expanded = localBounds.Value;
                            expanded.Encapsulate(rootCorner);
                            localBounds = expanded;
                        }
                        else
                        {
                            localBounds = new Bounds(rootCorner, Vector3.zero);
                        }
                    }
                }
            }
        }

        if (!localBounds.HasValue) return null;

        var collider = instance.AddComponent<BoxCollider>();
        collider.center = localBounds.Value.center;
        collider.size = localBounds.Value.size;
        return collider;
    }

    static void Shuffle<T>(IList<T> values, System.Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
        }
    }
}
