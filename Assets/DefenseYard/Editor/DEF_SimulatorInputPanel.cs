using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

// Editor-only accessibility controls for the installed simulator. No gameplay APIs or transform writes.
public sealed class DEF_SimulatorInputPanel : EditorWindow
{
    XRInteractionSimulator simulator;
    readonly List<Action> restore = new();
    bool grip, trigger, aimController;
    int movementDurationIndex;
    readonly double[] movementDurations={.5,2,5};
    bool movementPending, movementActive, movementSawFocus;
    SceneHandle movementScene;
    double stopAt, triggerReleaseAt;
    bool triggerPulse;
    int releaseFrame=-1;
    int sampleFrame=-1;
    double sampleUntil;
    Action pendingInput;
    double pendingUntil, focusedSince = -1;
    void WhenFocused(Action input)
    {
        Zero();pendingInput=input;focusedSince=-1;pendingUntil=EditorApplication.timeSinceStartup+15;
    }
    void Sample()
    {
        if(Time.frameCount==sampleFrame || EditorApplication.timeSinceStartup>sampleUntil)return;
        sampleFrame=Time.frameCount;
        var move=FindFirstObjectByType<ContinuousMoveProvider>();
        var camera=Camera.main;
        var cc=FindFirstObjectByType<CharacterController>();
        var s=$"frame={Time.frameCount} time={Time.time:F3} focus={Application.isFocused} axis={simulator.axis2DInput.ReadValue()} leftSelector={simulator.leftDeviceActionsInput.ReadIsPerformed()} target={simulator.targetedDeviceInput} camera={(camera?camera.transform.position.ToString("F4"):"null")} move={(move?move.isActiveAndEnabled.ToString():"null")} ";
        foreach(var d in InputSystem.devices.Where(d=>d.layout.Contains("Controller")))
            s+=$"device={d.name}/{d.usages} axis={d.TryGetChildControl<UnityEngine.InputSystem.Controls.Vector2Control>("primary2DAxis")?.ReadValue()} ";
        if(move!=null)
        {
            var so=new SerializedObject(move);var p=so.GetIterator();
            while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue is InputActionReference r)
                s+=$"action={r.action?.name} enabled={r.action?.enabled} value={r.action?.ReadValue<Vector2>()} ";
        }
        foreach(var ray in FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(FindObjectsSortMode.None))
        {
            ray.TryGetCurrentUIRaycastResult(out var hit);
            var origin=ray.curveOrigin;
            s+=$" parent={ray.transform.parent?.name} select={ray.selectInput.ReadIsPerformed()} origin={origin?.position.ToString("F3")} forward={origin?.forward.ToString("F3")}";
            if(origin && Physics.Raycast(origin.position,origin.forward,out var physicalHit,100))s+=$" physicalHit={physicalHit.collider.name}/{physicalHit.distance:F3}";
            foreach(var selected in ray.interactablesSelected)s+=$" selectedObject={selected.transform.name}";
            foreach(var hovered in ray.interactablesHovered)s+=$" hoveredObject={hovered.transform.name}";
            s+=$" ray={ray.name} active={ray.isActiveAndEnabled} ui={ray.enableUIInteraction} press={ray.uiPressInput.ReadIsPerformed()} hit={hit.gameObject?.name} selected={ray.interactablesSelected.Count} hovered={ray.interactablesHovered.Count}";
        }
        if(cc)s+=$"cc={cc.transform.position:F4} enabled={cc.enabled} flags={cc.collisionFlags} velocity={cc.velocity:F4}";
        File.AppendAllText(Path.GetFullPath("../simulator-live-trace.txt"),s+Environment.NewLine);
    }
    [MenuItem("Defense Yard/MP2B/Desktop Simulator Input Panel")]
    public static void Open() => GetWindow<DEF_SimulatorInputPanel>("TEST simulator controls");
    void OnEnable() { EditorApplication.update+=Tick; EditorApplication.playModeStateChanged+=Mode; }
    void OnDisable() { Restore(); EditorApplication.update-=Tick; EditorApplication.playModeStateChanged-=Mode; }
    void Mode(PlayModeStateChange state) { if(state==PlayModeStateChange.ExitingPlayMode)Restore(); }
    bool Allowed => Application.isPlaying && (SceneManager.GetActiveScene().name=="DEF_Desktop_Recording_Demo" || SceneManager.GetActiveScene().name=="DEF_Power_Desktop_Demo" || SceneManager.GetActiveScene().name=="DEF_Full_Desktop_Demo");
    void Manual(XRInputValueReader<float> reader)
    {
        var mode=reader.inputSourceMode;var value=reader.manualValue;
        restore.Add(()=>{reader.manualValue=value;reader.inputSourceMode=mode;});
        reader.inputSourceMode=XRInputValueReader.InputSourceMode.ManualValue;reader.manualValue=0;
    }
    void Manual(XRInputValueReader<Vector2> reader)
    {
        var mode=reader.inputSourceMode;var value=reader.manualValue;
        restore.Add(()=>{reader.manualValue=value;reader.inputSourceMode=mode;});
        reader.inputSourceMode=XRInputValueReader.InputSourceMode.ManualValue;reader.manualValue=Vector2.zero;
    }
    void Manual(XRInputButtonReader reader)
    {
        var mode=reader.inputSourceMode;var value=reader.manualValue;var performed=reader.manualPerformed;
        restore.Add(()=>{reader.manualValue=value;reader.manualPerformed=performed;reader.inputSourceMode=mode;});
        reader.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;reader.QueueManualState(false,0);
    }
    [MenuItem("Defense Yard/MP2B/Repair Desktop Simulator Ownership")]
    public static void RepairOwnership()
    {
        if(Application.isPlaying)throw new Exception("Stop Play before repairing simulator ownership");
        var scene=SceneManager.GetActiveScene();
        if(scene.name!="DEF_Full_Desktop_Demo")throw new Exception("Open DEF_Full_Desktop_Demo first");
        var modern=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<XRInteractionSimulator>(true)).ToArray();
        if(modern.Length!=1)throw new Exception("Expected exactly one intended XR Interaction Simulator");
        foreach(var legacy in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<XRDeviceSimulator>(true)).ToArray())
        {
            if(legacy.GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>(true)!=null)throw new Exception("Refusing to remove simulator containing XR rig");
            Undo.DestroyObjectImmediate(legacy.gameObject);
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("Desktop simulator ownership repaired; one modern simulator retained. Restart Play and bind.");
    }
    void Bind()
    {
        if(!Allowed){Debug.LogError("Simulator binding requires Play Mode in a supported Defense desktop demo.");return;}
        simulator=FindFirstObjectByType<XRInteractionSimulator>();if(simulator==null){Debug.LogError("No active XR Interaction Simulator. Stop Play, run Defense Yard/MP2B/Repair Desktop Simulator Ownership, then restart Play.");return;}
        // Test-only deterministic braking: XRI can retain airborne velocity after
        // input becomes zero. Full air control makes neutral input clear it on Update.
        foreach(var provider in FindObjectsByType<ContinuousMoveProvider>(FindObjectsSortMode.None))
        {
            var captured=provider;float prior=provider.inAirControlModifier;
            restore.Add(()=>{if(captured!=null)captured.inAirControlModifier=prior;});
            provider.inAirControlModifier=1f;
        }
        var bound=simulator;var target=bound.targetedDeviceInput;var point=bound.usePointAndClick;var axes=bound.axis2DTargets;
        restore.Add(()=>{if(bound!=null){bound.targetedDeviceInput=target;bound.usePointAndClick=point;bound.axis2DTargets=axes;}});
        Manual(simulator.axis2DInput);
        Manual(simulator.leftDeviceActionsInput);
        Manual(simulator.translateXInput);Manual(simulator.translateYInput);Manual(simulator.translateZInput);
        Manual(simulator.keyboardRotationDeltaInput);Manual(simulator.toggleMouseInput);
        Manual(simulator.gripInput);Manual(simulator.triggerInput);
        simulator.usePointAndClick=false;simulator.targetedDeviceInput=TargetedDevices.FPS|TargetedDevices.RightDevice;
    }
    void Zero()
    {
        if(simulator==null)return;
        simulator.translateXInput.manualValue=simulator.translateYInput.manualValue=simulator.translateZInput.manualValue=0;
        simulator.keyboardRotationDeltaInput.manualValue=Vector2.zero;
        simulator.axis2DInput.manualValue=Vector2.zero;
    }
    void Restore()
    {
        CancelMovement();pendingInput=null;triggerPulse=false;
        Zero();
        var actions=restore.ToArray();restore.Clear();
        var bound=simulator;simulator=null;grip=trigger=false;releaseFrame=-1;
        void Finish() {for(int i=actions.Length-1;i>=0;i--)actions[i]();}
        if(bound!=null && Application.isPlaying)
        {
            bound.gripInput.QueueManualState(false,0);bound.triggerInput.QueueManualState(false,0);
            int frame=Time.frameCount+2;
            void AfterRelease() {if(Application.isPlaying && Time.frameCount<frame)return;EditorApplication.update-=AfterRelease;Finish();}
            EditorApplication.update+=AfterRelease;
        }
        else Finish();
    }
    void Tick()
    {
        if(simulator==null)return;
        if(!Allowed){Restore();return;}
        if(movementPending||movementActive)
        {
            if(!SceneManager.GetActiveScene().handle.Equals(movementScene) || (movementSawFocus&&!Application.isFocused))CancelMovement();
            else if(Application.isFocused)movementSawFocus=true;
            if(movementActive&&EditorApplication.timeSinceStartup>=stopAt)CancelMovement();
        }
        if(pendingInput!=null)
        {
            if(EditorApplication.timeSinceStartup>pendingUntil){pendingInput=null;movementPending=false;}
            else if(!Application.isFocused)focusedSince=-1;
            else if(focusedSince<0)focusedSince=EditorApplication.timeSinceStartup;
            else if(EditorApplication.timeSinceStartup-focusedSince>=.3){var input=pendingInput;pendingInput=null;input();}
        }
        if(triggerPulse && EditorApplication.timeSinceStartup>=triggerReleaseAt){simulator.triggerInput.QueueManualState(false,0);triggerPulse=false;}
        Sample();
        if(EditorApplication.timeSinceStartup>=stopAt)Zero();
        if(releaseFrame>=0 && Time.frameCount>=releaseFrame)Restore();
        Repaint();
    }
    void Move(float x,float z)
    {
        CancelMovement();
        movementPending=true;movementScene=SceneManager.GetActiveScene().handle;movementSawFocus=false;
        double duration=movementDurations[movementDurationIndex];
        WhenFocused(()=>MoveFocused(x,z,duration));
    }
    void CancelMovement()
    {
        if(movementPending)pendingInput=null;
        movementPending=false;movementActive=false;movementSawFocus=false;Zero();
    }
    void MoveFocused(float x,float z,double duration)
    {
        movementPending=false;movementActive=true;movementSawFocus=true;
        Zero();simulator.targetedDeviceInput=TargetedDevices.LeftDevice;
        simulator.axis2DTargets=Axis2DTargets.Primary2DAxis;
        simulator.leftDeviceActionsInput.QueueManualState(true,1);
        simulator.axis2DInput.manualValue=new Vector2(x,z);stopAt=EditorApplication.timeSinceStartup+duration;
        sampleUntil=stopAt+1.5;sampleFrame=-1;
    }
    void Turn(float x,float y)
    {
        WhenFocused(()=>TurnFocused(x,y));
    }
    void TurnFocused(float x,float y)
    {
        Zero();simulator.targetedDeviceInput=aimController?TargetedDevices.RightDevice:TargetedDevices.HMD;
        simulator.keyboardRotationDeltaInput.manualValue=new Vector2(x,y)*5;stopAt=EditorApplication.timeSinceStartup+.5;
    }
    void Hand(float y)
    {
        WhenFocused(()=>{Zero();simulator.usePointAndClick=false;simulator.targetedDeviceInput=TargetedDevices.RightDevice;
            simulator.leftDeviceActionsInput.QueueManualState(false,0);simulator.translateYInput.manualValue=y;stopAt=EditorApplication.timeSinceStartup+.5;});
    }
    void OnGUI()
    {
        EditorGUILayout.HelpBox("TEST ONLY: choose movement/aim, then click Game view within 15 seconds. Pulses start only with game focus. No completion overrides.",MessageType.Info);
        if(!Allowed){EditorGUILayout.LabelField("Play a named desktop demo to enable controls.");return;}
        if(simulator==null){if(GUILayout.Button("Bind test simulator inputs"))Bind();return;}
        EditorGUILayout.LabelField("Camera",Camera.main!=null?Camera.main.transform.position.ToString("F2"):"missing");
        movementDurationIndex=GUILayout.Toolbar(movementDurationIndex,new[]{"0.5 seconds","2 seconds","5 seconds"});
        EditorGUILayout.LabelField("Movement",movementPending?"Waiting for Game focus":movementActive?$"{Math.Max(0,stopAt-EditorApplication.timeSinceStartup):F1}s remaining":"Stopped");
        if(GUILayout.Button("Forward timed pulse"))Move(0,1);
        if(GUILayout.Button("Back timed pulse"))Move(0,-1);
        EditorGUILayout.BeginHorizontal();if(GUILayout.Button("Left"))Move(-1,0);if(GUILayout.Button("Right"))Move(1,0);EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();if(GUILayout.Button("Turn left"))Turn(-1,0);if(GUILayout.Button("Turn right"))Turn(1,0);EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();if(GUILayout.Button("Aim up"))Turn(0,1);if(GUILayout.Button("Aim down"))Turn(0,-1);EditorGUILayout.EndHorizontal();
        if(GUILayout.Button(aimController?"Aim head instead":"Aim right controller instead"))aimController=!aimController;
        if(GUILayout.Button(grip?"Release grip":"Hold grip"))WhenFocused(()=>{simulator.targetedDeviceInput=TargetedDevices.RightDevice;simulator.leftDeviceActionsInput.QueueManualState(false,0);grip=!grip;simulator.gripInput.QueueManualState(grip,grip?1:0);sampleUntil=EditorApplication.timeSinceStartup+2;});
        if(GUILayout.Button(trigger?"Release trigger":"Hold trigger")){simulator.targetedDeviceInput=TargetedDevices.RightDevice;simulator.leftDeviceActionsInput.QueueManualState(false,0);trigger=!trigger;simulator.triggerInput.QueueManualState(trigger,trigger?1:0);}
        if(GUILayout.Button("Trigger pulse after Game focus"))WhenFocused(()=>{simulator.targetedDeviceInput=TargetedDevices.RightDevice;simulator.leftDeviceActionsInput.QueueManualState(false,0);simulator.triggerInput.QueueManualState(true,1);triggerPulse=true;triggerReleaseAt=EditorApplication.timeSinceStartup+.5;sampleUntil=EditorApplication.timeSinceStartup+1.5;});
        EditorGUILayout.BeginHorizontal();if(GUILayout.Button("Right hand down"))Hand(-1);if(GUILayout.Button("Right hand up"))Hand(1);EditorGUILayout.EndHorizontal();
        if(GUILayout.Button("Trace pointing for 15 seconds")){sampleUntil=EditorApplication.timeSinceStartup+15;sampleFrame=-1;simulator.usePointAndClick=true;simulator.targetedDeviceInput=TargetedDevices.RightDevice;simulator.leftDeviceActionsInput.QueueManualState(false,0);}
        if(GUILayout.Button("Stop and restore test inputs"))
        {CancelMovement();pendingInput=null;Zero();simulator.gripInput.QueueManualState(false,0);simulator.triggerInput.QueueManualState(false,0);releaseFrame=Time.frameCount+2;}
    }
}



