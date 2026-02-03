using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UnityEngine;

namespace BonelabNpcMod;

public sealed class NpcGunResponderMod : MelonMod
{
    private GameObject? _host;

    public override void OnInitializeMelon()
    {
        _host = new GameObject("BonelabNpcGunResponder");
        _host.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(_host);
        _host.AddComponent<NpcGunResponder>();
    }
}

public sealed class NpcGunResponder : MonoBehaviour
{
    private const float ThreatRange = 25f;
    private const float ThreatAimCosine = 0.85f;
    private const float ThreatCheckInterval = 0.2f;
    private const float GunSearchRange = 10f;
    private const float ShotsPerBurst = 3f;
    private const float ShotCooldown = 0.2f;
    private const float BurstCooldown = 1.2f;

    private float _nextThreatCheck;
    private readonly Dictionary<int, NpcCombatant> _npcCombatants = new();

    private static readonly string[] NpcTypeNames =
    {
        "SLZ.AI.AIBrain",
        "SLZ.AI.AIBase",
    };

    private static readonly string[] GunTypeNames =
    {
        "SLZ.Gun",
        "SLZ.Weapon.Gun",
    };

    private static List<Type>? _cachedNpcTypes;
    private static List<Type>? _cachedGunTypes;

    private void Start()
    {
        NpcAssetSpawner.TrySpawn();
    }

    private void Update()
    {
        if (Time.time < _nextThreatCheck)
        {
            return;
        }

        _nextThreatCheck = Time.time + ThreatCheckInterval;

        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        var playerGun = FindPlayerGun(camera.transform);
        if (playerGun == null)
        {
            return;
        }

        var npcs = FindNpcBehaviours();
        foreach (var npc in npcs)
        {
            if (npc == null)
            {
                continue;
            }

            var threatened = IsThreatened(playerGun, camera.transform, npc.transform);
            var combatant = GetCombatant(npc);
            combatant.Tick(Time.deltaTime);

            if (combatant.Gun == null || !combatant.Gun.IsValid)
            {
                if (threatened)
                {
                    combatant.Gun = FindNearestGun(npc.transform.position, playerGun);
                }
            }

            combatant.UpdateState(isThreatened: threatened, hasGun: combatant.Gun != null);

            if (combatant.IsDead)
            {
                continue;
            }

            if (combatant.ShouldSurrender)
            {
                combatant.DropGun();
                continue;
            }

            if (!threatened)
            {
                continue;
            }

            if (combatant.Gun == null)
            {
                continue;
            }

            AttachGunToNpc(combatant.Gun, npc.transform);
            TryFireAtPlayer(combatant, camera.transform.position);
        }
    }

    private static IEnumerable<MonoBehaviour> FindNpcBehaviours()
    {
        foreach (var type in ResolveNpcTypes())
        {
            foreach (var found in UnityEngine.Object.FindObjectsOfType(type))
            {
                if (found is MonoBehaviour behaviour)
                {
                    yield return behaviour;
                }
            }
        }
    }

    private static GunAdapter? FindPlayerGun(Transform cameraTransform)
    {
        foreach (var gun in FindAllGuns())
        {
            if (!gun.IsHeld)
            {
                continue;
            }

            if (Vector3.Distance(gun.Transform.position, cameraTransform.position) > 2f)
            {
                continue;
            }

            return gun;
        }

        return null;
    }

    private static IEnumerable<GunAdapter> FindAllGuns()
    {
        var seen = new HashSet<int>();
        foreach (var type in ResolveGunTypes())
        {
            foreach (var found in UnityEngine.Object.FindObjectsOfType(type))
            {
                if (found is not MonoBehaviour behaviour)
                {
                    continue;
                }

                if (!seen.Add(behaviour.GetInstanceID()))
                {
                    continue;
                }

                if (GunAdapter.TryCreate(behaviour, out var adapter))
                {
                    yield return adapter;
                }
            }
        }
    }

