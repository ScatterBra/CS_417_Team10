using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public static class DEF_IntegratedCarrySettings
{
    public static void Apply(DEF_VRInput input)
    {
        if (input == null || input.xrCamera == null) return;
        bool integrated = false;
        foreach (var clinic in Object.FindObjectsByType<DEF_ClinicTraining>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (clinic.gameObject.scene == input.gameObject.scene) { integrated = true; break; }
        if (!integrated) return;
        var origin = input.xrCamera.GetComponentInParent<XROrigin>();
        if (origin == null) return;
        foreach (var attachment in origin.GetComponentsInChildren<InteractionAttachController>(true))
        {
            attachment.useManipulationInput = false;
            attachment.manipulationInput.inputSourceMode = XRInputValueReader.InputSourceMode.Unused;
        }
    }
}
