using UnityEngine;

/// <summary>
/// Helper script to quickly set up ragdoll physics on a humanoid.
/// Attach to root of character, configure bone references, click "Setup Ragdoll" in context menu.
/// </summary>
public class RagdollSetup : MonoBehaviour
{
    [Header("Bone References (Auto-detected if using standard naming)")]
    public Transform pelvis;
    public Transform spine;
    public Transform chest;
    public Transform head;
    public Transform leftUpperArm;
    public Transform leftLowerArm;
    public Transform rightUpperArm;
    public Transform rightLowerArm;
    public Transform leftUpperLeg;
    public Transform leftLowerLeg;
    public Transform rightUpperLeg;
    public Transform rightLowerLeg;

    [Header("Physics Settings")]
    public float totalMass = 70f;
    public float drag = 0.5f;
    public float angularDrag = 1f;

    [Header("Collider Settings")]
    public float colliderRadius = 0.1f;
    public bool addBalanceScript = true;
    public float balanceForce = 100f;

    [ContextMenu("Auto-Find Bones")]
    public void AutoFindBones()
    {
        // Common bone name patterns
        pelvis = FindBone("pelvis", "hips", "hip");
        spine = FindBone("spine");
        chest = FindBone("chest", "spine1", "spine2");
        head = FindBone("head");
        leftUpperArm = FindBone("leftupperarm", "l_upperarm", "leftshoulder", "l_arm");
        leftLowerArm = FindBone("leftlowerarm", "l_lowerarm", "leftforearm", "l_forearm");
        rightUpperArm = FindBone("rightupperarm", "r_upperarm", "rightshoulder", "r_arm");
        rightLowerArm = FindBone("rightlowerarm", "r_lowerarm", "rightforearm", "r_forearm");
        leftUpperLeg = FindBone("leftupperleg", "l_upperleg", "leftthigh", "l_thigh");
        leftLowerLeg = FindBone("leftlowerleg", "l_lowerleg", "leftcalf", "l_calf", "leftshin");
        rightUpperLeg = FindBone("rightupperleg", "r_upperleg", "rightthigh", "r_thigh");
        rightLowerLeg = FindBone("rightlowerleg", "r_lowerleg", "rightcalf", "r_calf", "rightshin");

        Debug.Log("RagdollSetup: Auto-find complete. Check bone references in inspector.");
    }

    Transform FindBone(params string[] names)
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>();

