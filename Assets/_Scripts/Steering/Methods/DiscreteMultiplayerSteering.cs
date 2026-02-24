using System.Collections.Generic;
using UnityEngine;

namespace EVP
{
    /// <summary>
    /// Discrete multiplayer steering: distributes 4 discrete actions (steer left/right, accelerate, brake)
    /// among active players randomly. Each player controls a subset of actions via assigned keys.
    /// Ported from MultiplayerSteeringManager + MultiplayerSteeringUI.
    /// </summary>
    public class DiscreteMultiplayerSteering : SteeringMethod
    {
        private readonly DiscreteSteeringConfig config;

        // Per-player state
        private MultiplayerSteeringPlayer[] players;

        // Combined output values (exposed for UI)
        public float CombinedSteer { get; private set; }
        public float CombinedThrottle { get; private set; }
        public float CombinedBrake { get; private set; }

        // UI texture
        private Texture2D backgroundTexture;

        public DiscreteMultiplayerSteering(DiscreteSteeringConfig config)
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
            CreateTextures();
            DistributeControls();
        }

        public override void Deactivate()
        {
            DestroyTextures();
            CombinedSteer = 0f;
            CombinedThrottle = 0f;
            CombinedBrake = 0f;
        }

        public override void OnPlayersChanged()
        {
            DistributeControls();
        }

        private void InitializePlayers()
        {
            players = new MultiplayerSteeringPlayer[4];
            for (int i = 0; i < 4; i++)
            {
                players[i] = new MultiplayerSteeringPlayer
                {
                    playerIndex = i,
                    availableKeys = config.GetPlayerKeys(i),
                    rampUpSpeed = config.rampUpSpeed,
                    rampDownSpeed = config.rampDownSpeed,
                    isEnabled = manager.PlayerEnabled[i]
                };
            }
        }

        public override void ReadInput(float deltaTime)
        {
            SyncPlayerEnabled();

            foreach (var player in players)
                player.ReadInput();
        }

        public override VehicleInput GetVehicleInput(float fixedDeltaTime)
        {
            // Update analog ramping
            foreach (var player in players)
                player.UpdateRamping(fixedDeltaTime);

            // Gather control values from all players
            float steerLeft = 0f;
            float steerRight = 0f;
            float accelerate = 0f;
            float brake = 0f;

            foreach (var player in players)
            {
                steerLeft = Mathf.Max(steerLeft, player.GetControlValue(VehicleControlAction.SteerLeft));
                steerRight = Mathf.Max(steerRight, player.GetControlValue(VehicleControlAction.SteerRight));
                accelerate = Mathf.Max(accelerate, player.GetControlValue(VehicleControlAction.Accelerate));
                brake = Mathf.Max(brake, player.GetControlValue(VehicleControlAction.Brake));
            }

            // Translate forward/reverse (continuous mode)
            float throttleInput = 0f;
            float brakeInput = 0f;
            float minSpeed = 0.1f;
            float minInput = 0.1f;

            if (vehicle.speed > minSpeed)
            {
                throttleInput = accelerate;
                brakeInput = brake;
            }
            else
            {
                if (brake > minInput)
                {
                    throttleInput = -brake;
                    brakeInput = 0f;
                }
                else if (accelerate > minInput)
                {
                    if (vehicle.speed < -minSpeed)
                    {
                        throttleInput = 0f;
                        brakeInput = accelerate;
                    }
                    else
                    {
                        throttleInput = accelerate;
                        brakeInput = 0f;
                    }
                }
            }

            CombinedSteer = Mathf.Clamp(-steerLeft + steerRight, -1f, 1f);
            CombinedThrottle = throttleInput;
            CombinedBrake = brakeInput;

            return new VehicleInput
            {
                steer = CombinedSteer,
                throttle = CombinedThrottle,
                brake = CombinedBrake
            };
        }

