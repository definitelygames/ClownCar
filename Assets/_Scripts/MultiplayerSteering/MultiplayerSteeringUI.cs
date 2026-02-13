using UnityEngine;

namespace EVP
{
    /// <summary>
    /// OnGUI-based display for multiplayer control assignments.
    /// Shows per-player control panels with key assignments and fill indicators,
    /// plus combined steer/throttle/brake bars at the bottom.
    /// </summary>
    public class MultiplayerSteeringUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The MultiplayerSteeringManager to display. If null, will try to find on this GameObject.")]
        public MultiplayerSteeringManager steeringManager;

        [Header("Display Settings")]
        public bool show = true;
        public KeyCode toggleKey = KeyCode.U;

        [Header("Colors")]
        public Color player1Color = Color.red;
        public Color player2Color = Color.blue;
        public Color player3Color = Color.green;
        public Color player4Color = Color.yellow;
        public Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        // Cached textures
        private Texture2D backgroundTexture;

        void OnEnable()
        {
            if (steeringManager == null)
                steeringManager = GetComponent<MultiplayerSteeringManager>();

            CreateTextures();
        }

        void OnDisable()
        {
            DestroyTextures();
        }

        void CreateTextures()
        {
            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
            backgroundTexture.Apply();
        }

        void DestroyTextures()
        {
            if (backgroundTexture != null)
            {
                Destroy(backgroundTexture);
                backgroundTexture = null;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                show = !show;
        }

        void OnGUI()
        {
            if (!show || steeringManager == null || steeringManager.players == null)
                return;

            int playerCount = steeringManager.players.Length;
            if (playerCount == 0) return;

            // Layout constants
            float playerPanelWidth = 140f;
            float playerPanelSpacing = 10f;
            float panelPadding = 10f;
            float bottomMargin = 20f;
            float barHeight = 60f; // combined indicators area

            // Calculate panel size
            float totalPlayersWidth = playerCount * playerPanelWidth + (playerCount - 1) * playerPanelSpacing;
            float panelWidth = totalPlayersWidth + panelPadding * 2f;
            float playerAreaHeight = 120f;
            float totalHeight = 25f + playerAreaHeight + barHeight + panelPadding * 2f;

            float panelX = (Screen.width - panelWidth) / 2f;
            float panelY = Screen.height - bottomMargin - totalHeight;

            // Draw background
            Rect panelRect = new Rect(panelX, panelY, panelWidth, totalHeight);
            GUI.DrawTexture(panelRect, backgroundTexture);

            // Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = Color.white;
            titleStyle.fontSize = 12;
            GUI.Label(new Rect(panelX, panelY + 5f, panelWidth, 20f),
                "Multiplayer Controls (Press 1-4 to toggle)", titleStyle);

            // Draw each player panel
            float playersStartX = panelX + panelPadding;
            float playersStartY = panelY + 28f;

            for (int i = 0; i < playerCount; i++)
            {
                MultiplayerSteeringPlayer player = steeringManager.players[i];
                if (player == null) continue;

                float px = playersStartX + i * (playerPanelWidth + playerPanelSpacing);
                DrawPlayerPanel(px, playersStartY, playerPanelWidth, playerAreaHeight, player, i);
            }

            // Draw combined indicators at bottom
            float barsY = playersStartY + playerAreaHeight + 5f;
            float barsWidth = totalPlayersWidth;
            DrawCombinedBars(playersStartX, barsY, barsWidth);
        }

        void DrawPlayerPanel(float x, float y, float width, float height, MultiplayerSteeringPlayer player, int playerIndex)
        {
            Color playerColor = GetPlayerColor(playerIndex);
            bool enabled = player.isEnabled;

            // Player panel background
            GUI.color = enabled ? new Color(playerColor.r, playerColor.g, playerColor.b, 0.15f) : new Color(0.2f, 0.2f, 0.2f, 0.3f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Border top
            GUI.color = enabled ? playerColor : disabledColor;
            GUI.DrawTexture(new Rect(x, y, width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Player label
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = enabled ? playerColor : disabledColor;
            labelStyle.fontSize = 12;
            labelStyle.fontStyle = FontStyle.Bold;

            string status = enabled ? "ON" : "OFF";
            GUI.Label(new Rect(x, y + 4f, width, 18f), $"P{playerIndex + 1} [{status}]", labelStyle);

            if (!enabled)
            {
                GUIStyle offStyle = new GUIStyle(GUI.skin.label);
                offStyle.alignment = TextAnchor.MiddleCenter;
                offStyle.normal.textColor = disabledColor;
                offStyle.fontSize = 10;
                GUI.Label(new Rect(x, y + 45f, width, 20f), "No controls", offStyle);
                return;
            }

            // Draw each assigned control
            GUIStyle controlStyle = new GUIStyle(GUI.skin.label);
            controlStyle.fontSize = 10;
            controlStyle.normal.textColor = Color.white;

            float cy = y + 24f;
            float rowHeight = 22f;
            float barWidthInner = width - 50f;

            foreach (var binding in player.assignedControls)
            {
                string keyLabel = FormatKeyCode(binding.key);
                string actionLabel = FormatAction(binding.action);

                // Key label
                controlStyle.alignment = TextAnchor.MiddleLeft;
                controlStyle.fontStyle = FontStyle.Bold;
                GUI.Label(new Rect(x + 4f, cy, 30f, rowHeight), keyLabel, controlStyle);

                // Action name
                controlStyle.fontStyle = FontStyle.Normal;
                controlStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                GUI.Label(new Rect(x + 34f, cy, 60f, rowHeight), actionLabel, controlStyle);
                controlStyle.normal.textColor = Color.white;

                // Fill bar background
                float barX = x + 4f;
                float barY = cy + rowHeight - 6f;
                float barW = width - 8f;
                float barH = 3f;

                GUI.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);

                // Fill bar value
                Color actionColor = GetActionColor(binding.action);
                GUI.color = actionColor;
                GUI.DrawTexture(new Rect(barX, barY, barW * binding.currentValue, barH), Texture2D.whiteTexture);
                GUI.color = Color.white;

                cy += rowHeight;
            }
        }

        void DrawCombinedBars(float x, float y, float width)
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

            // Center line
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            GUI.DrawTexture(new Rect(barX + barW / 2f - 1f, y + 4f, 2f, barH + 4f), Texture2D.whiteTexture);

            // Steer marker
            float steerValue = steeringManager.CombinedSteer;
            float markerX = barX + barW / 2f + steerValue * (barW / 2f) - 3f;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(markerX, y + 4f, 6f, barH + 4f), Texture2D.whiteTexture);

            // Throttle bar
            float ty = y + rowH;
            GUI.color = Color.white;
            GUI.Label(new Rect(x, ty, 55f, rowH), "Throttle", labelStyle);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(barX, ty + 6f, barW, barH), Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.9f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(barX, ty + 6f, barW * steeringManager.CombinedThrottle, barH), Texture2D.whiteTexture);

            // Brake bar
            float by = y + rowH * 2f;
            GUI.color = Color.white;
            GUI.Label(new Rect(x, by, 55f, rowH), "Brake", labelStyle);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(barX, by + 6f, barW, barH), Texture2D.whiteTexture);
            GUI.color = new Color(0.9f, 0.2f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(barX, by + 6f, barW * steeringManager.CombinedBrake, barH), Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        string FormatKeyCode(KeyCode key)
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

        string FormatAction(VehicleControlAction action)
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

        Color GetActionColor(VehicleControlAction action)
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

        Color GetPlayerColor(int index)
        {
            switch (index)
            {
                case 0: return player1Color;
                case 1: return player2Color;
                case 2: return player3Color;
                case 3: return player4Color;
                default: return Color.white;
            }
        }
    }
}
