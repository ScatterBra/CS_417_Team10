using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEditor.XR.Management;
public static class DEF_QuestXRValidation
{
    [MenuItem("Defense Yard/MP2B/Validate Android XR Assignment")]
    public static void Validate()
    {
        var general=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if(general==null||general.Manager==null||!general.Manager.activeLoaders.Any(l=>l!=null&&l.GetType().Name=="OpenXRLoader"))throw new Exception("Android OpenXR loader is not assigned");
        var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if(settings==null)throw new Exception("Android OpenXR settings missing");
        var enabled=settings.GetFeatures<OpenXRFeature>().Where(f=>f!=null&&f.enabled).Select(f=>f.GetType().Name).ToArray();
        if(!enabled.Any(n=>n.Contains("OculusTouch")||n.Contains("MetaQuestTouch")))throw new Exception("Android Quest/Touch controller profile not enabled");
        if(!enabled.Contains("MetaQuestFeature"))throw new Exception("Android Meta Quest support not enabled");
        System.IO.File.WriteAllLines("../quest-xr-assignment.txt",new[]{"PASS Android OpenXR loader assigned","PASS enabled features: "+string.Join(",",enabled),"Configuration only; no headset runtime claim."});
        Debug.Log("Android OpenXR loader and Quest controller profile verified");
    }
}
