using UnityEngine;

namespace EVP
{
    [CreateAssetMenu(fileName = "LeanSteering", menuName = "Vehicle/Steering/Lean Multiplayer")]
    public class LeanSteeringConfig : SteeringMethodConfig
    {
        [Header("Steering Settings")]
        [Tooltip("Whether left/right lean controls vehicle steering.")]
        public bool leanAffectsSteering = true;
        [Tooltip("Multiplier per player's steering contribution.")]
        public float steeringMultiplier = 1.0f;

        [Header("Lean Physics")]
        [Tooltip("Whether left/right lean applies roll torque to the vehicle.")]
        public bool leanAffectsLateralTorque = true;
        [Tooltip("Left/right roll torque.")]
        public float leanTorqueLateral = 5000f;
        [Tooltip("Forward/back pitch torque.")]
        public float leanTorqueLongitudinal = 3000f;

        [Header("Pop")]
        [Tooltip("How long (seconds) a player stays in the pop window after hitting the edge.")]
        public float popWindow = 0.1f;
        [Tooltip("Threshold for dot magnitude to count as 'at edge' (0-1).")]
        public float popEdgeThreshold = 0.95f;
        [Tooltip("Instantaneous impulse torque when all players pop together.")]
        public float popForce = 10000f;

        [Header("Keyboard Movement")]
        [Tooltip("How fast keyboard moves dot (normalized units/sec).")]
        public float keyboardMoveSpeed = 2.0f;
        [Tooltip("How fast dot drifts back to center when no input.")]
        public float keyboardReturnSpeed = 3.0f;

        [Header("Default Player Input Types")]
        public LeanInputType player1InputType = LeanInputType.WASD;
        public LeanInputType player2InputType = LeanInputType.ArrowKeys;
        public LeanInputType player3InputType = LeanInputType.Gamepad1;
        public LeanInputType player4InputType = LeanInputType.Gamepad2;

        [Header("UI Layout")]
        public float boxWidth = 120f;
        public float boxHeight = 120f;
        public float boxSpacing = 15f;
        public float bottomMargin = 30f;
        public float dotRadius = 6f;

        public LeanInputType GetDefaultInputType(int playerIndex)
        {
            switch (playerIndex)
            {
                case 0: return player1InputType;
                case 1: return player2InputType;
                case 2: return player3InputType;
                case 3: return player4InputType;
                default: return LeanInputType.WASD;
            }
        }

        public override SteeringMethod CreateMethod()
        {
            return new LeanMultiplayerSteering(this);
        }
    }
}
