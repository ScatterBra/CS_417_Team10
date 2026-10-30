#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

// Recording-branch-only protection against an untracked zero-height camera pose.
// Keeps collision-based locomotion; never changes the camera, input or origin pose.
[DisallowMultipleComponent]
[DefaultExecutionOrder(-206)]
public sealed class DEF_RecordingMovementGuard : MonoBehaviour, IConstrainedXRBodyManipulator
{
    XRBodyTransformer transformer;
    CharacterController controller;
    IConstrainedXRBodyManipulator original;
    float lastValidHeight, originalStep;
    bool configured;
    public XRMovableBody linkedBody { get; private set; }
    public bool isGrounded => controller != null && controller.isGrounded;
    public CollisionFlags lastCollisionFlags { get; private set; }

    public static void Install(Transform origin)
    {
        if (origin == null || origin.gameObject.scene.name != "Deadzone_Final")
            throw new InvalidOperationException("Recording movement guard requires the final-scene player origin.");
        var existing = origin.GetComponent<DEF_RecordingMovementGuard>();
        if (existing != null) return;
        var bodies = origin.GetComponentsInChildren<XRBodyTransformer>(true);
        if (bodies.Length != 1 || !bodies[0].isActiveAndEnabled)
            throw new InvalidOperationException("Recording movement guard requires exactly one active XR body transformer.");
        var cc = origin.GetComponent<CharacterController>();
        if (cc == null || !cc.enabled || !(bodies[0].constrainedBodyManipulator is CharacterControllerBodyManipulator))
            throw new InvalidOperationException("Recording movement guard requires the existing collision-based character controller.");
        var guard = origin.gameObject.AddComponent<DEF_RecordingMovementGuard>();
        guard.transformer = bodies[0]; guard.controller = cc;
        guard.original = bodies[0].constrainedBodyManipulator;
        guard.originalStep = cc.stepOffset;
        guard.lastValidHeight = guard.ValidHeight(cc.height) ? cc.height : 1.36144f;
        guard.configured = true;
        bodies[0].constrainedBodyManipulator = guard;
        guard.RefreshCapsule();
    }
    void OnEnable()
    {
        if (configured && transformer != null && ReferenceEquals(transformer.constrainedBodyManipulator, original))
            transformer.constrainedBodyManipulator = this;
    }
    void Update()
    {
        // XRI OnEnable re-reads its serialized default. Reattach after a rig re-enable,
        // but never replace another custom manipulator or force a disabled rig on.
        if (configured && transformer != null && transformer.isActiveAndEnabled &&
            transformer.constrainedBodyManipulator is CharacterControllerBodyManipulator)
        {
            original = transformer.constrainedBodyManipulator;
            transformer.constrainedBodyManipulator = this;
        }
    }
    void OnDisable()
    {
        if (configured && transformer != null && ReferenceEquals(transformer.constrainedBodyManipulator, this))
        {
            transformer.constrainedBodyManipulator = original;
            if (controller != null) controller.stepOffset = Mathf.Min(originalStep, Mathf.Max(0, controller.height - .01f));
        }
    }
    public void OnLinkedToBody(XRMovableBody body) { linkedBody = body; }
    public void OnUnlinkedFromBody() { linkedBody = null; }
    bool ValidHeight(float value) => Finite(value) && value >= Mathf.Max(.05f, controller.radius * 2);
    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    bool RefreshCapsule()
    {
        if (linkedBody == null || controller == null || !controller.enabled || !controller.gameObject.activeInHierarchy ||
            linkedBody.originTransform != controller.transform) return false;
        Vector3 ground = linkedBody.GetBodyGroundLocalPosition();
        if (!Finite(ground.x) || !Finite(ground.y) || !Finite(ground.z)) return false;
        float measured = linkedBody.xrOrigin.CameraInOriginSpaceHeight - ground.y;
        if (ValidHeight(measured)) lastValidHeight = measured;
        float height = Mathf.Max(lastValidHeight, controller.radius * 2);
        // Lower the step before shrinking/repairing the capsule; avoid rewriting stable
        // native controller properties each frame (which can recreate its contacts).
        float safeStep = Mathf.Max(0, Mathf.Min(controller.height, height) - .01f);
        if (controller.stepOffset > safeStep) controller.stepOffset = safeStep;
        if (!Mathf.Approximately(controller.height, height)) controller.height = height;
        float desiredStep = Mathf.Min(originalStep, Mathf.Max(0, height - .01f));
        if (!Mathf.Approximately(controller.stepOffset, desiredStep)) controller.stepOffset = desiredStep;
        Vector3 center = ground + Vector3.up * (height * .5f + controller.skinWidth);
        if (controller.center != center) controller.center = center;
        return true;
    }
    public CollisionFlags MoveBody(Vector3 motion)
    {
        if (!isActiveAndEnabled || !RefreshCapsule() || !Finite(motion.x) || !Finite(motion.y) || !Finite(motion.z))
            return lastCollisionFlags;
        lastCollisionFlags = controller.Move(motion);
        return lastCollisionFlags;
    }
}
#endif
