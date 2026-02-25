using UnityEngine;
using EVP;

/// <summary>
/// Manages per-player seated avatars. Lives on the vehicle root alongside VehicleMultiplayerSteering.
/// Subscribes to OnPlayerToggled and spawns/despawns avatars in the corresponding seat slots.
/// Wire the seats array in the Inspector (one SeatedAvatar per player slot).
/// </summary>
public class SeatedAvatarManager : MonoBehaviour
{
    [Tooltip("Auto-detected on this GameObject if left empty.")]
    public VehicleMultiplayerSteering steering;

    [Tooltip("One SeatedAvatar per player slot (index 0 = Player 1, etc). Assign in Inspector.")]
    public SeatedAvatar[] seats;

    void Awake()
    {
        if (steering == null)
            steering = GetComponent<VehicleMultiplayerSteering>();
    }

    void OnEnable()
    {
        if (steering == null) return;

        steering.OnPlayerToggled += HandlePlayerToggled;

        // Sync initial state — spawn avatars for players already enabled
        for (int i = 0; i < steering.PlayerEnabled.Length; i++)
        {
            if (i < seats.Length && seats[i] != null && steering.PlayerEnabled[i])
                seats[i].SpawnAvatar();
        }
    }

    void OnDisable()
    {
        if (steering != null)
            steering.OnPlayerToggled -= HandlePlayerToggled;

        // Despawn all
        if (seats != null)
        {
            foreach (var seat in seats)
            {
                if (seat != null)
                    seat.DespawnAvatar();
            }
        }
    }

    void HandlePlayerToggled(int index, bool enabled)
    {
        if (seats == null || index < 0 || index >= seats.Length) return;
        if (seats[index] == null) return;

        if (enabled)
            seats[index].SpawnAvatar();
        else
            seats[index].DespawnAvatar();
    }
}
