using UnityEngine;
using UnityEngine.InputSystem;

namespace EVP
{
    /// <summary>
    /// Lean-based multiplayer steering: 2D dot positions control steering direction and apply
    /// torque physics. Supports keyboard, gamepad, and mouse input per player.
    /// Includes synchronized "pop" mechanic when all players hit the edge simultaneously.
    /// Ported from MultiplayerLeanSteering.
    /// </summary>
    public class LeanMultiplayerSteering : SteeringMethod
    {
        private readonly LeanSteeringConfig config;

        // Per-player lean data
        private LeanPlayerData[] players;

        // Pop state
        private bool popFired;

        public LeanMultiplayerSteering(LeanSteeringConfig config)
        {
            this.config = config;
        }

        public override void Initialize(VehicleMultiplayerSteering manager, VehicleController vehicle)
        {
            base.Initialize(manager, vehicle);
            InitializePlayers();
        }

        public override void Activate()
        {
            // Reset all dots to center
            foreach (var player in players)
            {
                if (player != null)
                    player.dotPosition = Vector2.zero;
            }
            popFired = false;
        }

        public override void Deactivate()
        {
            // Nothing to clean up
        }

        public override void OnPlayersChanged()
        {
            SyncPlayerEnabled();
        }

        private void InitializePlayers()
        {
            players = new LeanPlayerData[4];
            for (int i = 0; i < 4; i++)
            {
                players[i] = new LeanPlayerData
                {
                    isEnabled = manager.PlayerEnabled[i],
                    inputType = config.GetDefaultInputType(i)
                };
            }
        }

        public override void ReadInput(float deltaTime)
        {
            SyncPlayerEnabled();
            UpdateDotPositions(deltaTime);
        }

        public override VehicleInput GetVehicleInput(float fixedDeltaTime)
        {
            float combinedSteer = 0f;
            int enabledCount = 0;

            foreach (var player in players)
            {
                if (player != null && player.isEnabled)
                {
                    combinedSteer += player.dotPosition.x * config.steeringMultiplier;
                    enabledCount++;
                }
            }

            if (enabledCount > 0)
                combinedSteer /= enabledCount;

            return new VehicleInput
            {
                steer = config.leanAffectsSteering ? Mathf.Clamp(combinedSteer, -1f, 1f) : 0f,
                throttle = 0f,
                brake = 0f
            };
        }

        public override void ApplyPhysics(Rigidbody rb, float fixedDeltaTime)
        {
            if (rb == null) return;

            float combinedLeanX = 0f;
            float combinedLeanY = 0f;
            int enabledCount = 0;

            foreach (var player in players)
            {
                if (player != null && player.isEnabled)
                {
                    combinedLeanX += player.dotPosition.x;
                    combinedLeanY += player.dotPosition.y;
                    enabledCount++;
                }
            }

            if (enabledCount > 0)
            {
                combinedLeanX /= enabledCount;
                combinedLeanY /= enabledCount;
            }

            // Apply torques
            if (config.leanAffectsLateralTorque)
                rb.AddTorque(rb.transform.forward * combinedLeanX * -config.leanTorqueLateral, ForceMode.Force);

            rb.AddTorque(rb.transform.right * combinedLeanY * config.leanTorqueLongitudinal, ForceMode.Force);

            // Pop detection
            UpdatePopWindows(rb, enabledCount);
        }

        private void UpdatePopWindows(Rigidbody rb, int enabledCount)
        {
            if (enabledCount == 0) return;

            float dt = Time.fixedDeltaTime;
            bool allInPopWindow = true;
            bool anyAtEdge = false;
            Vector2 popDirection = Vector2.zero;

            foreach (var player in players)
            {
                if (player == null || !player.isEnabled) continue;

                bool atEdge = player.dotPosition.magnitude >= config.popEdgeThreshold;

                if (atEdge && !player.wasAtEdge)
                    player.popTimeRemaining = config.popWindow;

                player.wasAtEdge = atEdge;

                if (player.popTimeRemaining > 0f)
                    player.popTimeRemaining -= dt;

                bool inPopWindow = player.popTimeRemaining > 0f;
                if (!inPopWindow)
                    allInPopWindow = false;

                if (atEdge)
                    anyAtEdge = true;

                popDirection += player.dotPosition;
            }

            popDirection /= enabledCount;

            if (!anyAtEdge)
                popFired = false;

            if (allInPopWindow && !popFired)
            {
                popFired = true;
                Vector2 dir = popDirection.normalized;
                rb.AddTorque(rb.transform.forward * dir.x * -config.popForce, ForceMode.Impulse);
                rb.AddTorque(rb.transform.right * dir.y * config.popForce, ForceMode.Impulse);
            }
        }

        // --- Input handling ---