        /// <summary>
        /// Randomly distribute the 4 vehicle actions among enabled players.
        /// Uses Fisher-Yates shuffle then round-robin dealing.
        /// </summary>
        public void DistributeControls()
        {
            // Clear all assignments
            foreach (var player in players)
                player.assignedControls.Clear();

            // Collect enabled players
            var enabledPlayers = new List<MultiplayerSteeringPlayer>();
            foreach (var player in players)
            {
                if (player.isEnabled)
                    enabledPlayers.Add(player);
            }

            if (enabledPlayers.Count == 0) return;

            // Shuffle actions (Fisher-Yates)
            var actions = new VehicleControlAction[]
            {
                VehicleControlAction.SteerLeft,
                VehicleControlAction.SteerRight,
                VehicleControlAction.Accelerate,
                VehicleControlAction.Brake
            };

            for (int i = actions.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = actions[i];
                actions[i] = actions[j];
                actions[j] = temp;
            }

            // Track key indices per player
            var keyIndices = new Dictionary<MultiplayerSteeringPlayer, int>();
            foreach (var player in enabledPlayers)
                keyIndices[player] = 0;

            // Deal round-robin
            for (int i = 0; i < actions.Length; i++)
            {
                var player = enabledPlayers[i % enabledPlayers.Count];
                int keyIdx = keyIndices[player];

                var binding = new ControlBinding
                {
                    action = actions[i],
                    key = player.availableKeys[keyIdx],
                    currentValue = 0f,
                    targetValue = 0f
                };

                player.assignedControls.Add(binding);
                keyIndices[player] = keyIdx + 1;
            }
        }

        private void SyncPlayerEnabled()
        {
            for (int i = 0; i < 4; i++)
                players[i].isEnabled = manager.PlayerEnabled[i];
        }

        // --- UI (ported from MultiplayerSteeringUI) ---

        private void CreateTextures()
        {
            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
            backgroundTexture.Apply();
        }

        private void DestroyTextures()
        {
            if (backgroundTexture != null)
            {
                Object.Destroy(backgroundTexture);
                backgroundTexture = null;
            }
        }

        public override void DrawGUI()
        {
            if (players == null) return;

            int playerCount = players.Length;
            if (playerCount == 0) return;

            float playerPanelWidth = 140f;
            float playerPanelSpacing = 10f;
            float panelPadding = 10f;
            float bottomMargin = 20f;
            float barHeight = 60f;

            float totalPlayersWidth = playerCount * playerPanelWidth + (playerCount - 1) * playerPanelSpacing;
            float panelWidth = totalPlayersWidth + panelPadding * 2f;
            float playerAreaHeight = 120f;
            float totalHeight = 25f + playerAreaHeight + barHeight + panelPadding * 2f;

            float panelX = (Screen.width - panelWidth) / 2f;
            float panelY = Screen.height - bottomMargin - totalHeight;

            // Background
            Rect panelRect = new Rect(panelX, panelY, panelWidth, totalHeight);
            if (backgroundTexture != null)
                GUI.DrawTexture(panelRect, backgroundTexture);

            // Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = Color.white;
            titleStyle.fontSize = 12;
            GUI.Label(new Rect(panelX, panelY + 5f, panelWidth, 20f),
                "Multiplayer Controls (Press 1-4 to toggle)", titleStyle);

            // Player panels
            float playersStartX = panelX + panelPadding;
            float playersStartY = panelY + 28f;

            for (int i = 0; i < playerCount; i++)
            {
                float px = playersStartX + i * (playerPanelWidth + playerPanelSpacing);
                DrawPlayerPanel(px, playersStartY, playerPanelWidth, playerAreaHeight, players[i], i);
            }

            // Combined bars
            float barsY = playersStartY + playerAreaHeight + 5f;
            DrawCombinedBars(playersStartX, barsY, totalPlayersWidth);
        }