    private static bool IsThreatened(GunAdapter playerGun, Transform cameraTransform, Transform npcTransform)
    {
        var aimTransform = playerGun.AimTransform ?? cameraTransform;
        var origin = aimTransform.position;
        var toNpc = npcTransform.position - origin;
        var distance = toNpc.magnitude;
        if (distance > ThreatRange)
        {
            return false;
        }

        if (Physics.Raycast(origin, aimTransform.forward, out var hit, ThreatRange))
        {
            if (hit.collider != null && hit.collider.transform.IsChildOf(npcTransform))
            {
                return true;
            }
        }

        var dot = Vector3.Dot(aimTransform.forward, toNpc.normalized);
        return dot >= ThreatAimCosine;
    }

    private static GunAdapter? FindNearestGun(Vector3 npcPosition, GunAdapter playerGun)
    {
        var nearest = (GunAdapter?)null;
        var bestDistance = float.MaxValue;

        foreach (var gun in FindAllGuns())
        {
            if (!gun.IsValid || gun.IsHeld)
            {
                continue;
            }

            if (gun.Transform == playerGun.Transform)
            {
                continue;
            }

            var distance = Vector3.Distance(npcPosition, gun.Transform.position);
            if (distance > GunSearchRange || distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            nearest = gun;
        }

        return nearest;
    }

    private static void AttachGunToNpc(GunAdapter gun, Transform npcTransform)
    {
        var hand = FindHandTransform(npcTransform) ?? npcTransform;
        if (gun.Transform.parent != hand)
        {
            gun.Transform.SetParent(hand, worldPositionStays: false);
        }

        gun.Transform.localPosition = Vector3.zero;
        gun.Transform.localRotation = Quaternion.identity;
        gun.SetKinematic(true);
    }

    private static Transform? FindHandTransform(Transform root)
    {
        var queue = new Queue<Transform>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var name = current.name.ToLowerInvariant();
            if (name.Contains("hand") && (name.Contains("r") || name.Contains("right")))
            {
                return current;
            }

            for (var i = 0; i < current.childCount; i++)
            {
                queue.Enqueue(current.GetChild(i));
            }
        }

        return null;
    }

    private void TryFireAtPlayer(NpcCombatant combatant, Vector3 playerPosition)
    {
        if (!combatant.CanShoot || Time.time < combatant.NextActionTime)
        {
            return;
        }

        if (!combatant.TryConsumeAmmoForShot())
        {
            return;
        }

        combatant.Gun?.AimAt(playerPosition);
        combatant.Gun?.Fire();
        combatant.ShotsFired += 1f;

        if (combatant.ShotsFired >= ShotsPerBurst)
        {
            combatant.ShotsFired = 0f;
            combatant.NextActionTime = Time.time + BurstCooldown;
        }
        else
        {
            combatant.NextActionTime = Time.time + ShotCooldown;
        }
    }

    private NpcCombatant GetCombatant(MonoBehaviour npc)
    {
        var id = npc.GetInstanceID();
        if (!_npcCombatants.TryGetValue(id, out var combatant))
        {
            combatant = npc.gameObject.GetComponent<NpcCombatant>() ?? npc.gameObject.AddComponent<NpcCombatant>();
            _npcCombatants[id] = combatant;
        }

        return combatant;
    }

    private static List<Type> ResolveNpcTypes()
    {
        _cachedNpcTypes ??= ResolveTypes(NpcTypeNames);
        return _cachedNpcTypes;
    }

    private static List<Type> ResolveGunTypes()
    {
        _cachedGunTypes ??= ResolveTypes(GunTypeNames);
        return _cachedGunTypes;
    }

    private static List<Type> ResolveTypes(IEnumerable<string> typeNames)
    {
        var types = new List<Type>();
        foreach (var typeName in typeNames)
        {
            var type = Type.GetType(typeName + ", Assembly-CSharp");
            if (type != null)
            {
                types.Add(type);
            }
        }

        return types;
    }
}
