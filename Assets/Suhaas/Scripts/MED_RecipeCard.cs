using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// One recipe button on the bench's recipe board. Colors update from events, never by polling.
[RequireComponent(typeof(XRSimpleInteractable))]
public class MED_RecipeCard : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] MED_PrepBench bench;
    [SerializeField] MED_Recipe recipe;
    [SerializeField] Renderer cardRenderer;
    [SerializeField] TextMeshPro label;
    [SerializeField] Color availableColor = new Color(0.3f, 0.75f, 0.4f);
    [SerializeField] Color unavailableColor = new Color(0.5f, 0.52f, 0.55f);
    [SerializeField] float pressDepth = 0.02f;

    XRSimpleInteractable interactable;
    MaterialPropertyBlock block;
    MED_PlayerResources player;
    Vector3 restPosition;
    bool hovered;
    bool available;
    bool setUp;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        restPosition = transform.localPosition;
    }

    void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
        interactable.selectEntered.AddListener(OnSelect);
        if (bench != null) bench.StateChanged += Refresh;
        MED_ProgressionManager.Changed += OnProgressionChanged;
        Bind();
    }

    void Start()
    {
        if (!setUp)
        {
            setUp = true;
            block = new MaterialPropertyBlock();
            UpdateLabel();
        }
        Bind();
        Refresh();
    }

    void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);
        interactable.selectEntered.RemoveListener(OnSelect);
        if (bench != null) bench.StateChanged -= Refresh;
        MED_ProgressionManager.Changed -= OnProgressionChanged;
        if (player != null) player.OnResourceChanged -= OnResourceChanged;
        player = null;
    }

    void Bind()
    {
        if (player != null) return;
        player = FindAnyObjectByType<MED_PlayerResources>();
        if (player != null) player.OnResourceChanged += OnResourceChanged;
    }

    void OnResourceChanged(MED_ResourceType type, int amount) => Refresh();

    bool IsLocked => MED_ProgressionManager.TryGetFeature(recipe, out var f) && MED_ProgressionManager.IsLocked(f);

    void OnProgressionChanged()
    {
        UpdateLabel();
        Refresh();
    }

    void UpdateLabel()
    {
        if (label == null || recipe == null) return;
        label.text = IsLocked
            ? $"<b>{recipe.displayName}</b>\n<color=#B0B4B8>LOCKED</color>"
            : $"<b>{recipe.displayName}</b>\n{recipe.Description().Replace(" + ", "\n+ ").Replace(" -> ", "\n-> ")}";
    }
    void OnHoverEnter(UnityEngine.XR.Interaction.Toolkit.HoverEnterEventArgs _) { hovered = true; Refresh(); }
    void OnHoverExit(UnityEngine.XR.Interaction.Toolkit.HoverExitEventArgs _) { hovered = false; Refresh(); }

    void OnSelect(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs _)
    {
        if (MED_AreaGate.Outside) return;
        StopAllCoroutines();
        StartCoroutine(PressAnimation());
        if (bench != null) bench.TryStartRecipe(recipe);   // explains why when the recipe is still locked
    }

    void Refresh()
    {
        if (cardRenderer == null || recipe == null || bench == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        available = !IsLocked && bench.IsIdle && bench.CanAfford(recipe, out _);
        var c = available ? availableColor : unavailableColor;
        if (hovered) c = Color.Lerp(c, Color.white, 0.35f);
        cardRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        cardRenderer.SetPropertyBlock(block);
    }

    IEnumerator PressAnimation()
    {
        var pressed = restPosition + Vector3.forward * pressDepth; // card face looks along -Z; +Z pushes it in
        const float half = 0.07f;
        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            transform.localPosition = Vector3.Lerp(restPosition, pressed, t / half);
            yield return null;
        }
        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            transform.localPosition = Vector3.Lerp(pressed, restPosition, t / half);
            yield return null;
        }
        transform.localPosition = restPosition;
    }
}
