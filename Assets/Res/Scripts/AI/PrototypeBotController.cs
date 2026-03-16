using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct PrototypeCombatNoiseEvent
{
    public PrototypeCombatNoiseEvent(Vector3 position, float radius, GameObject source)
    {
        Position = position;
        Radius = Mathf.Max(0f, radius);
        Source = source;
    }

    public Vector3 Position { get; }
    public float Radius { get; }
    public GameObject Source { get; }
}

public static class PrototypeCombatNoiseSystem
{
    public static event Action<PrototypeCombatNoiseEvent> NoiseReported;

    public static void ReportNoise(Vector3 position, float radius, GameObject source = null)
    {
        if (radius <= 0f)
        {
            return;
        }

        NoiseReported?.Invoke(new PrototypeCombatNoiseEvent(position, radius, source));
    }
}

public enum PrototypeEnemyArchetype
{
    RegularZombie = 0,
    PoliceZombie = 1,
    SoldierZombie = 2,
    ZombieDog = 3
}

[RequireComponent(typeof(Rigidbody), typeof(PrototypeUnitVitals))]
public class PrototypeBotController : MonoBehaviour
{
    private enum BotState
    {
        Patrol = 0,
        Chase = 1,
        Attack = 2,
        Search = 3,
        Dead = 4
    }

    [Header("Identity")]
    [SerializeField] private PrototypeEnemyArchetype archetype = PrototypeEnemyArchetype.RegularZombie;
    [SerializeField] private bool useArchetypeDefaults = true;
    [SerializeField, HideInInspector] private PrototypeEnemyArchetype lastConfiguredArchetype = (PrototypeEnemyArchetype)(-1);
    [SerializeField, HideInInspector] private bool previousUseArchetypeDefaults;

    [Header("References")]
    [SerializeField] private Transform combatTarget;
    [SerializeField] private Transform eyeAnchor;
    [SerializeField] private PrototypeWeaponDefinition primaryWeapon;
    [SerializeField] private ItemRarity primaryWeaponRarity = ItemRarity.Common;
    [SerializeField] private List<ItemAffix> primaryWeaponAffixes = new List<ItemAffix>();
    [SerializeField] private List<ItemSkill> primaryWeaponSkills = new List<ItemSkill>();
    [SerializeField] private LayerMask perceptionMask = Physics.DefaultRaycastLayers;

    [Header("Perception")]
    [Min(1f)]
    [SerializeField] private float detectionRadius = 18f;
    [Range(25f, 180f)]
    [SerializeField] private float fieldOfView = 120f;
    [Min(0f)]
    [SerializeField] private float smellDetectionRadius = 0f;
    [Min(0.1f)]
    [SerializeField] private float loseSightGraceTime = 1.4f;
    [Min(0.1f)]
    [SerializeField] private float searchGiveUpTime = 5f;
    [Min(60f)]
    [SerializeField] private float turnSpeedDegrees = 120f;
    [Min(0f)]
    [SerializeField] private float guaranteedAwarenessRadius = 2.5f;
    [Min(0.1f)]
    [SerializeField] private float hearingSearchRadius = 3.4f;
    [Min(1f)]
    [SerializeField] private float hearingRangeMultiplier = 1.3f;
    [Min(0f)]
    [SerializeField] private float hearingFacingWeight = 0.5f;

    [Header("Movement")]
    [Min(0.1f)]
    [SerializeField] private float patrolSpeed = 1f;
    [Min(0.1f)]
    [SerializeField] private float chaseSpeed = 2f;
    [Min(0.05f)]
    [SerializeField] private float patrolPointTolerance = 0.45f;
    [Min(0f)]
    [SerializeField] private float patrolWaitTime = 0.75f;
    [Min(0.05f)]
    [SerializeField] private float searchStopDistance = 0.6f;
    [Min(0f)]
    [SerializeField] private float searchPauseTime = 0.35f;
    [SerializeField] private List<Vector3> patrolPoints = new List<Vector3>();

    [Header("Combat")]
    [Min(0.1f)]
    [SerializeField] private float combatRange = 1.8f;
    [Min(0.1f)]
    [SerializeField] private float preferredCombatDistance = 1f;
    [Min(0f)]
    [SerializeField] private float attackSpreadMultiplier = 1.25f;
    [Min(0f)]
    [SerializeField] private float attackCadenceJitter = 0.03f;
    [Min(1)]
    [SerializeField] private int rangedAttackBurstMinShots = 1;
    [Min(1)]
    [SerializeField] private int rangedAttackBurstMaxShots = 1;
    [Min(0.1f)]
    [SerializeField] private float rangedBurstShotIntervalMultiplier = 1f;
    [Min(0f)]
    [SerializeField] private float rangedAttackRecoveryCooldown = 0.9f;
    [Min(0f)]
    [SerializeField] private float rangedAttackRecoveryJitter = 0.08f;
    [Min(0f)]
    [SerializeField] private float rangedAimLockRadius = 0.45f;
    [Min(0.05f)]
    [SerializeField] private float rangedAimLockRefreshIntervalMin = 0.85f;
    [Min(0.05f)]
    [SerializeField] private float rangedAimLockRefreshIntervalMax = 1.2f;
    [Min(0f)]
    [SerializeField] private float fallbackDamage = 18f;
    [Min(0f)]
    [SerializeField] private float fallbackImpactForce = 10f;

    [Header("Loot")]
    [SerializeField] private string corpseInteractionLabel = "Search Corpse";
    [Min(1)]
    [SerializeField] private int corpseLootSlots = 6;
    [SerializeField] private LootTableDefinition carriedLootTable;
    [SerializeField] private RaidGameMode raidGameMode;
    [SerializeField] private bool bossLootProfile;
    [Range(0, 3)]
    [SerializeField] private int bonusLootRarityBias;
    [Min(0)]
    [SerializeField] private int bonusLootRolls;
    [Min(0)]
    [SerializeField] private int bonusLootItemLevel;
    [SerializeField, HideInInspector] private int enemyLevel = 1;
    [SerializeField] private bool rollCarriedLootOnDeath = true;
    [SerializeField] private bool transferEquippedArmorToCorpse = true;
    [SerializeField] private bool transferMagazineAmmoToCorpse = true;
    [SerializeField] private bool transferPrimaryWeaponToCorpse = true;

    private Rigidbody botRigidbody;
    private PrototypeUnitVitals vitals;
    private PrototypeUnitVitals targetVitals;
    private Vector3 desiredPlanarVelocity;
    private Quaternion desiredRotation;
    private Vector3 lastKnownTargetPosition;
    private Vector3 currentAimPoint;
    private Vector3 lockedAimPoint;
    private Vector3 lastAwarenessDirection = Vector3.forward;
    private ItemAffixSummary primaryWeaponAffixSummary = ItemAffixSummary.CreateDefault();
    private int patrolIndex;
    private int startingMagazineAmmo;
    private int magazineAmmo;
    private int searchPointIndex;
    private int currentBurstShotsRemaining;
    private float patrolWaitTimer;
    private float searchTimer;
    private float timeSinceTargetConfirmed = float.PositiveInfinity;
    private float nextAttackTime;
    private float nextBurstShotTime;
    private float reloadEndTime;
    private float attackSequenceCooldownUntil;
    private float nextAimLockRefreshTime;
    private float searchPauseTimer;
    private bool isReloading;
    private bool hasInvestigationPoint;
    private bool hasLockedAimPoint;
    private bool corpseLootBuilt;
    private BotState state;
    private readonly List<Vector3> searchPoints = new List<Vector3>();

