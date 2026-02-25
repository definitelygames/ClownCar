using UnityEngine;

namespace EVP
{
    /// <summary>
    /// Displays vehicle health bar and per-player health bars in the top-right corner.
    /// Follows the SpeedPanel pattern using OnGUI/GUIStyle rendering.
    /// </summary>
    public class HealthPanel : HUDPanel
    {
        Texture2D backgroundTexture;
        Texture2D barBackgroundTexture;
        GUIStyle labelStyle;
        GUIStyle ejectedStyle;

        VehicleDamageReceiver damageReceiver;
        VehicleMultiplayerSteering steeringManager;

        const float panelWidth = 220f;
        const float rowHeight = 22f;
        const float padding = 10f;
        const float margin = 20f;
        const float barX = 70f;
        const float barHeight = 14f;

        public override void Initialize(VehicleHUD hud)
        {
            base.Initialize(hud);
            damageReceiver = hud.damageReceiver;
            steeringManager = hud.steeringManager;
        }

        public override void OnActivate()
        {
            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            backgroundTexture.Apply();

            barBackgroundTexture = new Texture2D(1, 1);
            barBackgroundTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            barBackgroundTexture.Apply();
        }

        public override void OnDeactivate()
        {
            if (backgroundTexture != null) { Object.Destroy(backgroundTexture); backgroundTexture = null; }
            if (barBackgroundTexture != null) { Object.Destroy(barBackgroundTexture); barBackgroundTexture = null; }
        }

        public override void DrawPanel()
        {
            if (damageReceiver == null) return;

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Bold
                };
                labelStyle.normal.textColor = Color.white;
            }

            if (ejectedStyle == null)
            {
                ejectedStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                ejectedStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);
            }

            // Count visible rows: vehicle + each player that is enabled or ejected
            int visiblePlayers = 0;
            if (steeringManager != null && damageReceiver.playerHealths != null)
            {
                for (int i = 0; i < damageReceiver.playerHealths.Length; i++)
                {
                    if (damageReceiver.playerHealths[i] == null) continue;
                    bool enabled = i < steeringManager.PlayerEnabled.Length && steeringManager.PlayerEnabled[i];
                    bool ejected = damageReceiver.playerHealths[i].IsEjected;
                    if (enabled || ejected)
                        visiblePlayers++;
                }
            }

            int totalRows = 1 + visiblePlayers; // vehicle + players
            float panelHeight = padding * 2f + totalRows * rowHeight;

            float x = Screen.width - margin - panelWidth;
            float y = margin;

            // Background
            if (backgroundTexture != null)
                GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), backgroundTexture);

            float rowY = y + padding;

            // Vehicle health bar
            DrawHealthBar(x + padding, rowY, "Vehicle", damageReceiver.VehicleHealthNormalized, false);
            rowY += rowHeight;

            // Per-player bars
            if (steeringManager != null && damageReceiver.playerHealths != null)
            {
                for (int i = 0; i < damageReceiver.playerHealths.Length; i++)
                {
                    var ph = damageReceiver.playerHealths[i];
                    if (ph == null) continue;

                    bool enabled = i < steeringManager.PlayerEnabled.Length && steeringManager.PlayerEnabled[i];
                    bool ejected = ph.IsEjected;
                    if (!enabled && !ejected) continue;

                    string label = "P" + (i + 1);

                    if (ejected)
                    {
                        GUI.Label(new Rect(x + padding, rowY, barX - 5f, rowHeight), label, labelStyle);
                        GUI.Label(new Rect(x + padding + barX, rowY, panelWidth - barX - padding * 2f, rowHeight), "EJECTED", ejectedStyle);
                    }
                    else
                    {
                        DrawHealthBar(x + padding, rowY, label, ph.HealthNormalized, false);
                    }

                    rowY += rowHeight;
                }
            }
        }

        void DrawHealthBar(float x, float y, string label, float normalized, bool dead)
        {
            float barWidth = panelWidth - barX - padding * 2f;
            float barY = y + (rowHeight - barHeight) * 0.5f;

            // Label
            GUI.Label(new Rect(x, y, barX - 5f, rowHeight), label, labelStyle);

            // Bar background
            if (barBackgroundTexture != null)
                GUI.DrawTexture(new Rect(x + barX, barY, barWidth, barHeight), barBackgroundTexture);

            // Bar fill with color gradient
            if (normalized > 0f)
            {
                Color barColor = HealthColor(normalized);
                Texture2D fillTex = new Texture2D(1, 1);
                fillTex.SetPixel(0, 0, barColor);
                fillTex.Apply();

                GUI.DrawTexture(new Rect(x + barX, barY, barWidth * normalized, barHeight), fillTex);

                Object.Destroy(fillTex);
            }
        }

        static Color HealthColor(float normalized)
        {
            if (normalized > 0.5f)
                return Color.Lerp(Color.yellow, Color.green, (normalized - 0.5f) * 2f);
            else
                return Color.Lerp(Color.red, Color.yellow, normalized * 2f);
        }
    }
}
