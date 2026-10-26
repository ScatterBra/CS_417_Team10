using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deadzone.Power
{
    [Serializable]
    public class PWR_Recipe
    {
        public string name = "Recipe";
        public List<PWR_ResourceAmount> inputs = new List<PWR_ResourceAmount>();
        public PWR_ResourceType output;
        public int outputAmount = 1;

        [Tooltip("Optional physical object that also appears on the bench (battery, light bulb).")]
        public GameObject spawnPrefab;

        /// <summary>Hidden from the panel until PWR_Progression unlocks it (runtime only).</summary>
        [NonSerialized] public bool locked;

        public string Describe() =>
            $"{string.Join(" + ", inputs.Select(i => i.ToString()))}  →  {outputAmount} {PWR_Names.Of(output)}";
    }

    /// <summary>
    /// Rubric: Crafting Affordance. Each recipe exchanges a decrease in some resources for an increase
    /// in another. Crafting takes a moment; the bench's CraftProgress stat advances passively and the
    /// output is granted when it fills. Batteries and light bulbs also appear on the bench as objects.
    /// </summary>
    [RequireComponent(typeof(PWR_StatBlock))]
    public class PWR_Workbench : MonoBehaviour
    {
        public List<PWR_Recipe> recipes = new List<PWR_Recipe>();
        public string progressStat = "CraftProgress";
        public float craftSeconds = 1.5f;
        public Transform outputPoint;
        public Transform panelAnchor;

        public static event Action<PWR_Workbench, PWR_Recipe> Crafted;

        public bool IsBusy => current != null;

        private PWR_StatBlock stats;
        private PWR_Recipe current;
        private readonly List<Button> buttons = new List<Button>();
        private TMP_Text statusText;
        private float refreshTimer;

        private void Awake() => stats = GetComponent<PWR_StatBlock>();

        private void Start() => BuildPanel();

        public bool CanCraft(PWR_Recipe recipe) =>
            current == null && !recipe.locked && PWR_PlayerResources.Instance != null && PWR_PlayerResources.Instance.CanAfford(recipe.inputs);

        public bool Craft(int index) => index >= 0 && index < recipes.Count && Craft(recipes[index]);

        public void SetRecipeLocked(string recipeName, bool locked)
        {
            foreach (var r in recipes)
                if (r.name == recipeName) r.locked = locked;
            RefreshPanel();
        }

        public bool Craft(PWR_Recipe recipe)
        {
            if (recipe.locked) return false;
            if (current != null)
            {
                PWR_Feedback.Show(FeedbackPos, $"The Workbench is busy making {current.name}. Wait for it to finish.", PWR_Feedback.Info);
                return false;
            }
            var res = PWR_PlayerResources.Instance;
            if (res == null || !res.TrySpend(recipe.inputs))
            {
                PWR_Feedback.Show(FeedbackPos, MissingText(recipe), PWR_Feedback.Bad);
                return false;
            }
            current = recipe;
            stats.Set(progressStat, 0f);
            stats.SetRate(progressStat, stats.GetMax(progressStat) / Mathf.Max(0.1f, craftSeconds));
            RefreshPanel();
            return true;
        }

        private void Update()
        {
            if (current != null && stats.GetStat(progressStat).IsFull) Finish();
            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.25f;
                RefreshPanel();
            }
        }

        private void Finish()
        {
            var recipe = current;
            current = null;
            stats.SetRate(progressStat, 0f);
            stats.Set(progressStat, 0f);
            PWR_PlayerResources.Instance.Add(recipe.output, recipe.outputAmount);
            if (recipe.spawnPrefab != null)
            {
                Transform at = outputPoint != null ? outputPoint : transform;
                Instantiate(recipe.spawnPrefab, at.position, at.rotation);
            }
            PWR_Feedback.Show(FeedbackPos, $"+{recipe.outputAmount} {PWR_Names.Of(recipe.output)}", PWR_Feedback.Good);
            Crafted?.Invoke(this, recipe);
            RefreshPanel();
        }

        private Vector3 FeedbackPos => (outputPoint != null ? outputPoint.position : transform.position) + Vector3.up * 0.5f;

        /// <summary>"Can't make X: missing 1 Electrolyte. Collect it from the Electrolyte Tank."</summary>
        private static string MissingText(PWR_Recipe recipe)
        {
            var res = PWR_PlayerResources.Instance;
            var short_ = recipe.inputs.Where(i => res == null || res.Get(i.type) < i.amount).ToList();
            var missing = short_.Select(i => $"{i.amount - (res == null ? 0 : res.Get(i.type))} {PWR_Names.Of(i.type)}");
            string how = short_.Count > 0 ? " " + PWR_Names.HowToGet(short_[0].type) : "";
            return $"Can't {recipe.name}: missing {string.Join(", ", missing)}.{how}";
        }

        // ---------- recipe panel ----------

        private void BuildPanel()
        {
            Transform anchor = panelAnchor != null ? panelAnchor : transform;
            const float unitsWide = 600f;
            var canvas = PWR_UIFactory.CreateWorldCanvas("WorkbenchPanel", anchor,
                new Vector2(unitsWide, 120 + recipes.Count * 90), 0.8f / unitsWide, true);
            // Keep a constant world size even if the bench model is scaled.
            var ls = anchor.lossyScale;
            canvas.transform.localScale = new Vector3(0.8f / unitsWide / ls.x, 0.8f / unitsWide / ls.y, 0.8f / unitsWide / ls.z);
            PWR_UIFactory.CreatePanel(canvas.transform, PWR_UIFactory.PanelColor);
            var layout = PWR_UIFactory.CreateVerticalLayout(canvas.transform, 8, 12);
            var title = PWR_UIFactory.CreateLayoutText(layout, "<b>WORKBENCH</b>", 30, 40);
            title.color = PWR_UIFactory.AccentColor;
            for (int i = 0; i < recipes.Count; i++)
            {
                int index = i;
                var r = recipes[i];
                buttons.Add(PWR_UIFactory.CreateButton(layout, $"<b>{r.name}</b>\n<size=70%>{r.Describe()}</size>", 80, 26,
                    () => Craft(index)));
            }
            statusText = PWR_UIFactory.CreateLayoutText(layout, "", 22, 34);
            RefreshPanel();
        }

        private void RefreshPanel()
        {
            for (int i = 0; i < buttons.Count && i < recipes.Count; i++)
            {
                if (buttons[i].gameObject.activeSelf == recipes[i].locked) buttons[i].gameObject.SetActive(!recipes[i].locked);
                // Unaffordable recipes stay pressable (pressing explains what is missing) but look dimmed.
                buttons[i].interactable = !recipes[i].locked;
                var label = buttons[i].GetComponentInChildren<TMP_Text>();
                if (label != null) label.alpha = CanCraft(recipes[i]) ? 1f : 0.45f;
            }
            if (statusText == null) return;
            statusText.text = current != null
                ? $"Crafting {current.name}... {Mathf.RoundToInt(stats.GetNormalized(progressStat) * 100f)}%"
                : recipes.All(r => r.locked) ? "No recipes available yet."
                : "Point at a recipe and pull the trigger.";
        }
    }
}