    public Transform CombatTarget => combatTarget;
    public PrototypeEnemyArchetype Archetype => archetype;
    public int EnemyLevel => Mathf.Max(1, enemyLevel);
    public bool IsBossProfile => bossLootProfile;
    public int ExperienceReward => PrototypePlayerProgressionUtility.GetEnemyExperienceReward(EnemyLevel, archetype, bossLootProfile);

    private void Awake()
    {
        botRigidbody = GetComponent<Rigidbody>();
        vitals = GetComponent<PrototypeUnitVitals>();
        ResolveReferences();
        RefreshArchetypeDefaults(true);
        EnsureSettings();
        RefreshEnemyProgression(true);
        InitializeWeaponState();
        desiredRotation = transform.rotation;
        lastKnownTargetPosition = transform.position;
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureSettings();
        RefreshEnemyProgression(false);
        PrototypeCombatNoiseSystem.NoiseReported += HandleNoiseReported;

        if (vitals != null)
        {
            vitals.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        PrototypeCombatNoiseSystem.NoiseReported -= HandleNoiseReported;

        if (vitals != null)
        {
            vitals.Died -= HandleDied;
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
        RefreshArchetypeDefaults();
        EnsureSettings();
        RefreshEnemyProgression(false);
    }

    public void Configure(
        Transform target,
        PrototypeEnemyArchetype enemyArchetype,
        PrototypeWeaponDefinition weaponDefinition,
        params Vector3[] worldPatrolPoints)
    {
        ConfigureInternal(
            target,
            enemyArchetype,
            weaponDefinition,
            ItemRarity.Common,
            null,
            null,
            weaponDefinition != null && !weaponDefinition.IsMeleeWeapon ? weaponDefinition.MagazineSize : 0,
            worldPatrolPoints);
    }

    public void Configure(
        Transform target,
        PrototypeEnemyArchetype enemyArchetype,
        WeaponInstance weaponInstance,
        params Vector3[] worldPatrolPoints)
    {
        ConfigureInternal(
            target,
            enemyArchetype,
            weaponInstance != null ? weaponInstance.Definition : null,
            weaponInstance != null ? weaponInstance.Rarity : ItemRarity.Common,
            weaponInstance != null ? weaponInstance.Affixes : null,
            weaponInstance != null ? weaponInstance.Skills : null,
            weaponInstance != null ? weaponInstance.MagazineAmmo : 0,
            worldPatrolPoints);
    }

    private void ConfigureInternal(
        Transform target,
        PrototypeEnemyArchetype enemyArchetype,
        PrototypeWeaponDefinition weaponDefinition,
        ItemRarity weaponRarity,
        IReadOnlyList<ItemAffix> weaponAffixes,
        IReadOnlyList<ItemSkill> weaponSkills,
        int loadedAmmo,
        params Vector3[] worldPatrolPoints)
    {
        archetype = enemyArchetype;
        combatTarget = target;
        primaryWeapon = weaponDefinition;
        ApplyPrimaryWeaponLootState(weaponRarity, weaponAffixes, weaponSkills);
        startingMagazineAmmo = weaponDefinition != null && !weaponDefinition.IsMeleeWeapon
            ? Mathf.Clamp(loadedAmmo, 0, weaponDefinition.MagazineSize)
            : 0;
        patrolPoints = new List<Vector3>();

        if (worldPatrolPoints != null)
        {
            for (int index = 0; index < worldPatrolPoints.Length; index++)
            {
                patrolPoints.Add(worldPatrolPoints[index]);
            }
        }

        RefreshArchetypeDefaults(true);
        EnsureSettings();
        InitializeWeaponState();
        ResolveReferences();
        RefreshEnemyProgression(true);
    }

    public void SetCarriedLootTable(LootTableDefinition lootTable)
    {
        carriedLootTable = lootTable;
    }

    public void ConfigureLootProfile(bool isBoss, int rarityBias, int bonusRollsOverride, int itemLevelBonus)
    {
        bossLootProfile = isBoss;
        bonusLootRarityBias = Mathf.Clamp(rarityBias, 0, 3);
        bonusLootRolls = Mathf.Max(0, bonusRollsOverride);
        bonusLootItemLevel = Mathf.Max(0, itemLevelBonus);
        RefreshEnemyProgression(false);
    }

    private void Update()
    {
        if (state == BotState.Dead || vitals == null || vitals.IsDead)
        {
            desiredPlanarVelocity = Vector3.zero;
            return;
        }

        ResolveTargetIfNeeded();
        bool hasRangedWeapon = primaryWeapon != null && !primaryWeapon.IsMeleeWeapon;

        bool hasVisibleTarget = TryGetVisibleTarget(out Vector3 visiblePoint, out float visibleDistance);
        bool hasSmellTarget = TryGetSmellTarget(out float smellDistance);
        bool hasPerceivedTarget = hasVisibleTarget || hasSmellTarget;
        float distanceToTarget = hasVisibleTarget ? visibleDistance : smellDistance;

        if (hasPerceivedTarget)
        {
            if (hasVisibleTarget && hasRangedWeapon)
            {
                currentAimPoint = GetOrRefreshRangedAimPoint(visiblePoint, false);
            }
            else
            {
                currentAimPoint = hasVisibleTarget ? visiblePoint : GetTargetAimPoint();
                ClearRangedAimLock();
            }

            lastKnownTargetPosition = combatTarget.position;
            hasInvestigationPoint = true;
            timeSinceTargetConfirmed = 0f;
            searchTimer = 0f;
            searchPauseTimer = 0f;
            searchPoints.Clear();
            searchPointIndex = 0;

            Vector3 awarenessDirection = Vector3.ProjectOnPlane(lastKnownTargetPosition - transform.position, Vector3.up).normalized;
            if (awarenessDirection.sqrMagnitude > 0.001f)
            {
                lastAwarenessDirection = awarenessDirection;
            }

            state = ShouldAttackNow(hasVisibleTarget, distanceToTarget)
                ? BotState.Attack
                : BotState.Chase;
        }
        else
        {
            ResetRangedAttackSequence(false);
            ClearRangedAimLock();
            timeSinceTargetConfirmed += Time.deltaTime;
            bool hasLivingTarget = combatTarget != null && targetVitals != null && !targetVitals.IsDead;
            if (hasLivingTarget && timeSinceTargetConfirmed <= loseSightGraceTime)
            {
                state = BotState.Chase;
            }
            else if (hasLivingTarget && hasInvestigationPoint && searchTimer < searchGiveUpTime)
            {
                state = BotState.Search;
                searchTimer += Time.deltaTime;
            }
            else
            {
                state = BotState.Patrol;
                ClearSearchState();
            }
        }

        switch (state)
        {
            case BotState.Attack:
                UpdateAttackState(hasVisibleTarget, distanceToTarget);
                break;

            case BotState.Chase:
                UpdateChaseState();
                break;

            case BotState.Search:
                UpdateSearchState();
                break;

            default:
                UpdatePatrolState();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (botRigidbody == null || state == BotState.Dead)
        {
            return;
        }

        Vector3 velocity = botRigidbody.linearVelocity;
        velocity.x = desiredPlanarVelocity.x;
        velocity.z = desiredPlanarVelocity.z;
        botRigidbody.linearVelocity = velocity;
        botRigidbody.MoveRotation(Quaternion.RotateTowards(botRigidbody.rotation, desiredRotation, turnSpeedDegrees * Time.fixedDeltaTime));
    }

    private void UpdateAttackState(bool hasVisibleTarget, float distanceToTarget)
    {
        desiredPlanarVelocity = Vector3.zero;

        if (combatTarget == null)
        {
            state = BotState.Patrol;
            return;
        }

        Vector3 toTarget = currentAimPoint - GetEyePosition();
        Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        if (flatToTarget.sqrMagnitude > 0.0001f)
        {
            desiredRotation = Quaternion.LookRotation(flatToTarget.normalized, Vector3.up);
        }

        if (primaryWeapon == null || primaryWeapon.IsMeleeWeapon)
        {
            float engageDistance = primaryWeapon != null
                ? Mathf.Max(primaryWeapon.MeleeRange * 0.92f, 0.75f)
                : combatRange;
            float retreatDistance = Mathf.Max(0.45f, engageDistance * 0.4f);

            if (distanceToTarget > engageDistance)
            {
                desiredPlanarVelocity = flatToTarget.normalized * chaseSpeed;
            }
            else if (distanceToTarget < retreatDistance)
            {
                desiredPlanarVelocity = -flatToTarget.normalized * patrolSpeed * 0.55f;
            }

            TryMeleeAttack(currentAimPoint, distanceToTarget);
            return;
        }

        if (!hasVisibleTarget)
        {
            ResetRangedAttackSequence(false);
            ClearRangedAimLock();
            state = BotState.Chase;
            return;
        }

        if (distanceToTarget > preferredCombatDistance * 1.05f)
        {
            desiredPlanarVelocity = flatToTarget.normalized * chaseSpeed;
        }
        else if (distanceToTarget < preferredCombatDistance * 0.58f)
        {
            desiredPlanarVelocity = -flatToTarget.normalized * patrolSpeed * 0.6f;
        }

        TryRangedAttack(currentAimPoint);
    }

    private void UpdateChaseState()
    {
        if (MoveTowardWorldPosition(lastKnownTargetPosition, chaseSpeed, patrolPointTolerance) && timeSinceTargetConfirmed > loseSightGraceTime)
        {
            BeginSearchAt(lastKnownTargetPosition, hearingSearchRadius);
        }
    }

    private void UpdateSearchState()
    {
        if (!hasInvestigationPoint)
        {
            state = BotState.Patrol;
            desiredPlanarVelocity = Vector3.zero;
            return;
        }

        if (searchTimer >= searchGiveUpTime)
        {
            state = BotState.Patrol;
            desiredPlanarVelocity = Vector3.zero;
            ClearSearchState();
            return;
        }

        EnsureSearchPattern();
        if (searchPoints.Count == 0)
        {
            desiredPlanarVelocity = Vector3.zero;
            return;
        }

        if (searchPauseTimer > 0f)
        {
            searchPauseTimer = Mathf.Max(0f, searchPauseTimer - Time.deltaTime);
            desiredPlanarVelocity = Vector3.zero;
            return;
        }

        Vector3 destination = searchPoints[Mathf.Clamp(searchPointIndex, 0, searchPoints.Count - 1)];
        if (MoveTowardWorldPosition(destination, chaseSpeed * 0.82f, searchStopDistance))
        {
            searchPauseTimer = searchPauseTime;
            searchPointIndex++;
            if (searchPointIndex >= searchPoints.Count)
            {
                BuildSearchPattern(lastKnownTargetPosition, hearingSearchRadius);
            }
        }
    }

    private void UpdatePatrolState()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
        {
            desiredPlanarVelocity = Vector3.zero;
            return;
        }

        if (patrolWaitTimer > 0f)
        {
            patrolWaitTimer = Mathf.Max(0f, patrolWaitTimer - Time.deltaTime);
            desiredPlanarVelocity = Vector3.zero;
            return;
        }

        Vector3 destination = patrolPoints[Mathf.Clamp(patrolIndex, 0, patrolPoints.Count - 1)];
        if (MoveTowardWorldPosition(destination, patrolSpeed, patrolPointTolerance))
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
            patrolWaitTimer = patrolWaitTime;
        }
    }

    private bool ShouldAttackNow(bool hasVisibleTarget, float distanceToTarget)
    {
        if (primaryWeapon == null || primaryWeapon.IsMeleeWeapon)
        {
            return distanceToTarget <= combatRange;
        }

        return hasVisibleTarget && distanceToTarget <= combatRange;
    }

    private bool TryGetSmellTarget(out float distanceToTarget)
    {
        distanceToTarget = float.PositiveInfinity;
        if (archetype != PrototypeEnemyArchetype.ZombieDog || smellDetectionRadius <= 0f || combatTarget == null || targetVitals == null || targetVitals.IsDead)
        {
            return false;
        }

        distanceToTarget = Vector3.Distance(transform.position, combatTarget.position);
        return distanceToTarget <= smellDetectionRadius;
    }

    private bool TryGetVisibleTarget(out Vector3 visiblePoint, out float distanceToTarget)
    {
        visiblePoint = Vector3.zero;
        distanceToTarget = float.PositiveInfinity;

        if (combatTarget == null || targetVitals == null || targetVitals.IsDead)
        {
            return false;
        }

        Vector3 eyePosition = GetEyePosition();
        visiblePoint = GetTargetAimPoint();
        Vector3 toTarget = visiblePoint - eyePosition;
        distanceToTarget = toTarget.magnitude;
        if (distanceToTarget > detectionRadius)
        {
            return false;
        }

        if (distanceToTarget > guaranteedAwarenessRadius)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
            if (flatForward.sqrMagnitude > 0.001f && flatToTarget.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(flatForward, flatToTarget);
                if (angle > fieldOfView * 0.5f)
                {
                    return false;
                }
            }
        }

        if (!TryGetCombatHit(eyePosition, toTarget.normalized, distanceToTarget + 0.25f, out RaycastHit hit))
        {
            return false;
        }

        PrototypeUnitHitbox hitbox = hit.collider.GetComponent<PrototypeUnitHitbox>();
        if (hitbox == null)
        {
            hitbox = hit.collider.GetComponentInParent<PrototypeUnitHitbox>();
        }

        return hitbox != null && hitbox.Owner == targetVitals;
    }

