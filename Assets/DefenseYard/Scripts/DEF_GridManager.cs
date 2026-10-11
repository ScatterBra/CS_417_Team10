using UnityEngine;

public class DEF_GridManager : MonoBehaviour
{
    public int width = 20;
    public int depth = 20;
    public float tileSize = 2f;

    public string[] zoneNames = { "Concrete", "Dirt", "Grass", "Metal" };

    Color[] zoneColors = {
        new Color(0.55f, 0.55f, 0.58f),
        new Color(0.55f, 0.42f, 0.28f),
        new Color(0.35f, 0.60f, 0.30f),
        new Color(0.30f, 0.32f, 0.38f)
    };

    GameObject[,] tiles;
    int[,] zones;
    Material[] mats;

    void Start()
    {
        mats = new Material[4];
        for (int i = 0; i < 4; i++)
        {
            mats[i] = new Material(Shader.Find("Standard"));
            mats[i].color = zoneColors[i];
        }

        tiles = new GameObject[width, depth];
        zones = new int[width, depth];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int zone = ZoneFor(x, z);
                zones[x, z] = zone;
                GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
                t.name = "Tile_" + x + "_" + z;
                t.transform.parent = transform;
                t.transform.position = TileCenter(x, z) + Vector3.down * 0.1f;
                t.transform.localScale = new Vector3(tileSize * 0.95f, 0.2f, tileSize * 0.95f);
                t.GetComponent<Renderer>().material = mats[zone];
                tiles[x, z] = t;
            }
        }
    }

    int ZoneFor(int x, int z)
    {
        if (x == 0 || z == 0 || x == width - 1 || z == depth - 1) return 0;
        if (x >= width / 2 - 2 && x < width / 2 + 2 && z >= depth / 2 - 2 && z < depth / 2 + 2) return 3;
        if (z == depth / 2) return 2;
        return 1;
    }

    public Vector3 TileCenter(int x, int z)
    {
        float ox = (x - width / 2f + 0.5f) * tileSize;
        float oz = (z - depth / 2f + 0.5f) * tileSize;
        return new Vector3(ox, 0f, oz);
    }

    public bool WorldToTile(Vector3 world, out int x, out int z)
    {
        x = Mathf.FloorToInt(world.x / tileSize + width / 2f);
        z = Mathf.FloorToInt(world.z / tileSize + depth / 2f);
        return x >= 0 && z >= 0 && x < width && z < depth;
    }

    public string ZoneName(int x, int z)
    {
        return zoneNames[zones[x, z]];
    }

    public int ZoneAt(int x, int z)
    {
        return zones[x, z];
    }

    public GameObject GetTile(int x, int z)
    {
        return tiles[x, z];
    }

    public Material ZoneMaterial(int i)
    {
        return mats[i];
    }
}
