using UnityEngine;

namespace BonelabNpcMod;

public sealed class NpcDamageReceiver : MonoBehaviour
{
    private NpcCombatant? _combatant;

    public void Initialize(NpcCombatant combatant)
    {
        _combatant = combatant;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_combatant == null || _combatant.IsDead)
        {
            return;
        }

        if (!IsProjectile(collision.collider))
        {
            return;
        }

        var impactSpeed = collision.relativeVelocity.magnitude;
        var damage = Mathf.Clamp(impactSpeed * 2f, 5f, 45f);
        _combatant.ApplyDamage(damage, collision.collider.name);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_combatant == null || _combatant.IsDead)
        {
            return;
        }

        if (!IsProjectile(other))
        {
            return;
        }

        var speed = other.attachedRigidbody != null ? other.attachedRigidbody.velocity.magnitude : 10f;
        var damage = Mathf.Clamp(speed * 1.5f, 5f, 40f);
        _combatant.ApplyDamage(damage, other.name);
    }

    private static bool IsProjectile(Collider collider)
    {
        if (collider.CompareTag("Bullet"))
        {
            return true;
        }

        var name = collider.name.ToLowerInvariant();
        return name.Contains("bullet") || name.Contains("projectile") || name.Contains("shot");
    }
}
