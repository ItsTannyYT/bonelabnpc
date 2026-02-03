using System;
using UnityEngine;

namespace BonelabNpcMod;

public enum NpcCombatState
{
    Idle,
    Alerted,
    Combat,
    Searching,
    Fleeing,
    Reloading,
    Downed,
    Dead,
}

public sealed class NpcCombatant : MonoBehaviour
{
    private const float MaxHealthDefault = 100f;
    private const float DownedThreshold = 15f;
    private const float FleeThreshold = 20f;
    private const float SurrenderThreshold = 25f;
    private const int MagCapacity = 12;
    private const int TotalMags = 3;
    private const float ReloadDuration = 2.2f;
    private const float SearchDuration = 4f;
    private const float BleedDamagePerSecond = 2f;

    private float _bleedEndTime;
    private float _staggerEndTime;
    private float _reloadEndTime;
    private float _searchEndTime;

    public GunAdapter? Gun { get; set; }
    public float NextActionTime { get; set; }
    public float ShotsFired { get; set; }

    public float Health { get; private set; } = MaxHealthDefault;
    public int CurrentMagAmmo { get; private set; } = MagCapacity;
    public int ReserveMags { get; private set; } = TotalMags - 1;

    public bool ArmDisabled { get; private set; }
    public bool ShouldSurrender { get; private set; }
    public bool IsDead => State == NpcCombatState.Dead;

    public NpcCombatState State { get; private set; } = NpcCombatState.Idle;

    public bool CanShoot => State == NpcCombatState.Combat && !ShouldSurrender && !ArmDisabled && Time.time >= _staggerEndTime;

    private void Awake()
    {
        var receiver = gameObject.GetComponent<NpcDamageReceiver>() ?? gameObject.AddComponent<NpcDamageReceiver>();
        receiver.Initialize(this);
    }

    public void Tick(float deltaTime)
    {
        if (IsDead)
        {
            return;
        }

        if (_bleedEndTime > Time.time)
        {
            ApplyDamage(BleedDamagePerSecond * deltaTime, "bleed", applyBleed: false, canStagger: false);
        }

        if (Health <= 0f && State != NpcCombatState.Dead)
        {
            Die();
            return;
        }

        if (Health <= DownedThreshold && State != NpcCombatState.Downed && State != NpcCombatState.Dead)
        {
            State = NpcCombatState.Downed;
        }

        if (State == NpcCombatState.Reloading && Time.time >= _reloadEndTime)
        {
            CurrentMagAmmo = MagCapacity;
            ReserveMags = Math.Max(0, ReserveMags - 1);
            State = NpcCombatState.Alerted;
        }

        if (State == NpcCombatState.Searching && Time.time >= _searchEndTime)
        {
            State = NpcCombatState.Idle;
        }
    }

    public void UpdateState(bool isThreatened, bool hasGun)
    {
        if (IsDead)
        {
            return;
        }

        if (State == NpcCombatState.Downed)
        {
            ShouldSurrender = isThreatened;
            return;
        }

        if (State == NpcCombatState.Reloading)
        {
            return;
        }

        if (Health <= FleeThreshold && isThreatened)
        {
            State = NpcCombatState.Fleeing;
            ShouldSurrender = true;
            return;
        }

        if (isThreatened)
        {
            if (!hasGun || ArmDisabled || Health <= SurrenderThreshold)
            {
                State = NpcCombatState.Alerted;
                ShouldSurrender = true;
            }
            else
            {
                State = NpcCombatState.Combat;
                ShouldSurrender = false;
            }
        }
        else
        {
            ShouldSurrender = false;
            if (State == NpcCombatState.Combat)
            {
                State = NpcCombatState.Searching;
                _searchEndTime = Time.time + SearchDuration;
            }
            else if (State == NpcCombatState.Alerted)
            {
                State = NpcCombatState.Idle;
            }
        }
    }

    public bool TryConsumeAmmoForShot()
    {
        if (!CanShoot)
        {
            return false;
        }

        if (CurrentMagAmmo <= 0)
        {
            HandleEmptyMag();
            return false;
        }

        CurrentMagAmmo = Math.Max(0, CurrentMagAmmo - 1);
        if (CurrentMagAmmo == 0)
        {
            HandleEmptyMag();
        }

        return true;
    }

    public void DropGun()
    {
        if (Gun == null)
        {
            return;
        }

        Gun.Drop();
        Gun = null;
    }

    public void ApplyDamage(float amount, string bodyPart, bool applyBleed = true, bool canStagger = true)
    {
        if (IsDead)
        {
            return;
        }

        var multiplier = ResolveDamageMultiplier(bodyPart);
        var finalDamage = Mathf.Max(0f, amount * multiplier);
        Health = Mathf.Max(0f, Health - finalDamage);

        if (applyBleed && finalDamage >= 8f)
        {
            _bleedEndTime = Mathf.Max(_bleedEndTime, Time.time + 8f);
        }

        if (canStagger && finalDamage >= 15f)
        {
            _staggerEndTime = Mathf.Max(_staggerEndTime, Time.time + 0.6f);
        }

        if (IsArmHit(bodyPart))
        {
            ArmDisabled = true;
            DropGun();
        }

        if (Health <= 0f)
        {
            Die();
        }
    }

    private void HandleEmptyMag()
    {
        if (ReserveMags > 0)
        {
            State = NpcCombatState.Reloading;
            _reloadEndTime = Time.time + ReloadDuration;
        }
        else
        {
            DropGun();
            State = NpcCombatState.Fleeing;
            ShouldSurrender = true;
        }
    }

    private void Die()
    {
        State = NpcCombatState.Dead;
        DropGun();
        NpcRagdollHelper.EnableRagdoll(gameObject);
    }

    private static float ResolveDamageMultiplier(string bodyPart)
    {
        var name = bodyPart.ToLowerInvariant();
        if (name.Contains("head"))
        {
            return 2.0f;
        }

        if (name.Contains("arm") || name.Contains("hand"))
        {
            return 0.85f;
        }

        if (name.Contains("leg") || name.Contains("foot"))
        {
            return 0.75f;
        }

        return 1f;
    }

    private static bool IsArmHit(string bodyPart)
    {
        var name = bodyPart.ToLowerInvariant();
        return name.Contains("arm") || name.Contains("hand");
    }
}
