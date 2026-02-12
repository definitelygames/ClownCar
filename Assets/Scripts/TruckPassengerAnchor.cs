using UnityEngine;

/// <summary>
/// Attach this to an empty GameObject in your truck bed.
/// It will anchor ragdoll passengers at spawn points.
/// </summary>
public class TruckPassengerAnchor : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Ragdoll prefab with Rigidbody on pelvis/hips")]
    public GameObject ragdollPrefab;

    [Tooltip("Where to spawn passengers (child transforms of this object)")]
    public Transform[] spawnPoints;

    [Header("Joint Settings")]
    [Tooltip("How much the anchor joint can stretch before breaking (0 = unbreakable)")]
    public float breakForce = 0f;

    [Tooltip("Allow some slack in the anchor")]
    public float anchorSlack = 0.2f;

    [Header("Auto-Setup")]
    [Tooltip("Automatically spawn passengers on Start")]
    public bool spawnOnStart = true;

    [Tooltip("Number of passengers to spawn (uses available spawn points)")]
    public int passengerCount = 1;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnPassengers(passengerCount);
        }
    }

    public void SpawnPassengers(int count)
    {
        int spawnCount = Mathf.Min(count, spawnPoints.Length);

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnPassengerAt(spawnPoints[i]);
        }
    }

    public GameObject SpawnPassengerAt(Transform spawnPoint)
    {
        if (ragdollPrefab == null || spawnPoint == null)
        {
            Debug.LogWarning("TruckPassengerAnchor: Missing ragdoll prefab or spawn point!");
            return null;
        }

        // Spawn ragdoll at the spawn point
        GameObject passenger = Instantiate(ragdollPrefab, spawnPoint.position, spawnPoint.rotation);

        // Find the pelvis/hips (first rigidbody, or tagged)
        Rigidbody pelvisRb = FindPelvis(passenger);

        if (pelvisRb == null)
        {
            Debug.LogWarning("TruckPassengerAnchor: No Rigidbody found on ragdoll!");
            return passenger;
        }

        // Create anchor joint
        ConfigurableJoint joint = pelvisRb.gameObject.AddComponent<ConfigurableJoint>();

        // Connect to spawn point (needs a kinematic rigidbody)
        Rigidbody anchorRb = spawnPoint.GetComponent<Rigidbody>();
        if (anchorRb == null)
        {
            anchorRb = spawnPoint.gameObject.AddComponent<Rigidbody>();
            anchorRb.isKinematic = true;
        }

        joint.connectedBody = anchorRb;

        // Configure joint for anchored but wobbly feel
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        // Set linear limits (how far they can move from anchor)
        SoftJointLimit linearLimit = new SoftJointLimit();
        linearLimit.limit = anchorSlack;
        joint.linearLimit = linearLimit;

        // Free rotation so they can wobble
        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;

        // Break force (0 = unbreakable)
        if (breakForce > 0)
        {
            joint.breakForce = breakForce;
            joint.breakTorque = breakForce;
        }

        return passenger;
    }

    Rigidbody FindPelvis(GameObject ragdoll)
    {
        // Try to find by common names
        string[] pelvisNames = { "Pelvis", "Hips", "pelvis", "hips", "Spine", "Root" };

        foreach (string name in pelvisNames)
        {
            Transform found = ragdoll.transform.Find(name);
            if (found == null)
            {
                // Search in children
                foreach (Transform child in ragdoll.GetComponentsInChildren<Transform>())
                {
                    if (child.name.ToLower().Contains(name.ToLower()))
                    {
                        Rigidbody rb = child.GetComponent<Rigidbody>();
                        if (rb != null) return rb;
                    }
                }
            }
            else
            {
                Rigidbody rb = found.GetComponent<Rigidbody>();
                if (rb != null) return rb;
            }
        }

        // Fallback: just get first rigidbody
        return ragdoll.GetComponentInChildren<Rigidbody>();
    }
}
