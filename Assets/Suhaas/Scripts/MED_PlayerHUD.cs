using System;
using System.Text;
using TMPro;
using UnityEngine;

public class MED_PlayerHUD : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] MED_HudFollow follow;
    [SerializeField] MED_StatBar healthBar;
    [SerializeField] TextMeshPro healthLabel;
    [SerializeField] GameObject resourcePanel;
    [SerializeField] TextMeshPro leftColumn;
    [SerializeField] TextMeshPro rightColumn;
    [SerializeField] Renderer panelBackdrop;
    [SerializeField, Range(0f, 1f)] float panelBackdropAlpha = 0.55f;

    MED_PlayerResources resources;
    MaterialPropertyBlock backdropBlock;
    float lastVisibility = -1f;

    void Start()
    {
        backdropBlock = new MaterialPropertyBlock();
        TryBind();
        SetPanelVisibility(0f);
    }

    void OnDestroy()
    {
        if (resources != null) resources.OnResourceChanged -= OnResourceChanged;
    }

    void TryBind()
    {
        resources = FindAnyObjectByType<MED_PlayerResources>();
        if (resources == null) return;
        resources.OnResourceChanged += OnResourceChanged;
        Refresh();
    }

    void OnResourceChanged(MED_ResourceType type, int amount) => Refresh();

    void Update()
    {
        if (resources == null) { TryBind(); return; }
        float v = follow != null ? follow.PanelVisibility : 0f;
        if (!Mathf.Approximately(v, lastVisibility)) SetPanelVisibility(v);
    }

    void Refresh()
    {
        if (resources == null) return;
        float n = resources.MaxHealth > 0 ? (float)resources.Health / resources.MaxHealth : 0f;
        if (healthBar != null) healthBar.SetNormalized(n);
        if (healthLabel != null) healthLabel.text = $"Health {resources.Health}/{resources.MaxHealth}";

        var types = (MED_ResourceType[])Enum.GetValues(typeof(MED_ResourceType));
        var left = new StringBuilder();
        var right = new StringBuilder();
        for (int i = 0; i < types.Length; i++)
        {
            var sb = i < (types.Length + 1) / 2 ? left : right;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(types[i]).Append("  ").Append(resources.Get(types[i]));
        }
        if (leftColumn != null) leftColumn.text = left.ToString();
        if (rightColumn != null) rightColumn.text = right.ToString();
    }

    void SetPanelVisibility(float v)
    {
        lastVisibility = v;
        bool on = v > 0.001f;
        if (resourcePanel != null && resourcePanel.activeSelf != on) resourcePanel.SetActive(on);
        if (!on) return;
        if (leftColumn != null) leftColumn.alpha = v;
        if (rightColumn != null) rightColumn.alpha = v;
        if (panelBackdrop != null)
        {
            panelBackdrop.GetPropertyBlock(backdropBlock);
            backdropBlock.SetColor(BaseColorId, new Color(0.05f, 0.08f, 0.1f, panelBackdropAlpha * v));
            panelBackdrop.SetPropertyBlock(backdropBlock);
        }
    }
}
