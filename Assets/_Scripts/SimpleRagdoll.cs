using UnityEngine;

/// <summary>
/// Lightweight ragdoll controller using standard Unity ragdoll components.
/// Caches all child Rigidbodies/Colliders on Awake and starts fully kinematic.
/// </summary>
public class SimpleRagdoll : MonoBehaviour
{
    Rigidbody[] boneRigidbodies;
    Collider[] boneColliders;
    Animator animator;

    void Awake()
    {
        boneRigidbodies = GetComponentsInChildren<Rigidbody>();
        boneColliders = GetComponentsInChildren<Collider>();
        animator = GetComponentInChildren<Animator>();
        SetAllKinematic(true);
    }

    /// <summary>
    /// Disable the Animator and set all bones non-kinematic with gravity — full ragdoll.
    /// </summary>
    public void EnableRagdoll()
    {
        if (animator != null)
            animator.enabled = false;

        foreach (var rb in boneRigidbodies)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    /// <summary>
    /// Toggle kinematic state on all bone Rigidbodies.
    /// </summary>
    public void SetAllKinematic(bool kinematic)
    {
        foreach (var rb in boneRigidbodies)
        {
            rb.isKinematic = kinematic;
            if (kinematic)
                rb.useGravity = false;
        }
    }

    /// <summary>
    /// Set ContinuousDynamic collision detection on all bone Rigidbodies.
    /// </summary>
    public void SetContinuousCollision()
    {
        foreach (var rb in boneRigidbodies)
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    /// <summary>
    /// Ignore (or re-enable) collisions between all bone colliders and the given colliders.
    /// </summary>
    public void IgnoreCollisionWith(Collider[] otherColliders, bool ignore = true)
    {
        foreach (var bc in boneColliders)
            foreach (var oc in otherColliders)
                if (bc != null && oc != null && bc != oc)
                    Physics.IgnoreCollision(bc, oc, ignore);
    }

    /// <summary>
    /// Apply a force to all non-kinematic bone Rigidbodies.
    /// </summary>
    public void ApplyForceToAll(Vector3 force, ForceMode mode)
    {
        foreach (var rb in boneRigidbodies)
            if (!rb.isKinematic)
                rb.AddForce(force, mode);
    }
}
