using UnityEngine;

namespace EVP
{
    /// <summary>
    /// Per-player steering input state and analog ramping.
    /// Handles binary input from keyboard and converts to smooth analog steering.
    /// </summary>
    [System.Serializable]
    public class MultiplayerSteeringPlayer
    {
        [Header("Player Settings")]
        public int playerIndex;
        public KeyCode leftKey = KeyCode.A;
        public KeyCode rightKey = KeyCode.D;
        public bool isEnabled = true;

        [Header("Analog Ramping")]
        [Tooltip("How fast steering ramps up when key is held")]
        public float rampUpSpeed = 3.0f;
        [Tooltip("How fast steering returns to center when key is released")]
        public float rampDownSpeed = 5.0f;

        // Current analog steering value (-1 to 1)
        [HideInInspector]
        public float currentSteer = 0f;

        // Immediate binary input target (-1, 0, or 1)
        [HideInInspector]
        public float targetSteer = 0f;

        /// <summary>
        /// Read keyboard input and set target steer value.
        /// </summary>
        public void ReadInput()
        {
            if (!isEnabled)
            {
                targetSteer = 0f;
                return;
            }

            float left = Input.GetKey(leftKey) ? -1f : 0f;
            float right = Input.GetKey(rightKey) ? 1f : 0f;
            targetSteer = left + right;
        }

        /// <summary>
        /// Update analog ramping using MoveTowards for smooth transitions.
        /// Call this in Update or FixedUpdate.
        /// </summary>
        public void UpdateRamping(float deltaTime)
        {
            if (!isEnabled)
            {
                // When disabled, quickly return to center
                currentSteer = Mathf.MoveTowards(currentSteer, 0f, rampDownSpeed * deltaTime);
                return;
            }

            // Use rampUpSpeed when moving toward target, rampDownSpeed when returning to center
            float speed = Mathf.Abs(targetSteer) > 0.01f ? rampUpSpeed : rampDownSpeed;
            currentSteer = Mathf.MoveTowards(currentSteer, targetSteer, speed * deltaTime);
        }

        /// <summary>
        /// Toggle the enabled state of this player.
        /// </summary>
        public void Toggle()
        {
            isEnabled = !isEnabled;
        }

        /// <summary>
        /// Reset steering to center position.
        /// </summary>
        public void Reset()
        {
            currentSteer = 0f;
            targetSteer = 0f;
        }
    }
}
