using UnityEngine;

namespace EVP
{
    /// <summary>
    /// Moves and rotates a vehicle to this transform's position on start,
    /// and resets it back when pressing the reset key.
    /// </summary>
    public class VehicleSpawnPoint : MonoBehaviour
    {
        [Header("Vehicle")]
        [Tooltip("The vehicle to spawn. If null, will try to find a VehicleController in the scene.")]
        public VehicleController vehicle;

        [Header("Reset Key")]
        public KeyCode resetKey = KeyCode.R;

        void Awake()
        {
            if (vehicle == null)
                vehicle = FindObjectOfType<VehicleController>();
        }

        void Start()
        {
            MoveVehicleToSpawn();
        }

        void Update()
        {
            if (Input.GetKeyDown(resetKey))
                MoveVehicleToSpawn();
        }

        void MoveVehicleToSpawn()
        {
            if (vehicle == null) return;

            var rb = vehicle.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.MovePosition(transform.position);
                rb.MoveRotation(transform.rotation);
            }
            else
            {
                vehicle.transform.SetPositionAndRotation(transform.position, transform.rotation);
            }
        }
    }
}
