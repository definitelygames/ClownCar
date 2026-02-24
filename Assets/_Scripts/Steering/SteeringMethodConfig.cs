using UnityEngine;

namespace EVP
{
    /// <summary>
    /// Abstract ScriptableObject base for steering method configurations.
    /// Each config holds tuning parameters and has a factory method to create its runtime method instance.
    /// </summary>
    public abstract class SteeringMethodConfig : ScriptableObject
    {
        [Tooltip("Display name shown in UI when this mode is active.")]
        public string displayName = "Steering Mode";

        /// <summary>
        /// Factory method: creates a new runtime instance of the corresponding SteeringMethod.
        /// </summary>
        public abstract SteeringMethod CreateMethod();
    }
}