        private void UpdateDotPositions(float dt)
        {
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

        private void UpdateKeyboardDot(LeanPlayerData player, KeyCode left, KeyCode right, KeyCode down, KeyCode up, float dt)
        {
            float hInput = 0f;
            float vInput = 0f;

            if (Input.GetKey(left)) hInput -= 1f;
            if (Input.GetKey(right)) hInput += 1f;
            if (Input.GetKey(down)) vInput -= 1f;
            if (Input.GetKey(up)) vInput += 1f;

            bool hasHInput = Mathf.Abs(hInput) > 0.01f;
            bool hasVInput = Mathf.Abs(vInput) > 0.01f;

            if (hasHInput)
                player.dotPosition.x = Mathf.MoveTowards(player.dotPosition.x, hInput, config.keyboardMoveSpeed * dt);
            else
                player.dotPosition.x = Mathf.MoveTowards(player.dotPosition.x, 0f, config.keyboardReturnSpeed * dt);

            if (hasVInput)
                player.dotPosition.y = Mathf.MoveTowards(player.dotPosition.y, vInput, config.keyboardMoveSpeed * dt);
            else
                player.dotPosition.y = Mathf.MoveTowards(player.dotPosition.y, 0f, config.keyboardReturnSpeed * dt);

            player.dotPosition.x = Mathf.Clamp(player.dotPosition.x, -1f, 1f);
            player.dotPosition.y = Mathf.Clamp(player.dotPosition.y, -1f, 1f);
        }

        private void UpdateGamepadDot(LeanPlayerData player, int gamepadIndex)
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

        private void UpdateMouseDot(LeanPlayerData player)
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

            float totalWidth = enabledCount * config.boxWidth + (enabledCount - 1) * config.boxSpacing;
            float startX = (Screen.width - totalWidth) / 2f;
            float boxX = startX + enabledIndex * (config.boxWidth + config.boxSpacing);
            float guiBoxY = Screen.height - config.bottomMargin - config.boxHeight;

            Vector3 mousePos = Input.mousePosition;
            float boxBottomScreenY = Screen.height - (guiBoxY + config.boxHeight);

            float normalizedX = ((mousePos.x - boxX) / config.boxWidth) * 2f - 1f;
            float normalizedY = ((mousePos.y - boxBottomScreenY) / config.boxHeight) * 2f - 1f;

            player.dotPosition.x = Mathf.Clamp(normalizedX, -1f, 1f);
            player.dotPosition.y = Mathf.Clamp(normalizedY, -1f, 1f);
        }

        private void SyncPlayerEnabled()
        {
            for (int i = 0; i < 4; i++)
                players[i].isEnabled = manager.PlayerEnabled[i];
        }

        // --- UI ---

        public override void DrawGUI()
        {
            if (players == null || players.Length == 0) return;

            int enabledCount = 0;
            foreach (var player in players)
            {
                if (player != null && player.isEnabled)
                    enabledCount++;
            }

            if (enabledCount == 0) return;

            float totalWidth = enabledCount * config.boxWidth + (enabledCount - 1) * config.boxSpacing;
            float startX = (Screen.width - totalWidth) / 2f;
            float boxY = Screen.height - config.bottomMargin - config.boxHeight;

            int drawnIndex = 0;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null || !players[i].isEnabled) continue;

                float bx = startX + drawnIndex * (config.boxWidth + config.boxSpacing);
                DrawPlayerBox(bx, boxY, players[i], i);
                drawnIndex++;
            }
        }

        private void DrawPlayerBox(float x, float y, LeanPlayerData player, int playerIndex)
        {
            float labelHeight = 20f;
            float labelY = y - labelHeight - 2f;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = 11;
            labelStyle.normal.textColor = Color.white;
            string inputName = GetInputTypeName(player.inputType);
            GUI.Label(new Rect(x, labelY, config.boxWidth, labelHeight), $"P{playerIndex + 1} ({inputName})", labelStyle);

            // Box background
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            GUI.DrawTexture(new Rect(x, y, config.boxWidth, config.boxHeight), Texture2D.whiteTexture);

            // White outline
            Color borderColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.color = borderColor;
            float b = 2f;
            GUI.DrawTexture(new Rect(x, y, config.boxWidth, b), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y + config.boxHeight - b, config.boxWidth, b), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y, b, config.boxHeight), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + config.boxWidth - b, y, b, config.boxHeight), Texture2D.whiteTexture);

            // Center crosshair
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            float centerX = x + config.boxWidth / 2f;
            float centerY = y + config.boxHeight / 2f;
            GUI.DrawTexture(new Rect(centerX - 0.5f, y + b, 1f, config.boxHeight - b * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + b, centerY - 0.5f, config.boxWidth - b * 2f, 1f), Texture2D.whiteTexture);

            // Dot
            float dotX = centerX + player.dotPosition.x * (config.boxWidth / 2f - config.dotRadius - b) - config.dotRadius;
            float dotY = centerY - player.dotPosition.y * (config.boxHeight / 2f - config.dotRadius - b) - config.dotRadius;

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(dotX, dotY, config.dotRadius * 2f, config.dotRadius * 2f), Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        private static string GetInputTypeName(LeanInputType inputType)
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
