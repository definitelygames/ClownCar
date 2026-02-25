using UnityEngine;

namespace EVP
{
    [CreateAssetMenu(fileName = "PerWheelSteering", menuName = "Vehicle/Steering/Per-Wheel Multiplayer")]
    public class PerWheelSteeringConfig : SteeringMethodConfig
    {
        [Header("Steering Angles")]
        [Tooltip("If true, use the vehicle's maxSteerAngle for front wheels.")]
        public bool useVehicleSteerAngle = true;
        [Tooltip("Custom front wheel max steer angle (used when useVehicleSteerAngle is false).")]
        public float maxSteerAngle = 35f;
        [Tooltip("Max steer angle for rear wheels (typically much less than front).")]
        public float rearMaxSteerAngle = 15f;

        [Header("Drive Force")]
        [Tooltip("Maximum drive force in Newtons per wheel.")]
        public float maxDriveForce = 1500f;
        [Tooltip("How much force reduces as speed approaches maxSpeed (0=none, 1=full).")]
        [Range(0f, 1f)]
        public float forceFalloffWithSpeed = 0.5f;
        [Tooltip("Speed (m/s) at which force reaches zero at full falloff.")]
        public float maxSpeed = 30f;

        [Header("Keyboard Input Ramping")]
        [Tooltip("How fast keyboard input moves toward target (units/sec).")]
        public float keyboardMoveSpeed = 2.0f;
        [Tooltip("How fast input returns to zero when no key held (units/sec).")]
        public float keyboardReturnSpeed = 3.0f;
        [Tooltip("If false, input stays at its last value when released instead of returning to center.")]
        public bool inputReturnToCenter = true;

        [Header("Uncontrolled Wheels")]
        [Tooltip("If true, wheels with no player gradually center their steering.")]
        public bool centerUncontrolledWheels = true;
        [Tooltip("How fast uncontrolled wheels center (degrees/sec).")]
        public float centeringSpeed = 45f;

        [Header("Default Player Input Types")]
        public LeanInputType player1InputType = LeanInputType.WASD;
        public LeanInputType player2InputType = LeanInputType.ArrowKeys;
        public LeanInputType player3InputType = LeanInputType.Gamepad1;
        public LeanInputType player4InputType = LeanInputType.Gamepad2;

        [Header("UI Colors")]
        public Color player1Color = Color.red;
        public Color player2Color = Color.blue;
        public Color player3Color = Color.green;
        public Color player4Color = Color.yellow;
        public Color uncontrolledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);

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

        public Color GetPlayerColor(int playerIndex)
        {
            switch (playerIndex)
            {
                case 0: return player1Color;
                case 1: return player2Color;
                case 2: return player3Color;
                case 3: return player4Color;
                default: return Color.white;
            }
        }

        public override SteeringMethod CreateMethod()
        {
            return new PerWheelMultiplayerSteering(this);
        }
    }
}
