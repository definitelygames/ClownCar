using UnityEngine;
using UnityEngine.InputSystem;

namespace EVP
{
    [CreateAssetMenu(fileName = "SinglePlayerSteering", menuName = "Vehicle/Steering/Single Player")]
    public class SinglePlayerSteeringConfig : SteeringMethodConfig
    {
        [Header("Input Asset")]
        [Tooltip("Assign the VehicleInputActions asset.")]
        public InputActionAsset inputActions;

        [Header("Input Smoothing")]
        [Tooltip("How fast input ramps toward the target value (units/sec).")]
        public float inputRampSpeed = 3f;
        [Tooltip("How fast input returns to center when released (units/sec).")]
        public float inputReturnSpeed = 5f;

        [Header("Behavior Settings")]
        [Tooltip("When true, holding handbrake reduces throttle proportionally.")]
        public bool handbrakeOverridesThrottle = false;

        public override SteeringMethod CreateMethod()
        {
            return new SinglePlayerSteering(this);
        }
    }
}
