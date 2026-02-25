namespace EVP
{
    /// <summary>
    /// Bridge panel that delegates to the active steering method's DrawGUI().
    /// Exists as a panel so steering-specific UI participates in enable/disable toggling.
    /// </summary>
    public class SteeringInputPanel : HUDPanel
    {
        public override void DrawPanel()
        {
            hud.DrawActiveMethodGUI();
        }
    }
}
