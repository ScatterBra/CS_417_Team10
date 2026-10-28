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
 void OnDestroy(){if(ownedDevice!=null&&ownedDevice.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(ownedDevice);if(moveDevice!=null&&moveDevice.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(moveDevice);}
 bool Walkable(Vector2Int cell)
 {
  var point=new Vector3(cell.x*.5f,.05f,cell.y*.5f);
  if(!Physics.Raycast(point+Vector3.up*.4f,Vector3.down,out var floor,.6f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||Mathf.Abs(floor.point.y)>.15f)return false;
  return !Physics.OverlapCapsule(point+Vector3.up*.3f,point+Vector3.up*1.5f,.25f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(walkRig.transform)&&!(c.GetComponentInParent<SCV_CraftedItem>() is SCV_CraftedItem item&&item.IsHeld));
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
 IEnumerator PressUI(Button button,Canvas canvas,UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRSimulatedController device,Transform hand)
 {
  Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;var center=rect.TransformPoint(rect.rect.center);
  yield return WalkTo(center-canvas.transform.forward*1.2f);
  var aim=walkRig.transform.position+Vector3.up*1.2f+walkRig.Camera.transform.right*.25f;
  var state=new SimState{isTracked=true,trackingState=3,devicePosition=hand.parent.InverseTransformPoint(aim),deviceRotation=Quaternion.Inverse(hand.parent.rotation)*Quaternion.LookRotation(center-aim)};
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,state);yield return new WaitForSecondsRealtime(.5f);
  state.trigger=1;state=state.WithButton(SimButton.TriggerButton);UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,state);yield return null;yield return null;
  state.trigger=0;state=state.WithButton(SimButton.TriggerButton,false);UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,state);yield return null;yield return null;
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
 void Check(bool condition,string label){results.Add((condition?"PASS ":"FAIL ")+label);System.IO.File.WriteAllLines(ResultPath,results);if(!condition)throw new Exception(label);}
 IEnumerator Start()
 {
  yield return new WaitForSecondsRealtime(2);
  var sequence=Run();bool failed=false;
  while(true){object current=null;bool more=false;try{more=sequence.MoveNext();if(more)current=sequence.Current;}catch(Exception e){results.Add("EXCEPTION "+e);failed=true;}if(failed||!more)break;yield return current;}
  System.IO.File.WriteAllLines(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../workshop-native-play-results.txt")),results);
  if(Application.isBatchMode)UnityEditor.EditorApplication.Exit(failed?1:0);else if(!failed)UnityEditor.EditorApplication.isPlaying=false;
 }
 IEnumerator Run()
 {
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
  Check(!rig.GetComponentsInChildren<MonoBehaviour>(true).Any(b=>b!=null&&b.GetType().Name=="JumpProvider"&&b.enabled),"Jump disabled; no A collision");
  var canvas=hud.GetComponent<Canvas>();
  Check(canvas.renderMode==RenderMode.WorldSpace&&canvas.worldCamera==rig.Camera,"saved world canvas camera assigned");
  Check(Vector3.Distance(canvas.transform.position,new Vector3(1,1.8f,3.7f))<.01f,"actual serialized Canvas position survives reload: "+canvas.transform.position);
  Check(canvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>()!=null,"tracked UI raycaster exists");
  var missingCameras=FindObjectsByType<Canvas>(FindObjectsInactive.Include).Where(c=>c.gameObject.scene==gameObject.scene&&c.renderMode==RenderMode.WorldSpace&&c.worldCamera!=rig.Camera).Select(c=>c.name).ToArray();
  Check(missingCameras.Length==0,"all world Canvas event cameras bound: "+string.Join(",",missingCameras));
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
  yield return new WaitForSecondsRealtime(7.3f);
  SCV_DefenseSupplyLink link=null;float supplyDeadline=Time.realtimeSinceStartup+30;
  while((link=FindAnyObjectByType<SCV_DefenseSupplyLink>())==null&&Time.realtimeSinceStartup<supplyDeadline)yield return null;
  Check(link!=null,"supply terminal becomes reachable after its introduction");
  string priorFailure=p.LastFailure;
  Check(!DEF_Turret.TryPlace(13,10,false)&&DEF_GameState.I.scrap==0&&p.LastFailure!=priorFailure&&p.LastFailure.Contains("not enough scrap")&&p.LastFailure.Contains("Transfer Workshop scrap"),"fresh insufficient funds rejection gives actual cause and remedy");
  var transfer=BoundButton(link,"DeliverScrap");yield return PressUI(transfer,transfer.GetComponentInParent<Canvas>(),device,hand);yield return PressUI(transfer,transfer.GetComponentInParent<Canvas>(),device,hand);
  Check(DEF_GameState.I.scrap==4&&ledger.scrap==2,"real scrap transfer funds sentry: Defense="+DEF_GameState.I.scrap+" Workshop="+ledger.scrap);
  priorFailure=p.LastFailure;Check(!DEF_Turret.TryPlace(10,10,false)&&DEF_GameState.I.scrap==4&&p.LastFailure!=priorFailure&&p.LastFailure.Contains("inside the base")&&p.LastFailure.Contains("outside the base"),"fresh inside-base rejection preserves scrap and gives cause/remedy");
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new SimState().WithButton(SimButton.PrimaryButton));yield return null;yield return null;
  Check(Time.timeScale==1&&hud.shopPanel.activeInHierarchy,"actual A binding opens shop without physics freeze");
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new SimState());yield return null;yield return null;
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new SimState().WithButton(SimButton.PrimaryButton));yield return null;yield return null;
  Check(!hud.shopPanel.activeSelf&&Time.timeScale==1,"actual A closes its own modal safely");
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new SimState());yield return null;yield return null;
  hud.ToggleShop();yield return null;yield return PressUI(hud.buyButtons[0],canvas,device,hand);yield return null;
  Check(DEF_VRInput.Instance.BuildArmed&&!hud.shopPanel.activeSelf,"real UI button arms sentry and closes menu");
  var groundPoint=FindAnyObjectByType<DEF_GridManager>().TileCenter(13,10);var aimPosition=groundPoint+new Vector3(0,2,-1);
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
  yield return new WaitForSecondsRealtime(4.3f);
  var receiver=FindObjectsByType<DEF_WorkshopRepairReceiver>(FindObjectsSortMode.None).Single(x=>x.target is DEF_Turret);
  Check(!receiver.TryDeliver(ammo)&&!ammo.Consumed&&receiver.LastMessage.Contains("not a RepairKit"),"wrong-item repair catch preserves real ammo and explains correct item");
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
  Check(!p.IsAllowed(DEF_Action.PhysicalAmmo),"ammo remains locked during introduction");
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
  yield return new WaitForSecondsRealtime(4.3f);
  Check(p.State.Current==DEF_ProgressionState.Stage.WaveReady,"wave ready only after introduction");
  yield return PressUI(hud.startButton,canvas,device,hand);yield return null;
  Check(p.State.Current==DEF_ProgressionState.Stage.WaveActive,"actual UI starts wave");
  float deadline=Time.realtimeSinceStartup+120;
  while(p.State.Current==DEF_ProgressionState.Stage.WaveActive&&Time.realtimeSinceStartup<deadline)yield return null;
  Check(p.State.Current==DEF_ProgressionState.Stage.FirstWaveComplete,"real turret combat clears first wave alive");
  yield return null;Check(awards.waveDefender.activeSelf,"normal first-wave clear awards third visible trophy");
  Check(!p.IsAllowed(DEF_Action.Barricade)&&!p.IsAllowed(DEF_Action.Shock),"advanced actions hidden during their introduction");
  yield return new WaitForSecondsRealtime(4.3f);
  Check(p.State.Current==DEF_ProgressionState.Stage.AdvancedPractice,"advanced practice follows the completed introduction");
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
 }
}
#endif




