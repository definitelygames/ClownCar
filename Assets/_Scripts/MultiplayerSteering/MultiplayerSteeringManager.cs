using System.Collections.Generic;
using UnityEngine;

namespace EVP
{
    /// <summary>
    /// Main controller for multiplayer vehicle control.
    /// Randomly distributes 4 discrete actions (steer left, steer right, accelerate, brake)
    /// among active players. Each player only controls a subset of actions.
    /// </summary>
    public class MultiplayerSteeringManager : MonoBehaviour
    {
        [Header("Vehicle Reference")]
        [Tooltip("The vehicle to control. If null, will try to find on this GameObject.")]
        public VehicleController vehicle;

        [Header("Input Override")]
        [Tooltip("Reference to VehicleNewInput to disable its inputs when this is active.")]
        public VehicleNewInput vehicleInput;

        [Header("Player Toggle Keys")]
        [Tooltip("Keys to toggle players on/off (1-4)")]
        public KeyCode player1ToggleKey = KeyCode.Alpha1;
        public KeyCode player2ToggleKey = KeyCode.Alpha2;
        public KeyCode player3ToggleKey = KeyCode.Alpha3;
        public KeyCode player4ToggleKey = KeyCode.Alpha4;

        [Header("Players")]
        public MultiplayerSteeringPlayer[] players = new MultiplayerSteeringPlayer[4];

        // Public properties for combined values
        public float CombinedSteer { get; private set; }
        public float CombinedThrottle { get; private set; }
        public float CombinedBrake { get; private set; }

        void Awake()
        {
            if (players == null || players.Length == 0)
            {
                InitializeDefaultPlayers();
            }
        }

        void OnEnable()
        {
            if (vehicle == null)
                vehicle = GetComponent<VehicleController>();

            if (vehicleInput == null)
                vehicleInput = GetComponent<VehicleNewInput>();

            if (vehicleInput != null)
            {
                vehicleInput.externalSteeringOverride = true;
                vehicleInput.externalThrottleOverride = true;
                vehicleInput.externalBrakeOverride = true;
            }

            DistributeControls();
        }

        void OnDisable()
        {
            if (vehicleInput != null)
            {
                vehicleInput.externalSteeringOverride = false;
                vehicleInput.externalThrottleOverride = false;
                vehicleInput.externalBrakeOverride = false;
            }

            if (vehicle != null)
            {
                vehicle.steerInput = 0f;
                vehicle.throttleInput = 0f;
                vehicle.brakeInput = 0f;
            }
        }

        void InitializeDefaultPlayers()
        {
            players = new MultiplayerSteeringPlayer[4];

            // Player 1: A, D, W, S (enabled by default)
            players[0] = new MultiplayerSteeringPlayer
            {
                playerIndex = 0,
                availableKeys = new[] { KeyCode.A, KeyCode.D, KeyCode.W, KeyCode.S },
                isEnabled = true
            };

            // Player 2: Arrow keys (disabled by default)
            players[1] = new MultiplayerSteeringPlayer
            {
                playerIndex = 1,
                availableKeys = new[] { KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow },
                isEnabled = false
            };

            // Player 3: J, L, I, K (disabled by default)
            players[2] = new MultiplayerSteeringPlayer
            {
                playerIndex = 2,
                availableKeys = new[] { KeyCode.J, KeyCode.L, KeyCode.I, KeyCode.K },
                isEnabled = false
            };

            // Player 4: Numpad 4, 6, 8, 5 (disabled by default)
            players[3] = new MultiplayerSteeringPlayer
            {
                playerIndex = 3,
                availableKeys = new[] { KeyCode.Keypad4, KeyCode.Keypad6, KeyCode.Keypad8, KeyCode.Keypad5 },
                isEnabled = false
            };
        }

