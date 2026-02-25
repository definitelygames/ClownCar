using UnityEngine;

namespace EVP
{
    /// <summary>
    /// Immutable struct returned by steering methods each FixedUpdate.
    /// </summary>
    public struct VehicleInput
    {
        public float steer;
        public float throttle;
        public float brake;

        public static VehicleInput Zero => new VehicleInput { steer = 0f, throttle = 0f, brake = 0f };
    }

    /// <summary>
    /// Abstract base for all steering method implementations.
    /// Each method is a plain C# class (not a MonoBehaviour) created by its config's factory method.
    /// The manager calls lifecycle methods in order: Initialize -> Activate/Deactivate -> ReadInput/GetVehicleInput/ApplyPhysics/DrawGUI.
    /// </summary>
    public abstract class SteeringMethod
    {
        protected VehicleMultiplayerSteering manager;
        protected VehicleController vehicle;

        /// <summary>
        /// Called once when the method is first created.
        /// </summary>
        public virtual void Initialize(VehicleMultiplayerSteering manager, VehicleController vehicle)
        {
            this.manager = manager;
            this.vehicle = vehicle;
        }

        /// <summary>
        /// Called when this method becomes the active steering mode.
        /// </summary>
        public virtual void Activate() { }

        /// <summary>
        /// Called when this method is no longer the active steering mode.
        /// </summary>
        public virtual void Deactivate() { }

        /// <summary>
        /// Called every Update. Read raw input here.
        /// </summary>
        public virtual void ReadInput(float deltaTime) { }

        /// <summary>
        /// Called every FixedUpdate. Return the desired vehicle input.
        /// </summary>
        public virtual VehicleInput GetVehicleInput(float fixedDeltaTime)
        {
            return VehicleInput.Zero;
        }

        /// <summary>
        /// Called every FixedUpdate after GetVehicleInput. Apply physics forces (torques, etc.) here.
        /// </summary>
        public virtual void ApplyPhysics(Rigidbody rb, float fixedDeltaTime) { }

        /// <summary>
        /// Called every LateUpdate. Override visuals after EVP's Update has run.
        /// </summary>
        public virtual void LateUpdate() { }

        /// <summary>
        /// Called every OnGUI. Draw method-specific UI here.
        /// </summary>
        public virtual void DrawGUI() { }

        /// <summary>
        /// Called when the set of enabled players changes.
        /// </summary>
        public virtual void OnPlayersChanged() { }
    }
}
