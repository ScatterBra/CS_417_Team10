using UnityEngine;

public class DEF_AmmoCrate : MonoBehaviour
{
    public int supply = 40;

    public static void SpawnAt(Vector3 pos, int amount)
    {
        GameObject go = new GameObject("AmmoCrate");
        go.transform.position = pos;
        DEF_AmmoCrate c = go.AddComponent<DEF_AmmoCrate>();
        c.supply = amount;
        c.BuildVisuals();
    }

    void BuildVisuals()
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.transform.SetParent(transform);
        box.transform.localPosition = Vector3.up * 0.4f;
        box.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
        box.GetComponent<Renderer>().material.color = new Color(0.35f, 0.4f, 0.2f);
    }

    public int Harvest(int amount)
    {
        int taken = Mathf.Min(amount, supply);
        supply -= taken;
        var s = DEF_GameState.I;
        if (s != null) s.ammo = Mathf.Min(s.maxAmmo, s.ammo + taken);
        float f = (float)supply / 40f;
        transform.localScale = Vector3.one * (0.5f + 0.5f * f);
        if (supply <= 0) Destroy(gameObject);
        return taken;
    }
}
