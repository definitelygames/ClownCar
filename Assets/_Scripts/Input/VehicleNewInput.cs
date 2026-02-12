using UnityEngine;
using UnityEngine.InputSystem;

namespace EVP
{
    /// <summary>
    /// Vehicle input using Unity's new Input System.
    /// Supports multiple simultaneous input methods (keyboard, gamepad, etc.)
    /// and multiple bindings per action.
    /// </summary>
    public class VehicleNewInput : MonoBehaviour
    {
        [Header("Target Vehicle")]
        public VehicleController target;

        [Header("Input Asset")]
        [Tooltip("Assign the VehicleInputActions asset. If null, will try to find one.")]
        public InputActionAsset inputActions;

        [Header("Behavior Settings")]
        [Tooltip("When true, pressing reverse while moving forward will brake, then reverse. " +
                 "When false, use ReverseModifier key to toggle reverse mode.")]
        public bool continuousForwardAndReverse = true;

        [Tooltip("When true, holding handbrake reduces throttle proportionally.")]
        public bool handbrakeOverridesThrottle = false;

        [Header("Input Combining")]
        [Tooltip("How to combine multiple simultaneous inputs for the same action.")]
        public InputCombineMode combineMode = InputCombineMode.TakeHighestMagnitude;

        public enum InputCombineMode
        {
            TakeHighestMagnitude,  // Use whichever input has the largest absolute value
            Sum,                    // Add all inputs together (clamped to -1,1)
            Average                 // Average all active inputs
        }

        // Input Actions
        private InputActionMap vehicleActionMap;
        private InputAction steerAction;
        private InputAction throttleAction;
        private InputAction brakeAction;
        private InputAction handbrakeAction;
        private InputAction resetAction;
        private InputAction reverseModifierAction;

        // State
        private bool doReset = false;
        private float steerInput = 0f;
        private float throttleInput = 0f;
        private float brakeInput = 0f;
        private float handbrakeInput = 0f;
        private bool reverseModifierHeld = false;

        // For tracking multiple input sources
        private float[] steerValues = new float[8];
        private float[] throttleValues = new float[8];
        private int activeSteerInputs = 0;
        private int activeThrottleInputs = 0;

        void OnEnable()
        {
            if (target == null)
                target = GetComponent<VehicleController>();

            SetupInputActions();
            EnableInput();
        }

        void OnDisable()
        {
            DisableInput();

            if (target != null)
            {
                target.steerInput = 0f;
                target.throttleInput = 0f;
                target.brakeInput = 0f;
                target.handbrakeInput = 0f;
            }
        }

        void SetupInputActions()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("VehicleNewInput: No InputActionAsset assigned. " +
                               "Please assign VehicleInputActions in the inspector.");
                return;
            }

            vehicleActionMap = inputActions.FindActionMap("Vehicle");
            if (vehicleActionMap == null)
            {
                Debug.LogError("VehicleNewInput: Could not find 'Vehicle' action map in InputActionAsset.");
                return;
            }

            steerAction = vehicleActionMap.FindAction("Steer");
            throttleAction = vehicleActionMap.FindAction("Throttle");
            brakeAction = vehicleActionMap.FindAction("Brake");
            handbrakeAction = vehicleActionMap.FindAction("Handbrake");
            resetAction = vehicleActionMap.FindAction("ResetVehicle");
            reverseModifierAction = vehicleActionMap.FindAction("ReverseModifier");

