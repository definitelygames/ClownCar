using UnityEngine;
using UnityEngine.InputSystem;

namespace EVP
{
    /// <summary>
    /// Single-player steering using Unity's Input System.
    /// Ported from VehicleNewInput: handles steer/throttle/brake with continuous forward/reverse logic.
    /// Handbrake and reset are handled by the manager.
    /// </summary>
    public class SinglePlayerSteering : SteeringMethod
    {
        private readonly SinglePlayerSteeringConfig config;

        // Input Actions
        private InputActionMap vehicleActionMap;
        private InputAction steerAction;
        private InputAction throttleAction;
        private InputAction brakeAction;
        private InputAction reverseModifierAction;

        // State
        private float steerInput;
        private float throttleInput;
        private float brakeInput;
        private bool reverseModifierHeld;

        public SinglePlayerSteering(SinglePlayerSteeringConfig config)
        {
            this.config = config;
        }

        public override void Activate()
        {
            SetupInputActions();
            vehicleActionMap?.Enable();
        }

        public override void Deactivate()
        {
            vehicleActionMap?.Disable();
            steerInput = 0f;
            throttleInput = 0f;
            brakeInput = 0f;
        }

        private void SetupInputActions()
        {
            if (config.inputActions == null)
            {
                Debug.LogWarning("SinglePlayerSteering: No InputActionAsset assigned in config.");
                return;
            }

            vehicleActionMap = config.inputActions.FindActionMap("Vehicle");
            if (vehicleActionMap == null)
            {
                Debug.LogError("SinglePlayerSteering: Could not find 'Vehicle' action map.");
                return;
            }

            steerAction = vehicleActionMap.FindAction("Steer");
            throttleAction = vehicleActionMap.FindAction("Throttle");
            brakeAction = vehicleActionMap.FindAction("Brake");
            reverseModifierAction = vehicleActionMap.FindAction("ReverseModifier");
        }

        public override void ReadInput(float deltaTime)
        {
            if (vehicleActionMap == null || !vehicleActionMap.enabled) return;

            // Steer
            steerInput = ReadAxisWithMultipleBindings(steerAction);
            steerInput = Mathf.Clamp(steerInput, -1f, 1f);

            // Throttle/Brake combined axis (keyboard W/S style)
            float combinedAxis = ReadAxisWithMultipleBindings(throttleAction);

            // Separate brake axis (gamepad triggers)
            float separateBrake = brakeAction?.ReadValue<float>() ?? 0f;

            float forwardInput = Mathf.Clamp01(combinedAxis);
            float reverseInput = Mathf.Max(Mathf.Clamp01(-combinedAxis), separateBrake);

            // Reverse modifier (for non-continuous mode)
            reverseModifierHeld = reverseModifierAction?.ReadValue<float>() > 0.5f;

            TranslateToVehicleInput(forwardInput, reverseInput);
        }

        public override VehicleInput GetVehicleInput(float fixedDeltaTime)
        {
            return new VehicleInput
            {
                steer = steerInput,
                throttle = throttleInput,
                brake = brakeInput
            };
        }

        private float ReadAxisWithMultipleBindings(InputAction action)
        {
            if (action == null) return 0f;

            float value = action.ReadValue<float>();

            if (config.combineMode != InputCombineMode.TakeHighestMagnitude)
                value = CombineBindingValues(action);

            return value;
        }

        private float CombineBindingValues(InputAction action)
        {
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

            switch (config.combineMode)
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

        private void TranslateToVehicleInput(float forwardInput, float reverseInput)
        {
            if (config.continuousForwardAndReverse)
            {
                float minSpeed = 0.1f;
                float minInput = 0.1f;

                if (vehicle.speed > minSpeed)
                {
                    throttleInput = forwardInput;
                    brakeInput = reverseInput;
                }
                else
                {
                    if (reverseInput > minInput)
                    {
                        throttleInput = -reverseInput;
                        brakeInput = 0f;
                    }
                    else if (forwardInput > minInput)
                    {
                        if (vehicle.speed < -minSpeed)
                        {
                            throttleInput = 0f;
                            brakeInput = forwardInput;
                        }
                        else
                        {
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
            if (config.handbrakeOverridesThrottle)
            {
                float handbrakeInput = manager.HandbrakeInput;
                throttleInput *= (1f - handbrakeInput);
            }
        }
    }
}
