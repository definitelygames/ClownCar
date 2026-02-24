using UnityEngine;

namespace EVP
{
    [CreateAssetMenu(fileName = "DiscreteSteering", menuName = "Vehicle/Steering/Discrete Multiplayer")]
    public class DiscreteSteeringConfig : SteeringMethodConfig
    {
        [Header("Player Key Pools")]
        [Tooltip("4 keys per player, mapped in order to assigned controls.")]
        public KeyCode[] player1Keys = { KeyCode.A, KeyCode.D, KeyCode.W, KeyCode.S };
        public KeyCode[] player2Keys = { KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow };
        public KeyCode[] player3Keys = { KeyCode.J, KeyCode.L, KeyCode.I, KeyCode.K };
        public KeyCode[] player4Keys = { KeyCode.Keypad4, KeyCode.Keypad6, KeyCode.Keypad8, KeyCode.Keypad5 };

        [Header("Analog Ramping")]
        public float rampUpSpeed = 3.0f;
        public float rampDownSpeed = 5.0f;

        [Header("UI Colors")]
        public Color player1Color = Color.red;
        public Color player2Color = Color.blue;
        public Color player3Color = Color.green;
        public Color player4Color = Color.yellow;
        public Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        public KeyCode[] GetPlayerKeys(int playerIndex)
        {
            switch (playerIndex)
            {
                case 0: return player1Keys;
                case 1: return player2Keys;
                case 2: return player3Keys;
                case 3: return player4Keys;
                default: return player1Keys;
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
            return new DiscreteMultiplayerSteering(this);
        }
    }
}
