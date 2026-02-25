using System.Collections.Generic;
using UnityEngine;

namespace EVP
{
    public enum VehicleControlAction { SteerLeft, SteerRight, Accelerate, Brake }

    [System.Serializable]
    public class ControlBinding
    {
        public VehicleControlAction action;
        public KeyCode key;
        [HideInInspector] public float currentValue; // 0-1, ramped
        [HideInInspector] public float targetValue;  // 0 or 1, raw input
    }

    /// <summary>
    /// Per-player input state and analog ramping for discrete multiplayer steering.
    /// Each player has a pool of keys and a set of assigned controls determined at runtime.
    /// </summary>
    [System.Serializable]
    public class MultiplayerSteeringPlayer
    {
        [Header("Player Settings")]
        public int playerIndex;
        public bool isEnabled = true;

        [Header("Key Pool")]
        [Tooltip("4 keys mapped in order to assigned controls")]
        public KeyCode[] availableKeys = new KeyCode[4];

        [Header("Analog Ramping")]
        [Tooltip("How fast input ramps up when key is held")]
        public float rampUpSpeed = 3.0f;
        [Tooltip("How fast input returns to zero when key is released")]
        public float rampDownSpeed = 5.0f;

        [Header("Assigned Controls (set at runtime)")]
        public List<ControlBinding> assignedControls = new List<ControlBinding>();

        public void ReadInput()
        {
            if (!isEnabled)
            {
                foreach (var binding in assignedControls)
                    binding.targetValue = 0f;
                return;
            }

            foreach (var binding in assignedControls)
                binding.targetValue = Input.GetKey(binding.key) ? 1f : 0f;
        }

        public void UpdateRamping(float deltaTime)
        {
            foreach (var binding in assignedControls)
            {
                if (!isEnabled)
                {
                    binding.currentValue = Mathf.MoveTowards(binding.currentValue, 0f, rampDownSpeed * deltaTime);
                    continue;
                }

                float speed = binding.targetValue > 0.01f ? rampUpSpeed : rampDownSpeed;
                binding.currentValue = Mathf.MoveTowards(binding.currentValue, binding.targetValue, speed * deltaTime);
            }
        }

        public float GetControlValue(VehicleControlAction action)
        {
            foreach (var binding in assignedControls)
            {
                if (binding.action == action)
                    return binding.currentValue;
            }
            return 0f;
        }

        public void Toggle()
        {
            isEnabled = !isEnabled;
        }

        public void Reset()
        {
            foreach (var binding in assignedControls)
            {
                binding.currentValue = 0f;
                binding.targetValue = 0f;
            }
        }
    }

    // Lean steering data types (used by LeanMultiplayerSteering)

    public enum LeanInputType
    {
        WASD,
        ArrowKeys,
        Gamepad1,
        Gamepad2,
        Gamepad3,
        Gamepad4,
        Mouse
    }

    [System.Serializable]
    public class LeanPlayerData
    {
        public bool isEnabled;
        public LeanInputType inputType;
        [HideInInspector] public Vector2 dotPosition;
        [HideInInspector] public float popTimeRemaining;
        [HideInInspector] public bool wasAtEdge;
    }

    [System.Serializable]
    public class PerWheelPlayerData
    {
        public bool isEnabled;
        public int wheelIndex = -1;
        public LeanInputType inputType;
        [HideInInspector] public Vector2 inputPosition; // x=steer, y=throttle
    }
}
