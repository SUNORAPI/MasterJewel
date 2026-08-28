using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// 仮想Gamepadへ入力を送り、通常プレイヤーと同じ入力経路で動くデバッグ用Bot。
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHPManager))]
public class DebugBotController : MonoBehaviour
{
    enum GoalType
    {
        None,
        DroppedCrystal,
        FieldCrystal,
        Enemy
    }

    [Header("Decision")]
    [SerializeField, Min(0.05f)] float decisionInterval = 0.2f;
    [SerializeField, Min(0.5f)] float enemyAggroRangeInCells = 4f;
    [SerializeField, Min(0.1f)] float pickupStopRangeInCells = 0.1f;
    [SerializeField, Min(0f)] float targetRetentionBonus = 1.25f;

    [Header("Goal Scoring")]
    [SerializeField, Min(0f)] float droppedCrystalBaseScore = 8f;
    [SerializeField, Min(0f)] float droppedCrystalPointScore = 1.5f;
    [SerializeField, Min(0f)] float droppedCrystalDistancePenalty = 1f;
    [SerializeField, Min(0f)] float enemyBaseScore = 7f;
    [SerializeField, Min(0f)] float enemyCarriedPointScore = 1.75f;
    [SerializeField, Min(0f)] float enemyMissingHealthScore = 0.035f;
    [SerializeField, Min(0f)] float enemyDistancePenalty = 1f;
    [SerializeField, Min(0f)] float lowHealthEnemyPenalty = 4f;
    [SerializeField, Range(1, 100)] int lowHealthThreshold = 30;
    [SerializeField, Min(0f)] float fieldCrystalBaseScore = 4f;
    [SerializeField, Min(0f)] float fieldCrystalDistancePenalty = 0.3f;

    [Header("Navigation")]
    [SerializeField, Min(0.1f)] float obstacleCheckRangeInCells = 0.65f;
    [SerializeField, Min(1f)] float sideProbeRangeMultiplier = 2.5f;
    [SerializeField, Range(0.1f, 2f)] float evadeForwardBias = 0.7f;
    [SerializeField, Min(0.1f)] float stuckCheckInterval = 0.75f;
    [SerializeField, Min(0.1f)] float evadeDuration = 0.8f;

    Gamepad gamepad;
    PlayerHPManager hp;
    PlayerAttackController attack;
    Component target;
    GoalType goal;
    Vector2 desiredDpad;
    bool pressButtonA;
    bool pressButtonB;
    float nextDecisionTime;
    float nextStuckCheckTime;
    float evadeUntil;
    Vector3 lastStuckCheckPosition;
    Vector3 evadeDirection;
    int evadeSign = 1;

    public void Initialize(Gamepad virtualGamepad)
    {
        gamepad = virtualGamepad;
        hp = GetComponent<PlayerHPManager>();
        attack = GetComponent<PlayerAttackController>();
        lastStuckCheckPosition = transform.position;
        nextStuckCheckTime = Time.time + stuckCheckInterval;

        if (gamepad != null)
            gameObject.name = $"{gameObject.name} [BOT]";
    }

    void Update()
    {
        if (gamepad == null || !gamepad.added) return;

        desiredDpad = Vector2.zero;
        pressButtonA = false;
        pressButtonB = false;

        // エントリー画面では待機し、ゲームシーンへ移ってから思考を開始する。
        if (GridSys.Instance != null && (hp == null || !hp.IsDead))
        {
            if (target == null || Time.time >= nextDecisionTime)
            {
                SelectGoal();
                nextDecisionTime = Time.time + decisionInterval;
            }

            BuildInput();
        }

        QueueVirtualControllerState();
    }

