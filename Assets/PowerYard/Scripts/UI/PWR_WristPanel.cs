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
    /// the left controller opens it; it then floats just above the right controller facing the player
    /// (the left wrist holds the shared outpost panel), lightly smoothed so hand tremor doesn't shake
    /// the buttons under the left controller ray (or a fingertip poke). While placing, the build status
    /// floats above the ghost instead. On desktop it is a screen overlay with hotkey hints.
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
        private float refresh;
        private InputAction toggle;
        private Canvas statusLabel;
        private TMP_Text statusLabelText;

        private void Awake()
        {
            Instance = this;
            toggle = new InputAction("PWR Menu Toggle", InputActionType.Button, "<XRController>{LeftHand}/{SecondaryButton}");
        }

        private void OnEnable() => toggle.Enable();

        private void OnDisable() => toggle.Disable();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (PWR_BuildSystem.Instance != null)
            {
                PWR_BuildSystem.Instance.OnSelectionChanged -= HandleSelectionChanged;
                PWR_BuildSystem.Instance.OnDemolishModeChanged -= HandleDemolishModeChanged;
            }
        }

        private IEnumerator Start()
        {
            yield return PWR_PlayerRig.WaitForXRStartup();
            var rig = PWR_PlayerRig.Instance;
            var bs = PWR_BuildSystem.Instance;
            int buttonCount = (bs != null ? bs.buildables.Count : 0) + 2; // + Demolish, Cancel
            xr = rig != null && rig.XRActive && rig.rightController != null;

            if (xr)
            {
                int rows = (buttonCount + 1) / 2;
                float gridHeight = rows * XrButtonHeight + (rows - 1) * XrButtonSpacing;
                float unitsHigh = 2 * Padding + 40 + GoalHeight + 5 * 30 + gridHeight + 50 + 4 * 6;
                canvas = PWR_UIFactory.CreateWorldCanvas("PWR_BuildMenu", null, new Vector2(UnitsWide, unitsHigh),
                    worldWidth / UnitsWide, true);
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
                var holder = new GameObject("Panel", typeof(RectTransform));
                holder.transform.SetParent(canvas.transform, false);
                var rt = (RectTransform)holder.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(16f, -16f);
                rt.sizeDelta = new Vector2(UnitsWide, unitsHigh + 60);
                canvas = holder.AddComponent<Canvas>();
                // This sub-canvas belongs to the desktop overlay, including after scene reload.
                // Do not leave its default WorldSpace mode awaiting a later scene scan.
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = Camera.main;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 10;
                holder.AddComponent<GraphicRaycaster>();
                BuildContents(bs, 0f);
                Anchor = canvas.transform;
            }

            if (bs != null)
            {
                bs.OnSelectionChanged += HandleSelectionChanged;
                bs.OnDemolishModeChanged += HandleDemolishModeChanged;
            }
            Refresh();
        }

        /// <summary>Title, resources, build buttons and status line. VR uses two columns of large buttons.</summary>
        private void BuildContents(PWR_BuildSystem bs, float gridHeight)
        {
            PWR_UIFactory.CreatePanel(canvas.transform, PWR_UIFactory.PanelColor);
            var layout = PWR_UIFactory.CreateVerticalLayout(canvas.transform, 6, Padding);
            var title = PWR_UIFactory.CreateLayoutText(layout, "<b>POWER STATION</b>", 28, xr ? 40 : 36);
            title.color = PWR_UIFactory.AccentColor;
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
                    buildButtons.Add(PWR_UIFactory.CreateButton(buttonParent, $"{hotkey}{def.displayName}  <size=75%>({def.CostText()})</size>",
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
            canvas.gameObject.SetActive(true);
            FollowHand(1f); // appear in place, no catch-up from the old spot
            Refresh();
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
            if (xr && canvas != null) canvas.gameObject.SetActive(false);
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

        private void LateUpdate()
        {
            if (canvas == null) return; // still waiting for the XR startup check
            // Outside the Power area (shared scene) the menu is closed and Y does nothing.
            bool inside = PWR_AreaGate.PlayerInside;
            if (!xr && canvas.enabled != inside) canvas.enabled = inside;
            // Disabling a nested overlay Canvas makes Unity treat it as its own WorldSpace root.
            // Assign after that transition, before returning outside the area; never depend on
            // another scene component's periodic scan or a camera from the previous scene.
            if (canvas.worldCamera != Camera.main) canvas.worldCamera = Camera.main;
            if (!inside)
            {
                if (xr) Close();
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
                if (IsOpen) FollowHand(1f - Mathf.Exp(-followSharpness * Time.deltaTime));
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
                      .Append(PWR_Names.Of(t)).Append(": ").Append(v).Append("</color>");
                    // VR packs the list into two columns to keep the menu short.
                    sb.Append(xr && i % 2 == 0 ? "<pos=50%>" : "\n");
                    i++;
                }
                resourceText.text = i > 0 ? sb.ToString() : "<color=#7A7A7A>No supplies yet</color>";
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
