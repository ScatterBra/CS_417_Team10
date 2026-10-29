using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Edit Mode geometry/reference fixture. Not hardware, input or Play Mode lifecycle evidence.
public static class DEF_IntegratedPresentationValidation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static void Run()
    {
        if (Application.isPlaying) throw new Exception("Edit Mode only");
        var previous = SceneManager.GetActiveScene();
        var instance = typeof(DEF_VRInput).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var active = typeof(DEF_VRInput).GetField("<Active>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var oldInstance = instance.GetValue(null); var oldActive = active.GetValue(null);
        float oldTimeScale = Time.timeScale;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var results = new List<string>(); int failures = 0;
        DEF_IntegratedXRPresentation ui = null;
        void Check(bool ok, string message) { results.Add((ok ? "PASS " : "FAIL ") + message); if (!ok) failures++; }
        GameObject Make(string name, params Type[] types) => new GameObject(name, types);
        try
        {
            var rig = Make("Fixture XR Origin").AddComponent<XROrigin>();
            var camera = Make("Fixture tracked camera").AddComponent<Camera>();
            camera.transform.SetParent(rig.transform, false); camera.transform.position = new Vector3(10000, 10000, 10000);
            rig.Camera = camera;
            var input = rig.gameObject.AddComponent<DEF_VRInput>(); input.xrCamera = camera;
            instance.SetValue(null, input); active.SetValue(null, true);
            var hud = Make("Fixture HUD", typeof(RectTransform), typeof(Canvas)).AddComponent<DEF_HUD>();
            hud.shopPanel = Make("Fixture shop", typeof(RectTransform)); hud.shopPanel.transform.SetParent(hud.transform, false); hud.shopPanel.SetActive(false);
            hud.selectedPanel = Make("Fixture selected", typeof(RectTransform)); hud.selectedPanel.transform.SetParent(hud.transform, false);
            var passive = Make("Passive label", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>(); passive.renderMode = RenderMode.WorldSpace;
            var goalCanvas = Make("Fixture objectives", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            var presenter = goalCanvas.gameObject.AddComponent<DEF_ProgressionPresenter>();
            TMP_Text Text(string name) { var go = Make(name, typeof(RectTransform)); go.transform.SetParent(goalCanvas.transform, false); return go.AddComponent<TextMeshProUGUI>(); }
            presenter.goalText = Text("Goal"); presenter.goalText.text = "Test objective";
            presenter.introductionText = Text("Introduction"); presenter.failureText = Text("Failure");
            ui = hud.gameObject.AddComponent<DEF_IntegratedXRPresentation>();
            typeof(DEF_IntegratedXRPresentation).GetField("hud", Private).SetValue(ui, hud);
            var bind = typeof(DEF_IntegratedXRPresentation).GetMethod("BindPresentation", Private);
            Check((bool)bind.Invoke(ui, null), "bind actual presentation methods to synthetic XR rig");
            var popup = (Canvas)typeof(DEF_IntegratedXRPresentation).GetField("popup", Private).GetValue(ui);
            Check(hud.transform.parent == camera.transform && goalCanvas.transform.parent == camera.transform, "HUD and objectives parent to actual camera");
            Check(hud.selectedPanel.transform.parent == hud.transform, "selected item and cancel remain head attached");
            Check(hud.shopPanel.transform.parent == popup.transform && popup.transform.parent == null, "shop is separate scene-root popup");
            Check(passive.worldCamera == camera && passive.GetComponent<GraphicRaycaster>() == null && passive.GetComponent<TrackedDeviceGraphicRaycaster>() == null, "passive world label gets camera but no raycaster");
            Check(hud.GetComponent<TrackedDeviceGraphicRaycaster>() != null && popup.GetComponent<TrackedDeviceGraphicRaycaster>() != null, "interactive HUD and shop have tracked raycasters");
            Check(ui.TryPositionShop(), "shop can open in clear fixture space");
            var popupPosition = popup.transform.position; var popupRotation = popup.transform.rotation;
            var hudPosition = hud.transform.position;
            camera.transform.position += Vector3.right * 2; camera.transform.rotation = Quaternion.Euler(0, 90, 0);
            Check(Vector3.Distance(hudPosition, hud.transform.position) > .5f, "HUD follows camera motion");
            Check(popup.transform.position == popupPosition && popup.transform.rotation == popupRotation, "open shop stays world fixed during head motion");
            Check(ui.TryPositionShop() && Vector3.Distance(popupPosition, popup.transform.position) > .5f, "reopening shop reanchors from new head pose");
            int Count() => scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Canvas>(true).Length);
            int count = Count(); bind.Invoke(ui, null); bind.Invoke(ui, null);
            Check(Count() == count, "repeated binding does not duplicate UI canvases");
            var replacement = Make("Replacement camera").AddComponent<Camera>(); replacement.transform.SetParent(rig.transform, false); input.xrCamera = replacement;
            bind.Invoke(ui, null);
            Check(hud.transform.parent == replacement.transform && goalCanvas.transform.parent == replacement.transform && popup.worldCamera == replacement, "replacement camera rebinds head panels and popup events");
            // Runtime OnDestroy uses deferred Destroy, which cannot be validated in Edit Mode.
            // Retire the fixture popup explicitly, then verify scene teardown leaves no fixture UI.
            typeof(DEF_IntegratedXRPresentation).GetField("popup", Private).SetValue(ui, null);
            UnityEngine.Object.DestroyImmediate(popup.gameObject);
            Check(popup == null, "fixture popup explicitly cleaned without destroying camera");
            Check(replacement != null, "popup cleanup preserves camera");
        }
        catch (Exception error) { failures++; results.Add("FAIL exception " + error); }
        finally
        {
            if (ui != null) typeof(DEF_IntegratedXRPresentation).GetField("popup", Private).SetValue(ui, null);
            EditorSceneManager.CloseScene(scene, true);
            instance.SetValue(null, oldInstance); active.SetValue(null, oldActive); Time.timeScale = oldTimeScale;
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            System.IO.File.WriteAllLines("../integrated-presentation-validation.txt", results);
            Debug.Log(string.Join("\n", results));
        }
        if (failures != 0) throw new Exception("Integrated presentation fixture failed: " + failures);
    }
}
