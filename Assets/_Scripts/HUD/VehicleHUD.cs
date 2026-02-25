using System.Collections.Generic;
using UnityEngine;

namespace EVP
{
    /// <summary>
    /// Manages all on-screen HUD panels for a vehicle.
    /// Registers default panels (speed, mode indicator, steering input) in Awake.
    /// Future panels are added via RegisterPanel.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class VehicleHUD : MonoBehaviour
    {
        [Header("References")]
        public VehicleController vehicle;
        public VehicleMultiplayerSteering steeringManager;
        public VehicleDamageReceiver damageReceiver;

        [Header("Toggle")]
        public KeyCode uiToggleKey = KeyCode.U;

        public bool ShowUI { get; private set; } = true;

        private readonly List<HUDPanel> panels = new List<HUDPanel>();

        // --- Convenience properties for panels ---

        /// <summary>Current speed in km/h.</summary>
        public float SpeedKmh => vehicle != null ? vehicle.speed * 3.6f : 0f;

        /// <summary>Display name of the active steering mode.</summary>
        public string ActiveModeName => steeringManager != null ? steeringManager.ActiveModeName : "None";

        void Awake()
        {
            if (vehicle == null)
                vehicle = GetComponent<VehicleController>();
            if (steeringManager == null)
                steeringManager = GetComponent<VehicleMultiplayerSteering>();
            if (damageReceiver == null)
                damageReceiver = GetComponent<VehicleDamageReceiver>();

            // Register default panels
            RegisterPanel(new SpeedPanel());
            RegisterPanel(new ModeIndicatorPanel());
            RegisterPanel(new SteeringInputPanel());
            if (damageReceiver != null)
                RegisterPanel(new HealthPanel());
        }

        void OnEnable()
        {
            if (steeringManager != null)
                steeringManager.externalUIManaged = true;

            foreach (var panel in panels)
                panel.OnActivate();
        }

        void OnDisable()
        {
            foreach (var panel in panels)
                panel.OnDeactivate();

            if (steeringManager != null)
                steeringManager.externalUIManaged = false;
        }

        void Update()
        {
            if (Input.GetKeyDown(uiToggleKey))
                ShowUI = !ShowUI;
        }

        void OnGUI()
        {
            if (!ShowUI) return;

            foreach (var panel in panels)
            {
                if (panel.Enabled)
                    panel.DrawPanel();
            }
        }

        // --- Public API ---

        /// <summary>
        /// Register a new HUD panel. It will be initialized and activated immediately
        /// if the HUD is already enabled.
        /// </summary>
        public void RegisterPanel(HUDPanel panel)
        {
            panel.Initialize(this);
            panels.Add(panel);

            if (isActiveAndEnabled)
                panel.OnActivate();
        }

        /// <summary>
        /// Get the first panel of type T, or null.
        /// </summary>
        public T GetPanel<T>() where T : HUDPanel
        {
            foreach (var panel in panels)
            {
                if (panel is T typed)
                    return typed;
            }
            return null;
        }

        /// <summary>
        /// Draw the active steering method's GUI. Called by SteeringInputPanel.
        /// </summary>
        public void DrawActiveMethodGUI()
        {
            if (steeringManager != null)
                steeringManager.DrawActiveMethodGUI();
        }
    }
}
