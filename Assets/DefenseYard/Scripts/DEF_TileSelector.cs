using UnityEngine;
using UnityEngine.InputSystem;

public class DEF_TileSelector : MonoBehaviour
{
    DEF_GridManager grid;
    Camera cam;
    GameObject highlighted;
    int highlightZone;
    Material highlightMat;

    void Start()
    {
        grid = FindObjectOfType<DEF_GridManager>();
        cam = Camera.main;
        highlightMat = new Material(Shader.Find("Standard"));
        highlightMat.color = Color.yellow;
        highlightMat.SetColor("_EmissionColor", Color.yellow * 0.4f);
        highlightMat.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        var ms = Mouse.current;
        if (ms == null || cam == null || grid == null) return;

        if (ms.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(ms.position.ReadValue());
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                string n = hit.collider.gameObject.name;
                if (n.StartsWith("Tile_"))
                {
                    string[] parts = n.Split('_');
                    Select(int.Parse(parts[1]), int.Parse(parts[2]));
                    return;
                }
            }
            Deselect();
        }
    }

    void Select(int x, int z)
    {
        Deselect();
        highlighted = grid.GetTile(x, z);
        highlightZone = grid.ZoneAt(x, z);
        highlighted.GetComponent<Renderer>().material = highlightMat;
        var s = DEF_GameState.I;
        if (s != null)
        {
            s.selectedTile = new Vector2Int(x, z);
            s.selectedZone = grid.ZoneName(x, z);
        }
    }

    void Deselect()
    {
        if (highlighted != null)
        {
            highlighted.GetComponent<Renderer>().material = grid.ZoneMaterial(highlightZone);
            highlighted = null;
        }
        var s = DEF_GameState.I;
        if (s != null) s.selectedTile = new Vector2Int(-1, -1);
    }
}