        private void DrawPlayerPanel(float x, float y, float width, float height, MultiplayerSteeringPlayer player, int playerIndex)
        {
            Color playerColor = config.GetPlayerColor(playerIndex);
            bool enabled = player.isEnabled;

            // Panel background
            GUI.color = enabled ? new Color(playerColor.r, playerColor.g, playerColor.b, 0.15f) : new Color(0.2f, 0.2f, 0.2f, 0.3f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Border top
            GUI.color = enabled ? playerColor : config.disabledColor;
            GUI.DrawTexture(new Rect(x, y, width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Player label
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = enabled ? playerColor : config.disabledColor;
            labelStyle.fontSize = 12;
            labelStyle.fontStyle = FontStyle.Bold;

            string status = enabled ? "ON" : "OFF";
            GUI.Label(new Rect(x, y + 4f, width, 18f), $"P{playerIndex + 1} [{status}]", labelStyle);

            if (!enabled)
            {
                GUIStyle offStyle = new GUIStyle(GUI.skin.label);
                offStyle.alignment = TextAnchor.MiddleCenter;
                offStyle.normal.textColor = config.disabledColor;
                offStyle.fontSize = 10;
                GUI.Label(new Rect(x, y + 45f, width, 20f), "No controls", offStyle);
                return;
            }

            // Assigned controls
            GUIStyle controlStyle = new GUIStyle(GUI.skin.label);
            controlStyle.fontSize = 10;
            controlStyle.normal.textColor = Color.white;

            float cy = y + 24f;
            float rowHeight = 22f;

            foreach (var binding in player.assignedControls)
            {
                string keyLabel = FormatKeyCode(binding.key);
                string actionLabel = FormatAction(binding.action);

                controlStyle.alignment = TextAnchor.MiddleLeft;
                controlStyle.fontStyle = FontStyle.Bold;
                GUI.Label(new Rect(x + 4f, cy, 30f, rowHeight), keyLabel, controlStyle);

                controlStyle.fontStyle = FontStyle.Normal;
                controlStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                GUI.Label(new Rect(x + 34f, cy, 60f, rowHeight), actionLabel, controlStyle);
                controlStyle.normal.textColor = Color.white;

                // Fill bar
                float barX = x + 4f;
                float barY = cy + rowHeight - 6f;
                float barW = width - 8f;
                float barH = 3f;

                GUI.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);

                Color actionColor = GetActionColor(binding.action);
                GUI.color = actionColor;
                GUI.DrawTexture(new Rect(barX, barY, barW * binding.currentValue, barH), Texture2D.whiteTexture);
                GUI.color = Color.white;

                cy += rowHeight;
            }
        }

        private void DrawCombinedBars(float x, float y, float width)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 10;
            labelStyle.normal.textColor = Color.white;
            labelStyle.alignment = TextAnchor.MiddleLeft;

            float barX = x + 55f;
            float barW = width - 60f;
            float barH = 6f;
            float rowH = 18f;

            // Steer bar
            GUI.Label(new Rect(x, y, 55f, rowH), "Steer", labelStyle);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(barX, y + 6f, barW, barH), Texture2D.whiteTexture);

            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            GUI.DrawTexture(new Rect(barX + barW / 2f - 1f, y + 4f, 2f, barH + 4f), Texture2D.whiteTexture);

            float markerX = barX + barW / 2f + CombinedSteer * (barW / 2f) - 3f;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(markerX, y + 4f, 6f, barH + 4f), Texture2D.whiteTexture);

            // Throttle bar
            float ty = y + rowH;
            GUI.color = Color.white;
            GUI.Label(new Rect(x, ty, 55f, rowH), "Throttle", labelStyle);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(barX, ty + 6f, barW, barH), Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.9f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(barX, ty + 6f, barW * CombinedThrottle, barH), Texture2D.whiteTexture);

            // Brake bar
            float by = y + rowH * 2f;
            GUI.color = Color.white;
            GUI.Label(new Rect(x, by, 55f, rowH), "Brake", labelStyle);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(barX, by + 6f, barW, barH), Texture2D.whiteTexture);
            GUI.color = new Color(0.9f, 0.2f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(barX, by + 6f, barW * CombinedBrake, barH), Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        private static string FormatKeyCode(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftArrow: return "<-";
                case KeyCode.RightArrow: return "->";
                case KeyCode.UpArrow: return "Up";
                case KeyCode.DownArrow: return "Dn";
                case KeyCode.Keypad4: return "N4";
                case KeyCode.Keypad5: return "N5";
                case KeyCode.Keypad6: return "N6";
                case KeyCode.Keypad8: return "N8";
                default: return key.ToString().ToUpper();
            }
        }

        private static string FormatAction(VehicleControlAction action)
        {
            switch (action)
            {
                case VehicleControlAction.SteerLeft: return "Left";
                case VehicleControlAction.SteerRight: return "Right";
                case VehicleControlAction.Accelerate: return "Gas";
                case VehicleControlAction.Brake: return "Brake";
                default: return action.ToString();
            }
        }

        private static Color GetActionColor(VehicleControlAction action)
        {
            switch (action)
            {
                case VehicleControlAction.SteerLeft: return new Color(1f, 0.8f, 0.2f, 1f);
                case VehicleControlAction.SteerRight: return new Color(1f, 0.8f, 0.2f, 1f);
                case VehicleControlAction.Accelerate: return new Color(0.2f, 0.9f, 0.2f, 1f);
                case VehicleControlAction.Brake: return new Color(0.9f, 0.2f, 0.2f, 1f);
                default: return Color.white;
            }
        }
    }
}