        foreach (Transform t in allTransforms)
        {
            string boneName = t.name.ToLower().Replace(" ", "").Replace("_", "");
            foreach (string name in names)
            {
                if (boneName.Contains(name.ToLower()))
                {
                    return t;
                }
            }
        }
        return null;
    }

    [ContextMenu("Setup Ragdoll")]
    public void SetupRagdoll()
    {
        if (pelvis == null)
        {
            Debug.LogError("RagdollSetup: Pelvis bone is required! Use 'Auto-Find Bones' first.");
            return;
        }

        // Mass distribution (approximate human proportions)
        float pelvisMass = totalMass * 0.15f;
        float spineMass = totalMass * 0.15f;
        float chestMass = totalMass * 0.15f;
        float headMass = totalMass * 0.08f;
        float upperArmMass = totalMass * 0.03f;
        float lowerArmMass = totalMass * 0.02f;
        float upperLegMass = totalMass * 0.1f;
        float lowerLegMass = totalMass * 0.05f;

        // Setup each body part
        SetupBodyPart(pelvis, pelvisMass, new Vector3(0.2f, 0.15f, 0.15f));

        if (spine != null)
            SetupBodyPart(spine, spineMass, new Vector3(0.15f, 0.12f, 0.1f), pelvis);

        if (chest != null)
            SetupBodyPart(chest, chestMass, new Vector3(0.2f, 0.15f, 0.12f), spine ?? pelvis);

        if (head != null)
            SetupBodyPart(head, headMass, new Vector3(0.1f, 0.12f, 0.1f), chest ?? spine ?? pelvis);

        // Arms
        Transform armParent = chest ?? spine ?? pelvis;
        if (leftUpperArm != null)
            SetupBodyPart(leftUpperArm, upperArmMass, new Vector3(0.15f, 0.05f, 0.05f), armParent);
        if (leftLowerArm != null)
            SetupBodyPart(leftLowerArm, lowerArmMass, new Vector3(0.12f, 0.04f, 0.04f), leftUpperArm);
        if (rightUpperArm != null)
            SetupBodyPart(rightUpperArm, upperArmMass, new Vector3(0.15f, 0.05f, 0.05f), armParent);
        if (rightLowerArm != null)
            SetupBodyPart(rightLowerArm, lowerArmMass, new Vector3(0.12f, 0.04f, 0.04f), rightUpperArm);

        // Legs
        if (leftUpperLeg != null)
            SetupBodyPart(leftUpperLeg, upperLegMass, new Vector3(0.08f, 0.2f, 0.08f), pelvis);
        if (leftLowerLeg != null)
            SetupBodyPart(leftLowerLeg, lowerLegMass, new Vector3(0.06f, 0.2f, 0.06f), leftUpperLeg);
        if (rightUpperLeg != null)
            SetupBodyPart(rightUpperLeg, upperLegMass, new Vector3(0.08f, 0.2f, 0.08f), pelvis);
        if (rightLowerLeg != null)
            SetupBodyPart(rightLowerLeg, lowerLegMass, new Vector3(0.06f, 0.2f, 0.06f), rightUpperLeg);

        // Add balance script to pelvis
        if (addBalanceScript && pelvis != null)
        {
            RagdollBalance balance = pelvis.gameObject.AddComponent<RagdollBalance>();
            balance.uprightTorque = balanceForce;
            balance.balanceEntireBody = false;
        }

        Debug.Log("RagdollSetup: Ragdoll created successfully!");
    }

    void SetupBodyPart(Transform bone, float mass, Vector3 colliderSize, Transform connectedBone = null)
    {
        // Add Rigidbody
        Rigidbody rb = bone.GetComponent<Rigidbody>();
        if (rb == null)
            rb = bone.gameObject.AddComponent<Rigidbody>();

        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Add Collider (capsule for limbs, box for torso)
        if (bone.GetComponent<Collider>() == null)
        {
            CapsuleCollider col = bone.gameObject.AddComponent<CapsuleCollider>();
            col.radius = colliderSize.y;
            col.height = colliderSize.x * 2f;
            col.direction = 0; // X-axis (along limb)
        }

        // Add Character Joint if there's a parent
        if (connectedBone != null)
        {
            CharacterJoint joint = bone.GetComponent<CharacterJoint>();
            if (joint == null)
                joint = bone.gameObject.AddComponent<CharacterJoint>();

            Rigidbody connectedRb = connectedBone.GetComponent<Rigidbody>();
            if (connectedRb != null)
            {
                joint.connectedBody = connectedRb;
            }

            // Set reasonable joint limits
            SoftJointLimit lowTwist = joint.lowTwistLimit;
            lowTwist.limit = -30f;
            joint.lowTwistLimit = lowTwist;

            SoftJointLimit highTwist = joint.highTwistLimit;
            highTwist.limit = 30f;
            joint.highTwistLimit = highTwist;

            SoftJointLimit swing1 = joint.swing1Limit;
            swing1.limit = 45f;
            joint.swing1Limit = swing1;

            SoftJointLimit swing2 = joint.swing2Limit;
            swing2.limit = 45f;
            joint.swing2Limit = swing2;
        }
    }

    [ContextMenu("Remove Ragdoll")]
    public void RemoveRagdoll()
    {
        // Remove all ragdoll components from hierarchy
        foreach (CharacterJoint joint in GetComponentsInChildren<CharacterJoint>())
            DestroyImmediate(joint);

        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
            DestroyImmediate(rb);

        foreach (Collider col in GetComponentsInChildren<Collider>())
            DestroyImmediate(col);

        foreach (RagdollBalance balance in GetComponentsInChildren<RagdollBalance>())
            DestroyImmediate(balance);

        Debug.Log("RagdollSetup: Ragdoll components removed.");
    }
}
