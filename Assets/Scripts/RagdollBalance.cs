using UnityEngine;

/// <summary>
/// Attach to the pelvis/hips of a ragdoll to make it try to stay upright.
/// Creates an "active ragdoll" effect - wobbly but fighting to stand.
/// </summary>
public class RagdollBalance : MonoBehaviour
{
    [Header("Balance Settings")]
    [Tooltip("How hard the ragdoll tries to stay upright")]
    public float uprightTorque = 100f;

    [Tooltip("Damping to reduce oscillation")]
    public float damping = 10f;

    [Tooltip("Which direction is 'up' for this body part")]
    public Vector3 localUpDirection = Vector3.up;

    [Header("Optional")]
    [Tooltip("Apply balance force to all rigidbodies in hierarchy")]
    public bool balanceEntireBody = false;

    [Tooltip("Reduce force on limbs (multiplier)")]
    [Range(0f, 1f)]
    public float limbForceMultiplier = 0.3f;

    Rigidbody rb;
    Rigidbody[] allBodies;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (balanceEntireBody)
        {
            allBodies = GetComponentsInChildren<Rigidbody>();
        }
    }

    void FixedUpdate()
    {
        if (balanceEntireBody && allBodies != null)
        {
            foreach (Rigidbody body in allBodies)
            {
                // Main body (pelvis) gets full force, limbs get reduced
                float multiplier = (body == rb) ? 1f : limbForceMultiplier;
                ApplyBalanceForce(body, multiplier);
            }
        }
        else if (rb != null)
        {
            ApplyBalanceForce(rb, 1f);
        }
    }

    void ApplyBalanceForce(Rigidbody body, float multiplier)
    {
        // Calculate the rotation needed to align local up with world up
        Vector3 currentUp = body.transform.TransformDirection(localUpDirection);
        Vector3 targetUp = Vector3.up;

        // Cross product gives us the axis to rotate around
        Vector3 rotationAxis = Vector3.Cross(currentUp, targetUp);

        // Angle between current and target (used for proportional force)
        float angle = Vector3.Angle(currentUp, targetUp);

        // Apply torque proportional to how far off we are
        Vector3 torque = rotationAxis * (angle * uprightTorque * multiplier * Mathf.Deg2Rad);
        body.AddTorque(torque, ForceMode.Force);

        // Apply damping to reduce wobble
        body.AddTorque(-body.angularVelocity * damping * multiplier, ForceMode.Force);
    }
}
