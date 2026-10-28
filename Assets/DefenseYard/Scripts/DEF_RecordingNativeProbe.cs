#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using SimState=UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRSimulatedControllerState;
using SimButton=UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.ControllerButton;
public sealed class DEF_RecordingNativeProbe : MonoBehaviour
{
 readonly List<string> results=new();
 UnityEngine.InputSystem.InputDevice ownedDevice;
 UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRSimulatedController moveDevice;
 Unity.XR.CoreUtils.XROrigin walkRig;
 bool secondRun;
 public bool presentationOnly;
 void OnDestroy(){if(ownedDevice!=null&&ownedDevice.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(ownedDevice);if(moveDevice!=null&&moveDevice.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(moveDevice);}
 bool Walkable(Vector2Int cell)
 {
  var point=new Vector3(cell.x*.5f,.05f,cell.y*.5f);
  if(!Physics.Raycast(point+Vector3.up*.4f,Vector3.down,out var floor,.6f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||floor.point.y<-.1f||floor.point.y>.4f)return false;
  return !Physics.OverlapCapsule(floor.point+Vector3.up*.35f,floor.point+Vector3.up*1.55f,.25f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(walkRig.transform)&&!(c.GetComponentInParent<SCV_CraftedItem>() is SCV_CraftedItem item&&item.IsHeld));
 }
 IEnumerator WalkTo(Vector3 target)
 {
  if(Vector2.Distance(new Vector2(walkRig.transform.position.x,walkRig.transform.position.z),new Vector2(target.x,target.z))<.6f)yield break;
  var start=new Vector2Int(Mathf.RoundToInt(walkRig.transform.position.x*2),Mathf.RoundToInt(walkRig.transform.position.z*2));
  var desired=new Vector2Int(Mathf.RoundToInt(target.x*2),Mathf.RoundToInt(target.z*2));
  var candidates=new List<Vector2Int>();for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++){var c=desired+new Vector2Int(x,z);if(Walkable(c))candidates.Add(c);}
  Check(candidates.Count>0,"walkable standing area near interaction "+target);
  var goal=candidates.OrderBy(c=>(c-desired).sqrMagnitude).First();
  var open=new List<Vector2Int>{start};var cost=new Dictionary<Vector2Int,int>{{start,0}};var previous=new Dictionary<Vector2Int,Vector2Int>();bool found=false;
  while(open.Count>0){var current=open.OrderBy(c=>cost[c]+Mathf.Abs(c.x-goal.x)+Mathf.Abs(c.y-goal.y)).First();open.Remove(current);if(current==goal){found=true;break;}
   foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}){var next=current+d;if(next.x< -10||next.x>96||next.y< -20||next.y>40||cost.ContainsKey(next)||!Walkable(next))continue;cost[next]=cost[current]+1;previous[next]=current;open.Add(next);}}
  Check(found,"capsule-safe contiguous route to "+target);
  var path=new List<Vector2Int>();for(var step=goal;step!=start;step=previous[step])path.Add(step);path.Reverse();
  var state=new SimState{isTracked=true,trackingState=3,devicePosition=new Vector3(-.2f,1.2f,.3f),deviceRotation=Quaternion.identity};
  float deadline=Time.realtimeSinceStartup+120;
  foreach(var cell in path){var point=new Vector3(cell.x*.5f,walkRig.transform.position.y,cell.y*.5f);
   while(Vector3.Distance(new Vector3(walkRig.transform.position.x,point.y,walkRig.transform.position.z),point)>.18f&&Time.realtimeSinceStartup<deadline){var delta=point-walkRig.transform.position;delta.y=0;var forward=Vector3.ProjectOnPlane(walkRig.Camera.transform.forward,Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,forward);var direction=delta.normalized;state.primary2DAxis=new Vector2(Vector3.Dot(direction,right),Vector3.Dot(direction,forward))*.6f;UnityEngine.InputSystem.InputSystem.QueueStateEvent(moveDevice,state);yield return new WaitForFixedUpdate();}
   if(Time.realtimeSinceStartup>=deadline)break;
  }
  state.primary2DAxis=Vector2.zero;UnityEngine.InputSystem.InputSystem.QueueStateEvent(moveDevice,state);yield return null;
  Check(Vector2.Distance(new Vector2(walkRig.transform.position.x,walkRig.transform.position.z),new Vector2(goal.x*.5f,goal.y*.5f))<.5f,"actual thumbstick locomotion reaches interaction: "+walkRig.transform.position);
 }
 Button BoundButton(UnityEngine.Object target,string method)=>FindObjectsByType<Button>(FindObjectsInactive.Include).Single(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentTarget(i)==target&&b.onClick.GetPersistentMethodName(i)==method));
 IEnumerator PressUI(Button button,Canvas canvas,UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRSimulatedController device,Transform hand,bool approach=true)
 {
  canvas=button.GetComponentInParent<Canvas>();
  Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;var center=rect.TransformPoint(rect.rect.center);
  if(approach&&!canvas.transform.IsChildOf(walkRig.Camera.transform))yield return WalkTo(center-canvas.transform.forward*1.2f);
  var aim=walkRig.transform.position+Vector3.up*1.2f+walkRig.Camera.transform.right*.25f;
  var state=new SimState{isTracked=true,trackingState=3,devicePosition=hand.parent.InverseTransformPoint(aim),deviceRotation=Quaternion.Inverse(hand.parent.rotation)*Quaternion.LookRotation(center-aim)};
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,state);yield return new WaitForSecondsRealtime(.5f);
  var interactor=hand.GetComponentInChildren<NearFarInteractor>(true);bool hovering=false;float hoverDeadline=Time.realtimeSinceStartup+5;GameObject hitObject=null;
  while(Time.realtimeSinceStartup<hoverDeadline){if(interactor.TryGetCurrentUIRaycastResult(out var hit)){hitObject=hit.gameObject;hovering=hitObject!=null&&(hitObject==button.gameObject||hitObject.transform.IsChildOf(button.transform));}if(hovering)break;yield return null;}
  Check(hovering&&button.IsActive()&&button.IsInteractable(),"actual ray hovers ready button "+button.name+"; hit="+(hitObject!=null?hitObject.name:"none"));
  string buttonName=button.name;var clickEvent=button.onClick;bool clicked=false;UnityEngine.Events.UnityAction observed=()=>clicked=true;clickEvent.AddListener(observed);
  state.trigger=1;state=state.WithButton(SimButton.TriggerButton);UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,state);yield return new WaitForSecondsRealtime(.12f);
  state.trigger=0;state=state.WithButton(SimButton.TriggerButton,false);UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,state);yield return new WaitForSecondsRealtime(.12f);
  clickEvent.RemoveListener(observed);Check(clicked,"actual trigger press/release clicks "+buttonName);
  if(buttonName=="Build shop")Capture("open-shop-"+DEF_Progression.Current.State.Current,canvas.transform.position);
 }
 IEnumerator CarryAndTurn(SCV_CraftedItem kit,SCV_CraftedItem ammo,UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRSimulatedController rightDevice,Transform rightHand)
 {
  var leftHand=walkRig.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Left Controller");
  var leftInteractor=leftHand.GetComponentInChildren<NearFarInteractor>(true);var rightInteractor=rightHand.GetComponentInChildren<NearFarInteractor>(true);
  Check(walkRig.GetComponentsInChildren<MonoBehaviour>(true).Where(b=>b!=null&&b.GetType().Name=="ControllerInputActionManager").All(b=>b.enabled),"normal controller input managers remain active during carry tests");
  var setup=FindAnyObjectByType<DEF_WorkshopRecordingSetup>();var originalId=kit.UnitId;var originalLedger=kit.SourceLedger;var body=kit.GetComponent<Rigidbody>();
  yield return new WaitForFixedUpdate();var originalPosition=body.position;var reservation=new object();
  Check(kit.TryClaim(reservation),"genuine item can reserve for recovery safety test");body.position=new Vector3(originalPosition.x,-3,originalPosition.z);setup.SendMessage("FixedUpdate");
  Check(Mathf.Abs(body.position.y+3)<.00001f&&kit.UnitId==originalId&&kit.SourceLedger==originalLedger,"recovery never moves a reserved genuine item");body.position=originalPosition;kit.Release(reservation);
  body.position=new Vector3(originalPosition.x,-3,originalPosition.z);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
  Check(body.position.y> -2&&kit.UnitId==originalId&&kit.SourceLedger==originalLedger&&!kit.Consumed,"off-route recovery preserves the same paid identity without consumption");
  foreach(int mask in new[]{1,2,3}){
   yield return WalkTo(kit.GetComponent<XRGrabInteractable>().colliders.First(c=>c!=null).bounds.center+Vector3.right*1.5f+Vector3.forward);
   var leftTarget=kit.GetComponent<XRGrabInteractable>().colliders.First(c=>c!=null).bounds.center;
   var rightItem=mask==3?ammo:kit;var rightTarget=rightItem.GetComponent<XRGrabInteractable>().colliders.First(c=>c!=null).bounds.center;
   var leftPosition=walkRig.Camera.transform.position-walkRig.Camera.transform.right*.25f-Vector3.up*.3f;var rightPosition=walkRig.Camera.transform.position+walkRig.Camera.transform.right*.25f-Vector3.up*.3f;
   var ls=new SimState{isTracked=true,trackingState=3,devicePosition=leftHand.parent.InverseTransformPoint(leftPosition),deviceRotation=Quaternion.Inverse(leftHand.parent.rotation)*Quaternion.LookRotation(leftTarget-leftPosition)};
   var rs=new SimState{isTracked=true,trackingState=3,devicePosition=rightHand.parent.InverseTransformPoint(rightPosition),deviceRotation=Quaternion.Inverse(rightHand.parent.rotation)*Quaternion.LookRotation(rightTarget-rightPosition)};
   UnityEngine.InputSystem.InputSystem.QueueStateEvent(moveDevice,ls);UnityEngine.InputSystem.InputSystem.QueueStateEvent(rightDevice,rs);yield return new WaitForSecondsRealtime(.5f);
   if((mask&1)!=0){ls.grip=1;ls=ls.WithButton(SimButton.GripButton);}if((mask&2)!=0){rs.grip=1;rs=rs.WithButton(SimButton.GripButton);}
   UnityEngine.InputSystem.InputSystem.QueueStateEvent(moveDevice,ls);UnityEngine.InputSystem.InputSystem.QueueStateEvent(rightDevice,rs);yield return new WaitForSecondsRealtime(.5f);
   Check(((mask&1)==0||kit.GetComponent<XRGrabInteractable>().interactorsSelecting.Contains(leftInteractor))&&((mask&2)==0||rightItem.GetComponent<XRGrabInteractable>().interactorsSelecting.Contains(rightInteractor)),"real far grip selection for hand mask "+mask+" left="+leftInteractor.selectionRegion.Value+" right="+rightInteractor.selectionRegion.Value);
   originalPosition=body.position;body.position=new Vector3(originalPosition.x,-3,originalPosition.z);setup.SendMessage("FixedUpdate");
   Check(Mathf.Abs(body.position.y+3)<.00001f&&kit.IsHeld,"recovery never moves a held genuine item mask "+mask);body.position=originalPosition;
   var position=walkRig.transform.position;float yaw=walkRig.transform.eulerAngles.y;
   ls.primary2DAxis=new Vector2(0,.6f);rs.primary2DAxis=new Vector2(.8f,0);
   for(int i=0;i<30;i++){UnityEngine.InputSystem.InputSystem.QueueStateEvent(moveDevice,ls);UnityEngine.InputSystem.InputSystem.QueueStateEvent(rightDevice,rs);yield return new WaitForFixedUpdate();}
   Check(Vector3.Distance(position,walkRig.transform.position)>.3f&&Mathf.Abs(Mathf.DeltaAngle(yaw,walkRig.transform.eulerAngles.y))>15,"sustained grip plus simultaneous real move and turn for hand mask "+mask);
   Check(((mask&1)==0||leftInteractor.hasSelection)&&((mask&2)==0||rightInteractor.hasSelection),"held genuine outputs retained during move/turn mask "+mask);
   ls.grip=rs.grip=0;ls=ls.WithButton(SimButton.GripButton,false);rs=rs.WithButton(SimButton.GripButton,false);
   for(int i=0;i<5;i++){UnityEngine.InputSystem.InputSystem.QueueStateEvent(moveDevice,ls);UnityEngine.InputSystem.InputSystem.QueueStateEvent(rightDevice,rs);yield return new WaitForFixedUpdate();}
   Check(!kit.IsHeld&&!ammo.IsHeld&&!kit.Consumed&&!ammo.Consumed,"release while sticks deflected preserves real outputs mask "+mask);
   ls.primary2DAxis=rs.primary2DAxis=Vector2.zero;UnityEngine.InputSystem.InputSystem.QueueStateEvent(moveDevice,ls);UnityEngine.InputSystem.InputSystem.QueueStateEvent(rightDevice,rs);yield return new WaitForSecondsRealtime(.5f);
   // After far release, approach the real dropped object and take it with near grip.
   yield return new WaitForSecondsRealtime(2f);
   yield return WalkTo(kit.transform.position);
   ls.devicePosition=leftHand.parent.InverseTransformPoint(kit.GetComponent<XRGrabInteractable>().colliders.First(c=>c!=null).bounds.center);
   UnityEngine.InputSystem.InputSystem.QueueStateEvent(moveDevice,ls);yield return new WaitForSecondsRealtime(.5f);
   ls.grip=1;ls=ls.WithButton(SimButton.GripButton);UnityEngine.InputSystem.InputSystem.QueueStateEvent(moveDevice,ls);yield return new WaitForSecondsRealtime(.5f);
   Check(kit.GetComponent<XRGrabInteractable>().interactorsSelecting.Contains(leftInteractor)&&leftInteractor.selectionRegion.Value.ToString()=="Near","far-to-near real re-grab after release mask "+mask);
   position=walkRig.transform.position;yaw=walkRig.transform.eulerAngles.y;ls.primary2DAxis=new Vector2(0,.6f);rs.primary2DAxis=new Vector2(.8f,0);
   for(int i=0;i<30;i++){UnityEngine.InputSystem.InputSystem.QueueStateEvent(moveDevice,ls);UnityEngine.InputSystem.InputSystem.QueueStateEvent(rightDevice,rs);yield return new WaitForFixedUpdate();}
   Check(Vector3.Distance(position,walkRig.transform.position)>.3f&&Mathf.Abs(Mathf.DeltaAngle(yaw,walkRig.transform.eulerAngles.y))>15,"near grip preserves simultaneous move and turn mask "+mask);
   ls.grip=0;ls=ls.WithButton(SimButton.GripButton,false);UnityEngine.InputSystem.InputSystem.QueueStateEvent(moveDevice,ls);yield return new WaitForSecondsRealtime(.1f);
   ls.primary2DAxis=rs.primary2DAxis=Vector2.zero;UnityEngine.InputSystem.InputSystem.QueueStateEvent(moveDevice,ls);UnityEngine.InputSystem.InputSystem.QueueStateEvent(rightDevice,rs);yield return new WaitForSecondsRealtime(2f);
   Check(!kit.Consumed&&!ammo.Consumed&&kit.transform.position.y> -2&&ammo.transform.position.y> -2,"released authentic supplies remain recoverable after locomotion mask "+mask);
  }
 }
 void RayClick(Button button,Canvas canvas)
 {
  Canvas.ForceUpdateCanvases();
  var rect=(RectTransform)button.transform;var center=rect.TransformPoint(rect.rect.center);
  var data=new UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceEventData(EventSystem.current);
  data.rayPoints=new List<Vector3>{center-canvas.transform.forward*1.5f,center+canvas.transform.forward*.1f};data.layerMask=~0;
  var hits=new List<RaycastResult>();canvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>().Raycast(data,hits);
  Check(hits.Any(h=>h.gameObject==button.gameObject || h.gameObject.transform.IsChildOf(button.transform)),"tracked UI ray reaches "+button.name);
  ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
 }
 static string ResultPath=>System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../workshop-native-play-results.txt"));
 // Render the real scene from the current player's position, looking at the inspected object.
 // This is a diagnostic camera view, not a claim of headset capture or simulated head input.
 void Capture(string name,Vector3 target)
 {
  var camera=walkRig.Camera;var oldRotation=camera.transform.rotation;var oldTarget=camera.targetTexture;
  var previous=RenderTexture.active;var texture=new RenderTexture(1280,720,24);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
  try{Canvas.ForceUpdateCanvases();camera.transform.LookAt(target);camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ResultPath),"recording-"+(secondRun?"second-":"first-")+name+".png"),pixels.EncodeToPNG());}
  finally{camera.transform.rotation=oldRotation;camera.targetTexture=oldTarget;RenderTexture.active=previous;texture.Release();Destroy(texture);Destroy(pixels);}
 }
 void Check(bool condition,string label){results.Add((condition?"PASS ":"FAIL ")+label);System.IO.File.WriteAllLines(ResultPath,results);if(!condition)throw new Exception(label);}
 IEnumerator Start()
 {
  yield return new WaitForSecondsRealtime(2);
  var sequences=new Stack<IEnumerator>();sequences.Push(Run());bool failed=false;
  while(sequences.Count>0){object current=null;bool more=false;try{var sequence=sequences.Peek();more=sequence.MoveNext();if(more)current=sequence.Current;else sequences.Pop();}catch(Exception e){results.Add("EXCEPTION "+e);failed=true;}if(failed)break;if(!more)continue;if(current is IEnumerator nested)sequences.Push(nested);else yield return current;}
  System.IO.File.WriteAllLines(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../workshop-native-play-results.txt")),results);
  if(Application.isBatchMode)UnityEditor.EditorApplication.Exit(failed?1:0);else if(!failed)UnityEditor.EditorApplication.isPlaying=false;
 }
 IEnumerator Run()
 {
  if(secondRun){if(ownedDevice!=null&&ownedDevice.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(ownedDevice);if(moveDevice!=null&&moveDevice.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(moveDevice);}
  var p=DEF_Progression.Current;var hud=DEF_HUD.Instance;var rig=FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
  walkRig=rig;
  var description=new UnityEngine.InputSystem.Layouts.InputDeviceDescription{product="XRSimulatedController",capabilities=new UnityEngine.InputSystem.XR.XRDeviceDescriptor{deviceName="Recording Right Controller",characteristics=UnityEngine.XR.InputDeviceCharacteristics.Right|UnityEngine.XR.InputDeviceCharacteristics.Controller|UnityEngine.XR.InputDeviceCharacteristics.TrackedDevice|UnityEngine.XR.InputDeviceCharacteristics.HeldInHand}.ToJson()};
  var device=(UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRSimulatedController)UnityEngine.InputSystem.InputSystem.AddDevice(description);ownedDevice=device;
  UnityEngine.InputSystem.InputSystem.SetDeviceUsage(device,UnityEngine.InputSystem.CommonUsages.RightHand);
  var leftDescription=new UnityEngine.InputSystem.Layouts.InputDeviceDescription{product="XRSimulatedController",capabilities=new UnityEngine.InputSystem.XR.XRDeviceDescriptor{deviceName="Recording Left Controller",characteristics=UnityEngine.XR.InputDeviceCharacteristics.Left|UnityEngine.XR.InputDeviceCharacteristics.Controller|UnityEngine.XR.InputDeviceCharacteristics.TrackedDevice|UnityEngine.XR.InputDeviceCharacteristics.HeldInHand}.ToJson()};
  moveDevice=(UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRSimulatedController)UnityEngine.InputSystem.InputSystem.AddDevice(leftDescription);UnityEngine.InputSystem.InputSystem.SetDeviceUsage(moveDevice,UnityEngine.InputSystem.CommonUsages.LeftHand);
  var hand=rig.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Right Controller");
  DEF_VRInput.Instance.simulateInEditor=true;yield return null;yield return null;
  Check(p!=null&&p.workshopRecording,"actual scene recording configuration");
  Check(p.State.Current==DEF_ProgressionState.Stage.TeamPrerequisites,"no initial mastery or readiness");
  var awards=FindAnyObjectByType<DEF_RecordingAchievements>();Check(awards!=null&&!awards.fieldMedic.activeSelf&&!awards.supplyLine.activeSelf&&!awards.waveDefender.activeSelf,"three trophies initially absent");
  Check(DEF_GameState.I.scrap==0 && DEF_GameState.I.ammo==0,"saved empty Defense inventory; no fixture preload");
  Check(FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None).Length==1,"one real rig");
  Check(Physics.Raycast(rig.transform.position+Vector3.up,Vector3.down,out var floor,2),"spawn has physical ground");
  Check(rig.transform.position.y>=floor.point.y-.02f,"spawn feet not below floor");
  var obstruction=Physics.OverlapCapsule(rig.transform.position+Vector3.up*.3f,rig.transform.position+Vector3.up*1.6f,.22f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(rig.transform)).ToArray();
  Check(obstruction.Length==0,"standing capsule clearance: "+string.Join(",",obstruction.Select(c=>c.name)));
  for(int z=-2;z<=2;z++)Check(!Physics.CheckCapsule(new Vector3(20.5f,.35f,z),new Vector3(20.5f,1.55f,z),.3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),"saved east entrance clears standing capsule at z="+z);
  Check(!rig.GetComponentsInChildren<MonoBehaviour>(true).Any(b=>b!=null&&b.GetType().Name=="JumpProvider"&&b.enabled),"Jump disabled; no A collision");
  var canvas=hud.GetComponent<Canvas>();
  Check(canvas.renderMode==RenderMode.WorldSpace&&canvas.worldCamera==rig.Camera,"saved world canvas camera assigned");
  Check(canvas.transform.parent==rig.Camera.transform&&Vector3.Distance(canvas.transform.localPosition,new Vector3(0,0,1.6f))<.01f,"saved HUD follows the actual tracked camera at a fixed local distance");
  var originalHeadPosition=rig.Camera.transform.localPosition;var originalHeadRotation=rig.Camera.transform.localRotation;
  rig.Camera.transform.localPosition+=new Vector3(.15f,.1f,.2f);rig.Camera.transform.localRotation=Quaternion.Euler(10,35,0);yield return null;
  Check(Vector3.Distance(canvas.transform.position,rig.Camera.transform.TransformPoint(new Vector3(0,0,1.6f)))<.001f&&Quaternion.Angle(canvas.transform.rotation,rig.Camera.transform.rotation)<.01f,"HUD follows head translation and rotation without a world-space lag");
  Capture("tracked-hud-pose",rig.Camera.transform.position+rig.Camera.transform.forward*1.6f);
  rig.Camera.transform.localPosition=originalHeadPosition;rig.Camera.transform.localRotation=originalHeadRotation;yield return null;
  Check(canvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>()!=null,"tracked UI raycaster exists");
  var missingCameras=FindObjectsByType<Canvas>(FindObjectsInactive.Include).Where(c=>c.gameObject.scene==rig.gameObject.scene&&c.renderMode==RenderMode.WorldSpace&&c.worldCamera!=rig.Camera).Select(c=>c.name).ToArray();
  Check(missingCameras.Length==0,"all world Canvas event cameras bound: "+string.Join(",",missingCameras));
  Capture("spawn",new Vector3(38.5f,1.7f,5));
  var wreck=FindAnyObjectByType<SCV_HarvestableWreck>();Check(wreck!=null,"real spawned salvage");
  var ledger=FindAnyObjectByType<SCV_ResourceManager>();
  int clicks=0;while(wreck!=null&&wreck.ScrapRemaining>0&&clicks++<10){var harvest=BoundButton(wreck,"Harvest");yield return PressUI(harvest,harvest.GetComponentInParent<Canvas>(),device,hand);yield return null;}
  Check(ledger.scrap==12&&clicks==6,"six real harvests yield twelve scrap");
  var workshop=FindAnyObjectByType<SCV_ProgressionManager>();
  Check(!workshop.CrusherReady,"crusher remains hidden on harvest completion frame");
  float crusherDeadline=Time.realtimeSinceStartup+45;while(!workshop.CrusherReady&&Time.realtimeSinceStartup<crusherDeadline)yield return null;
  Check(workshop.CrusherReady,"crusher unlocks after complete introduction");
  var crusher=FindAnyObjectByType<SCV_Crusher>();
  for(int i=0;i<3;i++){var process=BoundButton(crusher,"ProcessScrap");yield return PressUI(process,process.GetComponentInParent<Canvas>(),device,hand);while(crusher.IsProcessing)yield return null;yield return null;}
  Check(ledger.parts==3&&ledger.scrap==6,"real timed crusher transactions conserve six scrap: parts="+ledger.parts+" scrap="+ledger.scrap+" feedback="+crusher.FeedbackMessage);
  float benchDeadline=Time.realtimeSinceStartup+45;while(!workshop.WorkbenchReady&&Time.realtimeSinceStartup<benchDeadline)yield return null;
  Check(workshop.WorkbenchReady,"workbench visible only after its completed introduction");
  var bench=FindAnyObjectByType<SCV_Workbench>();var craftAmmo=BoundButton(bench,"CraftAmmo");yield return PressUI(craftAmmo,craftAmmo.GetComponentInParent<Canvas>(),device,hand);var craftKit=BoundButton(bench,"CraftRepairKit");yield return PressUI(craftKit,craftKit.GetComponentInParent<Canvas>(),device,hand);yield return null;
  Check(p.State.Current==DEF_ProgressionState.Stage.TeamPrerequisites&&!p.IsAllowed(DEF_Action.BasicSentry),"second craft introduces Defense before placement becomes available");
  float readyDeadline=Time.realtimeSinceStartup+30;while(p.State.Current==DEF_ProgressionState.Stage.TeamPrerequisites&&Time.realtimeSinceStartup<readyDeadline)yield return null;
  Check(p.State.Current==DEF_ProgressionState.Stage.PlaceSentry,"both authentic crafts and completed introduction unlock placement");
  var items=FindObjectsByType<SCV_CraftedItem>(FindObjectsSortMode.None);
  var kit=items.Single(x=>x.Type==SCV_CraftedItem.ItemType.RepairKit);var ammo=items.Single(x=>x.Type==SCV_CraftedItem.ItemType.Ammo);
  Check(kit.SourceLedger==ledger&&ammo.SourceLedger==ledger&&!kit.Consumed&&!ammo.Consumed,"same two real item identities preserved");
  yield return CarryAndTurn(kit,ammo,device,hand);
  yield return new WaitForSecondsRealtime(7.3f);
  SCV_DefenseSupplyLink link=null;float supplyDeadline=Time.realtimeSinceStartup+30;
  while((link=FindAnyObjectByType<SCV_DefenseSupplyLink>())==null&&Time.realtimeSinceStartup<supplyDeadline)yield return null;
  Check(link!=null,"supply terminal becomes reachable after its introduction");
  string priorFailure=p.LastFailure;
  Check(!DEF_Turret.TryPlace(13,10,false)&&DEF_GameState.I.scrap==0&&p.LastFailure!=priorFailure&&p.LastFailure.Contains("not enough scrap")&&p.LastFailure.Contains("Transfer Workshop scrap"),"fresh insufficient funds rejection gives actual cause and remedy");
  var recording=FindAnyObjectByType<DEF_WorkshopRecordingSetup>();
  var testWall=GameObject.CreatePrimitive(PrimitiveType.Cube);testWall.name="Probe temporary popup obstruction";testWall.transform.position=rig.Camera.transform.position+Vector3.ProjectOnPlane(rig.Camera.transform.forward,Vector3.up).normalized;testWall.transform.localScale=Vector3.one*.5f;Physics.SyncTransforms();hud.ToggleShop();yield return null;
  Check(!hud.shopPanel.activeSelf&&hud.noticeText.text.Contains("clear space"),"obstructed popup refuses to open and explains face-walkway retry");Destroy(testWall);yield return null;Physics.SyncTransforms();
  hud.ToggleShop();yield return null;
  Check(recording.popupCanvas.renderMode==RenderMode.WorldSpace&&recording.popupCanvas.worldCamera==rig.Camera&&rig.Camera.stereoTargetEye==StereoTargetEyeMask.Both,"shop popup uses the tracked stereo world-space camera");
  var popupOffset=recording.popupCanvas.transform.position-rig.Camera.transform.position;Check(popupOffset.magnitude>1&&popupOffset.magnitude<1.6f&&Vector3.Dot(popupOffset.normalized,Vector3.ProjectOnPlane(rig.Camera.transform.forward,Vector3.up).normalized)>.95f,"A shop popup anchors comfortably in current head direction");
  var transfer=recording.transferScrapButton;yield return PressUI(transfer,recording.popupCanvas,device,hand);yield return PressUI(transfer,recording.popupCanvas,device,hand);yield return PressUI(hud.shopClose,recording.popupCanvas,device,hand);
  Check(DEF_GameState.I.scrap==4&&ledger.scrap==2,"real scrap transfer funds sentry: Defense="+DEF_GameState.I.scrap+" Workshop="+ledger.scrap);
  yield return WalkTo(new Vector3(1,0,7));yield return WalkTo(new Vector3(1,0,3));
  Check(walkRig.transform.position.y>.2f,"actual thumbstick path reaches raised outpost interior without Jump");
  yield return WalkTo(new Vector3(1,0,7));
  priorFailure=p.LastFailure;Check(!DEF_Turret.TryPlace(10,10,false)&&DEF_GameState.I.scrap==4&&p.LastFailure!=priorFailure&&p.LastFailure.Contains("inside the base")&&p.LastFailure.Contains("outside the base"),"fresh inside-base rejection preserves scrap and gives cause/remedy");
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new SimState().WithButton(SimButton.PrimaryButton));yield return null;yield return null;
  Check(Time.timeScale==1&&hud.shopPanel.activeInHierarchy,"actual A binding opens shop without physics freeze");
  var stationaryPopup=recording.popupCanvas.transform.position;var stationaryRotation=recording.popupCanvas.transform.rotation;
  for(int turnFrame=0;turnFrame<20;turnFrame++){UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new SimState{isTracked=true,trackingState=3,primary2DAxis=new Vector2(.8f,0)});yield return new WaitForFixedUpdate();}
  Check(Vector3.Distance(stationaryPopup,recording.popupCanvas.transform.position)<.001f&&Quaternion.Angle(stationaryRotation,recording.popupCanvas.transform.rotation)<.01f,"open popup stays world-fixed while player turns");
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new SimState());yield return null;yield return null;
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new SimState().WithButton(SimButton.PrimaryButton));yield return null;yield return null;
  Check(!hud.shopPanel.activeSelf&&Time.timeScale==1,"actual A closes its own modal safely");
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new SimState());yield return null;yield return null;
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new SimState().WithButton(SimButton.PrimaryButton));yield return null;yield return null;
  popupOffset=recording.popupCanvas.transform.position-rig.Camera.transform.position;
  Check(hud.shopPanel.activeInHierarchy&&Vector3.Dot(popupOffset.normalized,Vector3.ProjectOnPlane(rig.Camera.transform.forward,Vector3.up).normalized)>.95f,"real A reopen reanchors popup to current view");
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new SimState());yield return null;
  Capture("view-popup",recording.popupCanvas.transform.position);
  yield return PressUI(hud.buyButtons[0],canvas,device,hand);yield return null;
  Check(DEF_VRInput.Instance.BuildArmed&&!hud.shopPanel.activeSelf,"real UI button arms sentry and closes menu");
  int cancelScrap=DEF_GameState.I.scrap;yield return PressUI(hud.cancelBuild,canvas,device,hand);
  Check(!DEF_VRInput.Instance.BuildArmed&&DEF_GameState.I.scrap==cancelScrap,"actual selected-panel X cancels placement without spending scrap");
  yield return PressUI(hud.shopButton,canvas,device,hand);yield return PressUI(hud.buyButtons[0],canvas,device,hand);
  var groundPoint=FindAnyObjectByType<DEF_GridManager>().TileCenter(13,10);yield return WalkTo(groundPoint+new Vector3(0,0,4));var aimPosition=walkRig.transform.position+Vector3.up*1.2f+walkRig.Camera.transform.right*.25f;
  var placementState=new SimState{isTracked=true,trackingState=3,devicePosition=hand.parent.InverseTransformPoint(aimPosition),deviceRotation=Quaternion.Inverse(hand.parent.rotation)*Quaternion.LookRotation(groundPoint-aimPosition)};
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,placementState);yield return new WaitForSecondsRealtime(.5f);
  Check(DEF_VRInput.Instance.TryGetAimHit(out var groundHit)&&groundHit.collider==FindAnyObjectByType<DEF_GridManager>().groundCollider,"controller ray reaches actual build ground");
  placementState.trigger=1;placementState=placementState.WithButton(SimButton.TriggerButton);UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,placementState);yield return null;yield return null;
  placementState.trigger=0;placementState=placementState.WithButton(SimButton.TriggerButton,false);UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,placementState);yield return null;
  Check(DEF_Pathfinder.I.OccupantAt(13,10)!=null&&DEF_GameState.I.scrap==0,"actual controller trigger places affordable sentry");
  Check(DEF_GameState.I.ammo==0,"sentry placement grants no fixture ammo");
  DEF_VRInput.Instance.CancelBuild();
  Check(p.State.Current==DEF_ProgressionState.Stage.RepairIntroduction,"placement introduces repair before use");
  Check(!p.IsAllowed(DEF_Action.Repair),"repair remains locked during intro");
  var introReceiver=FindObjectsByType<DEF_WorkshopRepairReceiver>(FindObjectsSortMode.None).Single(x=>x.target is DEF_Turret);
  var introTurret=(DEF_Turret)introReceiver.target;
  Check(introTurret.health==introTurret.maxHealth&&!introReceiver.GetComponent<BoxCollider>().enabled,"repair intro precedes both deliberate damage and receiver collision");
  yield return null;
  Check(introTurret.health==introTurret.maxHealth&&introReceiver.GetComponentsInChildren<Renderer>(true).All(r=>r.forceRenderingOff)&&!hud.buyButtons[1].transform.parent.gameObject.activeSelf&&!hud.buyButtons[2].transform.parent.gameObject.activeSelf&&!hud.buyButtons[3].transform.parent.gameObject.activeSelf,"rendered introduction frame keeps repair and actual advanced card content hidden");
  yield return new WaitForSecondsRealtime(4.3f);
  Check(introTurret.health<introTurret.maxHealth&&introReceiver.GetComponent<BoxCollider>().enabled,"training damage and physical receiver reveal only after repair introduction");
  var receiver=FindObjectsByType<DEF_WorkshopRepairReceiver>(FindObjectsSortMode.None).Single(x=>x.target is DEF_Turret);
  Check(!receiver.TryDeliver(ammo)&&!ammo.Consumed&&receiver.LastMessage.Contains("not a RepairKit"),"wrong-item repair catch preserves real ammo and explains correct item");
  yield return null;Capture("repair-rejection-card",receiver.transform.position+Vector3.up*.85f);
  Check(receiver.GetComponent<DEF_RecordingReceiverLabel>()!=null,"repair intake has local visible feedback");
  var interactor=hand.GetComponentInChildren<NearFarInteractor>(true);
  var grab=kit.GetComponent<XRGrabInteractable>();
  yield return WalkTo(kit.GetComponentInChildren<Collider>().bounds.center+Vector3.right);
  var heldState=new SimState{isTracked=true,trackingState=3,devicePosition=hand.parent.InverseTransformPoint(kit.transform.position),deviceRotation=Quaternion.identity};
  UnityEngine.InputSystem.LowLevel.InputState.Change(device,heldState);yield return new WaitForSecondsRealtime(2);
  Check(hand.gameObject.activeInHierarchy&&Vector3.Distance(hand.position,kit.transform.position)<.5f,"tracked pose reaches unmodified crafted item: hand="+hand.position+" item="+kit.transform.position+" tracked="+device.isTracked.ReadValue());
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,heldState);
  yield return null;yield return null;
  heldState.grip=1;heldState=heldState.WithButton(SimButton.GripButton);
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,heldState);
  for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
  Check(grab.isSelected&&kit.IsHeld,"actual grip input selects crafted kit; hand="+hand.position+" item="+kit.transform.position+" hover="+interactor.hasHover+" input="+interactor.selectInput.ReadIsPerformed()+" grip="+device.grip.ReadValue()+" selected="+string.Join(",",interactor.interactablesSelected.Select(x=>x.transform.name))+" hovered="+string.Join(",",interactor.interactablesHovered.Select(x=>x.transform.name)));
  var priorDelivery=receiver.LastMessage;Check(!receiver.TryDeliver(kit)&&!kit.Consumed&&receiver.LastMessage!=priorDelivery&&receiver.LastMessage.Contains("still held")&&receiver.LastMessage.Contains("Release"),"fresh held-kit rejection explains cause and release remedy without consumption");
  Check(!awards.fieldMedic.activeSelf,"held or rejected kit does not award Field Medic");
  yield return WalkTo(receiver.transform.position+Vector3.right*1.5f);
  var carryStart=heldState.devicePosition;var carryEnd=hand.parent.InverseTransformPoint(receiver.transform.position);
  for(int step=1;step<=60;step++){heldState.devicePosition=Vector3.Lerp(carryStart,carryEnd,step/60f);UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,heldState);yield return new WaitForFixedUpdate();}
  Check(grab.isSelected&&Vector3.Distance(kit.transform.position,receiver.transform.position)<.75f,"gripped kit follows moving controller to receiver");
  heldState.grip=0;heldState=heldState.WithButton(SimButton.GripButton,false);
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,heldState);
  yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
  Check(p.State.Current==DEF_ProgressionState.Stage.AmmoIntroduction,"physical released kit overlap commits repair");
  yield return null;Check(awards.fieldMedic.activeSelf&&!awards.supplyLine.activeSelf,"positive real repair awards only Field Medic");
  yield return null;
  Check(FindAnyObjectByType<DEF_WorkshopRecordingSetup>().RepairTeachingComplete,"first genuine repair retires teaching for this run");
  Check(!receiver.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Physical delivery instructions and feedback").gameObject.activeSelf,"successful repair hides the receiver teaching card");
  Check(!DEF_Tutorials.Allowed("repair")&&!DEF_Tutorials.Allowed("repair-turret"),"later repair tutorials stay retired without disabling repair transactions");
  Check(!p.IsAllowed(DEF_Action.PhysicalAmmo),"ammo remains locked during introduction");
  var introAmmo=FindAnyObjectByType<DEF_WorkshopAmmoReceiver>();
  Check(introAmmo.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled)&&introAmmo.GetComponentsInChildren<Renderer>(true).All(r=>r.forceRenderingOff),"actual ammo receiver presentation and collision stay hidden during introduction");
  yield return new WaitForSecondsRealtime(4.3f);
  var intake=FindAnyObjectByType<DEF_WorkshopAmmoReceiver>();
  var ammoGrab=ammo.GetComponent<XRGrabInteractable>();
  var ammoCollider=ammoGrab.colliders.First(c=>c!=null&&c.enabled);
  yield return WalkTo(ammoCollider.bounds.center+Vector3.right);
  heldState.devicePosition=hand.parent.InverseTransformPoint(ammoCollider.bounds.center);UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,heldState);
  yield return new WaitForSecondsRealtime(.5f);
  heldState.grip=1;heldState=heldState.WithButton(SimButton.GripButton);UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,heldState);
  for(int step=0;step<10;step++)yield return new WaitForFixedUpdate();
  Check(ammo.IsHeld,"actual grip selects same crafted ammo box: hand="+hand.position+" mesh="+ammoCollider.bounds.center+" hover="+interactor.hasHover);
  yield return WalkTo(intake.transform.position+Vector3.right*1.5f);
  carryStart=heldState.devicePosition;carryEnd=hand.parent.InverseTransformPoint(intake.transform.position);
  for(int step=1;step<=60;step++){heldState.devicePosition=Vector3.Lerp(carryStart,carryEnd,step/60f);UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,heldState);yield return new WaitForFixedUpdate();}
  Check(ammo.IsHeld&&!ammo.Consumed,"carried ammo remains unconsumed while held");
  heldState.grip=0;heldState=heldState.WithButton(SimButton.GripButton,false);UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,heldState);
  for(int step=0;step<10;step++)yield return new WaitForFixedUpdate();
  Check(p.State.Current==DEF_ProgressionState.Stage.WaveIntroduction,"same physical ammo reaches real intake");
  yield return null;Check(awards.supplyLine.activeSelf&&!awards.waveDefender.activeSelf,"positive physical refill awards Supply Line, not wave trophy");
  Check(!hud.startButton.gameObject.activeSelf,"actual Start wave control stays hidden during wave introduction");
  yield return new WaitForSecondsRealtime(4.3f);
  Capture("wave-ready-hud",rig.Camera.transform.position+rig.Camera.transform.forward*1.6f);
  Check(p.State.Current==DEF_ProgressionState.Stage.WaveReady,"wave ready only after introduction");
  if(presentationOnly)
  {
   Check(Vector3.Distance(FindAnyObjectByType<DEF_WorkshopAmmoReceiver>().transform.position,new Vector3(1,1.35f,0))<.01f,"same physical intake accepts inside the outpost at its new position");
   Check(awards.fieldMedic.GetComponentsInChildren<Renderer>().Length>=6&&awards.supplyLine.GetComponentsInChildren<Renderer>().Length>=6,"earned trophies contain cup, stem, base and plaque geometry");
   Capture("tidy-earned-shelf",awards.transform.position+Vector3.up*1.1f);
   DontDestroyOnLoad(gameObject);p.BeginNewSession();yield return null;yield return null;yield return null;
   var resetSetup=FindAnyObjectByType<DEF_WorkshopRecordingSetup>();var resetAwards=FindAnyObjectByType<DEF_RecordingAchievements>();
   Check(!resetSetup.RepairTeachingComplete,"full new run restores repair teaching");
   Check(!resetAwards.fieldMedic.activeSelf&&!resetAwards.supplyLine.activeSelf&&!resetAwards.waveDefender.activeSelf,"full new run restores initially absent trophies");
   yield break;
  }
  yield return PressUI(hud.startButton,canvas,device,hand);yield return null;
  Check(p.State.Current==DEF_ProgressionState.Stage.WaveActive,"actual UI starts wave");
  yield return new WaitForSecondsRealtime(1);yield return PressUI(hud.pauseButton,canvas,device,hand);yield return null;
  Check(Time.timeScale==0&&hud.pausePanel.activeInHierarchy,"explicit Pause freezes the active wave while XR shop remains nonpausing");
  var pausedEnemies=DEF_Zombie.all.ToDictionary(z=>z,z=>z.transform.position);float pausedHealth=DEF_GameState.I.playerHealth;int pausedAmmo=DEF_GameState.I.ammo;
  Check(pausedEnemies.Count>0,"pause regression observes a genuinely live enemy, not an empty wave");
  Check(!hud.startButton.interactable,"Start Wave is disabled during active combat");
  yield return new WaitForSecondsRealtime(.5f);
  Check(DEF_GameState.I.playerHealth==pausedHealth&&DEF_GameState.I.ammo==pausedAmmo&&pausedEnemies.All(pair=>pair.Key!=null&&Vector3.Distance(pair.Value,pair.Key.transform.position)<.001f),"paused combat cannot move enemies, spend ammo or damage base");
  yield return PressUI(hud.resumeButton,canvas,device,hand,false);yield return null;
  Check(Time.timeScale==1&&!hud.pausePanel.activeSelf,"actual XR Resume click restores gameplay");
  float deadline=Time.realtimeSinceStartup+120;
  while(p.State.Current==DEF_ProgressionState.Stage.WaveActive&&Time.realtimeSinceStartup<deadline)yield return null;
  Check(p.State.Current==DEF_ProgressionState.Stage.FirstWaveComplete,"real turret combat clears first wave alive");
  yield return null;Check(awards.waveDefender.activeSelf,"normal first-wave clear awards third visible trophy");
  yield return WalkTo(new Vector3(3.9f,0,7.5f));
  Capture("trophies",awards.supplyLine.transform.position+Vector3.up*.2f);
  Check(!p.IsAllowed(DEF_Action.Barricade)&&!p.IsAllowed(DEF_Action.Shock),"advanced actions hidden during their introduction");
  Check(!hud.buyButtons[2].transform.parent.gameObject.activeSelf&&!hud.buyButtons[3].transform.parent.gameObject.activeSelf,"actual Barricade and Shock pictures and descriptions remain hidden during introduction");
  yield return new WaitForSecondsRealtime(4.3f);
  Check(p.State.Current==DEF_ProgressionState.Stage.AdvancedPractice,"advanced practice follows the completed introduction");
  Check(hud.buyButtons[2].transform.parent.gameObject.activeSelf&&hud.buyButtons[3].transform.parent.gameObject.activeSelf,"actual advanced cards reveal after introduction");
  int beforeOccupied=DEF_GameState.I.scrap;priorFailure=p.LastFailure;Check(!DEF_Turret.TryPlace(13,10,false)&&DEF_GameState.I.scrap==beforeOccupied&&p.LastFailure!=priorFailure&&p.LastFailure.Contains("occupied")&&p.LastFailure.Contains("empty valid ground"),"fresh occupied-tile rejection preserves scrap and gives cause/remedy");
  yield return PressUI(hud.startButton,canvas,device,hand);yield return null;
  float spawnDeadline=Time.realtimeSinceStartup+15;while(DEF_Zombie.all.Count==0&&Time.realtimeSinceStartup<spawnDeadline)yield return null;
  Check(DEF_Zombie.all.Count>0,"normal practice wave spawns live enemies");
  var routeEnemy=DEF_Zombie.all.First();var fields=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  var actualTrail=(List<Vector3>)typeof(DEF_Zombie).GetField("trail",fields).GetValue(routeEnemy);
  var points=new List<Vector3>();
  if(actualTrail!=null){int index=(int)typeof(DEF_Zombie).GetField("trailIndex",fields).GetValue(routeEnemy);points.AddRange(actualTrail.Skip(index));}
  else{var cells=(List<Vector2Int>)typeof(DEF_Zombie).GetField("path",fields).GetValue(routeEnemy);int index=(int)typeof(DEF_Zombie).GetField("pathIndex",fields).GetValue(routeEnemy);points.AddRange(cells.Skip(index).Select(c=>DEF_Pathfinder.I.CellCenter(c)));}
  var grid=FindAnyObjectByType<DEF_GridManager>();var routeFrom=routeEnemy.transform.position;float remaining=3f;bool intercepted=false;Vector2Int interceptCell=default;
  foreach(var point in points){float distance=Vector3.Distance(routeFrom,point);if(distance<remaining){remaining-=distance;routeFrom=point;continue;}var intercept=Vector3.MoveTowards(routeFrom,point,remaining);if(grid.WorldToTile(intercept,out int bx,out int bz)){interceptCell=new Vector2Int(bx,bz);intercepted=DEF_Barricade.TryPlace(bx,bz);}break;}
  Check(intercepted,"real barricade inserted on observed live route at "+interceptCell);
  var routeFlag=typeof(DEF_ProgressionState).GetField("routeControlled",fields);
  float routeDeadline=Time.realtimeSinceStartup+30;while(!(bool)routeFlag.GetValue(p.State)&&Time.realtimeSinceStartup<routeDeadline)yield return null;
  Check((bool)routeFlag.GetValue(p.State),"enemy actually walks a new detour; recompute alone is insufficient");
  var stunTarget=DEF_Zombie.all.FirstOrDefault(z=>z!=null&&z.health>0);
  if(stunTarget==null){while(DEF_WaveManager.I.phase!=DEF_WaveManager.Phase.Cleared)yield return null;yield return PressUI(hud.startButton,canvas,device,hand);while(DEF_Zombie.all.Count==0)yield return null;stunTarget=DEF_Zombie.all.First();}
  grid.WorldToTile(stunTarget.transform.position,out int sx,out int sz);bool shockPlaced=false;
  foreach(var offset in new[]{Vector2Int.down,Vector2Int.up,Vector2Int.right,Vector2Int.left})if(DEF_ShockTrap.TryPlace(sx+offset.x,sz+offset.y)){shockPlaced=true;break;}
  Check(shockPlaced,"real Shock placed beside observed live detour with earned scrap");
  float controlDeadline=Time.realtimeSinceStartup+90;while(p.State.Current==DEF_ProgressionState.Stage.AdvancedPractice&&Time.realtimeSinceStartup<controlDeadline)yield return null;
  Check(p.State.Current==DEF_ProgressionState.Stage.MissileIntroduction||p.State.Current==DEF_ProgressionState.Stage.MissilePractice,"real enemy detour and real Shock stun unlock Missile introduction");
  if(p.State.Current==DEF_ProgressionState.Stage.MissileIntroduction)Check(!hud.buyButtons[1].transform.parent.gameObject.activeSelf,"actual Missile picture and description remain hidden during its introduction");
  float missileDeadline=Time.realtimeSinceStartup+90;while((p.State.IntroductionActive||DEF_WaveManager.I.phase!=DEF_WaveManager.Phase.Cleared)&&Time.realtimeSinceStartup<missileDeadline)yield return null;
  Check(p.IsAllowed(DEF_Action.Missile)&&DEF_WaveManager.I.phase==DEF_WaveManager.Phase.Cleared,"Missile becomes available after its introduction and genuine practice-wave clear");
  yield return PressUI(hud.shopButton,canvas,device,hand);yield return PressUI(hud.buyButtons[1],canvas,device,hand);
  Check(DEF_VRInput.Instance.BuildArmed&&DEF_VRInput.Instance.mode==DEF_VRInput.PlaceMode.BoomTurret,"earned Missile affordance is selectable through actual XR UI");
  DEF_VRInput.Instance.CancelBuild();
  Capture("earned-missile-ui",canvas.transform.position);
  if(secondRun){Check(true,"second complete playable run reaches earned Missile after full reset with fresh items and evidence");yield break;}
  // Craft a second authentic kit solely to exercise reset while actually held.
  wreck=FindAnyObjectByType<SCV_HarvestableWreck>();Check(wreck!=null,"a real salvage source remains for reset regression");
  while(wreck!=null&&wreck.ScrapRemaining>0){wreck.Harvest();yield return null;}
  while(ledger.parts<bench.RepairPartsCost){crusher.ProcessScrap();while(crusher.IsProcessing)yield return null;yield return null;}
  bench.CraftRepairKit();yield return null;
  var resetKit=FindObjectsByType<SCV_CraftedItem>(FindObjectsSortMode.None).Single(x=>x.Type==SCV_CraftedItem.ItemType.RepairKit&&!x.Consumed);
  var resetCollider=resetKit.GetComponent<XRGrabInteractable>().colliders.First(c=>c!=null&&c.enabled);
  heldState.devicePosition=hand.parent.InverseTransformPoint(resetCollider.bounds.center);heldState.grip=1;heldState=heldState.WithButton(SimButton.GripButton);UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,heldState);
  for(int step=0;step<10;step++)yield return new WaitForFixedUpdate();Check(resetKit.IsHeld,"reset test starts with a genuinely held crafted kit");
  DontDestroyOnLoad(gameObject);p.BeginNewSession();yield return null;yield return null;yield return null;
  var fresh=DEF_Progression.Current;var freshLedger=FindAnyObjectByType<SCV_ResourceManager>();var freshAwards=FindAnyObjectByType<DEF_RecordingAchievements>();
  Check(resetKit==null&&fresh!=p&&fresh.State.Current==DEF_ProgressionState.Stage.TeamPrerequisites,"full reset destroys held item and clears mastery");
  Check(freshLedger.scrap==0&&freshLedger.parts==0&&freshLedger.ammo==0&&freshLedger.repairKits==0&&DEF_GameState.I.scrap==0,"full reset clears both inventories without reviving supplies");
  Check(!freshAwards.fieldMedic.activeSelf&&!freshAwards.supplyLine.activeSelf&&!freshAwards.waveDefender.activeSelf,"full reset clears all trophies");
  secondRun=true;yield return Run();
 }
}
#endif




