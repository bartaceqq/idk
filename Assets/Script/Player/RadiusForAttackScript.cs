using System.Collections.Generic;
using UnityEngine;

// Controls proximity-based enemy hit detection for melee attacks.
public class RadiusForAttackScript : MonoBehaviour
{
    [System.Serializable]
    public class SwordHitWindow
    {
        [Range(0f, 1f)] public float startNormalized = 0.25f;
        [Range(0f, 1f)] public float endNormalized = 0.5f;
        [Min(0f)] public float damageMultiplier = 1f;
    }

    [System.Serializable]
    public class SwordHitProfile
    {
        public string stateName;
        [Min(0.1f)] public float radiusMultiplier = 1f;
        public List<SwordHitWindow> windows = new List<SwordHitWindow>();
    }

    public GameObject player;
    public Transform attackOrigin;
    public EnemiesHandler enemiesHandler;
    public ActionScript actionScript;
    public ItemSwitchScript itemSwitchScript;
    public float attackRadius = 5f;
    public float attackDamage = 40f;
    public Vector3 attackOriginLocalOffset = new Vector3(0f, 1f, 1.25f);
    public LayerMask enemyMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    public List<SwordHitProfile> swordHitProfiles = new List<SwordHitProfile>();

    private readonly Collider[] _overlapHits = new Collider[128];
    private readonly HashSet<NPCDemageScript> _uniqueTargets = new HashSet<NPCDemageScript>();
    private readonly HashSet<Animalec> _uniqueAnimals = new HashSet<Animalec>();
    private readonly List<HashSet<int>> _windowHitTargetIds = new List<HashSet<int>>();
    private int _trackedSwordStateHash;
    private float _lastTrackedSwordProgress;

    private void Awake()
    {
        ResolveReferences();
        EnsureSwordHitProfiles();
    }

    private void OnValidate()
    {
        EnsureSwordHitProfiles();
    }

    private void Update()
    {
        UpdateSwordAttackHits();
    }

    // Handle Resolve References.
    private void ResolveReferences()
    {
        if (player == null)
        {
            player = gameObject;
        }

        if (enemiesHandler == null)
        {
            enemiesHandler = FindFirstObjectByType<EnemiesHandler>();
        }

        if (actionScript == null)
        {
            actionScript = GetComponent<ActionScript>();
            if (actionScript == null)
            {
                actionScript = GetComponentInParent<ActionScript>();
            }

            if (actionScript == null)
            {
                actionScript = FindFirstObjectByType<ActionScript>(FindObjectsInactive.Include);
            }
        }

        if (itemSwitchScript == null)
        {
            itemSwitchScript = GetComponent<ItemSwitchScript>();
            if (itemSwitchScript == null)
            {
                itemSwitchScript = GetComponentInParent<ItemSwitchScript>();
            }

            if (itemSwitchScript == null)
            {
                itemSwitchScript = FindFirstObjectByType<ItemSwitchScript>(FindObjectsInactive.Include);
            }
        }
    }