    void SelectGoal()
    {
        float cellSize = GridSys.Instance != null ? GridSys.Instance.CellSize : 1f;
        float bestScore = float.NegativeInfinity;
        Component bestTarget = null;
        GoalType bestGoal = GoalType.None;

        foreach (var crystal in FindObjectsByType<DroppedCrystal>())
        {
            float distanceInCells = HorizontalDistance(transform.position, crystal.transform.position) / cellSize;
            float score = droppedCrystalBaseScore
                + crystal.Points * droppedCrystalPointScore
                - distanceInCells * droppedCrystalDistancePenalty;
            ConsiderGoal(crystal, GoalType.DroppedCrystal, score, ref bestTarget, ref bestGoal, ref bestScore);
        }

        if (hp == null) hp = GetComponent<PlayerHPManager>();
        PlayerStatus ownStatus = GetStatus(hp != null ? hp.PlayerId : -1);
        foreach (var enemy in FindObjectsByType<PlayerHPManager>())
        {
            if (enemy == hp || enemy.IsDead || hp == null || enemy.Team == hp.Team) continue;

            PlayerStatus enemyStatus = GetStatus(enemy.PlayerId);
            int carriedPoints = enemyStatus != null ? Mathf.Max(0, enemyStatus.Crystals) : 0;
            float distanceInCells = HorizontalDistance(transform.position, enemy.transform.position) / cellSize;

            // 遠くの無得点プレイヤーを追い続けず、採掘や回収へ戻れるようにする。
            if (distanceInCells > enemyAggroRangeInCells && carriedPoints == 0) continue;

            float missingHealth = enemyStatus != null ? Mathf.Clamp(100 - enemyStatus.health, 0, 100) : 0f;
            float score = enemyBaseScore
                + carriedPoints * enemyCarriedPointScore
                + missingHealth * enemyMissingHealthScore
                - distanceInCells * enemyDistancePenalty;
            if (ownStatus != null && ownStatus.health <= lowHealthThreshold)
                score -= lowHealthEnemyPenalty;

            ConsiderGoal(enemy, GoalType.Enemy, score, ref bestTarget, ref bestGoal, ref bestScore);
        }

        foreach (var crystal in FindObjectsByType<FieldCrystal>())
        {
            float distanceInCells = HorizontalDistance(transform.position, crystal.transform.position) / cellSize;
            float score = fieldCrystalBaseScore - distanceInCells * fieldCrystalDistancePenalty;
            ConsiderGoal(crystal, GoalType.FieldCrystal, score, ref bestTarget, ref bestGoal, ref bestScore);
        }

        if (bestTarget != target)
        {
            lastStuckCheckPosition = transform.position;
            nextStuckCheckTime = Time.time + stuckCheckInterval;
            evadeUntil = 0f;
        }

        target = bestTarget;
        goal = bestGoal;
    }

    void BuildInput()
    {
        if (target == null)
        {
            goal = GoalType.None;
            return;
        }

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance <= Mathf.Epsilon) return;

        float cellSize = GridSys.Instance != null ? GridSys.Instance.CellSize : 1f;
        Vector3 targetDirection = toTarget / distance;

        if (goal == GoalType.DroppedCrystal)
        {
            if (distance > pickupStopRangeInCells * cellSize)
                desiredDpad = ToDpad(GetNavigationDirection(targetDirection, cellSize));
            return;
        }

