using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Deadzone.Power
{
    /// <summary>
    /// The player's wrist menu: resource list, build menu and build status. In VR it is hidden until Y on
    /// the left controller opens it. In the integrated scene it stays fixed in front of the player,
    /// like the Defense shop. Standalone retains the smoothed right-wrist panel. While placing,
    /// build status floats above the ghost instead. Desktop keeps its hotkey overlay.
    /// </summary>
    public class PWR_WristPanel : MonoBehaviour
    {
        public static PWR_WristPanel Instance { get; private set; }

        [Header("VR menu")]
        [Tooltip("Gap between the right controller and the bottom edge of the menu.")]
        public float wristGap = 0.03f;
        [Tooltip("How quickly the menu catches up with the hand. Lower = steadier but laggier.")]
        public float followSharpness = 18f;
        public float worldWidth = 0.28f;

        /// <summary>Transform tutorials about the menu attach to (the right controller in VR).</summary>
        public Transform Anchor { get; private set; }

        public bool IsOpen => canvas != null && canvas.gameObject.activeSelf;
        public bool IsXRMenuOpen => xr && XRActiveNow && IsOpen;
        private static bool XRActiveNow => PWR_PlayerRig.Instance != null && PWR_PlayerRig.Instance.XRActive;
        // A button may close the menu before another component processes this frame's trigger.
        public static bool BlockingXRWorldInput => Instance != null && Instance.xr && XRActiveNow &&
            (Instance.IsOpen || Time.unscaledTime < Instance.ignoreWorldInputUntil);
        private float ignoreWorldInputUntil;
        private bool integrated;
        private float nextNoticeTime;

        const float UnitsWide = 420f;
        const int Padding = 10;
        const float XrButtonHeight = 72f;
        const float XrButtonSpacing = 8f;
        const float GoalHeight = 56f;

        private TMP_Text resourceText;
        private TMP_Text goalText;
        private TMP_Text statusText;
        private readonly List<Button> buildButtons = new List<Button>();
        private readonly StringBuilder sb = new StringBuilder();
        private Canvas canvas;
        private Transform hand;
        private bool xr;
        private GameObject uiRoot;
        private PWR_BuildSystem subscribedBuildSystem;
        private float refresh;
        private InputAction toggle;
        private Canvas statusLabel;
        private TMP_Text statusLabelText;
        private Canvas objectiveCanvas;
        private TMP_Text objectiveText;
        private Canvas inventoryCanvas;
        private TMP_Text inventoryText;
        private PWR_PlayerResources inventorySource;
        private readonly StringBuilder inventoryLines = new StringBuilder();
        private PWR_Stage observedStage;
        private string completedGoal;
        private float completionUntil;

        private void Awake()
        {
            Instance = this;
            toggle = new InputAction("PWR Menu Toggle", InputActionType.Button, "<XRController>{LeftHand}/{SecondaryButton}");
        }

        private void OnEnable()
        {
            toggle.Enable();
            PWR_Progression.StageChanged += HandleStageChanged;
        }

        private void OnDisable()
        {
            toggle.Disable();
            PWR_Progression.StageChanged -= HandleStageChanged;
            UnbindInventory();
            if (inventoryCanvas != null) inventoryCanvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            DestroyMenu();
            toggle?.Dispose();
        }

        private IEnumerator Start()
        {
            yield return PWR_PlayerRig.WaitForXRStartup();
            CreateMenu();
        }

        private void CreateMenu()
        {
            var rig = PWR_PlayerRig.Instance;
            if (rig != null) rig.ResolveControllers();
            var bs = PWR_BuildSystem.Instance;
            int buttonCount = (bs != null ? bs.buildables.Count : 0) + 2; // + Demolish, Cancel
            xr = rig != null && rig.XRActive && rig.rightController != null;
            integrated = DEF_IntegratedXRPresentation.IsIntegratedScene(gameObject.scene);

            if (xr)
            {
                int rows = (buttonCount + 1) / 2;
                float gridHeight = rows * XrButtonHeight + (rows - 1) * XrButtonSpacing;
                float unitsHigh = 2 * Padding + 40 + 30 + GoalHeight + 5 * 30 + gridHeight + 50 + 4 * 6;
                canvas = PWR_UIFactory.CreateWorldCanvas("PWR_BuildMenu", null, new Vector2(UnitsWide, unitsHigh),
                    (integrated ? 0.56f : worldWidth) / UnitsWide, true);
                uiRoot = canvas.gameObject;
                // PWR_BuildInput checks for this collider so a trigger pull on the menu doesn't also place.
                var box = canvas.gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(UnitsWide, unitsHigh, 4f);
                BuildContents(bs, gridHeight);
                canvas.gameObject.SetActive(false);
                hand = rig.rightController;
                Anchor = hand;
                CreateStatusLabel();
            }
            else
            {
                float unitsHigh = 70 + GoalHeight + 10 * 30 + buttonCount * 58 + 90;
                canvas = PWR_UIFactory.CreateOverlayCanvas("PWR_DesktopHUD");
                uiRoot = canvas.gameObject;
                var holder = new GameObject("Panel", typeof(RectTransform));
                holder.transform.SetParent(canvas.transform, false);
                var rt = (RectTransform)holder.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(16f, -16f);
                rt.sizeDelta = new Vector2(UnitsWide, unitsHigh + 60);
                canvas = holder.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 10;
                holder.AddComponent<GraphicRaycaster>();
                BuildContents(bs, 0f);
                Anchor = canvas.transform;
            }

            if (bs != null)
            {
                subscribedBuildSystem = bs;
                bs.OnSelectionChanged += HandleSelectionChanged;
                bs.OnDemolishModeChanged += HandleDemolishModeChanged;
            }
            if (integrated) CreateObjective();
            Refresh();
        }

        private void CreateObjective()
        {
            objectiveCanvas = PWR_UIFactory.CreateWorldCanvas("Power current objective", null,
                new Vector2(520, 165), .6f / 520f, false);
            PWR_UIFactory.CreatePanel(objectiveCanvas.transform, new Color(.015f, .02f, .025f, .94f)).raycastTarget = false;
            objectiveText = PWR_UIFactory.CreateText(objectiveCanvas.transform, "", 22, TextAlignmentOptions.TopLeft, 12);
            inventoryCanvas = PWR_UIFactory.CreateWorldCanvas("Power carried inventory", null,
                new Vector2(600, 220), .65f / 600f, false);
            PWR_UIFactory.CreatePanel(inventoryCanvas.transform, new Color(.015f, .02f, .025f, .94f)).raycastTarget = false;
            inventoryText = PWR_UIFactory.CreateText(inventoryCanvas.transform, "", 22, TextAlignmentOptions.TopLeft, 12);
            BindInventory();
            observedStage = PWR_Progression.Instance != null ? PWR_Progression.Instance.Stage : PWR_Stage.RestoreLight;
            UpdateObjective();
        }

        private void UnbindInventory()
        {
            if (inventorySource != null) inventorySource.OnResourceChanged -= InventoryChanged;
            inventorySource = null;
        }

        private void BindInventory()
        {
            if (inventorySource == PWR_PlayerResources.Instance) return;
            UnbindInventory();
            inventorySource = PWR_PlayerResources.Instance;
            if (inventorySource != null) inventorySource.OnResourceChanged += InventoryChanged;
            RefreshInventory();
        }

        private void InventoryChanged(PWR_ResourceType type, int amount, int delta) => RefreshInventory();

        private void RefreshInventory()
        {
            if (inventoryText == null) return;
            if (inventorySource == null) { inventoryText.text = "POWER INVENTORY — waiting for resources"; return; }
            inventoryLines.Clear();
            inventoryLines.Append("<b>POWER — CARRIED INVENTORY</b>\n<size=85%>Excludes fuel already loaded in machines</size>");
            InventoryRow(PWR_ResourceType.Scrap, "Power Scrap", PWR_ResourceType.Coal, "Coal", true);
            InventoryRow(PWR_ResourceType.CrudeOil, "Crude Oil", PWR_ResourceType.RefinedOil, "Refined Oil", true);
            InventoryRow(PWR_ResourceType.CopperWire, "Copper Wire", PWR_ResourceType.Glass, "Glass");
            InventoryRow(PWR_ResourceType.Electrolyte, "Electrolyte", PWR_ResourceType.Fuse, "Fuse");
            InventoryRow(PWR_ResourceType.Battery, "Battery", PWR_ResourceType.LightBulb, "Light Bulb");
            inventoryText.text = inventoryLines.ToString();
        }

        private void InventoryRow(PWR_ResourceType left, string leftName, PWR_ResourceType right, string rightName, bool always = false)
        {
            int a = inventorySource.Get(left), b = inventorySource.Get(right);
            if (!always && a == 0 && b == 0 && !PWR_Progression.IsResourceVisible(left) && !PWR_Progression.IsResourceVisible(right)) return;
            inventoryLines.Append('\n').Append(leftName).Append(": ").Append(a)
                .Append("<pos=52%>").Append(rightName).Append(": ").Append(b);
        }

        private void HandleStageChanged(PWR_Stage stage)
        {
            if (!integrated) return;
            // Only a real progression event earns a check, never entering an area or refreshing text.
            if ((int)stage > (int)observedStage)
            {
                completedGoal = observedStage switch
                {
                    PWR_Stage.RestoreLight => "Restore the Floodlight",
                    PWR_Stage.GeneratePower => "Start a generator",
                    PWR_Stage.SupplyLine => "Collect conveyor supplies",
                    PWR_Stage.CraftBattery => "Craft a Battery",
                    PWR_Stage.ChargeCell => "Restore power and collect the Charged Cell",
                    _ => "Power objective"
                };
                completionUntil = Time.unscaledTime + 2f;
            }
            observedStage = stage;
            RefreshInventory();
        }

        private void UpdateObjective()
        {
            if (objectiveCanvas == null) return;
            var rig = PWR_PlayerRig.Instance;
            var cam = Camera.main;
            var defense = DEF_VRInput.Instance;
            if (rig != null && rig.XRActive && defense != null && defense.gameObject.scene == gameObject.scene && defense.xrCamera != null)
                cam = defense.xrCamera;
            bool visible = PWR_AreaGate.PlayerInside && cam != null;
            objectiveCanvas.gameObject.SetActive(visible);
            BindInventory(); // Handles startup ordering and a replaced inventory owner.
            if (inventoryCanvas != null)
            {
                // Desktop already has the always-visible resource menu. XR menu shows the same ledger.
                bool showInventory = visible && XRActiveNow && !IsXRMenuOpen;
                inventoryCanvas.gameObject.SetActive(showInventory);
                if (showInventory)
                {
                    inventoryCanvas.worldCamera = cam;
                    inventoryCanvas.transform.SetPositionAndRotation(
                        cam.transform.TransformPoint(new Vector3(-.50f, -.29f, 1.25f)), cam.transform.rotation);
                }
            }
            if (!visible) return;
            objectiveCanvas.worldCamera = cam;
            objectiveCanvas.transform.SetPositionAndRotation(cam.transform.TransformPoint(new Vector3(.65f, .25f, 1.25f)), cam.transform.rotation);
            var progression = PWR_Progression.Instance;
            DEF_UIPresentationPolicy.SetCompletionMark(objectiveText, Time.unscaledTime < completionUntil);
            objectiveText.text = Time.unscaledTime < completionUntil
                ? "<color=#7CFC7C><b>PREVIOUS GOAL COMPLETE</b></color>\nNext: " + (progression != null ? progression.NextGoal : "")
                : "<color=#FFC840><b>POWER — CURRENT GOAL</b></color>\n" +
                  (progression != null ? progression.NextGoal : "Press Y to open the Power menu.");
        }

        /// <summary>Title, resources, build buttons and status line. VR uses two columns of large buttons.</summary>
        private void BuildContents(PWR_BuildSystem bs, float gridHeight)
        {
            PWR_UIFactory.CreatePanel(canvas.transform, PWR_UIFactory.PanelColor);
            var layout = PWR_UIFactory.CreateVerticalLayout(canvas.transform, 6, Padding);
            var title = PWR_UIFactory.CreateLayoutText(layout, "<b>POWER STATION</b>", 28, xr ? 40 : 36);
            title.color = PWR_UIFactory.AccentColor;
            if (xr) PWR_UIFactory.CreateLayoutText(layout, "Y: close  |  Empty hand: aim + trigger", 18, 30);
            goalText = PWR_UIFactory.CreateLayoutText(layout, "", 18, GoalHeight);
            resourceText = PWR_UIFactory.CreateLayoutText(layout, "", 22, xr ? 5 * 30 : 10 * 30);

            var input = FindAnyObjectByType<PWR_BuildInput>();
            if (bs != null)
            {
                Transform buttonParent = layout;
                float buttonHeight = 52;
                if (xr)
                {
                    float cellWidth = (UnitsWide - 2 * Padding - XrButtonSpacing) / 2f;
                    buttonParent = PWR_UIFactory.CreateGrid(layout, 2, gridHeight,
                        new Vector2(cellWidth, XrButtonHeight), new Vector2(XrButtonSpacing, XrButtonSpacing));
                    buttonHeight = XrButtonHeight;
                }
                for (int i = 0; i < bs.buildables.Count; i++)
                {
                    int index = i;
                    var def = bs.buildables[i];
                    string hotkey = xr ? "" : $"[{i + 1}] ";
                    buildButtons.Add(PWR_UIFactory.CreateButton(buttonParent, $"{hotkey}{def.displayName}  <size=75%>({(integrated ? def.CostText().Replace("Scrap", "Power Scrap") : def.CostText())})</size>",
                        buttonHeight, 22, () => { if (input != null) input.SelectIndex(index); }));
                }
                PWR_UIFactory.CreateButton(buttonParent, xr ? "Demolish" : "[X] Demolish", xr ? buttonHeight : 46, 22,
                    () => { if (input != null) input.SelectDemolish(); });
                PWR_UIFactory.CreateButton(buttonParent, xr ? "Cancel (B)" : "[Q] Cancel", xr ? buttonHeight : 46, 22,
                    () => bs.Select(null));
            }
            statusText = PWR_UIFactory.CreateLayoutText(layout, "", 18, xr ? 50 : 80);
            if (!xr)
                statusText.text = "WASD move, hold RMB look, E use, F grab/drop";
        }

        /// <summary>Small floating label above the placement ghost (VR): can I build here, what it connects to.</summary>
        private void CreateStatusLabel()
        {
            statusLabel = PWR_UIFactory.CreateWorldCanvas("PWR_BuildStatus", null, new Vector2(520f, 110f), 0.001f, false);
            statusLabel.sortingOrder = 30;
            PWR_UIFactory.CreatePanel(statusLabel.transform, new Color(0.05f, 0.08f, 0.12f, 0.85f));
            statusLabelText = PWR_UIFactory.CreateText(statusLabel.transform, "", 24, TextAlignmentOptions.Center, 8f);
            statusLabel.gameObject.SetActive(false);
        }

        public void Open()
        {
            if (!xr || canvas == null) return;
            if (!PWR_AreaGate.PlayerInside)
            {
                MenuNotice("Enter the Power Station, then press Y to open its build menu.");
                return;
            }
            if (integrated && !TryPositionMenu()) return;
            if (integrated)
            {
                DEF_HUD.Instance?.CloseMenus();
                DEF_VRInput.Instance?.CancelBuild();
                PWR_BuildSystem.Instance?.Select(null);
            }
            // The arrival card occupies the same viewing space; never hide buttons behind it.
            PWR_TutorialManager.Instance?.Dismiss("wrist_menu");
            canvas.gameObject.SetActive(true);
            if (!integrated) FollowHand(1f);
            Refresh();
        }

        private bool TryPositionMenu()
        {
            var cam = Camera.main;
            if (cam == null) { MenuNotice("Power menu is waiting for the headset camera. Try Y again."); return false; }
            var rig = PWR_PlayerRig.Instance;
            Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.5f && rig != null && rig.origin != null)
                forward = Vector3.ProjectOnPlane(rig.origin.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.5f) forward = Vector3.forward;
            Vector3 position = cam.transform.position + forward * 1.25f - Vector3.up * 0.08f;
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            var rect = (RectTransform)canvas.transform;
            var halfSize = new Vector3(0.28f, rect.sizeDelta.y * rect.lossyScale.y * 0.5f, 0.03f);
            foreach (var collider in Physics.OverlapBox(position, halfSize, rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (BlocksMenu(collider, rig)) return MenuBlocked();
            foreach (var hit in Physics.RaycastAll(cam.transform.position, forward, 1.25f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (BlocksMenu(hit.collider, rig)) return MenuBlocked();
            canvas.worldCamera = cam;
            canvas.transform.SetPositionAndRotation(position, rotation);
            return true;
        }

        private static bool BlocksMenu(Collider collider, PWR_PlayerRig rig) =>
            !(rig != null && rig.origin != null && collider.transform.IsChildOf(rig.origin)) &&
            collider.GetComponentInParent<PWR_Grabbable>() == null && collider.GetComponentInParent<SCV_CraftedItem>() == null;

        private bool MenuBlocked()
        {
            MenuNotice("Power menu needs clear space. Face the open walkway and press Y again.");
            return false;
        }

        private void MenuNotice(string message)
        {
            if (Time.unscaledTime < nextNoticeTime) return;
            nextNoticeTime = Time.unscaledTime + 1f;
            var cam = Camera.main;
            if (cam != null) PWR_Feedback.Show(cam.transform.position + cam.transform.forward * 1.2f, message, PWR_Feedback.Info);
            else Debug.LogWarning("[PWR] " + message);
        }

        /// <summary>
        /// Sits above the controller in world up (not the controller's own axes, so tilting the hand
        /// doesn't swing the menu around) and faces the head. t = 1 snaps, smaller values smooth.
        /// </summary>
        private void FollowHand(float t)
        {
            var cam = Camera.main;
            if (hand == null || cam == null) return;
            var rt = (RectTransform)canvas.transform;
            float halfHeight = rt.sizeDelta.y * rt.lossyScale.y * 0.5f;
            Vector3 target = hand.position + Vector3.up * (wristGap + halfHeight);
            Vector3 pos = Vector3.Lerp(canvas.transform.position, target, t);
            canvas.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cam.transform.position));
        }

        public void Close()
        {
            if (!xr || canvas == null || !IsOpen) return;
            ignoreWorldInputUntil = Time.unscaledTime + 0.35f;
            canvas.gameObject.SetActive(false);
        }

        private void HandleSelectionChanged(PWR_BuildableDef def)
        {
            if (def == null) return;
            // Get the menu out of the way of aiming at the floor; the status label takes over.
            Close();
            var mgr = PWR_TutorialManager.Instance;
            if (mgr != null && Anchor != null)
                mgr.ShowOnce("build_controls", xr
                        ? "Building: aim the right controller at the floor and pull the trigger to place. " +
                          "Flick the right thumbstick to rotate. Press B to stop building."
                        : "Building: click to place, R to rotate, Q or Esc to stop building.",
                    Anchor, new Vector3(0f, xr ? 0.3f : 0f, 0f));
        }

        private void HandleDemolishModeChanged(bool on)
        {
            if (!on) return;
            Close();
            var mgr = PWR_TutorialManager.Instance;
            if (mgr != null && Anchor != null)
                mgr.ShowOnce("demolish_controls", xr
                        ? "Demolish: aim the right controller at something you built and pull the trigger. " +
                          "You get its cost back, and a machine's charge or fuel carries over to the next one you build. Press B to stop."
                        : "Demolish: click something you built to remove it (cost refunded). Q or Esc to stop.",
                    Anchor, new Vector3(0f, xr ? 0.3f : 0f, 0f));
        }

        private void DestroyMenu()
        {
            if (subscribedBuildSystem != null)
            {
                subscribedBuildSystem.OnSelectionChanged -= HandleSelectionChanged;
                subscribedBuildSystem.OnDemolishModeChanged -= HandleDemolishModeChanged;
            }
            subscribedBuildSystem = null;
            if (uiRoot != null) { uiRoot.SetActive(false); Destroy(uiRoot); }
            if (statusLabel != null) { statusLabel.gameObject.SetActive(false); Destroy(statusLabel.gameObject); }
            if (objectiveCanvas != null) Destroy(objectiveCanvas.gameObject);
            objectiveCanvas = null; objectiveText = null;
            UnbindInventory();
            if (inventoryCanvas != null) { inventoryCanvas.gameObject.SetActive(false); Destroy(inventoryCanvas.gameObject); }
            inventoryCanvas = null; inventoryText = null;
            uiRoot = null; canvas = null; statusLabel = null; statusLabelText = null;
            resourceText = null; goalText = null; statusText = null; hand = null; Anchor = null;
            buildButtons.Clear();
        }
        private void LateUpdate()
        {
            var rig = PWR_PlayerRig.Instance;
            if (xr && canvas != null && !XRActiveNow)
            {
                Close();
                DestroyMenu();
                CreateMenu();
            }
            if (!xr && canvas != null && rig != null && rig.XRActive)
            {
                rig.ResolveControllers();
                if (rig.rightController != null)
                {
                    DestroyMenu();
                    CreateMenu();
                }
            }
            UpdateObjective();
            if (canvas == null) return; // still waiting for the XR startup check
            // Keep area gating, but explain why Y cannot open the menu outside the station.
            bool inside = PWR_AreaGate.PlayerInside;
            if (!xr && canvas.enabled != inside) canvas.enabled = inside;
            if (!inside)
            {
                if (xr)
                {
                    Close();
                    if (toggle.WasPressedThisFrame()) MenuNotice("Enter the Power Station, then press Y to open its build menu.");
                }
                if (statusLabel != null && statusLabel.gameObject.activeSelf) statusLabel.gameObject.SetActive(false);
                return;
            }
            if (xr)
            {
                if (toggle.WasPressedThisFrame())
                {
                    if (IsOpen) Close();
                    else Open();
                }
                if (IsOpen && !integrated) FollowHand(1f - Mathf.Exp(-followSharpness * Time.deltaTime));
                UpdateStatusLabel(Camera.main);
            }
            refresh -= Time.deltaTime;
            if (refresh > 0f) return;
            refresh = 0.2f;
            Refresh();
        }

        private void UpdateStatusLabel(Camera cam)
        {
            var bs = PWR_BuildSystem.Instance;
            Vector3 top = default;
            bool show = bs != null && cam != null && bs.IsActive && bs.TryGetPreviewTop(out top);
            if (statusLabel.gameObject.activeSelf != show) statusLabel.gameObject.SetActive(show);
            if (!show) return;
            // Keep the label the same apparent size however far away the ghost is.
            float dist = Mathf.Max(0.5f, Vector3.Distance(cam.transform.position, top));
            float scale = 0.0005f * dist;
            statusLabel.transform.localScale = Vector3.one * scale;
            Vector3 pos = top + Vector3.up * (0.15f + 55f * scale);
            statusLabel.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cam.transform.position));
            statusLabelText.text = bs.Demolishing ? bs.LastMessage : $"{bs.LastMessage}\nRotation: {bs.QuarterTurns * 90}°";
        }

        private void Refresh()
        {
            var res = PWR_PlayerResources.Instance;
            if (resourceText != null && res != null)
            {
                sb.Clear();
                int i = 0;
                foreach (PWR_ResourceType t in Enum.GetValues(typeof(PWR_ResourceType)))
                {
                    int v = res.Get(t);
                    if (v <= 0 && !PWR_Progression.IsResourceVisible(t)) continue; // not introduced yet
                    sb.Append(v > 0 ? "<color=#FFFFFF>" : "<color=#7A7A7A>")
                      .Append(integrated && t == PWR_ResourceType.Scrap ? "Power Scrap" : PWR_Names.Of(t)).Append(": ").Append(v).Append("</color>");
                    // VR packs the list into two columns to keep the menu short.
                    sb.Append(xr && i % 2 == 0 ? "<pos=50%>" : "\n");
                    i++;
                }
                resourceText.text = i > 0 ? sb.ToString() : integrated ? "<color=#7A7A7A>No Power supplies yet</color>" : "<color=#7A7A7A>No supplies yet</color>";
            }
            if (goalText != null)
            {
                var progression = PWR_Progression.Instance;
                goalText.text = progression != null ? $"<color=#FFC840>Next goal:</color> {progression.NextGoal}" : "";
            }

            var bs = PWR_BuildSystem.Instance;
            if (bs == null) return;
            for (int i = 0; i < buildButtons.Count && i < bs.buildables.Count; i++)
            {
                var def = bs.buildables[i];
                bool limitReached = def.maxCount > 0 && bs.CountBuilt(def.BuildingId) >= def.maxCount;
                buildButtons[i].interactable = !limitReached;
                bool unlocked = bs.IsUnlocked(def);
                if (buildButtons[i].gameObject.activeSelf != unlocked) buildButtons[i].gameObject.SetActive(unlocked);
            }
            if (statusText == null) return;
            if (bs.Demolishing)
                statusText.text = bs.LastMessage;
            else if (bs.selected != null)
                statusText.text = $"{bs.LastMessage}\nRotation: {bs.QuarterTurns * 90}°";
            else
                statusText.text = xr ? bs.LastMessage : "WASD move, hold RMB look, E use, F grab/drop\n" + bs.LastMessage;
            if (PWR_LevelDirector.StationComplete && !bs.IsActive) // the status box only fits two lines
                statusText.text = "<color=#7CFC7C><b>POWER STATION ONLINE</b></color>\n" + statusText.text;
        }
    }
}
