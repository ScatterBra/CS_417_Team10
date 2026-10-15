using System.Text;
using TMPro;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Floating gauge above an instance listing its Stats (rubric: Statted Instances must be visible).
    /// Entries are stat names; append "%" to show a percentage, e.g. "Progress%".
    /// </summary>
    public class PWR_StatLabel : MonoBehaviour
    {
        public string title = "Machine";
        public string[] entries = { "Charge" };
        public Vector3 offset = new Vector3(0f, 1.6f, 0f);
        public float width = 0.75f;
        public float maxDistance = 7f;
        public PWR_StatBlock statBlock;

        [Tooltip("Optional extra line provided by another script.")]
        public string extraLine;

        private TMP_Text text;
        private GameObject canvasGo;
        private float refreshTimer;
        private readonly StringBuilder sb = new StringBuilder();

        private void Start()
        {
            if (statBlock == null) statBlock = GetComponentInParent<PWR_StatBlock>();
            const float unitsWide = 320f;
            int lines = entries.Length + 2; // title + entries + optional extra line
            var canvas = PWR_UIFactory.CreateWorldCanvas("StatLabel", transform,
                new Vector2(unitsWide, 34 + lines * 34), width / unitsWide, false);
            canvasGo = canvas.gameObject;
            canvasGo.transform.localPosition = offset;
            canvasGo.transform.localRotation = Quaternion.identity;
            // Undo parent scale so the label keeps its size on scaled models.
            var ls = transform.lossyScale;
            canvasGo.transform.localScale = new Vector3(
                width / unitsWide / Mathf.Max(0.001f, ls.x),
                width / unitsWide / Mathf.Max(0.001f, ls.y),
                width / unitsWide / Mathf.Max(0.001f, ls.z));
            PWR_UIFactory.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.55f));
            text = PWR_UIFactory.CreateText(canvas.transform, "", 26, TextAlignmentOptions.TopLeft, 8);
            canvasGo.AddComponent<PWR_Billboard>();
            Refresh();
        }

        private void Update()
        {
            if (canvasGo == null) return;
            var cam = Camera.main;
            if (cam != null)
            {
                bool near = (cam.transform.position - transform.position).sqrMagnitude < maxDistance * maxDistance;
                if (canvasGo.activeSelf != near) canvasGo.SetActive(near);
            }
            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.2f;
            Refresh();
        }

        private void Refresh()
        {
            if (text == null) return;
            sb.Clear();
            sb.Append("<b><color=#FFC840>").Append(title).Append("</color></b>");
            if (statBlock != null)
            {
                foreach (var e in entries)
                {
                    bool percent = e.EndsWith("%");
                    string statName = percent ? e.Substring(0, e.Length - 1) : e;
                    var s = statBlock.GetStat(statName);
                    if (s == null) continue;
                    sb.Append('\n').Append(statName).Append(": ");
                    if (percent) sb.Append(Mathf.RoundToInt(s.Normalized * 100f)).Append('%');
                    else sb.Append(s.value.ToString("0")).Append(" / ").Append(s.max.ToString("0"));
                }
            }
            if (!string.IsNullOrEmpty(extraLine)) sb.Append('\n').Append(extraLine);
            text.text = sb.ToString();
        }
    }
}
