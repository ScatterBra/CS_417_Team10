using UnityEngine;

// The 2x2 cell floor pad where the supply cart parks. Its pivot is the pad center; the grid manager reserves its cells.
public class MED_DeliveryBay : MonoBehaviour
{
    [SerializeField] Vector2 size = new Vector2(2f, 2f);

    public Vector2 Size => size;

    void OnDrawGizmos()
    {
        var old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.5f);
        Gizmos.DrawWireCube(Vector3.up * 0.03f, new Vector3(size.x, 0.02f, size.y));
        Gizmos.matrix = old;
    }
}
