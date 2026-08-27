using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputManager))]
public class PlayerEntryManager : MonoBehaviour
{
    [Header("Stage Prefab")]
    [SerializeField] GameObject stagePrefab;
    [SerializeField, Range(1, 8)] int maxPlayers = 8;

    [Header("Layout")]
    [SerializeField] Vector3 waitingCenter = new Vector3(0f, -20.7f, 80.17f);
    [SerializeField, Min(0.1f)] float waitingSpacing = 18.1f;
    [SerializeField] Vector3 confirmedCenter = new Vector3(0f, -20.7f, 87f);
    [SerializeField, Min(0f)] float teamCenterOffset = 40.5f;
    [SerializeField, Range(0f, 90f)] float teamAngle = 15f;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] float layoutAnimationDuration = 0.6f;
    [SerializeField, Min(0f)] float joinRiseDistance = 4f;
    [SerializeField] AnimationCurve layoutEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Stage Colors")]
    [SerializeField] Color pendingStageColor = new Color(1f, 0.7843137f, 0f, 1f); // #FFC800
    [SerializeField] Material leftTeamStageMaterial;
    [SerializeField] Material rightTeamStageMaterial;

    const float SpawnClearance = 0.05f;

    sealed class Entry
    {
        public PlayerInput player;
        public Transform stage;
        public Renderer[] stageRenderers;
        public Material[] stageMaterials;
    }

    sealed class RigidGroup
    {
        public readonly List<int> entryIndices = new List<int>();
        public Vector3 startCenter;
        public Vector3 targetCenter;
        public Quaternion targetRotation;
        public Vector3[] localOffsets;
        public Quaternion[] startRotations;
        public Vector3[] startScales;
        public Material targetMaterial;
        public Color targetColor;
    }

    struct LayoutPose
    {
        public Vector3 position;
        public Quaternion rotation;

        public LayoutPose(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }

    readonly List<Entry> entries = new List<Entry>();
    readonly Dictionary<Transform, Vector3> stageScales = new Dictionary<Transform, Vector3>();

    PlayerInputManager manager;
    Coroutine layoutCoroutine;
    // 正面補正はアバター側で行うため、待機中の親回転は加えない。
    Quaternion waitingRotation = Quaternion.identity;

    public bool TeamsConfirmed { get; private set; }
    public bool IsAnimating { get; private set; }

    void Awake()
    {
        manager = GetComponent<PlayerInputManager>();
        if (stagePrefab == null)
        {
            Debug.LogError(
                "PlayerEntryManager: stagePrefabが未設定です。PlayerEntryStage PrefabをInspectorで指定してください",
                this);
        }
    }

    void OnEnable()
    {
        manager.onPlayerJoined += OnPlayerJoined;
        manager.onPlayerLeft += OnPlayerLeft;
    }

    void OnDisable()
    {
        if (manager == null) return;
        manager.onPlayerJoined -= OnPlayerJoined;
        manager.onPlayerLeft -= OnPlayerLeft;
    }

    void OnDestroy()
    {
        foreach (var entry in entries) ReleaseStageMaterials(entry);
    }

    void OnPlayerJoined(PlayerInput input)
    {
        if (entries.Count >= maxPlayers)
        {
            Debug.LogWarning("PlayerEntryManager: ステージエラー");
            Destroy(input.gameObject);
            return;
        }

        int id = PlayerStatusManager.Instance.AddPlayer();
        var registrar = input.GetComponent<PlayerRegistrar>();
        if (registrar != null) registrar.SetPlayerId(id);

        DontDestroyOnLoad(input.gameObject);
        SetPresentationFrozen(input, true);

        var stage = CreateStage(id + 1);
        if (stage == null)
        {
            Debug.LogWarning("PlayerEntryManager: 配置エラー");
            Destroy(input.gameObject);
            return;
        }

        stage.gameObject.SetActive(true);
        stage.localScale = Vector3.zero;

        var entry = new Entry
        {
            player = input,
            stage = stage,
            stageRenderers = stage.GetComponentsInChildren<Renderer>(true)
        };
        InitializeStageMaterials(entry);
        SetStageColor(entry, pendingStageColor);
        entries.Add(entry);
        StartWaitingLayout(stage);

        var deviceName = input.devices.Count > 0 ? input.devices[0].displayName : "(no device)";
        Debug.Log($"Player joined: id={id}, team=pending, device={deviceName}");
    }

    void OnPlayerLeft(PlayerInput input)
    {
        StopLayoutAnimation();

        var index = entries.FindIndex(entry => entry.player == input);
        if (index >= 0)
        {
            var entry = entries[index];
            ReleaseStageMaterials(entry);
            var stage = entry.stage;
            if (stage != null) Destroy(stage.gameObject);
            entries.RemoveAt(index);
        }

        if (!TeamsConfirmed) StartWaitingLayout();
        Debug.Log($"Player left: playerIndex={input.playerIndex}");
    }

    void StopLayoutAnimation()
    {
        if (layoutCoroutine != null)
        {
            StopCoroutine(layoutCoroutine);
            layoutCoroutine = null;
        }

        IsAnimating = false;
    }

    public bool ConfirmTeams()
    {
        if (TeamsConfirmed) return true;
        if (entries.Count == 0)
        {
            Debug.LogWarning("PlayerEntryManager: プレイヤーなし");
            return false;
        }

        PlayerStatusManager.Instance.ConfirmTeamsByHalf();
        TeamsConfirmed = true;
        manager.DisableJoining();
        StartConfirmedLayout();

        Debug.Log($"Teams confirmed: left={(entries.Count + 1) / 2}, right={entries.Count / 2}");
        return true;
    }

    public void PreparePlayersForGame()
    {
        foreach (var entry in entries)
        {
            if (entry.player != null) SetPresentationFrozen(entry.player, false);
        }
    }

    Transform CreateStage(int playerNumber)
    {
        if (stagePrefab == null)
        {
            Debug.LogError("PlayerEntryManager: Prefabからステージを生成できません", this);
            return null;
        }

        // エントリー用ステージは、Sceneに設定されたPrefabからのみ生成する。
        var instance = Instantiate(stagePrefab, transform, false);
        instance.name = $"PlayerEntryStage P{entries.Count + 1}";
        var stageLabel = instance.GetComponent<PlayerEntryStageLabel>();
        if (stageLabel != null) stageLabel.SetPlayerNumber(playerNumber);
        var stage = instance.transform;
        stageScales[stage] = stage.localScale;
        return stage;
    }

    void StartWaitingLayout(Transform joiningStage = null)
    {
        if (entries.Count == 0) return;

        var poses = new List<LayoutPose>();
        var leftOffset = (entries.Count - 1) * waitingSpacing * 0.5f;
        for (var i = 0; i < entries.Count; i++)
        {
            var position = waitingCenter + Vector3.right * (i * waitingSpacing - leftOffset);
            poses.Add(new LayoutPose(position, waitingRotation));
        }

        if (joiningStage != null)
        {
            var target = poses[poses.Count - 1];
            joiningStage.SetPositionAndRotation(
                target.position + Vector3.down * joinRiseDistance,
                target.rotation);
        }

        StartLayoutAnimation(poses);
    }

    void StartConfirmedLayout()
    {
        var leftIndices = new List<int>();
        var rightIndices = new List<int>();
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var registrar = entry.player != null ? entry.player.GetComponent<PlayerRegistrar>() : null;
            var status = registrar != null
                ? PlayerStatusManager.Instance.GetStatus(registrar.PlayerId)
                : null;

            if (status != null && status.teamNumber == 0)
                leftIndices.Add(i);
            else
                rightIndices.Add(i);
        }

        var groups = new List<RigidGroup>();
        if (leftIndices.Count > 0) groups.Add(BuildRigidGroup(leftIndices, true));
        if (rightIndices.Count > 0) groups.Add(BuildRigidGroup(rightIndices, false));

        if (layoutCoroutine != null) StopCoroutine(layoutCoroutine);
        layoutCoroutine = StartCoroutine(AnimateConfirmedGroups(groups));
    }

    RigidGroup BuildRigidGroup(List<int> entryIndices, bool isLeft)
    {
        var group = new RigidGroup();
        group.entryIndices.AddRange(entryIndices);

        foreach (var index in entryIndices)
            group.startCenter += entries[index].stage.position;
        group.startCenter /= entryIndices.Count;

        group.targetCenter = confirmedCenter
            + Vector3.right * (isLeft ? -teamCenterOffset : teamCenterOffset);
        group.targetRotation = Quaternion.Euler(0f, isLeft ? -teamAngle : teamAngle, 0f);
        group.targetMaterial = isLeft ? leftTeamStageMaterial : rightTeamStageMaterial;
        group.targetColor = group.targetMaterial != null
            ? group.targetMaterial.color
            : (isLeft ? Color.blue : Color.red);

        group.localOffsets = new Vector3[entryIndices.Count];
        group.startRotations = new Quaternion[entryIndices.Count];
        group.startScales = new Vector3[entryIndices.Count];
        for (var i = 0; i < entryIndices.Count; i++)
        {
            var stage = entries[entryIndices[i]].stage;
            group.localOffsets[i] = stage.position - group.startCenter;
            group.startRotations[i] = stage.rotation;
            group.startScales[i] = stage.localScale;
            SetStageMaterial(entries[entryIndices[i]], group.targetMaterial);
        }

        return group;
    }

    IEnumerator AnimateConfirmedGroups(List<RigidGroup> groups)
    {
        IsAnimating = true;
        var elapsed = 0f;
        while (elapsed < layoutAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var normalizedTime = Mathf.Clamp01(elapsed / layoutAnimationDuration);
            var t = layoutEase != null
                ? layoutEase.Evaluate(normalizedTime)
                : Mathf.SmoothStep(0f, 1f, normalizedTime);

            foreach (var group in groups) ApplyRigidGroup(group, t);
            yield return null;
        }

        foreach (var group in groups) ApplyRigidGroup(group, 1f);
        IsAnimating = false;
        layoutCoroutine = null;
    }

    void ApplyRigidGroup(RigidGroup group, float t)
    {
        var center = Vector3.LerpUnclamped(group.startCenter, group.targetCenter, t);
        var rotation = Quaternion.SlerpUnclamped(Quaternion.identity, group.targetRotation, t);

        for (var i = 0; i < group.entryIndices.Count; i++)
        {
            var entry = entries[group.entryIndices[i]];
            var stage = entry.stage;
            stage.position = center + rotation * group.localOffsets[i];
            stage.rotation = rotation * group.startRotations[i];
            stage.localScale = Vector3.LerpUnclamped(
                group.startScales[i],
                stageScales[stage],
                t);
            SetStageColor(entry, Color.LerpUnclamped(pendingStageColor, group.targetColor, t));
            MovePlayerToStage(entry.player, stage);
        }
    }

    void SetStageColor(Entry entry, Color color)
    {
        if (entry.stageMaterials == null) return;

        foreach (var material in entry.stageMaterials)
        {
            if (material != null) material.color = color;
        }
    }

    void InitializeStageMaterials(Entry entry)
    {
        entry.stageMaterials = new Material[entry.stageRenderers.Length];
        for (var i = 0; i < entry.stageRenderers.Length; i++)
        {
            var renderer = entry.stageRenderers[i];
            if (renderer != null) entry.stageMaterials[i] = renderer.material;
        }
    }

    void SetStageMaterial(Entry entry, Material materialTemplate)
    {
        if (materialTemplate == null || entry.stageRenderers == null) return;

        ReleaseStageMaterials(entry);
        entry.stageMaterials = new Material[entry.stageRenderers.Length];
        for (var i = 0; i < entry.stageRenderers.Length; i++)
        {
            var renderer = entry.stageRenderers[i];
            if (renderer == null) continue;

            renderer.sharedMaterial = materialTemplate;
            var material = renderer.material;
            material.color = pendingStageColor;
            entry.stageMaterials[i] = material;
        }
    }

    void ReleaseStageMaterials(Entry entry)
    {
        if (entry.stageMaterials == null) return;

        foreach (var material in entry.stageMaterials)
        {
            if (material != null) Destroy(material);
        }

        entry.stageMaterials = null;
    }

    void StartLayoutAnimation(List<LayoutPose> targets)
    {
        if (layoutCoroutine != null) StopCoroutine(layoutCoroutine);
        layoutCoroutine = StartCoroutine(AnimateLayout(targets));
    }

    IEnumerator AnimateLayout(List<LayoutPose> targets)
    {
        IsAnimating = true;

        var count = Mathf.Min(entries.Count, targets.Count);
        var startPositions = new Vector3[count];
        var startRotations = new Quaternion[count];
        var startScales = new Vector3[count];
        for (var i = 0; i < count; i++)
        {
            var stage = entries[i].stage;
            startPositions[i] = stage.position;
            startRotations[i] = stage.rotation;
            startScales[i] = stage.localScale;
        }

        var elapsed = 0f;
        while (elapsed < layoutAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var normalizedTime = Mathf.Clamp01(elapsed / layoutAnimationDuration);
            var t = layoutEase != null
                ? layoutEase.Evaluate(normalizedTime)
                : Mathf.SmoothStep(0f, 1f, normalizedTime);

            for (var i = 0; i < count; i++)
            {
                var entry = entries[i];
                var stage = entry.stage;
                stage.position = Vector3.LerpUnclamped(startPositions[i], targets[i].position, t);
                stage.rotation = Quaternion.SlerpUnclamped(startRotations[i], targets[i].rotation, t);
                stage.localScale = Vector3.LerpUnclamped(startScales[i], stageScales[stage], t);
                MovePlayerToStage(entry.player, stage);
            }

            yield return null;
        }

        for (var i = 0; i < count; i++)
        {
            var entry = entries[i];
            entry.stage.SetPositionAndRotation(targets[i].position, targets[i].rotation);
            entry.stage.localScale = stageScales[entry.stage];
            MovePlayerToStage(entry.player, entry.stage);
        }

        IsAnimating = false;
        layoutCoroutine = null;
    }

    static void MovePlayerToStage(PlayerInput player, Transform stage)
    {
        if (player == null || stage == null) return;

        var position = stage.position;
        var stageCollider = stage.GetComponent<Collider>();
        var playerCollider = player.GetComponent<Collider>();
        if (stageCollider != null)
        {
            var stageTop = GetStageTop(stageCollider);
            var playerBottomOffset = GetPlayerBottomOffset(player, playerCollider);
            position.y = stageTop + playerBottomOffset + SpawnClearance;
        }

        if (!IsFinite(position)) return;
        player.transform.SetPositionAndRotation(position, stage.rotation);
    }

    static float GetStageTop(Collider stageCollider)
    {
        if (stageCollider is BoxCollider box)
        {
            return box.transform.TransformPoint(
                box.center + Vector3.up * box.size.y * 0.5f).y;
        }

        return stageCollider.bounds.max.y;
    }

    static float GetPlayerBottomOffset(PlayerInput player, Collider playerCollider)
    {
        if (playerCollider is BoxCollider box)
        {
            var scaleY = Mathf.Abs(box.transform.lossyScale.y);
            return Mathf.Max(0f, (box.size.y * 0.5f - box.center.y) * scaleY);
        }

        if (playerCollider != null)
        {
            var offset = player.transform.position.y - playerCollider.bounds.min.y;
            if (!float.IsNaN(offset) && !float.IsInfinity(offset)) return Mathf.Max(0f, offset);
        }

        return 0.5f;
    }

    static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
            && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }

    static void SetPresentationFrozen(PlayerInput player, bool frozen)
    {
        var movement = player.GetComponent<PlayerMoveControll>();
        if (movement != null) movement.enabled = !frozen;

        var body = player.GetComponent<Rigidbody>();
        if (body == null) return;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = frozen;
    }
}