    private void HandleNoiseReported(PrototypeCombatNoiseEvent noiseEvent)
    {
        if (state == BotState.Dead || vitals == null || vitals.IsDead || noiseEvent.Source == gameObject)
        {
            return;
        }

        Vector3 earPosition = GetEyePosition();
        Vector3 toNoise = noiseEvent.Position - earPosition;
        float distance = toNoise.magnitude;
        float hearingLimit = Mathf.Max(noiseEvent.Radius, detectionRadius * hearingRangeMultiplier);
        if (distance > hearingLimit)
        {
            return;
        }

        float audibility = 1f - Mathf.Clamp01(distance / Mathf.Max(hearingLimit, 0.01f));
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 flatToNoise = Vector3.ProjectOnPlane(toNoise, Vector3.up).normalized;
        if (flatForward.sqrMagnitude > 0.001f && flatToNoise.sqrMagnitude > 0.001f)
        {
            float facingDot = Mathf.Clamp01((Vector3.Dot(flatForward, flatToNoise) + 1f) * 0.5f);
            audibility += facingDot * hearingFacingWeight;
        }

        if (audibility < 0.18f)
        {
            return;
        }

        lastKnownTargetPosition = noiseEvent.Position;
        Vector3 awarenessDirection = Vector3.ProjectOnPlane(toNoise, Vector3.up).normalized;
        if (awarenessDirection.sqrMagnitude > 0.001f)
        {
            lastAwarenessDirection = awarenessDirection;
            desiredRotation = Quaternion.LookRotation(awarenessDirection, Vector3.up);
        }

        if (noiseEvent.Source != null && combatTarget == null)
        {
            combatTarget = noiseEvent.Source.transform;
            targetVitals = combatTarget.GetComponent<PrototypeUnitVitals>();
        }

        BeginSearchAt(noiseEvent.Position, hearingSearchRadius + noiseEvent.Radius * 0.18f);
    }