        // D-padは移動と照準を兼ねる。射線が通る時だけ対象へ照準を固定して攻撃し、
        // 障害物がある時は誤った方向へ撃たず回り込む。
        if (attack == null) attack = GetComponent<PlayerAttackController>();
        float closeRange = attack != null ? attack.CloseRange : cellSize * 2f;
        float farRange = attack != null ? attack.FarRange : cellSize * 4f;
        bool hasLineOfSight = HasLineOfSight(targetDirection, distance);
        if (hasLineOfSight && distance <= closeRange * 0.9f)
        {
            desiredDpad = ToDpad(targetDirection);
            pressButtonA = true;
        }
        else if (hasLineOfSight && distance <= farRange * 0.9f)
        {
            desiredDpad = ToDpad(targetDirection);
            pressButtonB = true;
        }
        else
        {
            desiredDpad = ToDpad(GetNavigationDirection(targetDirection, cellSize));
        }
    }

    Vector3 GetNavigationDirection(Vector3 targetDirection, float cellSize)
    {
        if (Time.time < evadeUntil) return evadeDirection;

        if (Time.time >= nextStuckCheckTime)
        {
            float movedDistance = Vector3.Distance(transform.position, lastStuckCheckPosition);
            if (movedDistance < cellSize * 0.08f)
                BeginEvade(targetDirection);

            lastStuckCheckPosition = transform.position;
            nextStuckCheckTime = Time.time + stuckCheckInterval;
        }

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(
                origin,
                targetDirection,
                out var hit,
                obstacleCheckRangeInCells * cellSize,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore)
            && !BelongsToTarget(hit.transform))
        {
            BeginEvade(targetDirection);
        }

        return Time.time < evadeUntil ? evadeDirection : targetDirection;
    }

    void BeginEvade(Vector3 forward)
    {
        Vector3 left = new Vector3(-forward.z, 0f, forward.x);
        Vector3 right = -left;
        Vector3 leftRoute = (left + forward * evadeForwardBias).normalized;
        Vector3 rightRoute = (right + forward * evadeForwardBias).normalized;
        float probeRange = obstacleCheckRangeInCells
            * sideProbeRangeMultiplier
            * (GridSys.Instance != null ? GridSys.Instance.CellSize : 1f);
        float leftClearance = GetClearance(leftRoute, probeRange);
        float rightClearance = GetClearance(rightRoute, probeRange);

        if (Mathf.Approximately(leftClearance, rightClearance))
        {
            evadeSign *= -1;
            evadeDirection = evadeSign > 0 ? leftRoute : rightRoute;
        }
        else
        {
            evadeDirection = leftClearance > rightClearance ? leftRoute : rightRoute;
            evadeSign = evadeDirection == leftRoute ? 1 : -1;
        }
        evadeUntil = Time.time + evadeDuration;
    }

    float GetClearance(Vector3 direction, float range)
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(
            origin,
            direction,
            out var hit,
            range,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore)
            ? hit.distance
            : range;
    }

    bool HasLineOfSight(Vector3 targetDirection, float distance)
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (!Physics.Raycast(
                origin,
                targetDirection,
                out var hit,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return BelongsToTarget(hit.transform);
    }

    bool BelongsToTarget(Transform hitTransform)
    {
        if (target == null || hitTransform == null) return false;
        return hitTransform == target.transform
            || hitTransform.IsChildOf(target.transform)
            || target.transform.IsChildOf(hitTransform);
    }

    void ConsiderGoal(
        Component candidate,
        GoalType candidateGoal,
        float score,
        ref Component bestTarget,
        ref GoalType bestGoal,
        ref float bestScore)
    {
        if (candidate == null) return;
        if (candidate == target && candidateGoal == goal) score += targetRetentionBonus;
        if (score <= bestScore) return;

        bestTarget = candidate;
        bestGoal = candidateGoal;
        bestScore = score;
    }

    static PlayerStatus GetStatus(int playerId)
    {
        var manager = PlayerStatusManager.Instance;
        if (manager == null || playerId < 0 || playerId >= manager.Count) return null;
        return manager.GetStatus(playerId);
    }

    void QueueVirtualControllerState()
    {
        var state = new GamepadState();
        if (desiredDpad.y > 0.25f) state = state.WithButton(GamepadButton.DpadUp);
        if (desiredDpad.y < -0.25f) state = state.WithButton(GamepadButton.DpadDown);
        if (desiredDpad.x < -0.25f) state = state.WithButton(GamepadButton.DpadLeft);
        if (desiredDpad.x > 0.25f) state = state.WithButton(GamepadButton.DpadRight);

        // InputAction上では ButtonA=East、ButtonB=South に割り当てられている。
        if (pressButtonA) state = state.WithButton(GamepadButton.East);
        if (pressButtonB) state = state.WithButton(GamepadButton.South);
        InputSystem.QueueStateEvent(gamepad, state);
    }

    void OnDestroy()
    {
        if (gamepad != null && gamepad.added)
            InputSystem.RemoveDevice(gamepad);
    }

    static Vector2 ToDpad(Vector3 direction)
    {
        return new Vector2(direction.x, direction.z).normalized;
    }

    static float HorizontalSqrDistance(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return x * x + z * z;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(HorizontalSqrDistance(a, b));
    }
}
