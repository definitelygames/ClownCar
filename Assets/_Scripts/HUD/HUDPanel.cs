namespace EVP
{
    /// <summary>
    /// Abstract base for all HUD panels. Follows the same pattern as SteeringMethod:
    /// plain C# class managed by a MonoBehaviour (VehicleHUD).
    /// </summary>
    public abstract class HUDPanel
    {
        protected VehicleHUD hud;

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Called once when the panel is registered with the HUD manager.
        /// </summary>
        public virtual void Initialize(VehicleHUD hud)
        {
            this.hud = hud;
        }

        /// <summary>
        /// Called when the HUD becomes active. Create textures and styles here.
        /// </summary>
        public virtual void OnActivate() { }

        /// <summary>
        /// Called when the HUD is deactivated. Destroy textures here.
        /// </summary>
        public virtual void OnDeactivate() { }

        /// <summary>
        /// Render this panel. Called from OnGUI when the panel is enabled.
        /// </summary>
        public abstract void DrawPanel();
    }
}