        /// <summary>
        /// Randomly distribute the 4 vehicle actions among enabled players.
        /// Uses Fisher-Yates shuffle then round-robin dealing.
        /// </summary>
        public void DistributeControls()
        {
            // Clear all player assignments
            foreach (var player in players)
            {
                if (player != null)
                    player.assignedControls.Clear();
            }

            // Collect enabled players
            var enabledPlayers = new List<MultiplayerSteeringPlayer>();
            foreach (var player in players)
            {
                if (player != null && player.isEnabled)
                    enabledPlayers.Add(player);
            }

            if (enabledPlayers.Count == 0) return;

            // Create and shuffle the 4 actions (Fisher-Yates)
            var actions = new VehicleControlAction[]
            {
                VehicleControlAction.SteerLeft,
                VehicleControlAction.SteerRight,
                VehicleControlAction.Accelerate,
                VehicleControlAction.Brake
            };

            for (int i = actions.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = actions[i];
                actions[i] = actions[j];
                actions[j] = temp;
            }

            // Track how many keys each player has used
            var keyIndices = new Dictionary<MultiplayerSteeringPlayer, int>();
            foreach (var player in enabledPlayers)
                keyIndices[player] = 0;

            // Deal round-robin to enabled players
            for (int i = 0; i < actions.Length; i++)
            {
                var player = enabledPlayers[i % enabledPlayers.Count];
                int keyIdx = keyIndices[player];

                var binding = new ControlBinding
                {
                    action = actions[i],
                    key = player.availableKeys[keyIdx],
                    currentValue = 0f,
                    targetValue = 0f
                };

                player.assignedControls.Add(binding);
                keyIndices[player] = keyIdx + 1;
            }
        }

        void Update()
        {
            HandlePlayerToggles();

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

            // Gather control values from all players
            float steerLeft = 0f;
            float steerRight = 0f;
            float accelerate = 0f;
            float brake = 0f;

            foreach (var player in players)
            {
                if (player == null) continue;
                steerLeft = Mathf.Max(steerLeft, player.GetControlValue(VehicleControlAction.SteerLeft));
                steerRight = Mathf.Max(steerRight, player.GetControlValue(VehicleControlAction.SteerRight));
                accelerate = Mathf.Max(accelerate, player.GetControlValue(VehicleControlAction.Accelerate));
                brake = Mathf.Max(brake, player.GetControlValue(VehicleControlAction.Brake));
            }

            // Translate forward/reverse like VehicleStandardInput's continuousForwardAndReverse
            float throttleInput = 0f;
            float brakeInput = 0f;
            float minSpeed = 0.1f;
            float minInput = 0.1f;

            if (vehicle.speed > minSpeed)
            {
                // Moving forward: accelerate = gas, brake = brake
                throttleInput = accelerate;
                brakeInput = brake;
            }
            else
            {
                if (brake > minInput)
                {
                    // Stopped/slow + brake held = reverse
                    throttleInput = -brake;
                    brakeInput = 0f;
                }
                else if (accelerate > minInput)
                {
                    if (vehicle.speed < -minSpeed)
                    {
                        // Moving backward + accelerate = brake to stop
                        throttleInput = 0f;
                        brakeInput = accelerate;
                    }
                    else
                    {
                        // Stopped + accelerate = go forward
                        throttleInput = accelerate;
                        brakeInput = 0f;
                    }
                }
            }

            CombinedSteer = Mathf.Clamp(-steerLeft + steerRight, -1f, 1f);
            CombinedThrottle = throttleInput;
            CombinedBrake = brakeInput;

            vehicle.steerInput = CombinedSteer;
            vehicle.throttleInput = CombinedThrottle;
            vehicle.brakeInput = CombinedBrake;
        }

        void HandlePlayerToggles()
        {
            bool changed = false;

            if (Input.GetKeyDown(player1ToggleKey) && players.Length > 0 && players[0] != null)
            {
                players[0].Toggle();
                changed = true;
            }

            if (Input.GetKeyDown(player2ToggleKey) && players.Length > 1 && players[1] != null)
            {
                players[1].Toggle();
                changed = true;
            }

            if (Input.GetKeyDown(player3ToggleKey) && players.Length > 2 && players[2] != null)
            {
                players[2].Toggle();
                changed = true;
            }

            if (Input.GetKeyDown(player4ToggleKey) && players.Length > 3 && players[3] != null)
            {
                players[3].Toggle();
                changed = true;
            }

            if (changed)
                DistributeControls();
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
        /// Redistributes controls after change.
        /// </summary>
        public void SetPlayerEnabled(int index, bool enabled)
        {
            if (index >= 0 && index < players.Length && players[index] != null)
            {
                players[index].isEnabled = enabled;
                DistributeControls();
            }
        }
    }
}
