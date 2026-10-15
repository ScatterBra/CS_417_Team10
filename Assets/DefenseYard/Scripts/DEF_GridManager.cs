using UnityEngine;

// The grid remains a gameplay coordinate system. Its visible surface is an ordinary
// saved mesh in YardGround.prefab, so the yard is visible and editable outside Play.
public class DEF_GridManager : MonoBehaviour
{
    public int width = 20;
    public int depth = 20;
    public float tileSize = 2f;
    public MeshRenderer groundRenderer;
    public Collider groundCollider;
    public string[] zoneNames = { "Perimeter", "Asphalt yard", "Service lane", "Base apron" };

    public Vector3 TileCenter(int x, int z)
    {
        return transform.TransformPoint(new Vector3((x - width / 2f + 0.5f) * tileSize,
            0f, (z - depth / 2f + 0.5f) * tileSize));
    }
    public bool WorldToTile(Vector3 world, out int x, out int z)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        x = Mathf.FloorToInt(local.x / tileSize + width / 2f);
        z = Mathf.FloorToInt(local.z / tileSize + depth / 2f);
        return x >= 0 && z >= 0 && x < width && z < depth;
    }
    public int ZoneAt(int x, int z)
    {
        if (x == 0 || z == 0 || x == width - 1 || z == depth - 1) return 0;
        if (x >= width / 2 - 2 && x < width / 2 + 2 && z >= depth / 2 - 2 && z < depth / 2 + 2) return 3;
        return z == depth / 2 ? 2 : 1;
    }
    public string ZoneName(int x, int z) { return zoneNames[ZoneAt(x, z)]; }
}
