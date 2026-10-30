using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Shared hover/press feedback and XRI wiring for the test buttons. Test-only; remove with MED_TestTools.
[RequireComponent(typeof(XRSimpleInteractable))]
public abstract class MED_TestButtonBase : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] Renderer capRenderer;
    [SerializeField] Color normalColor = Color.gray;
    [SerializeField] Color hoverColor = Color.white;
    [SerializeField] Color pressedColor = Color.black;
    [SerializeField] float pushDistance = 0.04f;
    [SerializeField] float pressTime = 0.2f;

    protected MED_PlayerResources Resources;
    XRSimpleInteractable interactable;
    MaterialPropertyBlock block;
    Vector3 capRestPosition;
    bool hovered;
    float pressTimer;

    protected abstract void OnPressed(MED_PlayerResources resources);

    public bool HideInIntegratedScene()
    {
        if (DEF_ClinicTraining.For(this) == null) return false;
        Transform tools = transform;
        for (var parent = transform.parent; parent != null; parent = parent.parent)
            if (parent.name == "MED_TestTools") { tools = parent; break; }
        tools.gameObject.SetActive(false);
        return true;
    }

    void Awake()
    {
        if (HideInIntegratedScene()) return;
        interactable = GetComponent<XRSimpleInteractable>();
        block = new MaterialPropertyBlock();
        if (capRenderer != null) capRestPosition = capRenderer.transform.localPosition;
        interactable.hoverEntered.AddListener(_ => hovered = true);
        interactable.hoverExited.AddListener(_ => hovered = false);
        interactable.selectEntered.AddListener(_ => Press());
    }

    void Start()
    {
        Resources = FindAnyObjectByType<MED_PlayerResources>();
        ApplyVisuals();
    }

    void Press()
    {
        if (HideInIntegratedScene()) return;
        if (MED_AreaGate.Outside) return;
        pressTimer = pressTime;
        if (Resources == null) Resources = FindAnyObjectByType<MED_PlayerResources>();
        if (Resources != null) OnPressed(Resources);
        else Debug.LogWarning("MED test button: no MED_PlayerResources found in scene.", this);
    }

    void Update()
    {
        if (pressTimer > 0f) pressTimer -= Time.deltaTime;
        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        if (capRenderer == null) return;
        bool pressed = pressTimer > 0f;
        var c = pressed ? pressedColor : hovered ? hoverColor : normalColor;
        capRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        capRenderer.SetPropertyBlock(block);
        var target = capRestPosition + Vector3.down * (pressed ? pushDistance : 0f);
        capRenderer.transform.localPosition = Vector3.Lerp(capRenderer.transform.localPosition, target, 0.5f);
    }
}
