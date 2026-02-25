using UnityEngine;

namespace EVP
{
    /// <summary>
    /// Displays the active steering mode name as a pill at the top-center of the screen.
    /// </summary>
    public class ModeIndicatorPanel : HUDPanel
    {
        private Texture2D backgroundTexture;
        private GUIStyle labelStyle;

        public override void OnActivate()
        {
            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.5f));
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
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                labelStyle.normal.textColor = Color.white;
            }

            float w = 220f;
            float h = 28f;
            float topMargin = 10f;
            float x = (Screen.width - w) / 2f;
            float y = topMargin;

            Rect bg = new Rect(x, y, w, h);
            if (backgroundTexture != null)
                GUI.DrawTexture(bg, backgroundTexture);

            GUI.Label(bg, hud.ActiveModeName, labelStyle);
        }
    }
}
