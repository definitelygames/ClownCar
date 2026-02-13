using UnityEngine;

namespace EVP
{
    /// <summary>
    /// Main controller for multiplayer steering.
    /// Combines input from all enabled players and applies to vehicle.
    /// </summary>
    public class MultiplayerSteeringManager : MonoBehaviour
    {
        [Header("Vehicle Reference")]
        [Tooltip("The vehicle to control. If null, will try to find on this GameObject.")]
        public VehicleController vehicle;

        [Header("Input Override")]
        [Tooltip("Reference to VehicleNewInput to disable its steering when this is active.")]
        public VehicleNewInput vehicleInput;

        [Header("Combine Mode")]
        [Tooltip("How to combine multiple player inputs")]
        public CombineMode combineMode = CombineMode.Average;

        public enum CombineMode
        {
            Average,  // Sum of all steering / number of enabled players (balanced, predictable)
            Sum       // All inputs add together, clamped to -1,1 (chaotic, encourages cooperation)
        }

        [Header("Player Toggle Keys")]
        [Tooltip("Keys to toggle players on/off (1-4)")]
        public KeyCode player1ToggleKey = KeyCode.Alpha1;
        public KeyCode player2ToggleKey = KeyCode.Alpha2;
        public KeyCode player3ToggleKey = KeyCode.Alpha3;
        public KeyCode player4ToggleKey = KeyCode.Alpha4;

        [Header("Players")]
        public MultiplayerSteeringPlayer[] players = new MultiplayerSteeringPlayer[4];

        // Public property to get combined steering value
        public float CombinedSteer { get; private set; }

        void Awake()
        {
            // Initialize players with default key bindings if not set up
            if (players == null || players.Length == 0)
            {
                InitializeDefaultPlayers();
            }
        }

        void OnEnable()
        {
            // Find vehicle controller if not assigned
            if (vehicle == null)
                vehicle = GetComponent<VehicleController>();

            // Find vehicle input if not assigned
            if (vehicleInput == null)
                vehicleInput = GetComponent<VehicleNewInput>();

            // Enable steering override on VehicleNewInput
            if (vehicleInput != null)
                vehicleInput.externalSteeringOverride = true;
        }

        void OnDisable()
        {
            // Disable steering override when this component is disabled
            if (vehicleInput != null)
                vehicleInput.externalSteeringOverride = false;

            // Reset vehicle steering
            if (vehicle != null)
                vehicle.steerInput = 0f;
        }

        void InitializeDefaultPlayers()
        {
            players = new MultiplayerSteeringPlayer[4];

            // Player 1: A/D (enabled by default)
            players[0] = new MultiplayerSteeringPlayer
            {
                playerIndex = 0,
                leftKey = KeyCode.A,
                rightKey = KeyCode.D,
                isEnabled = true
            };

            // Player 2: Arrow keys (disabled by default)
            players[1] = new MultiplayerSteeringPlayer
            {
                playerIndex = 1,
                leftKey = KeyCode.LeftArrow,
                rightKey = KeyCode.RightArrow,
                isEnabled = false
            };

            // Player 3: J/K (disabled by default)
            players[2] = new MultiplayerSteeringPlayer
            {
                playerIndex = 2,
                leftKey = KeyCode.J,
                rightKey = KeyCode.K,
                isEnabled = false
            };

            // Player 4: O/P (disabled by default)
            players[3] = new MultiplayerSteeringPlayer
            {
                playerIndex = 3,
                leftKey = KeyCode.O,
                rightKey = KeyCode.P,
                isEnabled = false
            };
        }

        void Update()
        {
            // Handle player toggle keys
            HandlePlayerToggles();

            // Read input from all players
            foreach (var player in players)
            {
                if (player != null)
                    player.ReadInput();
            }
        }

        void FixedUpdate()
        {
            if (vehicle == null) return;

            // Update analog ramping for all players
            foreach (var player in players)
            {
                if (player != null)
                    player.UpdateRamping(Time.fixedDeltaTime);
            }

            // Combine all player inputs
            CombinedSteer = CombinePlayerInputs();

            // Apply to vehicle
            vehicle.steerInput = CombinedSteer;
        }

        void HandlePlayerToggles()
        {
            if (Input.GetKeyDown(player1ToggleKey) && players.Length > 0 && players[0] != null)
                players[0].Toggle();

            if (Input.GetKeyDown(player2ToggleKey) && players.Length > 1 && players[1] != null)
                players[1].Toggle();

            if (Input.GetKeyDown(player3ToggleKey) && players.Length > 2 && players[2] != null)
                players[2].Toggle();

            if (Input.GetKeyDown(player4ToggleKey) && players.Length > 3 && players[3] != null)
                players[3].Toggle();
        }

        float CombinePlayerInputs()
        {
            float sum = 0f;
            int enabledCount = 0;

            foreach (var player in players)
            {
                if (player != null && player.isEnabled)
                {
                    sum += player.currentSteer;
                    enabledCount++;
                }
            }

            if (enabledCount == 0)
                return 0f;

            switch (combineMode)
            {
                case CombineMode.Average:
                    return Mathf.Clamp(sum / enabledCount, -1f, 1f);

                case CombineMode.Sum:
                    return Mathf.Clamp(sum, -1f, 1f);

                default:
                    return Mathf.Clamp(sum / enabledCount, -1f, 1f);
            }
        }

        /// <summary>
        /// Get the number of currently enabled players.
        /// </summary>
        public int GetEnabledPlayerCount()
        {
            int count = 0;
            foreach (var player in players)
            {
                if (player != null && player.isEnabled)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Enable or disable a specific player by index (0-3).
        /// </summary>
        public void SetPlayerEnabled(int index, bool enabled)
        {
            if (index >= 0 && index < players.Length && players[index] != null)
            {
                players[index].isEnabled = enabled;
            }
        }
    }
}