    // Handle Ensure Sword Hit Profiles.
    private void EnsureSwordHitProfiles()
    {
        if (swordHitProfiles != null && swordHitProfiles.Count > 0)
        {
            return;
        }

        swordHitProfiles = new List<SwordHitProfile>
        {
            CreateProfile("SwordAttack", 1f, new SwordHitWindow
            {
                startNormalized = 0.14f,
                endNormalized = 0.40f,
                damageMultiplier = 1f
            }),
            CreateProfile(
                "SwordAttack2",
                1f,
                new SwordHitWindow
                {
                    startNormalized = 0.10f,
                    endNormalized = 0.30f,
                    damageMultiplier = 0.55f
                },
                new SwordHitWindow
                {
                    startNormalized = 0.50f,
                    endNormalized = 0.70f,
                    damageMultiplier = 0.65f
                }),
            CreateProfile(
                "SpecialAttack1",
                1.15f,
                new SwordHitWindow
                {
                    startNormalized = 0.12f,
                    endNormalized = 0.32f,
                    damageMultiplier = 0.65f
                },
                new SwordHitWindow
                {
                    startNormalized = 0.48f,
                    endNormalized = 0.74f,
                    damageMultiplier = 0.7f
                }),
            CreateProfile(
                "SpecialAttack2",
                1.2f,
                new SwordHitWindow
                {
                    startNormalized = 0.05f,
                    endNormalized = 0.18f,
                    damageMultiplier = 0.35f
                },
                new SwordHitWindow
                {
                    startNormalized = 0.28f,
                    endNormalized = 0.44f,
                    damageMultiplier = 0.35f
                },
                new SwordHitWindow
                {
                    startNormalized = 0.52f,
                    endNormalized = 0.68f,
                    damageMultiplier = 0.35f
                },
                new SwordHitWindow
                {
                    startNormalized = 0.80f,
                    endNormalized = 0.94f,
                    damageMultiplier = 0.5f
                }),
            CreateProfile(
                "SpecialAttack3",
                1.15f,
                new SwordHitWindow
                {
                    startNormalized = 0.28f,
                    endNormalized = 0.50f,
                    damageMultiplier = 0.55f
                },
                new SwordHitWindow
                {
                    startNormalized = 0.58f,
                    endNormalized = 0.80f,
                    damageMultiplier = 0.75f
                })
        };
    }

