using UnityEngine;
using UnityEngine.InputSystem;

namespace EVP
{
    /// <summary>
    /// Unified vehicle steering manager. Holds an array of SteeringMethodConfig assets
    /// and delegates to the active method. Handles universal controls (handbrake, reset),
    /// player toggles (1-4 keys), and UI toggle (U key).
    /// </summary>
    public class VehicleMultiplayerSteering : MonoBehaviour
    {
        [Header("Vehicle")]
        public VehicleController vehicle;

        [Header("Steering Modes")]
        [Tooltip("Available steering mode configurations. Drag ScriptableObject assets here.")]
        public SteeringMethodConfig[] modes;

        [Tooltip("Index of the initially active mode.")]
        public int activeModeIndex = 0;

        [Header("Universal Input")]
        [Tooltip("InputActionAsset for handbrake and reset (shared across all modes).")]
        public InputActionAsset universalInputActions;

        [Header("Player Toggles")]
        public KeyCode player1ToggleKey = KeyCode.Alpha1;
        public KeyCode player2ToggleKey = KeyCode.Alpha2;
        public KeyCode player3ToggleKey = KeyCode.Alpha3;
        public KeyCode player4ToggleKey = KeyCode.Alpha4;

        [Header("UI Toggle")]
        public KeyCode uiToggleKey = KeyCode.U;

        [Header("Mode Switching")]
        [Tooltip("Keys to switch between steering modes (M for next, or direct keys).")]
        public KeyCode nextModeKey = KeyCode.M;

        // Player state (shared across all methods)
        public bool[] PlayerEnabled { get; private set; } = { true, false, false, false };

        // Universal input state
        public float HandbrakeInput { get; private set; }
        public bool ShowUI { get; private set; } = true;

        // Runtime
        private SteeringMethod[] methods;
        private SteeringMethod activeMethod;
        private int currentModeIndex = -1;

        // Universal input actions
        private InputActionMap vehicleActionMap;
        private InputAction handbrakeAction;
        private InputAction resetAction;
        private bool doReset;

        void Awake()
        {
            if (vehicle == null)
                vehicle = GetComponent<VehicleController>();

            // Create method instances from configs
            if (modes != null && modes.Length > 0)
            {
                methods = new SteeringMethod[modes.Length];
                for (int i = 0; i < modes.Length; i++)
                {
                    if (modes[i] != null)
                    {
                        methods[i] = modes[i].CreateMethod();
                        methods[i].Initialize(this, vehicle);
                    }
                }
            }
        }

        void OnEnable()
        {
            SetupUniversalInput();
            vehicleActionMap?.Enable();

            // Activate the initial mode
            if (methods != null && methods.Length > 0)
            {
                int idx = Mathf.Clamp(activeModeIndex, 0, methods.Length - 1);
                SetMode(idx);
            }
        }

        void OnDisable()
        {
            // Deactivate current method
            if (activeMethod != null)
            {
                activeMethod.Deactivate();
                activeMethod = null;
                currentModeIndex = -1;
            }

            if (resetAction != null)
                resetAction.performed -= OnResetPerformed;
            vehicleActionMap?.Disable();

            // Zero out vehicle
            if (vehicle != null)
            {
                vehicle.steerInput = 0f;
                vehicle.throttleInput = 0f;
                vehicle.brakeInput = 0f;
                vehicle.handbrakeInput = 0f;
            }
        }

        private void SetupUniversalInput()
        {
            if (universalInputActions == null) return;

            vehicleActionMap = universalInputActions.FindActionMap("Vehicle");
            if (vehicleActionMap == null) return;

            handbrakeAction = vehicleActionMap.FindAction("Handbrake");
            resetAction = vehicleActionMap.FindAction("ResetVehicle");

            if (resetAction != null)
                resetAction.performed += OnResetPerformed;
        }

        private void OnResetPerformed(InputAction.CallbackContext context)
        {
            doReset = true;
        }

        void Update()
        {
            // Universal: UI toggle
            if (Input.GetKeyDown(uiToggleKey))
                ShowUI = !ShowUI;

            // Universal: player toggles
            HandlePlayerToggles();

            // Universal: mode switching
            if (Input.GetKeyDown(nextModeKey) && methods != null && methods.Length > 1)
            {
                int next = (currentModeIndex + 1) % methods.Length;
                SetMode(next);
            }

            // Universal: handbrake
            HandbrakeInput = Mathf.Clamp01(handbrakeAction?.ReadValue<float>() ?? 0f);

            // Delegate to active method
            if (activeMethod != null)
                activeMethod.ReadInput(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (vehicle == null) return;

            if (activeMethod != null)
            {
                VehicleInput input = activeMethod.GetVehicleInput(Time.fixedDeltaTime);

                vehicle.steerInput = input.steer;
                vehicle.throttleInput = input.throttle;
                vehicle.brakeInput = input.brake;

                // Apply physics (torques, etc.)
                Rigidbody rb = vehicle.cachedRigidbody;
                if (rb != null)
                    activeMethod.ApplyPhysics(rb, Time.fixedDeltaTime);
            }

            // Universal: handbrake
            vehicle.handbrakeInput = HandbrakeInput;

            // Universal: reset
            if (doReset)
            {
                vehicle.ResetVehicle();
                doReset = false;
            }
        }

        void OnGUI()
        {
            if (!ShowUI) return;
            activeMethod?.DrawGUI();
        }

        // --- Player Toggles ---

        private void HandlePlayerToggles()
        {
            bool changed = false;

            if (Input.GetKeyDown(player1ToggleKey))
            {
                PlayerEnabled[0] = !PlayerEnabled[0];
                changed = true;
            }
            if (Input.GetKeyDown(player2ToggleKey))
            {
                PlayerEnabled[1] = !PlayerEnabled[1];
                changed = true;
            }
            if (Input.GetKeyDown(player3ToggleKey))
            {
                PlayerEnabled[2] = !PlayerEnabled[2];
                changed = true;
            }
            if (Input.GetKeyDown(player4ToggleKey))
            {
                PlayerEnabled[3] = !PlayerEnabled[3];
                changed = true;
            }

            if (changed)
                activeMethod?.OnPlayersChanged();
        }

        // --- Mode Switching ---

        /// <summary>
        /// Switch to a different steering mode by index.
        /// </summary>
        public void SetMode(int index)
        {
            if (methods == null || index < 0 || index >= methods.Length) return;
            if (methods[index] == null) return;
            if (index == currentModeIndex) return;

            // Deactivate old
            activeMethod?.Deactivate();

            // Activate new
            currentModeIndex = index;
            activeMethod = methods[index];
            activeMethod.Activate();
            activeMethod.OnPlayersChanged();
        }

        /// <summary>
        /// Get the display name of the currently active mode.
        /// </summary>
        public string ActiveModeName
        {
            get
            {
                if (modes != null && currentModeIndex >= 0 && currentModeIndex < modes.Length && modes[currentModeIndex] != null)
                    return modes[currentModeIndex].displayName;
                return "None";
            }
        }

        /// <summary>
        /// Get the number of currently enabled players.
        /// </summary>
        public int GetEnabledPlayerCount()
        {
            int count = 0;
            foreach (bool enabled in PlayerEnabled)
            {
                if (enabled) count++;
            }
            return count;
        }

        /// <summary>
        /// Enable or disable a specific player by index (0-3).
        /// </summary>
        public void SetPlayerEnabled(int index, bool enabled)
        {
            if (index >= 0 && index < PlayerEnabled.Length)
            {
                PlayerEnabled[index] = enabled;
                activeMethod?.OnPlayersChanged();
            }
        }
    }
}
