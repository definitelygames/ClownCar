using UnityEngine;

namespace EVP
{
    /// <summary>
    /// OnGUI-based display for multiplayer steering wheels.
    /// Shows a horizontal row of steering wheel indicators at the bottom of the screen.
    /// </summary>
    public class MultiplayerSteeringUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The MultiplayerSteeringManager to display. If null, will try to find on this GameObject.")]
        public MultiplayerSteeringManager steeringManager;

        [Header("Display Settings")]
        public bool show = true;
        public KeyCode toggleKey = KeyCode.U;

        [Header("Wheel Appearance")]
        [Tooltip("Size of each steering wheel indicator")]
        public float wheelSize = 80f;
        [Tooltip("Spacing between wheels")]
        public float wheelSpacing = 20f;
        [Tooltip("Distance from bottom of screen")]
        public float bottomMargin = 30f;
        [Tooltip("Maximum rotation angle in degrees")]
        public float maxRotationAngle = 90f;

        [Header("Colors")]
        public Color player1Color = Color.red;
        public Color player2Color = Color.blue;
        public Color player3Color = Color.green;
        public Color player4Color = Color.yellow;
        public Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        // Cached textures
        private Texture2D wheelTexture;
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
            // Create a simple wheel texture (circle with spokes)
            int size = 64;
            wheelTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
            backgroundTexture.Apply();

            float center = size / 2f;
            float radius = size / 2f - 2f;
            float innerRadius = radius * 0.3f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    Color pixel = Color.clear;

                    // Outer ring
                    if (dist >= radius - 4f && dist <= radius)
                    {
                        pixel = Color.white;
                    }
                    // Center hub
                    else if (dist <= innerRadius)
                    {
                        pixel = Color.white;
                    }
                    // Spokes (horizontal and vertical)
                    else if (dist > innerRadius && dist < radius - 4f)
                    {
                        // Horizontal spoke
                        if (Mathf.Abs(dy) < 3f)
                            pixel = Color.white;
                        // Vertical spoke
                        else if (Mathf.Abs(dx) < 3f)
                            pixel = Color.white;
                    }

                    wheelTexture.SetPixel(x, y, pixel);
                }
            }

            wheelTexture.Apply();
        }

        void DestroyTextures()
        {
            if (wheelTexture != null)
            {
                Destroy(wheelTexture);
                wheelTexture = null;
            }
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

            // Calculate total width and starting position
            float totalWidth = playerCount * wheelSize + (playerCount - 1) * wheelSpacing;
            float startX = (Screen.width - totalWidth) / 2f;
            float y = Screen.height - bottomMargin - wheelSize;

            // Draw background panel
            float panelPadding = 15f;
            Rect panelRect = new Rect(
                startX - panelPadding,
                y - panelPadding - 20f,
                totalWidth + panelPadding * 2f,
                wheelSize + panelPadding * 2f + 25f
            );
            GUI.DrawTexture(panelRect, backgroundTexture);

            // Draw title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = Color.white;
            titleStyle.fontSize = 12;
            GUI.Label(new Rect(panelRect.x, panelRect.y + 5f, panelRect.width, 20f),
                "Multiplayer Steering (Press 1-4 to toggle)", titleStyle);

            // Draw each player's wheel
            for (int i = 0; i < playerCount; i++)
            {
                MultiplayerSteeringPlayer player = steeringManager.players[i];
                if (player == null) continue;

                float x = startX + i * (wheelSize + wheelSpacing);
                DrawPlayerWheel(x, y, player, i);
            }

            // Draw combined steering indicator
            DrawCombinedIndicator(panelRect);
        }

        void DrawPlayerWheel(float x, float y, MultiplayerSteeringPlayer player, int playerIndex)
        {
            Color wheelColor = GetPlayerColor(playerIndex);

            if (!player.isEnabled)
            {
                wheelColor = disabledColor;
            }

            // Calculate rotation based on player's current steer
            float rotation = -player.currentSteer * maxRotationAngle;

            // Save current matrix
            Matrix4x4 savedMatrix = GUI.matrix;

            // Set up rotation around wheel center
            Vector2 pivotPoint = new Vector2(x + wheelSize / 2f, y + wheelSize / 2f);
            GUIUtility.RotateAroundPivot(rotation, pivotPoint);

            // Draw the wheel with color tint
            GUI.color = wheelColor;
            GUI.DrawTexture(new Rect(x, y, wheelSize, wheelSize), wheelTexture);
            GUI.color = Color.white;

            // Restore matrix
            GUI.matrix = savedMatrix;

            // Draw player label below wheel
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = player.isEnabled ? wheelColor : disabledColor;
            labelStyle.fontSize = 11;
            labelStyle.fontStyle = FontStyle.Bold;

            string status = player.isEnabled ? "ON" : "OFF";
            string label = $"P{playerIndex + 1} [{status}]";
            GUI.Label(new Rect(x, y + wheelSize + 2f, wheelSize, 20f), label, labelStyle);
        }

        void DrawCombinedIndicator(Rect panelRect)
        {
            // Draw a small indicator showing combined steering
            float indicatorWidth = panelRect.width - 30f;
            float indicatorHeight = 6f;
            float indicatorX = panelRect.x + 15f;
            float indicatorY = panelRect.y + panelRect.height - 12f;

            // Background bar
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(indicatorX, indicatorY, indicatorWidth, indicatorHeight), Texture2D.whiteTexture);

            // Center line
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            GUI.DrawTexture(new Rect(indicatorX + indicatorWidth / 2f - 1f, indicatorY - 2f, 2f, indicatorHeight + 4f), Texture2D.whiteTexture);

            // Combined steer indicator
            float steerValue = steeringManager.CombinedSteer;
            float markerX = indicatorX + indicatorWidth / 2f + steerValue * (indicatorWidth / 2f) - 3f;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(markerX, indicatorY - 2f, 6f, indicatorHeight + 4f), Texture2D.whiteTexture);

            GUI.color = Color.white;
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
