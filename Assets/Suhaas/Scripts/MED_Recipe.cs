using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName = "MED/Recipe", fileName = "MED_Recipe_New")]
public class MED_Recipe : ScriptableObject
{
    public string displayName;
    public List<MED_ResourceCost> inputs = new List<MED_ResourceCost>();
    public List<MED_ResourceCost> outputs = new List<MED_ResourceCost>();
    [Min(0.1f)] public float duration = 3f;

    /// <summary>Short readable line, e.g. "2 Herbs + 1 Water -> Medicine".</summary>
    public string Description()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < inputs.Count; i++)
        {
            if (i > 0) sb.Append(" + ");
            sb.Append(inputs[i].amount).Append(' ').Append(inputs[i].type);
        }
        sb.Append(" -> ");
        for (int i = 0; i < outputs.Count; i++)
        {
            if (i > 0) sb.Append(" + ");
            if (outputs[i].amount != 1) sb.Append(outputs[i].amount).Append(' ');
            sb.Append(outputs[i].type);
        }
        return sb.ToString();
    }
}