            // Subscribe to reset action
            if (resetAction != null)
            {
                resetAction.performed += OnResetPerformed;
            }
        }

        void EnableInput()
        {
            vehicleActionMap?.Enable();
        }

        void DisableInput()
        {
            if (resetAction != null)
            {
                resetAction.performed -= OnResetPerformed;
            }
            vehicleActionMap?.Disable();
        }

        void OnResetPerformed(InputAction.CallbackContext context)
        {
            doReset = true;
        }

        void Update()
        {
            if (vehicleActionMap == null || !vehicleActionMap.enabled) return;

            // Read inputs every frame for responsive feel
            ReadInputs();
        }

        void ReadInputs()
        {
            // Steer - supports multiple bindings via composite
            steerInput = ReadAxisWithMultipleBindings(steerAction);
            steerInput = Mathf.Clamp(steerInput, -1f, 1f);

            // Throttle/Brake combined axis (for keyboard W/S style)
            float combinedAxis = ReadAxisWithMultipleBindings(throttleAction);

            // Separate brake axis (for gamepad triggers)
            float separateBrake = brakeAction?.ReadValue<float>() ?? 0f;

            // Determine forward and reverse input
            float forwardInput = Mathf.Clamp01(combinedAxis);
            float reverseInput = Mathf.Max(Mathf.Clamp01(-combinedAxis), separateBrake);

            // Handbrake
            handbrakeInput = Mathf.Clamp01(handbrakeAction?.ReadValue<float>() ?? 0f);

            // Reverse modifier (for non-continuous mode)
            reverseModifierHeld = reverseModifierAction?.ReadValue<float>() > 0.5f;

            // Translate to throttle/brake based on mode
            TranslateToVehicleInput(forwardInput, reverseInput);
        }

        float ReadAxisWithMultipleBindings(InputAction action)
        {
            if (action == null) return 0f;

            // The Input System automatically combines multiple bindings
            // When using 1D Axis composites, it takes the highest magnitude by default
            // We can read the combined value directly
            float value = action.ReadValue<float>();

            // If you need custom combining logic, you can iterate bindings:
            // This is useful if you want Sum or Average mode
            if (combineMode != InputCombineMode.TakeHighestMagnitude)
            {
                value = CombineBindingValues(action);
            }

            return value;
        }

        float CombineBindingValues(InputAction action)
        {
            // For advanced combining of multiple active bindings
            float sum = 0f;
            int count = 0;
            float maxMagnitude = 0f;
            float maxValue = 0f;

            foreach (var control in action.controls)
            {
                if (control.IsPressed() || Mathf.Abs((float)control.ReadValueAsObject()) > 0.01f)
                {
                    float val = (float)control.ReadValueAsObject();
                    sum += val;
                    count++;

                    if (Mathf.Abs(val) > maxMagnitude)
                    {
                        maxMagnitude = Mathf.Abs(val);
                        maxValue = val;
                    }
                }
            }

            switch (combineMode)
            {
                case InputCombineMode.Sum:
                    return Mathf.Clamp(sum, -1f, 1f);
                case InputCombineMode.Average:
                    return count > 0 ? sum / count : 0f;
                case InputCombineMode.TakeHighestMagnitude:
                default:
                    return maxValue;
            }
        }

        void TranslateToVehicleInput(float forwardInput, float reverseInput)
        {
            if (continuousForwardAndReverse)
            {
                // Automatic forward/reverse based on vehicle speed
                float minSpeed = 0.1f;
                float minInput = 0.1f;

                if (target.speed > minSpeed)
                {
                    // Moving forward: forward = throttle, reverse = brake
                    throttleInput = forwardInput;
                    brakeInput = reverseInput;
                }
                else
                {
                    if (reverseInput > minInput)
                    {
                        // Want to reverse
                        throttleInput = -reverseInput;
                        brakeInput = 0f;
                    }
                    else if (forwardInput > minInput)
                    {
                        if (target.speed < -minSpeed)
                        {
                            // Moving backward, pressing forward = brake
                            throttleInput = 0f;
                            brakeInput = forwardInput;
                        }
                        else
                        {
                            // Stationary or slow, pressing forward = accelerate
                            throttleInput = forwardInput;
                            brakeInput = 0f;
                        }
                    }
                    else
                    {
                        throttleInput = 0f;
                        brakeInput = 0f;
                    }
                }
            }
            else
            {
                // Manual reverse mode using modifier key
                if (!reverseModifierHeld)
                {
                    throttleInput = forwardInput;
                    brakeInput = reverseInput;
                }
                else
                {
                    throttleInput = -reverseInput;
                    brakeInput = 0f;
                }
            }

            // Handbrake overrides throttle if enabled
            if (handbrakeOverridesThrottle)
            {
                throttleInput *= (1f - handbrakeInput);
            }
        }

        void FixedUpdate()
        {
            if (target == null) return;

            // Apply input to vehicle
            target.steerInput = steerInput;
            target.throttleInput = throttleInput;
            target.brakeInput = brakeInput;
            target.handbrakeInput = handbrakeInput;

            // Handle reset
            if (doReset)
            {
                target.ResetVehicle();
                doReset = false;
            }
        }

        /// <summary>
        /// Call this to add a runtime binding for steering.
        /// Example: AddSteerBinding("<Keyboard>/q", "<Keyboard>/e");
        /// </summary>
        public void AddSteerBinding(string negativeKey, string positiveKey)
        {
            if (steerAction == null) return;

            steerAction.AddCompositeBinding("1DAxis")
                .With("Negative", negativeKey)
                .With("Positive", positiveKey);
        }

        /// <summary>
        /// Call this to add a runtime binding for throttle/brake.
        /// Example: AddThrottleBinding("<Keyboard>/i", "<Keyboard>/k");
        /// </summary>
        public void AddThrottleBinding(string negativeKey, string positiveKey)
        {
            if (throttleAction == null) return;

            throttleAction.AddCompositeBinding("1DAxis")
                .With("Negative", negativeKey)
                .With("Positive", positiveKey);
        }

        /// <summary>
        /// Add a single key binding for handbrake.
        /// </summary>
        public void AddHandbrakeBinding(string key)
        {
            handbrakeAction?.AddBinding(key);
        }

        /// <summary>
        /// Add a single key binding for reset.
        /// </summary>
        public void AddResetBinding(string key)
        {
            resetAction?.AddBinding(key);
        }

        // Public getters for current input values (useful for UI, networking, etc.)
        public float SteerInput => steerInput;
        public float ThrottleInput => throttleInput;
        public float BrakeInput => brakeInput;
        public float HandbrakeInput => handbrakeInput;
    }
}
