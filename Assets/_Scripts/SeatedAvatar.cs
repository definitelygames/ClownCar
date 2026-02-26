using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FIMSpace.FProceduralAnimation;

/// <summary>
/// Attach to a seat pivot (e.g. DriverFrontPivot) that has a Rigidbody + ConfigurableJoint.
/// Call SpawnAvatar()/DespawnAvatar() to control the avatar lifecycle (managed by SeatedAvatarManager).
/// Awake() only handles physics tuning and vehicle root detection.
/// When seated, limbs are kinematic (follow animation) and hands are locked to the steering wheel
/// via direct source bone positioning in LateUpdate (runs after RagdollAnimator2).
/// </summary>
public class SeatedAvatar : MonoBehaviour
{
    [Header("Avatar")]
    public GameObject avatarPrefab;
    public Vector3 offset = new Vector3(0f, 0.5f, 0f);
    public RuntimeAnimatorController seatedAnimatorController;

    [Header("Seated Pose")]
    public bool seated = true;
    public Transform steeringWheel;
    public Vector3 leftGripOffset = new Vector3(-0.15f, 0f, 0f);
    public Vector3 rightGripOffset = new Vector3(0.15f, 0f, 0f);

    [Header("Physics Tuning")]
    [Tooltip("Override Rigidbody mass at runtime. 0 = keep existing value.")]
    public float massOverride = 10f;
    [Tooltip("Local Y offset for center of mass. Negative = lower = more stable lean.")]
    public float centerOfMassY = -0.3f;

    [Header("References")]
    [Tooltip("Auto-detected from ConfigurableJoint.connectedBody if left empty.")]
    public Transform vehicleRoot;

    GameObject avatarInstance;
    RagdollAnimator2 ragdoll;
    Transform leftHandSource;
    Transform rightHandSource;

    public bool IsSpawned => avatarInstance != null;
    public Transform AvatarTransform => avatarInstance != null ? avatarInstance.transform : null;
    public RagdollAnimator2 Ragdoll => ragdoll;
    public bool IsEjected { get; set; }

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();

        // Apply physics tuning
        if (rb != null)
        {
            if (massOverride > 0f)
                rb.mass = massOverride;
            rb.centerOfMass = new Vector3(0f, centerOfMassY, 0f);
        }

        // Auto-detect vehicle root from joint
        if (vehicleRoot == null)
        {
            var joint = GetComponent<ConfigurableJoint>();
            if (joint != null && joint.connectedBody != null)
                vehicleRoot = joint.connectedBody.transform;
        }
    }

    /// <summary>
    /// Instantiate the avatar, set up collision ignoring, seated limbs, and hand tracking.
    /// No-op if already spawned.
    /// </summary>
    public void SpawnAvatar()
    {
        if (IsSpawned) return;

        if (avatarPrefab == null)
        {
            Debug.LogError("[SeatedAvatar] No avatarPrefab assigned on " + name);
            return;
        }

        avatarInstance = Instantiate(avatarPrefab, transform);
        avatarInstance.transform.localPosition = offset;
        avatarInstance.transform.localRotation = Quaternion.identity;

        // Assign animator controller if provided
        if (seatedAnimatorController != null)
        {
            var animator = avatarInstance.GetComponentInChildren<Animator>();
            if (animator != null)
                animator.runtimeAnimatorController = seatedAnimatorController;
        }

        // Defer collision ignoring and seated setup — RagdollAnimator2 creates dummy colliders in Start()
        if (vehicleRoot != null)
            StartCoroutine(InitializeSeatedAvatar());
    }

    public void ClearHandTracking()
    {
        leftHandSource = null;
        rightHandSource = null;
    }

    /// <summary>
    /// Unparent the avatar instance from the seat pivot so the ragdoll simulates freely,
    /// and set continuous collision detection on all ragdoll bone rigidbodies.
    /// </summary>
    public void DetachAvatar()
    {
        if (avatarInstance == null) return;

        avatarInstance.transform.SetParent(null, true);

        if (ragdoll != null)
        {
            foreach (var chain in ragdoll.Handler.Chains)
            {
                foreach (var bone in chain.BoneSetups)
                {
                    if (bone.GameRigidbody == null) continue;
                    bone.GameRigidbody.isKinematic = false;
                    bone.GameRigidbody.useGravity = true;
                    bone.GameRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
            }
        }
    }

    /// <summary>
    /// Destroy the avatar instance and clear references. No-op if not spawned or ejected.
    /// </summary>
    public void DespawnAvatar()
    {
        if (!IsSpawned || IsEjected) return;

        StopAllCoroutines();
        Destroy(avatarInstance);
        avatarInstance = null;
        ragdoll = null;
        leftHandSource = null;
        rightHandSource = null;
    }

    IEnumerator InitializeSeatedAvatar()
    {
        // Wait for RagdollAnimator2.Start() to create dummy colliders
        yield return null;

        var vehicleColliders = vehicleRoot.GetComponentsInChildren<Collider>();

        // Tell the ragdoll handler to ignore vehicle colliders (covers dummy bones)
        ragdoll = avatarInstance.GetComponentInChildren<RagdollAnimator2>();
        if (ragdoll != null)
        {
            var vehicleList = new List<Collider>(vehicleColliders);
            ragdoll.Handler.IgnoreCollisionWith(vehicleList);
        }

        // Also ignore collisions on any non-ragdoll colliders on the avatar
        var avatarColliders = avatarInstance.GetComponentsInChildren<Collider>();
        foreach (var ac in avatarColliders)
            foreach (var vc in vehicleColliders)
                if (ac != vc)
                    Physics.IgnoreCollision(ac, vc);

        // Setup seated pose
        if (ragdoll != null)
        {
            if (seated)
                ApplySeatedLimbs(true);

            if (steeringWheel != null)
                SetupHandTracking();
        }
    }

    void ApplySeatedLimbs(bool kinematic)
    {
        var handler = ragdoll.Handler;
        SetChainKinematic(handler.GetChain(ERagdollChainType.LeftLeg), kinematic);
        SetChainKinematic(handler.GetChain(ERagdollChainType.RightLeg), kinematic);
        SetChainKinematic(handler.GetChain(ERagdollChainType.LeftArm), kinematic);
        SetChainKinematic(handler.GetChain(ERagdollChainType.RightArm), kinematic);
        handler.RefreshAllChainsDynamicParameters();
    }

    void SetChainKinematic(RagdollBonesChain chain, bool kinematic)
    {
        if (chain == null) return;
        foreach (var bone in chain.BoneSetups)
            bone.ForceKinematicOnStanding = kinematic;
    }

    void SetupHandTracking()
    {
        var animator = avatarInstance.GetComponentInChildren<Animator>();
        if (animator == null) return;

        leftHandSource = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        rightHandSource = animator.GetBoneTransform(HumanBodyBones.RightHand);
    }

    // Runs after RagdollAnimator2 (execution order -1) so this is the final word on hand positions
    void LateUpdate()
    {
        if (!seated || steeringWheel == null || !IsSpawned || IsEjected) return;

        if (leftHandSource != null)
            leftHandSource.position = steeringWheel.TransformPoint(leftGripOffset);
        if (rightHandSource != null)
            rightHandSource.position = steeringWheel.TransformPoint(rightGripOffset);
    }

    /// <summary>
    /// Toggle seated mode at runtime. When seated, limbs are kinematic and hands lock to the wheel.
    /// </summary>
    public void SetSeated(bool value)
    {
        seated = value;
        if (ragdoll == null) return;
        ApplySeatedLimbs(value);
    }
}