    // Handle Create Profile.
    private static SwordHitProfile CreateProfile(string stateName, float radiusMultiplier, params SwordHitWindow[] windows)
    {
        SwordHitProfile profile = new SwordHitProfile
        {
            stateName = stateName,
            radiusMultiplier = Mathf.Max(0.1f, radiusMultiplier),
            windows = new List<SwordHitWindow>()
        };

        if (windows != null)
        {
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] != null)
                {
                    profile.windows.Add(windows[i]);
                }
            }
        }

        return profile;
    }

    // Handle Update Sword Attack Hits.
    private void UpdateSwordAttackHits()
    {
        ResolveReferences();
        if (actionScript == null)
        {
            ResetTrackedSwordAttack();
            return;
        }

        if (!actionScript.TryGetActiveSwordAttackStateInfo(out AnimatorStateInfo stateInfo, out _))
        {
            ResetTrackedSwordAttack();
            return;
        }

        if (!TryResolveSwordHitProfile(stateInfo, out SwordHitProfile profile))
        {
            ResetTrackedSwordAttack();
            return;
        }

        float normalizedProgress = NormalizeStateProgress(stateInfo);
        bool stateRestarted = _trackedSwordStateHash != stateInfo.fullPathHash ||
            normalizedProgress + 0.05f < _lastTrackedSwordProgress;
        if (stateRestarted)
        {
            BeginTrackedSwordAttack(stateInfo.fullPathHash, profile.windows.Count);
        }

        _lastTrackedSwordProgress = normalizedProgress;
        int activeWindowIndex = FindActiveWindowIndex(profile, normalizedProgress);
        if (activeWindowIndex < 0)
        {
            return;
        }

        EnsureWindowRegistryCapacity(profile.windows.Count);
        SwordHitWindow activeWindow = profile.windows[activeWindowIndex];
        float radius = Mathf.Max(0.01f, attackRadius * Mathf.Max(0.1f, profile.radiusMultiplier));
        float baseDamage = ResolveSwordBaseDamage();
        float damage = baseDamage * Mathf.Max(0f, activeWindow.damageMultiplier);
        if (damage <= 0f)
        {
            return;
        }

        PerformAttackSweep(
            ResolveAttackOrigin(),
            radius,
            damage,
            _windowHitTargetIds[activeWindowIndex]);
    }

    // Handle Begin Tracked Sword Attack.
    private void BeginTrackedSwordAttack(int stateHash, int windowCount)
    {
        _trackedSwordStateHash = stateHash;
        _lastTrackedSwordProgress = 0f;
        EnsureWindowRegistryCapacity(windowCount);
        for (int i = 0; i < _windowHitTargetIds.Count; i++)
        {
            _windowHitTargetIds[i].Clear();
        }
    }

    // Handle Reset Tracked Sword Attack.
    private void ResetTrackedSwordAttack()
    {
        _trackedSwordStateHash = 0;
        _lastTrackedSwordProgress = 0f;
        for (int i = 0; i < _windowHitTargetIds.Count; i++)
        {
            _windowHitTargetIds[i].Clear();
        }
    }

    // Handle Ensure Window Registry Capacity.
    private void EnsureWindowRegistryCapacity(int windowCount)
    {
        int requiredCount = Mathf.Max(0, windowCount);
        while (_windowHitTargetIds.Count < requiredCount)
        {
            _windowHitTargetIds.Add(new HashSet<int>());
        }
    }

    // Handle Try Resolve Sword Hit Profile.
    private bool TryResolveSwordHitProfile(AnimatorStateInfo stateInfo, out SwordHitProfile resolvedProfile)
    {
        resolvedProfile = null;
        if (swordHitProfiles == null)
        {
            return false;
        }

        for (int i = 0; i < swordHitProfiles.Count; i++)
        {
            SwordHitProfile profile = swordHitProfiles[i];
            if (profile == null || string.IsNullOrWhiteSpace(profile.stateName) ||
                profile.windows == null || profile.windows.Count == 0)
            {
                continue;
            }

            if (!MatchesStateName(stateInfo, profile.stateName))
            {
                continue;
            }

            resolvedProfile = profile;
            return true;
        }

        return false;
    }

    // Handle Find Active Window Index.
    private static int FindActiveWindowIndex(SwordHitProfile profile, float normalizedProgress)
    {
        if (profile == null || profile.windows == null)
        {
            return -1;
        }

        for (int i = 0; i < profile.windows.Count; i++)
        {
            SwordHitWindow window = profile.windows[i];
            if (window == null)
            {
                continue;
            }

            float start = Mathf.Clamp01(window.startNormalized);
            float end = Mathf.Clamp(window.endNormalized, start, 1f);
            if (normalizedProgress >= start && normalizedProgress <= end)
            {
                return i;
            }
        }

        return -1;
    }

    // Handle Attack.
    public void Attack()
    {
        ResolveReferences();
        PerformAttackSweep(
            ResolveAttackOrigin(),
            Mathf.Max(0.01f, attackRadius),
            ResolveSwordBaseDamage(),
            null);
    }

    // Handle Resolve Sword Base Damage.
    private float ResolveSwordBaseDamage()
    {
        if (itemSwitchScript != null && itemSwitchScript.TryGetEquippedSword(out Sword equippedSword))
        {
            return equippedSword.GetResolvedDamage();
        }

        return Mathf.Max(0f, attackDamage);
    }

    // Handle Resolve Attack Origin.
    private Vector3 ResolveAttackOrigin()
    {
        if (attackOrigin != null)
        {
            return attackOrigin.position;
        }

        if (player != null)
        {
            return player.transform.TransformPoint(attackOriginLocalOffset);
        }

        return transform.TransformPoint(attackOriginLocalOffset);
    }

    // Handle Perform Attack Sweep.
    private void PerformAttackSweep(Vector3 origin, float radius, float damage, HashSet<int> alreadyHitTargetIds)
    {
        if (damage <= 0f)
        {
            return;
        }

        float radiusSqr = radius * radius;
        CollectTargets(origin, radius, radiusSqr);

        foreach (NPCDemageScript damageTarget in _uniqueTargets)
        {
            if (damageTarget == null)
            {
                continue;
            }

            Vector3 delta = damageTarget.transform.position - origin;
            if (delta.sqrMagnitude > radiusSqr)
            {
                continue;
            }

            int targetId = damageTarget.GetInstanceID();
            if (alreadyHitTargetIds != null && alreadyHitTargetIds.Contains(targetId))
            {
                continue;
            }

            damageTarget.TakeDemage(damage);
            alreadyHitTargetIds?.Add(targetId);
        }

        foreach (Animalec animalTarget in _uniqueAnimals)
        {
            if (animalTarget == null)
            {
                continue;
            }

            Vector3 delta = animalTarget.transform.position - origin;
            if (delta.sqrMagnitude > radiusSqr)
            {
                continue;
            }

            int targetId = animalTarget.GetInstanceID();
            if (alreadyHitTargetIds != null && alreadyHitTargetIds.Contains(targetId))
            {
                continue;
            }

            animalTarget.TakeDamage(damage);
            alreadyHitTargetIds?.Add(targetId);
        }
    }

    // Handle Collect Targets.
    private void CollectTargets(Vector3 origin, float radius, float radiusSqr)
    {
        _uniqueTargets.Clear();
        _uniqueAnimals.Clear();

        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            Mathf.Max(0.01f, radius),
            _overlapHits,
            enemyMask,
            triggerInteraction);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _overlapHits[i];
            if (hit == null || (player != null && hit.transform.IsChildOf(player.transform)))
            {
                continue;
            }

            NPCDemageScript damageTarget = hit.GetComponent<NPCDemageScript>();
            if (damageTarget == null)
            {
                damageTarget = hit.GetComponentInParent<NPCDemageScript>();
            }

            if (damageTarget == null)
            {
                damageTarget = hit.GetComponentInChildren<NPCDemageScript>();
            }

            if (damageTarget != null)
            {
                _uniqueTargets.Add(damageTarget);
            }

            Animalec animalTarget = hit.GetComponent<Animalec>();
            if (animalTarget == null)
            {
                animalTarget = hit.GetComponentInParent<Animalec>();
            }

            if (animalTarget == null)
            {
                animalTarget = hit.GetComponentInChildren<Animalec>();
            }

            if (animalTarget != null)
            {
                _uniqueAnimals.Add(animalTarget);
            }
        }

        if (_uniqueTargets.Count == 0)
        {
            CollectTargetsFromEnemyLists(origin, radiusSqr);
        }

        if (_uniqueAnimals.Count == 0)
        {
            CollectAnimalTargetsFromScene(origin, radiusSqr);
        }
    }

    // Handle Collect Targets From Enemy Lists.
    private void CollectTargetsFromEnemyLists(Vector3 origin, float radiusSqr)
    {
        if (enemiesHandler != null && enemiesHandler.enemies != null)
        {
            foreach (GameObject enemy in enemiesHandler.enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                Vector3 delta = enemy.transform.position - origin;
                if (delta.sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                NPCDemageScript fromList = enemy.GetComponent<NPCDemageScript>();
                if (fromList == null)
                {
                    fromList = enemy.GetComponentInChildren<NPCDemageScript>();
                }

                if (fromList == null)
                {
                    fromList = enemy.GetComponentInParent<NPCDemageScript>();
                }

                if (fromList != null)
                {
                    _uniqueTargets.Add(fromList);
                }
            }
        }

        NPCDemageScript[] allDamageTargets = FindObjectsByType<NPCDemageScript>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < allDamageTargets.Length; i++)
        {
            NPCDemageScript damageTarget = allDamageTargets[i];
            if (damageTarget == null)
            {
                continue;
            }

            Vector3 delta = damageTarget.transform.position - origin;
            if (delta.sqrMagnitude <= radiusSqr)
            {
                _uniqueTargets.Add(damageTarget);
            }
        }
    }

    // Handle Collect Animal Targets From Scene.
    private void CollectAnimalTargetsFromScene(Vector3 origin, float radiusSqr)
    {
        Animalec[] allAnimals = FindObjectsByType<Animalec>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < allAnimals.Length; i++)
        {
            Animalec animal = allAnimals[i];
            if (animal == null)
            {
                continue;
            }

            Vector3 delta = animal.transform.position - origin;
            if (delta.sqrMagnitude <= radiusSqr)
            {
                _uniqueAnimals.Add(animal);
            }
        }
    }

    // Handle Matches State Name.
    private static bool MatchesStateName(AnimatorStateInfo state, string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        return state.IsName(stateName) || state.IsName($"Base Layer.{stateName}");
    }

    // Handle Normalize State Progress.
    private static float NormalizeStateProgress(AnimatorStateInfo state)
    {
        float normalizedTime = state.normalizedTime;
        if (normalizedTime > 1f)
        {
            normalizedTime %= 1f;
        }

        return Mathf.Clamp01(normalizedTime);
    }
}
