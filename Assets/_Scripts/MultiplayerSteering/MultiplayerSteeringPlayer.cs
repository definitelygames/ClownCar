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
    /// Per-player input state and analog ramping.
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

        /// <summary>
        /// Read keyboard input and set target values for all assigned controls.
        /// </summary>
        public void ReadInput()
        {
            if (!isEnabled)
            {
                foreach (var binding in assignedControls)
                    binding.targetValue = 0f;
                return;
            }

            foreach (var binding in assignedControls)
            {
                binding.targetValue = Input.GetKey(binding.key) ? 1f : 0f;
            }
        }

        /// <summary>
        /// Update analog ramping for all assigned controls.
        /// </summary>
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

        /// <summary>
        /// Get the current ramped value for a specific action (0 if not assigned).
        /// </summary>
        public float GetControlValue(VehicleControlAction action)
        {
            foreach (var binding in assignedControls)
            {
                if (binding.action == action)
                    return binding.currentValue;
            }
            return 0f;
        }

        /// <summary>
        /// Toggle the enabled state of this player.
        /// </summary>
        public void Toggle()
        {
            isEnabled = !isEnabled;
        }

        /// <summary>
        /// Reset all control values to zero.
        /// </summary>
        public void Reset()
        {
            foreach (var binding in assignedControls)
            {
                binding.currentValue = 0f;
                binding.targetValue = 0f;
            }
        }
    }
}
