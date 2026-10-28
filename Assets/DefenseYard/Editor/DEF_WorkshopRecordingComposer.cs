using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
public static class DEF_WorkshopRecordingComposer
{
 public const string Path="Assets/DefenseYard/Scenes/DEF_Workshop_Defense_Recording.unity";
 static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
 static void Value(UnityEngine.Object o,string name,int value){var so=new SerializedObject(o);so.FindProperty(name).intValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 [MenuItem("Defense Yard/Recording/Prepare Workshop Defense Scene")]
 public static void Prepare()
 {
  if(Application.isPlaying)throw new Exception("Stop Play first");
  if(!System.IO.File.Exists(Path))AssetDatabase.CopyAsset("Assets/DefenseYard/Scenes/DEF_Clinic_Integration_Quest.unity",Path);
  var s=EditorSceneManager.OpenScene(Path);
  var p=All<DEF_Progression>(s).Single();p.workshopRecording=true;p.trainingAmmo=0;
  All<DEF_WorkshopReadiness>(s).Single().craftingOnlyRecording=true;
  All<SCV_ProgressionManager>(s).Single().defenseRecording=true;
  var rig=All<XROrigin>(s).Single();rig.transform.position=new Vector3(36.5316f,.05f,2.45f);
  var hud=All<DEF_HUD>(s).Single();hud.movementSafeMenus=true;
  var canvas=hud.GetComponent<Canvas>();if(canvas==null)throw new Exception("HUD Canvas missing");
  if(PrefabUtility.IsPartOfPrefabInstance(canvas.gameObject))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(canvas.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=rig.Camera;
  var rect=(RectTransform)canvas.transform;rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(1600,900);rect.localScale=Vector3.one*.001f;rect.rotation=Quaternion.Euler(0,180,0);rect.anchoredPosition3D=new Vector3(0,1.8f,5.5f);
  DEF_IntegrationStaging.EnsureTrackedUI(canvas);
  var presentation=All<DEF_ProgressionPresenter>(s).Single().goalText.GetComponentInParent<Canvas>();
  if(presentation!=canvas){var pr=(RectTransform)presentation.transform;pr.localScale=Vector3.one*.001f;pr.anchoredPosition3D=new Vector3(-2,1.8f,5.5f);pr.rotation=Quaternion.Euler(0,180,0);presentation.worldCamera=rig.Camera;EditorUtility.SetDirty(pr);}
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Path,true)};
  foreach(var old in s.GetRootGameObjects().Where(g=>g.name=="Direct Workshop Defense walkway"))UnityEngine.Object.DestroyImmediate(old);
  var walkway=GameObject.CreatePrimitive(PrimitiveType.Cube);walkway.name="Direct Workshop Defense walkway";SceneManager.MoveGameObjectToScene(walkway,s);walkway.transform.position=new Vector3(25.5f,-.1f,0);walkway.transform.localScale=new Vector3(12,.2f,6);
  foreach(var old in All<Transform>(s).Where(t=>t.name.StartsWith("Recording east gate segment ")).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
  var eastWallParts=All<Transform>(s).Where(t=>(t.name=="Wall"||t.name=="Coping"||t.name=="Fence rail")&&Mathf.Abs(t.position.x-20.5f)<.05f&&Mathf.Abs(t.position.z)<.05f&&t.localScale.x>=40).ToArray();
  if(eastWallParts.Length!=5)throw new Exception("Expected the measured east Wall, Coping and three fence rails before composing the recording entrance.");
  foreach(var original in eastWallParts){original.gameObject.SetActive(false);if(PrefabUtility.IsPartOfPrefabInstance(original.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(original.gameObject);
   foreach(int side in new[]{-1,1}){var segment=UnityEngine.Object.Instantiate(original.gameObject,original.parent);segment.name="Recording east gate segment "+original.name+" "+side;segment.SetActive(true);var scale=segment.transform.localScale;scale.x=(original.localScale.x-7)*.5f;segment.transform.localScale=scale;var position=original.position;position.z=side*(3.5f+scale.x*.5f);segment.transform.position=position;}}
  foreach(var post in All<Transform>(s).Where(t=>t.name=="Fence post"&&Mathf.Abs(t.position.x-20.5f)<.05f&&Mathf.Abs(t.position.z)<3.5f)){post.gameObject.SetActive(false);if(PrefabUtility.IsPartOfPrefabInstance(post.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(post.gameObject);}
  Physics.SyncTransforms();for(int z=-2;z<=2;z++)if(Physics.CheckCapsule(new Vector3(20.5f,.35f,z),new Vector3(20.5f,1.55f,z),.3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))throw new Exception("Recording entrance still has an obstructed standing capsule at z="+z);
  var setup=p.GetComponent<DEF_WorkshopRecordingSetup>()??p.gameObject.AddComponent<DEF_WorkshopRecordingSetup>();setup.hud=hud;setup.interfaceCanvas=canvas;setup.xrOrigin=rig.transform;
  hud.shopPanel.transform.SetParent(hud.transform,false);hud.selectedPanel.transform.SetParent(hud.transform,false);
  foreach(var old in s.GetRootGameObjects().Where(g=>g.name=="Recording view-anchored shop"))UnityEngine.Object.DestroyImmediate(old);
  var popup=new GameObject("Recording view-anchored shop",typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(popup,s);
  var popupRect=(RectTransform)popup.transform;popupRect.sizeDelta=new Vector2(1600,900);popupRect.localScale=Vector3.one*.001f;
  setup.popupCanvas=popup.GetComponent<Canvas>();setup.popupCanvas.renderMode=RenderMode.WorldSpace;setup.popupCanvas.worldCamera=rig.Camera;DEF_IntegrationStaging.EnsureTrackedUI(setup.popupCanvas);
  hud.shopPanel.transform.SetParent(popup.transform,false);hud.selectedPanel.transform.SetParent(popup.transform,false);
  foreach(var old in hud.shopBalance.transform.parent.GetComponentsInChildren<Button>(true).Where(b=>b.name=="Transfer Workshop scrap").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
  var transfer=UnityEngine.Object.Instantiate(hud.shopClose.gameObject,hud.shopBalance.transform.parent);transfer.name="Transfer Workshop scrap";
  var transferRect=(RectTransform)transfer.transform;transferRect.anchorMin=transferRect.anchorMax=Vector2.one*0+new Vector2(1,0);transferRect.pivot=new Vector2(1,0);transferRect.anchoredPosition=new Vector2(-28,20);transferRect.sizeDelta=new Vector2(420,44);
  setup.transferScrapButton=transfer.GetComponent<Button>();setup.transferScrapButton.onClick=new Button.ButtonClickedEvent();var transferText=transfer.GetComponentInChildren<Text>();transferText.text="TRANSFER 2 SCRAP\nWORKSHOP > DEFENSE";transferText.fontSize=16;transferText.alignment=TextAnchor.MiddleCenter;
  var textRect=transferText.rectTransform;textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=new Vector2(8,2);textRect.offsetMax=new Vector2(-8,-2);
  hud.shopBalance.rectTransform.sizeDelta=new Vector2(620,44);
  foreach(var b in rig.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null&&b.GetType().Name=="JumpProvider")b.enabled=false;
  foreach(var b in rig.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null&&(b.GetType().Name=="GrabMoveProvider"||b.GetType().Name=="TwoHandedGrabMoveProvider"))b.enabled=false;
  foreach(var manager in rig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.ControllerInputActionManager>(true)){
   var so=new SerializedObject(manager);so.FindProperty("m_UIScrollingEnabled").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();}
  foreach(var attach in rig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Attachment.InteractionAttachController>(true)){
   attach.useManipulationInput=false;attach.manipulationInput.inputSourceMode=UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputValueReader.InputSourceMode.Unused;}
  foreach(var move in rig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.DynamicMoveProvider>(true)){
   move.leftHandMovementDirection=move.rightHandMovementDirection=UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.DynamicMoveProvider.MovementDirection.HeadRelative;}
  foreach(var old in All<Transform>(s).Where(t=>t.name.StartsWith("Recording doorway segment ")||t.name=="Recording outpost access ramp").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
  var northWalls=All<Transform>(s).Where(t=>(t.name=="Armored lower wall"||t.name=="Window sill")&&Mathf.Abs(t.position.z-4.5f)<.05f&&Mathf.Abs(t.position.x-1)<.05f).ToArray();
  if(northWalls.Length!=2)throw new Exception("Expected north wall and sill for the walk-in doorway");
  foreach(var wall in northWalls){wall.gameObject.SetActive(false);foreach(int side in new[]{-1,1}){var segment=UnityEngine.Object.Instantiate(wall.gameObject,wall.parent);segment.name="Recording doorway segment "+wall.name+" "+side;segment.SetActive(true);var scale=wall.localScale;scale.x=(scale.x-1.8f)*.5f;segment.transform.localScale=scale;var position=wall.position;position.x=1+side*(.9f+scale.x*.5f);segment.transform.position=position;}if(PrefabUtility.IsPartOfPrefabInstance(wall.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(wall.gameObject);}
  var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.name="Recording outpost access ramp";SceneManager.MoveGameObjectToScene(ramp,s);ramp.transform.position=new Vector3(1,.085f,5.25f);ramp.transform.rotation=Quaternion.Euler(Mathf.Atan(.27f/2.5f)*Mathf.Rad2Deg,0,0);ramp.transform.localScale=new Vector3(1.8f,.1f,2.5f);
  // A held grip may begin before the item is hovered: acquire on contact, release to drop.
  foreach(var hand in rig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(true))
   hand.selectActionTrigger=UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor.InputTriggerType.State;
  var state=All<DEF_GameState>(s).Single();state.scrap=0;state.ammo=0;
  foreach(var link in All<SCV_DefenseSupplyLink>(s))Value(link,"scrapTransferAmount",2);
  foreach(var t in All<MonoBehaviour>(s).Where(t=>t is SCV_MissionStatus || t is SCV_AmmoStorageDisplay || t is SCV_AmmoTestDisplay || t is SCV_DamagedPanelDisplay)){
   t.enabled=false;var serialized=new SerializedObject(t);var field=serialized.GetIterator();while(field.Next(true))if(field.propertyType==SerializedPropertyType.ObjectReference && field.objectReferenceValue is TMPro.TMP_Text label)label.gameObject.SetActive(false);
  }
  var waves=All<DEF_WaveManager>(s).Single();waves.baseZombies=2;waves.extraPerWave=1;waves.threatPerSecond=0;
  // Teaching-only receivers are not part of this simplified contract; keep the crafted items for Defense.
  foreach(var t in All<SCV_AmmoReceiver>(s))t.gameObject.SetActive(false);
  foreach(var t in All<SCV_RepairReceiver>(s))t.gameObject.SetActive(false);
  foreach(var t in All<TMPro.TMP_Text>(s)){
   if(t.text.Contains("RESTORE OUTPOST")||t.text.Contains("AMMO DEPOT")||t.text.Contains("REPAIR STATION"))t.gameObject.SetActive(false);
  }
  // These owner HUD labels are serialized legacy UI.Text, not TMP. Keep the recording
  // instructions consistent with nonpausing shops and authentic physical repair.
  foreach(var label in All<Text>(s))label.text=label.text
   .Replace("The yard pauses while you shop.","Movement continues while you shop.")
   .Replace("Right grip repairs 40 HP for 10 scrap","Release a crafted RepairKit to repair.");
  foreach(var directions in All<TextMesh>(s).Where(t=>t.text.Contains("WORKSHOP 44")&&t.text.Contains("CLINIC 80")))
   directions.GetComponent<MeshRenderer>().enabled=false;
  foreach(var old in s.GetRootGameObjects().Where(g=>g.name=="Simplified recording instructions"))UnityEngine.Object.DestroyImmediate(old);
  foreach(var old in s.GetRootGameObjects().Where(g=>g.name=="Earned Defense achievements"))UnityEngine.Object.DestroyImmediate(old);
  var shelf=new GameObject("Earned Defense achievements");SceneManager.MoveGameObjectToScene(shelf,s);shelf.transform.position=new Vector3(4.8f,0,5.5f);shelf.transform.rotation=Quaternion.Euler(0,180,0);
  var achievements=shelf.AddComponent<DEF_RecordingAchievements>();achievements.owner=p;
  GameObject Trophy(string title,float x)
  {
   var stand=GameObject.CreatePrimitive(PrimitiveType.Cube);stand.name=title+" pedestal";stand.transform.SetParent(shelf.transform,false);stand.transform.localPosition=new Vector3(x,.65f,0);stand.transform.localScale=new Vector3(.4f,.15f,.4f);
   var trophy=GameObject.CreatePrimitive(PrimitiveType.Sphere);trophy.name=title+" earned trophy";trophy.transform.SetParent(shelf.transform,false);trophy.transform.localPosition=new Vector3(x,.95f,0);trophy.transform.localScale=Vector3.one*.24f;UnityEngine.Object.DestroyImmediate(trophy.GetComponent<Collider>());
   var label=new GameObject(title+" label");label.transform.SetParent(shelf.transform,false);label.transform.localPosition=new Vector3(x,1.25f,0);var tm=label.AddComponent<TextMesh>();tm.text=title;tm.characterSize=.015f;tm.fontSize=40;tm.anchor=TextAnchor.MiddleCenter;
   trophy.SetActive(false);return trophy;
  }
  achievements.fieldMedic=Trophy("FIELD MEDIC",0);achievements.supplyLine=Trophy("SUPPLY LINE",.9f);achievements.waveDefender=Trophy("WAVE DEFENDER",1.8f);
  foreach(var worldCanvas in All<Canvas>(s))if(worldCanvas.renderMode==RenderMode.WorldSpace){worldCanvas.worldCamera=rig.Camera;DEF_IntegrationStaging.EnsureTrackedUI(worldCanvas);}
  foreach(var component in All<Component>(s))if(component!=null && PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
  EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
  Debug.Log("Recording scene saved; native acceptance still required: "+Path);
 }
}



