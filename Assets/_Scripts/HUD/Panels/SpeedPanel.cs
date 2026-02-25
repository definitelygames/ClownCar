using UnityEngine;

namespace EVP
{
    /// <summary>
    /// Displays current vehicle speed in km/h in the bottom-left corner.
    /// </summary>
    public class SpeedPanel : HUDPanel
    {
        private Texture2D backgroundTexture;
        private GUIStyle speedStyle;
        private GUIStyle unitStyle;

        public override void OnActivate()
        {
            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            backgroundTexture.Apply();
        }

        public override void OnDeactivate()
        {
            if (backgroundTexture != null)
            {
                Object.Destroy(backgroundTexture);
                backgroundTexture = null;
            }
        }

        public override void DrawPanel()
        {
            if (speedStyle == null)
            {
                speedStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 28,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                speedStyle.normal.textColor = Color.white;
            }

            if (unitStyle == null)
            {
                unitStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter
                };
                unitStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            }

            float w = 120f;
            float h = 60f;
            float margin = 20f;
            float x = margin;
            float y = Screen.height - margin - h;

            Rect bg = new Rect(x, y, w, h);
            if (backgroundTexture != null)
                GUI.DrawTexture(bg, backgroundTexture);

            int kmh = Mathf.RoundToInt(Mathf.Abs(hud.SpeedKmh));
            GUI.Label(new Rect(x, y, w, h - 12f), kmh.ToString(), speedStyle);
            GUI.Label(new Rect(x, y + h - 20f, w, 20f), "km/h", unitStyle);
        }
    }
}
