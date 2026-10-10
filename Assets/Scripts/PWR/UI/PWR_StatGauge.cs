using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deadzone.Power
{
    /// <summary>
    /// Visual indication of one stat (rubric: Statted Instances must be visible).
    /// Any combination of: UI fill image, text label, a bar transform scaled on X, an emissive/colored renderer.
    /// </summary>
    public class PWR_StatGauge : MonoBehaviour
    {
        public PWR_StatBlock statBlock;
        public string statName = "Charge";

        [Header("Optional outputs")]
        public Image fillImage;
        public TMP_Text label;
        public Transform barTransform;
        public Renderer colorRenderer;
        public Gradient colorByValue = DefaultGradient();

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock mpb;
        private Vector3 barFullScale;

        private void Awake()
        {
            if (statBlock == null) statBlock = GetComponentInParent<PWR_StatBlock>();
            if (barTransform) barFullScale = barTransform.localScale;
            mpb = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (statBlock) statBlock.OnStatChanged += HandleChanged;
        }

        private void OnDisable()
        {
            if (statBlock) statBlock.OnStatChanged -= HandleChanged;
        }

        private void Start() => Refresh();

        private void HandleChanged(string stat, float value, float delta)
        {
            if (stat == statName) Refresh();
        }

        public void Refresh()
        {
            var s = statBlock ? statBlock.GetStat(statName) : null;
            if (s == null) return;
            float t = s.Normalized;

            if (fillImage) fillImage.fillAmount = t;
            if (label) label.text = $"{s.name}: {s.value:0}/{s.max:0}";
            if (barTransform)
                barTransform.localScale = new Vector3(barFullScale.x * t, barFullScale.y, barFullScale.z);
            if (colorRenderer)
            {
                Color c = colorByValue.Evaluate(t);
                colorRenderer.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, c);
                mpb.SetColor(EmissionId, c * 2f);
                colorRenderer.SetPropertyBlock(mpb);
            }
        }

        private static Gradient DefaultGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.yellow, 0.5f), new GradientColorKey(Color.green, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }
    }
}
