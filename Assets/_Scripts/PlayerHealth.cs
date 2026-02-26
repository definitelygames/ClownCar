using UnityEngine;
using EVP;

/// <summary>
/// Per-player health tracking and ejection. Attach to a seat pivot alongside SeatedAvatar.
/// When health reaches 0, the avatar is ejected as a ragdoll and steering is disabled.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Player")]
    [Tooltip("Which player slot (0-3). Must match the SeatedAvatarManager/steering index.")]
    public int playerIndex;

    [Header("Ejection Forces")]
    public float ejectionUpForce = 8f;
    public float ejectionImpactForce = 5f;
    public float ejectionRagdollForce = 5f;

    [Header("Camera")]
    [Tooltip("Switch camera to follow the ejected avatar.")]
    public bool followEjectedAvatar;
    [Tooltip("Auto-detected from scene if left empty.")]
    public VehicleCameraController cameraController;

    float currentHealth;
    Vector3 lastImpactDirection;
    SeatedAvatar seatedAvatar;
    Rigidbody seatRigidbody;
    ConfigurableJoint seatJoint;

    public bool IsDead => currentHealth <= 0f;
    public bool IsEjected { get; private set; }
    public float HealthNormalized => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

    public System.Action<PlayerHealth> OnDeath;

    void Awake()
    {
        currentHealth = maxHealth;
        seatedAvatar = GetComponent<SeatedAvatar>();
        seatRigidbody = GetComponent<Rigidbody>();
        seatJoint = GetComponent<ConfigurableJoint>();
        if (cameraController == null)
            cameraController = FindObjectOfType<VehicleCameraController>();
    }

    public void TakeDamage(float amount, Vector3 impactDirection)
    {
        if (IsDead || IsEjected) return;

        lastImpactDirection = impactDirection;
        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Eject();
        }
    }

    void Eject()
    {
        if (IsEjected) return;

        IsEjected = true;
        seatedAvatar.IsEjected = true;

        // Release seated pose (un-kinematic limbs)
        seatedAvatar.SetSeated(false);

        // Stop hand override
        seatedAvatar.ClearHandTracking();

        // Enable full ragdoll (disables animator, sets bones non-kinematic)
        var ragdoll = seatedAvatar.Ragdoll;
        if (ragdoll != null)
            ragdoll.EnableRagdoll();

        // Break tether to vehicle
        if (seatJoint != null)
        {
            Destroy(seatJoint);
            seatJoint = null;
        }

        // Apply ejection force: upward + away from impact
        if (seatRigidbody != null)
        {
            Vector3 impactHorizontal = lastImpactDirection;
            impactHorizontal.y = 0f;
            if (impactHorizontal.sqrMagnitude > 0.001f)
                impactHorizontal.Normalize();

            Vector3 ejectionVelocity = Vector3.up * ejectionUpForce + impactHorizontal * ejectionImpactForce;
            seatRigidbody.AddForce(ejectionVelocity, ForceMode.VelocityChange);

            // Push ragdoll bones
            if (ragdoll != null)
            {
                Vector3 ragdollPush = Vector3.up * ejectionRagdollForce + impactHorizontal * ejectionImpactForce * 0.5f;
                ragdoll.ApplyForceToAll(ragdollPush, ForceMode.VelocityChange);
            }
        }

        // Play ejection sound effects before detaching
        PlayEjectSounds();

        // Detach avatar from seat pivot and enable continuous collision on ragdoll bones
        seatedAvatar.DetachAvatar();

        // Re-enable collisions between avatar and vehicle
        ReenableAvatarCollisions();

        // Disable this player's steering contribution
        var vehicleRoot = seatedAvatar.vehicleRoot;
        if (vehicleRoot != null)
        {
            var steering = vehicleRoot.GetComponent<VehicleMultiplayerSteering>();
            if (steering != null)
                steering.SetPlayerEnabled(playerIndex, false);
        }

        // Switch camera to follow ejected avatar
        if (followEjectedAvatar && cameraController != null)
        {
            var avatarTransform = seatedAvatar.AvatarTransform;
            if (avatarTransform != null)
                cameraController.target = avatarTransform;
        }

        OnDeath?.Invoke(this);
    }

    void ReenableAvatarCollisions()
    {
        if (seatedAvatar == null || seatedAvatar.vehicleRoot == null) return;

        var vehicleColliders = seatedAvatar.vehicleRoot.GetComponentsInChildren<Collider>();
        var avatarColliders = seatedAvatar.GetComponentsInChildren<Collider>(true);

        foreach (var ac in avatarColliders)
            foreach (var vc in vehicleColliders)
                if (ac != null && vc != null && ac != vc)
                    Physics.IgnoreCollision(ac, vc, false);
    }

    void PlayEjectSounds()
    {
        if (seatedAvatar == null || !seatedAvatar.IsSpawned) return;

        var sources = seatedAvatar.GetComponentsInChildren<AudioSource>(true);
        foreach (var source in sources)
        {
            if (source.gameObject.CompareTag("EjectSound"))
                source.PlayOneShot(source.clip);
        }
    }
}
