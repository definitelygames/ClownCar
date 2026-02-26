using UnityEngine;
using EVP;

/// <summary>
/// Subscribes to VehicleController.onImpact and distributes damage to vehicle health
/// and per-player health based on proximity to impact point.
/// Attach to the vehicle root alongside VehicleController.
/// </summary>
public class VehicleDamageReceiver : MonoBehaviour
{
    [Header("Vehicle")]
    [Tooltip("Auto-detected on this GameObject if left empty.")]
    public VehicleController vehicle;

    [Header("Players")]
    [Tooltip("One PlayerHealth per player slot (index 0 = Player 1, etc). Assign in Inspector.")]
    public PlayerHealth[] playerHealths;

    [Header("Damage Settings")]
    public float vehicleDamageMultiplier = 1f;
    public float playerDamageMultiplier = 1f;
    public float minImpactVelocity = 3f;
    public float maxDamagePerImpact = 60f;
    [Tooltip("Higher = more damage falloff with distance from impact point.")]
    public float distanceDamping = 2f;

    [Header("Vehicle Health")]
    public float vehicleMaxHealth = 200f;
    float vehicleHealth;

    public float VehicleHealthNormalized => vehicleMaxHealth > 0f ? Mathf.Clamp01(vehicleHealth / vehicleMaxHealth) : 0f;

    void Awake()
    {
        if (vehicle == null)
            vehicle = GetComponent<VehicleController>();
        vehicleHealth = vehicleMaxHealth;
    }

    void OnEnable()
    {
        if (vehicle == null) return;
        vehicle.processContacts = true;
        vehicle.onImpact += ProcessImpact;
    }

    void OnDisable()
    {
        if (vehicle == null) return;
        vehicle.onImpact -= ProcessImpact;
    }

    void ProcessImpact()
    {
        float impactMagnitude = vehicle.localImpactVelocity.magnitude;
        if (impactMagnitude < minImpactVelocity) return;

        // Reduce vehicle health
        float vehicleDamage = impactMagnitude * vehicleDamageMultiplier;
        vehicleHealth -= vehicleDamage;
        if (vehicleHealth < 0f) vehicleHealth = 0f;

        // Convert local impact to world space
        Vector3 worldImpactPos = vehicle.transform.TransformPoint(vehicle.localImpactPosition);
        Vector3 worldImpactVelocity = vehicle.transform.TransformDirection(vehicle.localImpactVelocity);

        // Distribute damage to players based on proximity
        if (playerHealths == null) return;

        float basePlayerDamage = impactMagnitude * playerDamageMultiplier;
        for (int i = 0; i < playerHealths.Length; i++)
        {
            var ph = playerHealths[i];
            if (ph == null || ph.IsDead || ph.IsEjected) continue;

            float distance = Vector3.Distance(worldImpactPos, ph.transform.position);
            float attenuation = 1f / (1f + distance * distanceDamping);
            float playerDamage = Mathf.Min(basePlayerDamage * attenuation, maxDamagePerImpact);

            // Direction from impact point toward the player (away from impact)
            Vector3 impactDir = (ph.transform.position - worldImpactPos).normalized;
            ph.TakeDamage(playerDamage, impactDir);
        }
    }
}