    private void BeginSearchAt(Vector3 investigationPoint, float patternRadius)
    {
        lastKnownTargetPosition = investigationPoint;
        hasInvestigationPoint = true;
        searchTimer = 0f;
        searchPauseTimer = 0f;
        state = BotState.Search;
        BuildSearchPattern(investigationPoint, patternRadius);
    }

    private void EnsureSearchPattern()
    {
        if (searchPoints.Count == 0 || searchPointIndex >= searchPoints.Count)
        {
            BuildSearchPattern(lastKnownTargetPosition, hearingSearchRadius);
        }
    }

    private void BuildSearchPattern(Vector3 origin, float patternRadius)
    {
        float radius = Mathf.Max(1f, patternRadius);
        searchPoints.Clear();
        searchPointIndex = 0;

        Vector3 forward = Vector3.ProjectOnPlane(lastAwarenessDirection, Vector3.up).normalized;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        }

        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        searchPoints.Add(origin);
        searchPoints.Add(origin + forward * radius);
        searchPoints.Add(origin + right * (radius * 0.8f));
        searchPoints.Add(origin - right * (radius * 0.8f));
        searchPoints.Add(origin - forward * (radius * 0.55f));
    }

    private void ClearSearchState()
    {
        searchTimer = 0f;
        searchPauseTimer = 0f;
        hasInvestigationPoint = false;
        searchPoints.Clear();
        searchPointIndex = 0;
    }

    private bool MoveTowardWorldPosition(Vector3 destination, float speed, float stopDistance)
    {
        Vector3 toDestination = destination - transform.position;
        Vector3 planarOffset = Vector3.ProjectOnPlane(toDestination, Vector3.up);
        float remainingDistance = planarOffset.magnitude;

        if (remainingDistance <= stopDistance)
        {
            desiredPlanarVelocity = Vector3.zero;
            return true;
        }

        Vector3 direction = planarOffset / Mathf.Max(remainingDistance, 0.0001f);
        desiredPlanarVelocity = direction * speed;
        desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
        return false;
    }

    private void TryRangedAttack(Vector3 aimPoint)
    {
        if (primaryWeapon == null || primaryWeapon.IsMeleeWeapon)
        {
            return;
        }

        if (isReloading)
        {
            if (Time.time >= reloadEndTime)
            {
                isReloading = false;
                magazineAmmo = GetWeaponMagazineSize();
            }

            return;
        }

        if (Time.time < attackSequenceCooldownUntil)
        {
            return;
        }

        if (magazineAmmo <= 0)
        {
            StartReload();
            return;
        }

        if (currentBurstShotsRemaining <= 0)
        {
            BeginRangedAttackSequence(aimPoint);
        }

        if (currentBurstShotsRemaining <= 0 || Time.time < nextBurstShotTime)
        {
            return;
        }

        Vector3 shotOrigin = GetEyePosition();
        Vector3 shotAimPoint = BuildRangedShotAimPoint(aimPoint, shotOrigin);
        Vector3 shotDirection = (shotAimPoint - shotOrigin).normalized;
        magazineAmmo--;
        currentBurstShotsRemaining--;
        ReportAttackNoise(primaryWeapon, shotOrigin, false);

        float spread = GetEffectiveSpreadAngle() * attackSpreadMultiplier;
        Vector3 spreadDirection = ApplySpread(shotDirection, spread);
        if (TryGetCombatHit(shotOrigin, spreadDirection, GetEffectiveWeaponRange(), out RaycastHit hit))
        {
            PrototypeUnitVitals.DamageInfo damageInfo = BuildFirearmDamageInfo(primaryWeapon);
            ApplyWeaponCriticalHit(ref damageInfo);
            ResolveCombatHit(hit, damageInfo, ResolveImpactForce(primaryWeapon));
        }

        if (currentBurstShotsRemaining > 0)
        {
            nextBurstShotTime = Time.time
                + GetWeaponSecondsPerShot() * rangedBurstShotIntervalMultiplier
                + UnityEngine.Random.Range(0f, attackCadenceJitter);
        }
        else
        {
            attackSequenceCooldownUntil = Time.time
                + rangedAttackRecoveryCooldown
                + UnityEngine.Random.Range(0f, rangedAttackRecoveryJitter);
            nextBurstShotTime = 0f;
            ClearRangedAimLock();
        }

        if (magazineAmmo <= 0)
        {
            StartReload();
        }
    }

    private void TryMeleeAttack(Vector3 aimPoint, float distanceToTarget)
    {
        float meleeRange = primaryWeapon != null ? primaryWeapon.MeleeRange : combatRange;
        float meleeRadius = primaryWeapon != null ? primaryWeapon.MeleeRadius : 0.35f;

        if (Time.time < nextAttackTime || distanceToTarget > meleeRange)
        {
            return;
        }

        Vector3 attackOrigin = GetEyePosition();
        Vector3 attackDirection = (aimPoint - attackOrigin).normalized;
        nextAttackTime = Time.time + (primaryWeapon != null ? primaryWeapon.MeleeCooldown : 0.7f);
        ReportAttackNoise(primaryWeapon, attackOrigin, true);

        RaycastHit[] hits = Physics.SphereCastAll(
            attackOrigin,
            meleeRadius,
            attackDirection,
            meleeRange,
            perceptionMask,
            QueryTriggerInteraction.Collide);

        if (TrySelectCombatHit(hits, out RaycastHit hit))
        {
            PrototypeUnitVitals.DamageInfo damageInfo = BuildMeleeDamageInfo(primaryWeapon);
            ApplyWeaponCriticalHit(ref damageInfo);
            ResolveCombatHit(hit, damageInfo, ResolveImpactForce(primaryWeapon));
        }
    }

    private bool TryGetCombatHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit hit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, perceptionMask, QueryTriggerInteraction.Collide);
        return TrySelectCombatHit(hits, out hit);
    }

    private bool TrySelectCombatHit(RaycastHit[] hits, out RaycastHit hit)
    {
        if (hits == null || hits.Length == 0)
        {
            hit = default;
            return false;
        }

        Array.Sort(hits, CompareHitDistance);
        for (int index = 0; index < hits.Length; index++)
        {
            RaycastHit candidate = hits[index];
            if (candidate.collider == null || candidate.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            PrototypeUnitHitbox hitbox = candidate.collider.GetComponent<PrototypeUnitHitbox>();
            if (hitbox == null)
            {
                hitbox = candidate.collider.GetComponentInParent<PrototypeUnitHitbox>();
            }

            if (candidate.collider.isTrigger && hitbox == null)
            {
                continue;
            }

            hit = candidate;
            return true;
        }

        hit = default;
        return false;
    }

    private PrototypeUnitVitals.DamageInfo BuildFirearmDamageInfo(PrototypeWeaponDefinition weaponDefinition)
    {
        AmmoDefinition ammo = weaponDefinition != null ? weaponDefinition.AmmoDefinition : null;
        float weaponDamage = weaponDefinition != null ? weaponDefinition.FirearmDamage : Mathf.Max(1f, fallbackDamage);
        float directDamage = Mathf.Max(1f, weaponDamage * (ammo != null ? ammo.DamageMultiplier : 1f));
        float armorDamage = ammo != null ? ammo.ArmorDamage : Mathf.Max(8f, directDamage * 0.5f);
        float rarityMultiplier = GetPrimaryWeaponStatMultiplier();
        float damageMultiplier = Mathf.Max(0.1f, primaryWeaponAffixSummary.DamageMultiplier);
        float enemyDamageMultiplier = PrototypePlayerProgressionUtility.GetEnemyDamageMultiplier(EnemyLevel, bossLootProfile);
        float penetrationPower = (ammo != null ? ammo.PenetrationPower : weaponDefinition != null ? weaponDefinition.PenetrationPower : 6f) * rarityMultiplier;

        return new PrototypeUnitVitals.DamageInfo
        {
            damage = Mathf.Max(1f, directDamage * rarityMultiplier * damageMultiplier * enemyDamageMultiplier),
            penetrationPower = Mathf.Max(0f, penetrationPower + primaryWeaponAffixSummary.ArmorPenetrationBonus),
            armorDamage = Mathf.Max(1f, armorDamage * rarityMultiplier * damageMultiplier * enemyDamageMultiplier),
            lightBleedChance = ammo != null ? ammo.LightBleedChance : weaponDefinition != null ? weaponDefinition.LightBleedChance : 0.08f,
            heavyBleedChance = ammo != null ? ammo.HeavyBleedChance : weaponDefinition != null ? weaponDefinition.HeavyBleedChance : 0.02f,
            fractureChance = ammo != null ? ammo.FractureChance : weaponDefinition != null ? weaponDefinition.FractureChance : 0.04f,
            bypassArmor = false,
            canApplyAfflictions = true,
            sourceUnit = vitals,
            sourceDisplayName = gameObject.name,
            sourceEffectDisplayName = string.Empty
        };
    }

    private PrototypeUnitVitals.DamageInfo BuildMeleeDamageInfo(PrototypeWeaponDefinition weaponDefinition)
    {
        float rarityMultiplier = GetPrimaryWeaponStatMultiplier();
        float damageMultiplier = Mathf.Max(0.1f, primaryWeaponAffixSummary.DamageMultiplier);
        float enemyDamageMultiplier = PrototypePlayerProgressionUtility.GetEnemyDamageMultiplier(EnemyLevel, bossLootProfile);
        float penetrationPower = weaponDefinition != null ? weaponDefinition.PenetrationPower : 4f;

        return new PrototypeUnitVitals.DamageInfo
        {
            damage = weaponDefinition != null
                ? Mathf.Max(1f, weaponDefinition.MeleeDamage * rarityMultiplier * damageMultiplier * enemyDamageMultiplier)
                : Mathf.Max(8f, fallbackDamage * enemyDamageMultiplier),
            penetrationPower = Mathf.Max(0f, penetrationPower * rarityMultiplier + primaryWeaponAffixSummary.ArmorPenetrationBonus),
            armorDamage = weaponDefinition != null
                ? Mathf.Max(6f, weaponDefinition.MeleeDamage * 0.25f * rarityMultiplier * damageMultiplier * enemyDamageMultiplier)
                : Mathf.Max(6f, 6f * enemyDamageMultiplier),
            lightBleedChance = weaponDefinition != null ? weaponDefinition.LightBleedChance : 0.3f,
            heavyBleedChance = weaponDefinition != null ? weaponDefinition.HeavyBleedChance : 0.06f,
            fractureChance = weaponDefinition != null ? weaponDefinition.FractureChance : 0.08f,
            bypassArmor = false,
            canApplyAfflictions = true,
            sourceUnit = vitals,
            sourceDisplayName = gameObject.name,
            sourceEffectDisplayName = string.Empty
        };
    }

    private void ResolveCombatHit(RaycastHit hit, PrototypeUnitVitals.DamageInfo damageInfo, float force)
    {
        PrototypeUnitHitbox hitbox = hit.collider.GetComponent<PrototypeUnitHitbox>();
        if (hitbox == null)
        {
            hitbox = hit.collider.GetComponentInParent<PrototypeUnitHitbox>();
        }

        bool shouldApplyImpactForce = true;
        if (hitbox != null)
        {
            hitbox.ApplyDamage(damageInfo);
            if (hitbox.Owner != null)
            {
                shouldApplyImpactForce = hitbox.Owner.ShouldReceiveImpactForce;
            }
        }
        else
        {
            PrototypeBreakable breakable = hit.collider.GetComponent<PrototypeBreakable>();
            if (breakable == null)
            {
                breakable = hit.collider.GetComponentInParent<PrototypeBreakable>();
            }

            if (breakable != null)
            {
                breakable.ApplyDamage(damageInfo, hit.point, (hit.point - GetEyePosition()).normalized, force);
                shouldApplyImpactForce = false;
            }
        }

        if (shouldApplyImpactForce && hit.rigidbody != null && force > 0f)
        {
            hit.rigidbody.AddForce((hit.point - GetEyePosition()).normalized * force, ForceMode.Impulse);
        }
    }

    private void StartReload()
    {
        if (primaryWeapon == null || primaryWeapon.IsMeleeWeapon || isReloading)
        {
            return;
        }

        isReloading = true;
        reloadEndTime = Time.time + GetWeaponReloadDuration();
    }

    private void InitializeWeaponState()
    {
        isReloading = false;
        magazineAmmo = primaryWeapon != null && !primaryWeapon.IsMeleeWeapon
            ? Mathf.Clamp(startingMagazineAmmo > 0 ? startingMagazineAmmo : primaryWeapon.MagazineSize, 0, primaryWeapon.MagazineSize)
            : 0;
        nextAttackTime = 0f;
        nextBurstShotTime = 0f;
        reloadEndTime = 0f;
        attackSequenceCooldownUntil = 0f;
        currentBurstShotsRemaining = 0;
        ClearRangedAimLock();
    }

    private void ApplyPrimaryWeaponLootState(ItemRarity rarity, IReadOnlyList<ItemAffix> affixes, IReadOnlyList<ItemSkill> skills)
    {
        primaryWeaponRarity = ItemRarityUtility.Sanitize(rarity);
        primaryWeaponAffixes = affixes != null ? ItemAffixUtility.CloneList(affixes) : new List<ItemAffix>();
        primaryWeaponSkills = skills != null ? ItemSkillUtility.CloneList(skills) : new List<ItemSkill>();
        SanitizePrimaryWeaponLootState();
    }

    private void SanitizePrimaryWeaponLootState()
    {
        if (primaryWeaponAffixes == null)
        {
            primaryWeaponAffixes = new List<ItemAffix>();
        }

        if (primaryWeaponSkills == null)
        {
            primaryWeaponSkills = new List<ItemSkill>();
        }

        ItemAffixUtility.SanitizeAffixes(primaryWeaponAffixes);
        ItemSkillUtility.SanitizeSkills(primaryWeaponSkills);
        primaryWeaponRarity = ItemRarityUtility.Sanitize(primaryWeaponRarity);
        primaryWeaponAffixSummary = ItemAffixUtility.BuildSummary(primaryWeaponAffixes);
    }

    private float GetPrimaryWeaponStatMultiplier()
    {
        return ItemRarityUtility.GetStatMultiplier(primaryWeaponRarity);
    }

    private float GetEffectiveSpreadAngle()
    {
        return primaryWeapon != null
            ? primaryWeapon.SpreadAngle * Mathf.Max(0.1f, primaryWeaponAffixSummary.SpreadMultiplier)
            : 0f;
    }

    private float GetEffectiveWeaponRange()
    {
        return primaryWeapon != null
            ? primaryWeapon.EffectiveRange * Mathf.Max(0.1f, primaryWeaponAffixSummary.EffectiveRangeMultiplier)
            : 0f;
    }

    private float GetWeaponSecondsPerShot()
    {
        return primaryWeapon != null
            ? primaryWeapon.SecondsPerShot / Mathf.Max(0.1f, primaryWeaponAffixSummary.FireRateMultiplier)
            : 0.25f;
    }

    private float GetWeaponReloadDuration()
    {
        return primaryWeapon != null
            ? primaryWeapon.ReloadDuration / Mathf.Max(0.1f, primaryWeaponAffixSummary.ReloadSpeedMultiplier)
            : 1f;
    }

    private int GetWeaponMagazineSize()
    {
        return primaryWeapon != null && !primaryWeapon.IsMeleeWeapon ? primaryWeapon.MagazineSize : 0;
    }

    private void ApplyWeaponCriticalHit(ref PrototypeUnitVitals.DamageInfo damageInfo)
    {
        if (primaryWeaponAffixSummary.CritChance <= 0f || UnityEngine.Random.value >= primaryWeaponAffixSummary.CritChance)
        {
            return;
        }

        damageInfo.damage *= Mathf.Max(1f, primaryWeaponAffixSummary.CritDamageMultiplier);
    }

    private void RefreshArchetypeDefaults(bool force = false)
    {
        bool shouldApply = useArchetypeDefaults
            && (force || !previousUseArchetypeDefaults || lastConfiguredArchetype != archetype);

        if (shouldApply)
        {
            ApplyArchetypeDefaults();
        }

        lastConfiguredArchetype = archetype;
        previousUseArchetypeDefaults = useArchetypeDefaults;
    }

    private void ApplyArchetypeDefaults()
    {
        switch (archetype)
        {
            case PrototypeEnemyArchetype.PoliceZombie:
                detectionRadius = 20f;
                fieldOfView = 115f;
                smellDetectionRadius = 0f;
                loseSightGraceTime = 1.5f;
                searchGiveUpTime = 5.4f;
                turnSpeedDegrees = 120f;
                guaranteedAwarenessRadius = 2.8f;
                hearingSearchRadius = 3.8f;
                hearingRangeMultiplier = 1.35f;
                hearingFacingWeight = 0.48f;
                patrolSpeed = 0.95f;
                chaseSpeed = 1.9f;
                patrolWaitTime = 0.65f;
                searchPauseTime = 0.3f;
                combatRange = 16f;
                preferredCombatDistance = 11f;
                attackSpreadMultiplier = 1.3f;
                attackCadenceJitter = 0.04f;
                rangedAttackBurstMinShots = 1;
                rangedAttackBurstMaxShots = 1;
                rangedBurstShotIntervalMultiplier = 1f;
                rangedAttackRecoveryCooldown = 1.1f;
                rangedAttackRecoveryJitter = 0.18f;
                rangedAimLockRadius = 0.72f;
                rangedAimLockRefreshIntervalMin = 1.05f;
                rangedAimLockRefreshIntervalMax = 1.45f;
                break;

            case PrototypeEnemyArchetype.SoldierZombie:
                detectionRadius = 22f;
                fieldOfView = 110f;
                smellDetectionRadius = 0f;
                loseSightGraceTime = 1.7f;
                searchGiveUpTime = 6f;
                turnSpeedDegrees = 92f;
                guaranteedAwarenessRadius = 3f;
                hearingSearchRadius = 4.2f;
                hearingRangeMultiplier = 1.4f;
                hearingFacingWeight = 0.45f;
                patrolSpeed = 1f;
                chaseSpeed = 2f;
                patrolWaitTime = 0.7f;
                searchPauseTime = 0.25f;
                combatRange = 19f;
                preferredCombatDistance = 13.5f;
                attackSpreadMultiplier = 1.05f;
                attackCadenceJitter = 0.03f;
                rangedAttackBurstMinShots = 3;
                rangedAttackBurstMaxShots = 4;
                rangedBurstShotIntervalMultiplier = 0.92f;
                rangedAttackRecoveryCooldown = 1.45f;
                rangedAttackRecoveryJitter = 0.2f;
                rangedAimLockRadius = 0.5f;
                rangedAimLockRefreshIntervalMin = 1.2f;
                rangedAimLockRefreshIntervalMax = 1.65f;
                break;

            case PrototypeEnemyArchetype.ZombieDog:
                detectionRadius = 19f;
                fieldOfView = 145f;
                smellDetectionRadius = 12f;
                loseSightGraceTime = 1.8f;
                searchGiveUpTime = 6.5f;
                turnSpeedDegrees = 420f;
                guaranteedAwarenessRadius = 4.5f;
                hearingSearchRadius = 4.8f;
                hearingRangeMultiplier = 1.45f;
                hearingFacingWeight = 0.35f;
                patrolSpeed = 1.8f;
                chaseSpeed = 4.6f;
                patrolWaitTime = 0.25f;
                searchPauseTime = 0.12f;
                combatRange = 2.25f;
                preferredCombatDistance = 1.15f;
                attackSpreadMultiplier = 1f;
                attackCadenceJitter = 0.02f;
                rangedAttackBurstMinShots = 1;
                rangedAttackBurstMaxShots = 1;
                rangedBurstShotIntervalMultiplier = 1f;
                rangedAttackRecoveryCooldown = 0.7f;
                rangedAttackRecoveryJitter = 0.05f;
                rangedAimLockRadius = 0.35f;
                rangedAimLockRefreshIntervalMin = 0.65f;
                rangedAimLockRefreshIntervalMax = 0.95f;
                break;

            default:
                detectionRadius = 18f;
                fieldOfView = 120f;
                smellDetectionRadius = 0f;
                loseSightGraceTime = 1.4f;
                searchGiveUpTime = 5f;
                turnSpeedDegrees = 120f;
                guaranteedAwarenessRadius = 2.5f;
                hearingSearchRadius = 3.4f;
                hearingRangeMultiplier = 1.3f;
                hearingFacingWeight = 0.5f;
                patrolSpeed = 0.9f;
                chaseSpeed = 1.8f;
                patrolWaitTime = 0.75f;
                searchPauseTime = 0.35f;
                combatRange = 1.9f;
                preferredCombatDistance = 1f;
                attackSpreadMultiplier = 1.15f;
                attackCadenceJitter = 0.02f;
                rangedAttackBurstMinShots = 1;
                rangedAttackBurstMaxShots = 1;
                rangedBurstShotIntervalMultiplier = 1f;
                rangedAttackRecoveryCooldown = 0.85f;
                rangedAttackRecoveryJitter = 0.08f;
                rangedAimLockRadius = 0.45f;
                rangedAimLockRefreshIntervalMin = 0.85f;
                rangedAimLockRefreshIntervalMax = 1.2f;
                break;
        }

        if (primaryWeapon != null)
        {
            if (primaryWeapon.IsMeleeWeapon)
            {
                combatRange = Mathf.Max(combatRange, primaryWeapon.MeleeRange + 0.2f);
                preferredCombatDistance = Mathf.Min(preferredCombatDistance, Mathf.Max(0.75f, primaryWeapon.MeleeRange * 0.82f));
            }
            else
            {
                combatRange = Mathf.Max(combatRange, Mathf.Min(primaryWeapon.EffectiveRange * 0.72f, detectionRadius));
                preferredCombatDistance = Mathf.Clamp(preferredCombatDistance, 4f, combatRange - 0.5f);
            }
        }
    }

    private void ReportAttackNoise(PrototypeWeaponDefinition weaponDefinition, Vector3 origin, bool isMelee)
    {
        if (weaponDefinition == null)
        {
            return;
        }

        float radius = isMelee
            ? Mathf.Max(4f, weaponDefinition.MeleeRange * 4.5f)
            : Mathf.Max(10f, weaponDefinition.EffectiveRange * 0.52f);
        PrototypeCombatNoiseSystem.ReportNoise(origin, radius, gameObject);
    }

    private void BeginRangedAttackSequence(Vector3 aimPoint)
    {
        int burstMin = Mathf.Max(1, rangedAttackBurstMinShots);
        int burstMax = Mathf.Max(burstMin, rangedAttackBurstMaxShots);
        currentBurstShotsRemaining = Mathf.Min(UnityEngine.Random.Range(burstMin, burstMax + 1), magazineAmmo);
        nextBurstShotTime = Time.time;
        currentAimPoint = GetOrRefreshRangedAimPoint(aimPoint, true);
    }

    private Vector3 GetOrRefreshRangedAimPoint(Vector3 visibleAimPoint, bool forceRefresh)
    {
        if (forceRefresh || !hasLockedAimPoint || Time.time >= nextAimLockRefreshTime)
        {
            lockedAimPoint = visibleAimPoint;
            hasLockedAimPoint = true;
            nextAimLockRefreshTime = Time.time + UnityEngine.Random.Range(
                rangedAimLockRefreshIntervalMin,
                rangedAimLockRefreshIntervalMax);
        }

        return lockedAimPoint;
    }

    private Vector3 BuildRangedShotAimPoint(Vector3 aimPoint, Vector3 shotOrigin)
    {
        if (rangedAimLockRadius <= 0f)
        {
            return aimPoint;
        }

        Vector3 toAimPoint = aimPoint - shotOrigin;
        Vector3 forward = toAimPoint.sqrMagnitude > 0.0001f ? toAimPoint.normalized : transform.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        if (right.sqrMagnitude <= 0.0001f)
        {
            right = transform.right;
        }

        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * rangedAimLockRadius;
        return aimPoint + right * randomOffset.x + Vector3.up * randomOffset.y;
    }

    private void ResetRangedAttackSequence(bool preserveRecoveryCooldown)
    {
        currentBurstShotsRemaining = 0;
        nextBurstShotTime = 0f;

        if (!preserveRecoveryCooldown)
        {
            attackSequenceCooldownUntil = 0f;
        }
    }

    private void ClearRangedAimLock()
    {
        hasLockedAimPoint = false;
        nextAimLockRefreshTime = 0f;
    }

    private Vector3 GetEyePosition()
    {
        return eyeAnchor != null ? eyeAnchor.position : transform.position + Vector3.up * 1.45f;
    }

    private Vector3 GetTargetAimPoint()
    {
        if (combatTarget == null)
        {
            return transform.position + Vector3.up;
        }

        Transform torsoHitbox = combatTarget.Find("Hitbox_Torso");
        if (torsoHitbox != null)
        {
            return torsoHitbox.position;
        }

        return combatTarget.position + Vector3.up * 1.05f;
    }

    private void ResolveReferences()
    {
        if (botRigidbody == null)
        {
            botRigidbody = GetComponent<Rigidbody>();
        }

        if (vitals == null)
        {
            vitals = GetComponent<PrototypeUnitVitals>();
        }

        if (eyeAnchor == null)
        {
            Transform headHitbox = transform.Find("Hitbox_Head");
            if (headHitbox != null)
            {
                eyeAnchor = headHitbox;
            }
        }

        if (raidGameMode == null)
        {
            raidGameMode = FindFirstObjectByType<RaidGameMode>();
        }

        ResolveTargetIfNeeded();
    }

    private void ResolveTargetIfNeeded()
    {
        if (combatTarget == null)
        {
            PrototypeFpsController player = FindFirstObjectByType<PrototypeFpsController>();
            if (player != null)
            {
                combatTarget = player.transform;
            }
        }

        targetVitals = combatTarget != null ? combatTarget.GetComponent<PrototypeUnitVitals>() : null;
    }

    private void EnsureSettings()
    {
        SanitizePrimaryWeaponLootState();
        detectionRadius = Mathf.Max(1f, detectionRadius);
        fieldOfView = Mathf.Clamp(fieldOfView, 25f, 180f);
        smellDetectionRadius = Mathf.Max(0f, smellDetectionRadius);
        loseSightGraceTime = Mathf.Max(0.1f, loseSightGraceTime);
        searchGiveUpTime = Mathf.Max(loseSightGraceTime, searchGiveUpTime);
        turnSpeedDegrees = Mathf.Max(60f, turnSpeedDegrees);
        guaranteedAwarenessRadius = Mathf.Max(0f, guaranteedAwarenessRadius);
        hearingSearchRadius = Mathf.Max(0.5f, hearingSearchRadius);
        hearingRangeMultiplier = Mathf.Max(1f, hearingRangeMultiplier);
        hearingFacingWeight = Mathf.Max(0f, hearingFacingWeight);
        patrolSpeed = Mathf.Max(0.1f, patrolSpeed);
        chaseSpeed = Mathf.Max(patrolSpeed, chaseSpeed);
        patrolPointTolerance = Mathf.Max(0.05f, patrolPointTolerance);
        patrolWaitTime = Mathf.Max(0f, patrolWaitTime);
        searchStopDistance = Mathf.Max(0.05f, searchStopDistance);
        searchPauseTime = Mathf.Max(0f, searchPauseTime);
        combatRange = Mathf.Max(0.1f, combatRange);
        preferredCombatDistance = Mathf.Clamp(preferredCombatDistance, 0.5f, combatRange);
        attackSpreadMultiplier = Mathf.Max(0f, attackSpreadMultiplier);
        attackCadenceJitter = Mathf.Max(0f, attackCadenceJitter);
        rangedAttackBurstMinShots = Mathf.Max(1, rangedAttackBurstMinShots);
        rangedAttackBurstMaxShots = Mathf.Max(rangedAttackBurstMinShots, rangedAttackBurstMaxShots);
        rangedBurstShotIntervalMultiplier = Mathf.Max(0.1f, rangedBurstShotIntervalMultiplier);
        rangedAttackRecoveryCooldown = Mathf.Max(0f, rangedAttackRecoveryCooldown);
        rangedAttackRecoveryJitter = Mathf.Max(0f, rangedAttackRecoveryJitter);
        rangedAimLockRadius = Mathf.Max(0f, rangedAimLockRadius);
        rangedAimLockRefreshIntervalMin = Mathf.Max(0.05f, rangedAimLockRefreshIntervalMin);
        rangedAimLockRefreshIntervalMax = Mathf.Max(rangedAimLockRefreshIntervalMin, rangedAimLockRefreshIntervalMax);
        fallbackDamage = Mathf.Max(1f, fallbackDamage);
        fallbackImpactForce = Mathf.Max(0f, fallbackImpactForce);
        corpseInteractionLabel = string.IsNullOrWhiteSpace(corpseInteractionLabel) ? "Search Corpse" : corpseInteractionLabel.Trim();
        corpseLootSlots = Mathf.Max(1, corpseLootSlots);
        bonusLootRarityBias = Mathf.Clamp(bonusLootRarityBias, 0, 3);
        bonusLootRolls = Mathf.Max(0, bonusLootRolls);
        bonusLootItemLevel = Mathf.Max(0, bonusLootItemLevel);
        startingMagazineAmmo = primaryWeapon != null && !primaryWeapon.IsMeleeWeapon
            ? Mathf.Clamp(startingMagazineAmmo > 0 ? startingMagazineAmmo : primaryWeapon.MagazineSize, 0, primaryWeapon.MagazineSize)
            : 0;

        if (patrolPoints == null)
        {
            patrolPoints = new List<Vector3>();
        }

        if (botRigidbody != null && state != BotState.Dead)
        {
            botRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            botRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (perceptionMask.value == 0 || perceptionMask.value == ~0)
        {
            perceptionMask = Physics.DefaultRaycastLayers;
        }
    }

    private void HandleDied(PrototypeUnitVitals ownerVitals)
    {
        state = BotState.Dead;
        desiredPlanarVelocity = Vector3.zero;
        ResetRangedAttackSequence(false);
        ClearRangedAimLock();
        BuildCorpseLoot();

        if (botRigidbody != null)
        {
            Vector3 velocity = botRigidbody.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            botRigidbody.linearVelocity = velocity;
            botRigidbody.constraints = RigidbodyConstraints.None;
        }
    }

    private void BuildCorpseLoot()
    {
        if (corpseLootBuilt)
        {
            return;
        }

        InventoryContainer corpseInventory = GetComponent<InventoryContainer>();
        if (corpseInventory == null)
        {
            corpseInventory = gameObject.AddComponent<InventoryContainer>();
        }

        corpseInventory.Configure(BuildCorpseLootLabel(), corpseLootSlots, 0f);

        if (rollCarriedLootOnDeath)
        {
            TransferCarriedLootToCorpse(corpseInventory);
        }

        if (transferEquippedArmorToCorpse)
        {
            TransferArmorToCorpse(corpseInventory);
        }

        if (transferMagazineAmmoToCorpse && (!transferPrimaryWeaponToCorpse || primaryWeapon == null || primaryWeapon.IsMeleeWeapon))
        {
            TransferMagazineAmmoToCorpse(corpseInventory);
        }

        LootContainer lootContainer = GetComponent<LootContainer>();
        if (lootContainer == null)
        {
            lootContainer = gameObject.AddComponent<LootContainer>();
        }

        lootContainer.Configure(BuildCorpseLootLabel(), corpseInventory, "Search");

        if (transferPrimaryWeaponToCorpse)
        {
            PrototypeCorpseLoot corpseLoot = GetComponent<PrototypeCorpseLoot>();
            if (corpseLoot == null)
            {
                corpseLoot = gameObject.AddComponent<PrototypeCorpseLoot>();
            }

            corpseLoot.Configure(BuildCorpseLootLabel());
            if (primaryWeapon != null)
            {
                ItemInstance droppedWeapon = ItemInstance.Create(
                    primaryWeapon,
                    primaryWeapon.IsMeleeWeapon ? 0 : magazineAmmo,
                    1f,
                    null,
                    primaryWeaponRarity,
                    primaryWeaponAffixes,
                    false,
                    primaryWeaponSkills,
                    false);
                corpseLoot.AddWeapon(droppedWeapon);
            }
        }

        corpseLootBuilt = true;
    }

    private void TransferArmorToCorpse(InventoryContainer corpseInventory)
    {
        if (corpseInventory == null || vitals == null || vitals.EquippedArmor == null)
        {
            return;
        }

        var addedArmorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < vitals.EquippedArmor.Count; index++)
        {
            PrototypeUnitVitals.ArmorState armorState = vitals.EquippedArmor[index];
            ArmorDefinition armorDefinition = armorState?.definition;
            if (armorDefinition == null || !addedArmorIds.Add(armorDefinition.ItemId))
            {
                continue;
            }

            corpseInventory.TryAddItemInstance(ItemInstance.Create(armorDefinition, armorState.currentDurability, null, armorState.Rarity));
        }
    }

    private void TransferCarriedLootToCorpse(InventoryContainer corpseInventory)
    {
        if (corpseInventory == null || carriedLootTable == null)
        {
            return;
        }

        List<LootTableDefinition.LootRoll> lootRolls = carriedLootTable.RollLoot(ResolveLootGenerationContext());
        for (int index = 0; index < lootRolls.Count; index++)
        {
            LootTableDefinition.LootRoll roll = lootRolls[index];
            if (roll.Instance != null && roll.Instance.IsDefined() && roll.Instance.Quantity > 0)
            {
                corpseInventory.TryAddItemInstance(roll.Instance.Clone());
            }
        }
    }

    private void TransferMagazineAmmoToCorpse(InventoryContainer corpseInventory)
    {
        if (corpseInventory == null
            || primaryWeapon == null
            || primaryWeapon.IsMeleeWeapon
            || primaryWeapon.AmmoDefinition == null
            || magazineAmmo <= 0)
        {
            return;
        }

        corpseInventory.TryAddItem(primaryWeapon.AmmoDefinition, magazineAmmo, out _);
    }

    private string BuildCorpseLootLabel()
    {
        string baseLabel = string.IsNullOrWhiteSpace(corpseInteractionLabel) ? "Search Corpse" : corpseInteractionLabel.Trim();
        return $"Lv {EnemyLevel} {gameObject.name} - {baseLabel}";
    }

    private LootTableDefinition.LootGenerationContext ResolveLootGenerationContext()
    {
        LootTableDefinition.LootGenerationContext context = raidGameMode != null
            ? raidGameMode.CreateLootContext()
            : default;

        if (!bossLootProfile)
        {
            return context;
        }

        return context.WithBonuses(bonusLootItemLevel, bonusLootRarityBias, bonusLootRolls);
    }

    private void RefreshEnemyProgression(bool resetHealth)
    {
        enemyLevel = ResolveEnemyLevel();
        if (vitals == null)
        {
            return;
        }

        float baseVitalHealth = 0f;
        IReadOnlyList<PrototypeUnitVitals.PartState> bodyPartStates = vitals.BodyParts;
        if (bodyPartStates != null)
        {
            for (int index = 0; index < bodyPartStates.Count; index++)
            {
                PrototypeUnitVitals.PartState state = bodyPartStates[index];
                if (state != null && state.contributesToUnitHealth)
                {
                    baseVitalHealth += Mathf.Max(1f, state.baseMaxHealth);
                }
            }
        }

        if (baseVitalHealth <= 0f)
        {
            baseVitalHealth = Mathf.Max(1f, vitals.TotalMaxHealth);
        }

        float healthMultiplier = PrototypePlayerProgressionUtility.GetEnemyHealthMultiplier(EnemyLevel, bossLootProfile);
        float bonusHealth = Mathf.Max(0f, baseVitalHealth * (healthMultiplier - 1f));
        vitals.ConfigureCharacterBonuses(bonusHealth, 0f, 1f, resetHealth);
    }

    private int ResolveEnemyLevel()
    {
        int baseLevel = PrototypePlayerProgressionUtility.GetEnemyBaseLevel(archetype);
        int bossBonus = bossLootProfile ? 1 : 0;
        return Mathf.Max(1, baseLevel + Mathf.Max(0, bonusLootItemLevel) + bossBonus);
    }

    private float ResolveImpactForce(PrototypeWeaponDefinition weaponDefinition)
    {
        if (weaponDefinition == null)
        {
            return fallbackImpactForce;
        }

        AmmoDefinition ammo = weaponDefinition.AmmoDefinition;
        float ammoImpact = ammo != null ? ammo.ImpactForce : fallbackImpactForce;
        return ammoImpact + weaponDefinition.AddedImpactForce;
    }

    private Vector3 ApplySpread(Vector3 direction, float spreadAngle)
    {
        if (spreadAngle <= 0f)
        {
            return direction;
        }

        Quaternion spreadRotation = Quaternion.Euler(
            UnityEngine.Random.Range(-spreadAngle, spreadAngle),
            UnityEngine.Random.Range(-spreadAngle, spreadAngle),
            0f);

        return spreadRotation * direction;
    }

    private static int CompareHitDistance(RaycastHit left, RaycastHit right)
    {
        return left.distance.CompareTo(right.distance);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.95f, 0.55f, 0.12f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (smellDetectionRadius > 0f)
        {
            Gizmos.color = new Color(0.4f, 1f, 0.52f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, smellDetectionRadius);
        }

        if (patrolPoints == null || patrolPoints.Count == 0)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.78f, 1f, 0.9f);
        for (int index = 0; index < patrolPoints.Count; index++)
        {
            Vector3 currentPoint = patrolPoints[index];
            Vector3 nextPoint = patrolPoints[(index + 1) % patrolPoints.Count];
            Gizmos.DrawSphere(currentPoint, 0.16f);
            Gizmos.DrawLine(currentPoint, nextPoint);
        }

        Gizmos.color = new Color(0.95f, 0.82f, 0.2f, 0.9f);
        for (int index = 0; index < searchPoints.Count; index++)
        {
            Gizmos.DrawWireSphere(searchPoints[index], 0.12f);
        }
    }
}
