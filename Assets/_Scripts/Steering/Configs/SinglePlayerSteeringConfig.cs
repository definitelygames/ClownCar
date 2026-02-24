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

        [Header("Behavior Settings")]
        [Tooltip("When true, pressing reverse while moving forward will brake, then reverse. " +
                 "When false, use ReverseModifier key to toggle reverse mode.")]
        public bool continuousForwardAndReverse = true;

        [Tooltip("When true, holding handbrake reduces throttle proportionally.")]
        public bool handbrakeOverridesThrottle = false;

        [Header("Input Combining")]
        [Tooltip("How to combine multiple simultaneous inputs for the same action.")]
        public InputCombineMode combineMode = InputCombineMode.TakeHighestMagnitude;

        public override SteeringMethod CreateMethod()
        {
            return new SinglePlayerSteering(this);
        }
    }

    public enum InputCombineMode
    {
        TakeHighestMagnitude,
        Sum,
        Average
    }
}
