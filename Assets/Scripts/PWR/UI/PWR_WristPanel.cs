using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deadzone.Power
{
    /// <summary>
    /// The player's wrist panel: resource list, build menu and build status. In VR it floats above the
    /// left controller facing the player; on desktop it is a screen overlay with hotkey hints.
    /// </summary>
    public class PWR_WristPanel : MonoBehaviour
    {
        public static PWR_WristPanel Instance { get; private set; }

        public Vector3 controllerOffset = new Vector3(0f, 0.12f, 0.06f);
        public float worldWidth = 0.26f;

        /// <summary>Transform tutorials about the menu attach to.</summary>
        public Transform Anchor { get; private set; }

        private TMP_Text resourceText;
        private TMP_Text statusText;
        private readonly List<Button> buildButtons = new List<Button>();
        private readonly StringBuilder sb = new StringBuilder();
        private Transform follow;
        private Canvas canvas;
        private float refresh;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            var rig = PWR_PlayerRig.Instance;
            var bs = PWR_BuildSystem.Instance;
            int buttonCount = (bs != null ? bs.buildables.Count : 0) + 1;
            bool xr = rig != null && rig.XRActive && rig.leftController != null;

            const float unitsWide = 420f;
            float unitsHigh = 70 + 10 * 30 + buttonCount * 58 + 90;
            if (xr)
            {
                canvas = PWR_UIFactory.CreateWorldCanvas("PWR_WristPanel", null, new Vector2(unitsWide, unitsHigh),
                    worldWidth / unitsWide, true);
                follow = rig.leftController;
                var box = canvas.gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(unitsWide, unitsHigh, 4f);
            }
            else
            {
                canvas = PWR_UIFactory.CreateOverlayCanvas("PWR_DesktopHUD");
                var holder = new GameObject("Panel", typeof(RectTransform));
                holder.transform.SetParent(canvas.transform, false);
                var rt = (RectTransform)holder.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(16f, -16f);
                rt.sizeDelta = new Vector2(unitsWide, unitsHigh + 60);
                canvas = holder.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 10;
                holder.AddComponent<GraphicRaycaster>();
            }
            Anchor = canvas.transform;

            PWR_UIFactory.CreatePanel(canvas.transform, PWR_UIFactory.PanelColor);
            var layout = PWR_UIFactory.CreateVerticalLayout(canvas.transform, 6, 10);
            var title = PWR_UIFactory.CreateLayoutText(layout, "<b>POWER STATION</b>", 28, 36);
            title.color = PWR_UIFactory.AccentColor;
            resourceText = PWR_UIFactory.CreateLayoutText(layout, "", 22, 10 * 30);

            var input = FindAnyObjectByType<PWR_BuildInput>();
            if (bs != null)
            {
                for (int i = 0; i < bs.buildables.Count; i++)
                {
                    int index = i;
                    var def = bs.buildables[i];
                    string hotkey = xr ? "" : $"[{i + 1}] ";
                    buildButtons.Add(PWR_UIFactory.CreateButton(layout, $"{hotkey}{def.displayName}  <size=75%>({def.CostText()})</size>",
                        52, 22, () => { if (input != null) input.SelectIndex(index); }));
                }
                PWR_UIFactory.CreateButton(layout, xr ? "Cancel (B)" : "[Q] Cancel", 46, 22,
                    () => bs.Select(null));
            }
            statusText = PWR_UIFactory.CreateLayoutText(layout, "", 18, 80);
            if (!xr)
                statusText.text = "WASD move, hold RMB look, E use, F grab/drop";
            Refresh();
        }

        private void LateUpdate()
        {
            if (follow != null && canvas != null)
            {
                canvas.transform.position = follow.TransformPoint(controllerOffset);
                var cam = Camera.main;
                if (cam != null)
                    canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - cam.transform.position);
            }
            refresh -= Time.deltaTime;
            if (refresh > 0f) return;
            refresh = 0.2f;
            Refresh();
        }

        private void Refresh()
        {
            var res = PWR_PlayerResources.Instance;
            if (resourceText != null && res != null)
            {
                sb.Clear();
                foreach (PWR_ResourceType t in Enum.GetValues(typeof(PWR_ResourceType)))
                {
                    int v = res.Get(t);
                    sb.Append(v > 0 ? "<color=#FFFFFF>" : "<color=#7A7A7A>")
                      .Append(PWR_Names.Of(t)).Append(": ").Append(v).Append("</color>\n");
                }
                resourceText.text = sb.ToString();
            }

            var bs = PWR_BuildSystem.Instance;
            if (bs == null) return;
            for (int i = 0; i < buildButtons.Count && i < bs.buildables.Count; i++)
            {
                var def = bs.buildables[i];
                bool limitReached = def.maxCount > 0 && bs.CountBuilt(def.BuildingId) >= def.maxCount;
                buildButtons[i].interactable = !limitReached;
            }
            if (statusText != null && bs.selected != null)
                statusText.text = $"{bs.LastMessage}\nRotation: {bs.QuarterTurns * 90}°";
            else if (statusText != null)
                statusText.text = PWR_PlayerRig.Instance != null && PWR_PlayerRig.Instance.XRActive
                    ? bs.LastMessage
                    : "WASD move, hold RMB look, E use, F grab/drop\n" + bs.LastMessage;
        }
    }
}
