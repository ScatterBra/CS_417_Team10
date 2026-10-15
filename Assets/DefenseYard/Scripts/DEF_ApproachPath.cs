using UnityEngine;

[CreateAssetMenu(menuName = "Defense Yard/Approach Path")]
public class DEF_ApproachPath : ScriptableObject
{
    [Tooltip("Entry offsets in metres: X is sideways; Y is inward. These form a short, smooth entrance before direct pursuit / obstacle planning.")]
    public Vector2[] waypoints = {
        new Vector2(0f, 1.5f), new Vector2(0.55f, 3f), new Vector2(0f, 4.5f)
    };
}
