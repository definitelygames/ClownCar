using UnityEngine;

namespace EVP
{
    /// <summary>
    /// Rotates up to 4 steering wheel meshes based on each wheel's individual steer angle.
    /// Works in all steering modes — in normal EVP mode, wheelData[i].steerAngle is set by EVP
    /// for steered wheels and 0 for non-steered.
    /// </summary>
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(VehicleController))]
    public class PerWheelVisualEffects : MonoBehaviour
    {
        [Header("Steering Wheels")]
        [Tooltip("One steering wheel mesh per player/wheel slot (P1=0, P2=1, etc.). Unassigned slots are skipped.")]
        public Transform[] steeringWheels = new Transform[4];

        [Tooltip("Degrees the steering wheel mesh rotates for full lock.")]
        public float degreesOfRotation = 420f;

        [Header("Brake Lights")]
        public Renderer brakesRenderer;
        public int brakesMaterialIndex;
        public Material brakesOnMaterial;
        public Material brakesOffMaterial;

        private VehicleController vehicle;
        private bool prevBrakes;

        void OnEnable()
        {
            vehicle = GetComponent<VehicleController>();
        }

        void Update()
        {
            if (vehicle == null) return;

            bool brakes = vehicle.brakeInput > 0.1f;
            if (brakes != prevBrakes)
            {
                if (brakesRenderer != null && brakesMaterialIndex >= 0 && brakesMaterialIndex < brakesRenderer.sharedMaterials.Length)
                {
                    Material[] materialsCopy = brakesRenderer.materials;
                    Destroy(materialsCopy[brakesMaterialIndex]);
                    materialsCopy[brakesMaterialIndex] = brakes ? brakesOnMaterial : brakesOffMaterial;
                    brakesRenderer.materials = materialsCopy;
                }
                prevBrakes = brakes;
            }
        }

        void LateUpdate()
        {
            if (vehicle == null || steeringWheels == null) return;

            WheelData[] wheels = vehicle.wheelData;
            if (wheels == null) return;

            float maxAngle = vehicle.maxSteerAngle;

            int count = Mathf.Min(steeringWheels.Length, wheels.Length);
            for (int i = 0; i < count; i++)
            {
                if (steeringWheels[i] == null) continue;

                Vector3 angles = steeringWheels[i].localEulerAngles;

                if (maxAngle > 0f)
                    angles.z = -0.5f * degreesOfRotation * wheels[i].steerAngle / maxAngle;
                else
                    angles.z = 0f;

                steeringWheels[i].localEulerAngles = angles;
            }
        }
    }
}
