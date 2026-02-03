using UnityEngine;

namespace BonelabNpcMod;

public static class NpcRagdollHelper
{
    public static void EnableRagdoll(GameObject npc)
    {
        var animator = npc.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }

        foreach (var body in npc.GetComponentsInChildren<Rigidbody>())
        {
            body.isKinematic = false;
            body.detectCollisions = true;
        }

        foreach (var collider in npc.GetComponentsInChildren<Collider>())
        {
            collider.enabled = true;
        }
    }
}
