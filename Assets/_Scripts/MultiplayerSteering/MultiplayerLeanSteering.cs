using UnityEngine;
using UnityEngine.InputSystem;

namespace EVP
{
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
        [HideInInspector] public Vector2 dotPosition; // normalized [-1, 1] on both axes
    }

    public class MultiplayerLeanSteering : MonoBehaviour
    {
        [Header("Vehicle Reference")]
        [Tooltip("The vehicle to control. If null, will try to find on this GameObject.")]
        public VehicleController vehicle;

        [Header("Input Override")]
        [Tooltip("Reference to VehicleNewInput to disable its steering when this is active.")]
        public VehicleNewInput vehicleInput;

        [Header("Steering Settings")]
        [Tooltip("Multiplier per player's steering contribution.")]
        public float steeringMultiplier = 1.0f;

        [Header("Lean Physics")]
        [Tooltip("Left/right roll torque. At full lean with all players, the car should tip over.")]
        public float leanTorqueLateral = 5000f;
        [Tooltip("Forward/back pitch torque. Positive lean (forward) shifts weight to the front.")]
        public float leanTorqueLongitudinal = 3000f;

        [Header("Keyboard Movement")]
        [Tooltip("How fast keyboard moves dot (normalized units/sec).")]
        public float keyboardMoveSpeed = 2.0f;
        [Tooltip("How fast dot drifts back to center when no input.")]
        public float keyboardReturnSpeed = 3.0f;

        [Header("Players")]
        public LeanPlayerData[] players = new LeanPlayerData[4];

        [Header("Player Toggle Keys")]
        public KeyCode player1ToggleKey = KeyCode.Alpha1;
        public KeyCode player2ToggleKey = KeyCode.Alpha2;
        public KeyCode player3ToggleKey = KeyCode.Alpha3;
        public KeyCode player4ToggleKey = KeyCode.Alpha4;

        [Header("UI Display")]
        public bool showUI = true;
        public KeyCode uiToggleKey = KeyCode.U;

        [Header("UI Layout")]
        public float boxWidth = 120f;
        public float boxHeight = 120f;
        public float boxSpacing = 15f;
        public float bottomMargin = 30f;
        public float dotRadius = 6f;

        void Awake()
        {
            if (players == null || players.Length == 0)
                InitializeDefaultPlayers();
        }

        void InitializeDefaultPlayers()
        {
            players = new LeanPlayerData[4];
            players[0] = new LeanPlayerData { isEnabled = true, inputType = LeanInputType.WASD };
            players[1] = new LeanPlayerData { isEnabled = false, inputType = LeanInputType.ArrowKeys };
            players[2] = new LeanPlayerData { isEnabled = false, inputType = LeanInputType.Gamepad1 };
            players[3] = new LeanPlayerData { isEnabled = false, inputType = LeanInputType.Gamepad2 };
        }

        void OnEnable()
        {
            if (vehicle == null)
                vehicle = GetComponent<VehicleController>();

            if (vehicleInput == null)
                vehicleInput = GetComponent<VehicleNewInput>();

            if (vehicleInput != null)
                vehicleInput.externalSteeringOverride = true;

            // Reset all dots to center
            foreach (var player in players)
            {
                if (player != null)
                    player.dotPosition = Vector2.zero;
            }
        }

        void OnDisable()
        {
            if (vehicleInput != null)
                vehicleInput.externalSteeringOverride = false;

            if (vehicle != null)
                vehicle.steerInput = 0f;
        }

        void Update()
        {
            HandleToggleKeys();
            UpdateDotPositions();
        }

        void FixedUpdate()
        {
            if (vehicle == null) return;

            float combinedSteer = 0f;
            float combinedLeanX = 0f;
            float combinedLeanY = 0f;
            int enabledCount = 0;
            foreach (var player in players)
            {
                if (player != null && player.isEnabled)
                {
                    combinedSteer += player.dotPosition.x * steeringMultiplier;
                    combinedLeanX += player.dotPosition.x;
                    combinedLeanY += player.dotPosition.y;
                    enabledCount++;
                }
            }

            if (enabledCount > 0)
            {
                combinedSteer /= enabledCount;
                combinedLeanX /= enabledCount;
                combinedLeanY /= enabledCount;
            }

            vehicle.steerInput = Mathf.Clamp(combinedSteer, -1f, 1f);

            // Apply torques to simulate weight shift
            Rigidbody rb = vehicle.cachedRigidbody;
            if (rb != null)
            {
                // Roll torque (left/right lean around forward axis)
                rb.AddTorque(rb.transform.forward * combinedLeanX * -leanTorqueLateral, ForceMode.Force);
                // Pitch torque (forward/back lean around right axis)
                rb.AddTorque(rb.transform.right * combinedLeanY * leanTorqueLongitudinal, ForceMode.Force);
            }
        }

        void HandleToggleKeys()
        {
            if (Input.GetKeyDown(uiToggleKey))
                showUI = !showUI;

            if (Input.GetKeyDown(player1ToggleKey) && players.Length > 0 && players[0] != null)
                players[0].isEnabled = !players[0].isEnabled;

            if (Input.GetKeyDown(player2ToggleKey) && players.Length > 1 && players[1] != null)
                players[1].isEnabled = !players[1].isEnabled;

            if (Input.GetKeyDown(player3ToggleKey) && players.Length > 2 && players[2] != null)
                players[2].isEnabled = !players[2].isEnabled;

            if (Input.GetKeyDown(player4ToggleKey) && players.Length > 3 && players[3] != null)
                players[3].isEnabled = !players[3].isEnabled;
        }

        void UpdateDotPositions()
        {
            float dt = Time.deltaTime;

            foreach (var player in players)
            {
                if (player == null || !player.isEnabled) continue;

                switch (player.inputType)
                {
                    case LeanInputType.WASD:
                        UpdateKeyboardDot(player, KeyCode.A, KeyCode.D, KeyCode.S, KeyCode.W, dt);
                        break;

                    case LeanInputType.ArrowKeys:
                        UpdateKeyboardDot(player, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.DownArrow, KeyCode.UpArrow, dt);
                        break;

                    case LeanInputType.Gamepad1:
                        UpdateGamepadDot(player, 0);
                        break;

                    case LeanInputType.Gamepad2:
                        UpdateGamepadDot(player, 1);
                        break;

                    case LeanInputType.Gamepad3:
                        UpdateGamepadDot(player, 2);
                        break;

                    case LeanInputType.Gamepad4:
                        UpdateGamepadDot(player, 3);
                        break;

                    case LeanInputType.Mouse:
                        UpdateMouseDot(player);
                        break;
                }
            }
        }

        void UpdateKeyboardDot(LeanPlayerData player, KeyCode left, KeyCode right, KeyCode down, KeyCode up, float dt)
        {
            float hInput = 0f;
            float vInput = 0f;

            if (Input.GetKey(left)) hInput -= 1f;
            if (Input.GetKey(right)) hInput += 1f;
            if (Input.GetKey(down)) vInput -= 1f;
            if (Input.GetKey(up)) vInput += 1f;

            bool hasHInput = Mathf.Abs(hInput) > 0.01f;
            bool hasVInput = Mathf.Abs(vInput) > 0.01f;

            // Horizontal: move with input, return to center when released
            if (hasHInput)
                player.dotPosition.x = Mathf.MoveTowards(player.dotPosition.x, hInput, keyboardMoveSpeed * dt);
            else
                player.dotPosition.x = Mathf.MoveTowards(player.dotPosition.x, 0f, keyboardReturnSpeed * dt);

            // Vertical: move with input, return to center when released
            if (hasVInput)
                player.dotPosition.y = Mathf.MoveTowards(player.dotPosition.y, vInput, keyboardMoveSpeed * dt);
            else
                player.dotPosition.y = Mathf.MoveTowards(player.dotPosition.y, 0f, keyboardReturnSpeed * dt);

            player.dotPosition.x = Mathf.Clamp(player.dotPosition.x, -1f, 1f);
            player.dotPosition.y = Mathf.Clamp(player.dotPosition.y, -1f, 1f);
        }

        void UpdateGamepadDot(LeanPlayerData player, int gamepadIndex)
        {
            var gamepads = Gamepad.all;
            if (gamepadIndex >= gamepads.Count)
            {
                player.dotPosition = Vector2.zero;
                return;
            }

            Vector2 stick = gamepads[gamepadIndex].leftStick.ReadValue();
            player.dotPosition.x = Mathf.Clamp(stick.x, -1f, 1f);
            player.dotPosition.y = Mathf.Clamp(stick.y, -1f, 1f);
        }

        void UpdateMouseDot(LeanPlayerData player)
        {
            // Find this player's drawn index among enabled players to match OnGUI layout
            int enabledIndex = -1;
            int enabledCount = 0;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null || !players[i].isEnabled) continue;
                if (players[i] == player)
                    enabledIndex = enabledCount;
                enabledCount++;
            }

            if (enabledIndex < 0 || enabledCount == 0) return;

            float totalWidth = enabledCount * boxWidth + (enabledCount - 1) * boxSpacing;
            float startX = (Screen.width - totalWidth) / 2f;
            float boxX = startX + enabledIndex * (boxWidth + boxSpacing);
            float guiBoxY = Screen.height - bottomMargin - boxHeight;

            Vector3 mousePos = Input.mousePosition;
            // Input.mousePosition Y is bottom-up, GUI Y is top-down
            // Convert GUI box coords to screen-space (bottom-up) for comparison
            float boxBottomScreenY = Screen.height - (guiBoxY + boxHeight);

            float normalizedX = ((mousePos.x - boxX) / boxWidth) * 2f - 1f;
            float normalizedY = ((mousePos.y - boxBottomScreenY) / boxHeight) * 2f - 1f;

            player.dotPosition.x = Mathf.Clamp(normalizedX, -1f, 1f);
            player.dotPosition.y = Mathf.Clamp(normalizedY, -1f, 1f);
        }

        void OnGUI()
        {
            if (!showUI || players == null || players.Length == 0) return;

            int enabledCount = 0;
            foreach (var player in players)
            {
                if (player != null && player.isEnabled)
                    enabledCount++;
            }

            if (enabledCount == 0) return;

            float totalWidth = enabledCount * boxWidth + (enabledCount - 1) * boxSpacing;
            float startX = (Screen.width - totalWidth) / 2f;
            float boxY = Screen.height - bottomMargin - boxHeight;

            int drawnIndex = 0;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null || !players[i].isEnabled) continue;

                float bx = startX + drawnIndex * (boxWidth + boxSpacing);
                DrawPlayerBox(bx, boxY, players[i], i, true);
                drawnIndex++;
            }
        }

        void DrawPlayerBox(float x, float y, LeanPlayerData player, int playerIndex, bool enabled)
        {
            float labelHeight = 20f;
            float labelY = y - labelHeight - 2f;

            // Label above box
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = 11;
            labelStyle.normal.textColor = enabled ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            string inputName = GetInputTypeName(player.inputType);
            GUI.Label(new Rect(x, labelY, boxWidth, labelHeight), $"P{playerIndex + 1} ({inputName})", labelStyle);

            // Box background (gray fill)
            Color bgColor = enabled ? new Color(0.2f, 0.2f, 0.2f, 0.7f) : new Color(0.15f, 0.15f, 0.15f, 0.4f);
            GUI.color = bgColor;
            GUI.DrawTexture(new Rect(x, y, boxWidth, boxHeight), Texture2D.whiteTexture);

            // White outline (2px border using 4 thin rects)
            Color borderColor = enabled ? new Color(1f, 1f, 1f, 0.8f) : new Color(0.4f, 0.4f, 0.4f, 0.4f);
            GUI.color = borderColor;
            float b = 2f;
            GUI.DrawTexture(new Rect(x, y, boxWidth, b), Texture2D.whiteTexture);             // top
            GUI.DrawTexture(new Rect(x, y + boxHeight - b, boxWidth, b), Texture2D.whiteTexture); // bottom
            GUI.DrawTexture(new Rect(x, y, b, boxHeight), Texture2D.whiteTexture);             // left
            GUI.DrawTexture(new Rect(x + boxWidth - b, y, b, boxHeight), Texture2D.whiteTexture); // right

            // Center crosshair lines (subtle gray)
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            float centerX = x + boxWidth / 2f;
            float centerY = y + boxHeight / 2f;
            GUI.DrawTexture(new Rect(centerX - 0.5f, y + b, 1f, boxHeight - b * 2f), Texture2D.whiteTexture); // vertical
            GUI.DrawTexture(new Rect(x + b, centerY - 0.5f, boxWidth - b * 2f, 1f), Texture2D.whiteTexture); // horizontal

            // Dot (only for enabled players)
            if (enabled)
            {
                // Map dotPosition [-1,1] to pixel position within box
                float dotX = centerX + player.dotPosition.x * (boxWidth / 2f - dotRadius - b) - dotRadius;
                float dotY = centerY - player.dotPosition.y * (boxHeight / 2f - dotRadius - b) - dotRadius; // Y inverted for GUI

                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(dotX, dotY, dotRadius * 2f, dotRadius * 2f), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        string GetInputTypeName(LeanInputType inputType)
        {
            switch (inputType)
            {
                case LeanInputType.WASD: return "WASD";
                case LeanInputType.ArrowKeys: return "Arrows";
                case LeanInputType.Gamepad1: return "Pad1";
                case LeanInputType.Gamepad2: return "Pad2";
                case LeanInputType.Gamepad3: return "Pad3";
                case LeanInputType.Gamepad4: return "Pad4";
                case LeanInputType.Mouse: return "Mouse";
                default: return inputType.ToString();
            }
        }
    }
}
