using UnityEngine;

public class DEF_BaseStructure : MonoBehaviour
{
    public static DEF_BaseStructure Instance { get; private set; }
    public float halfSize = 3.5f;
    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }
    public bool Contains(Vector3 p, float margin = 0f)
    {
        Vector3 d = p - transform.position;
        return Mathf.Abs(d.x) < halfSize + margin && Mathf.Abs(d.z) < halfSize + margin;
    }
    public float DistanceToWall(Vector3 p)
    {
        Vector3 d = p - transform.position;
        return new Vector2(Mathf.Max(0f, Mathf.Abs(d.x) - halfSize),
            Mathf.Max(0f, Mathf.Abs(d.z) - halfSize)).magnitude;
    }
}
