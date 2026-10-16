using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Reusable wall/console button: hover highlight, press-in animation, green/grey availability.
// The owner sets availability (event-driven) and listens to Pressed; the button always fires Pressed so the
// owner can explain why an action failed.
[RequireComponent(typeof(XRSimpleInteractable))]
public class MED_ActionButton : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] Renderer buttonRenderer;
    [SerializeField] Color availableColor = new Color(0.3f, 0.75f, 0.4f);
    [SerializeField] Color unavailableColor = new Color(0.5f, 0.52f, 0.55f);
    [SerializeField] float pressDepth = 0.02f;

    XRSimpleInteractable interactable;
    MaterialPropertyBlock block;
    Vector3 restPosition;
    bool hovered;
    bool available;
    bool restCached;

    public event Action Pressed;
    public bool Available => available;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        restPosition = transform.localPosition;
        restCached = true;
    }

    void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
        interactable.selectEntered.AddListener(OnSelect);
    }

    void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);
        interactable.selectEntered.RemoveListener(OnSelect);
    }

    void Start() => ApplyColor();

    public void SetAvailable(bool value)
    {
        available = value;
        ApplyColor();
    }

    void OnHoverEnter(HoverEnterEventArgs _) { hovered = true; ApplyColor(); }
    void OnHoverExit(HoverExitEventArgs _) { hovered = false; ApplyColor(); }

    void OnSelect(SelectEnterEventArgs _)
    {
        StopAllCoroutines();
        if (isActiveAndEnabled) StartCoroutine(PressAnimation());
        Pressed?.Invoke();
    }

    void ApplyColor()
    {
        if (buttonRenderer == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        var c = available ? availableColor : unavailableColor;
        if (hovered) c = Color.Lerp(c, Color.white, 0.35f);
        buttonRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        buttonRenderer.SetPropertyBlock(block);
    }

    IEnumerator PressAnimation()
    {
        if (!restCached) yield break;
        var pressed = restPosition + Vector3.forward * pressDepth; // faces -Z; +Z pushes the button into the building
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
