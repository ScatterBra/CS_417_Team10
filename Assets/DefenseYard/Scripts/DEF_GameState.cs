using UnityEngine;

public class DEF_GameState : MonoBehaviour
{
    public static DEF_GameState I;

    public float playerHealth = 100f;
    public float maxHealth = 100f;
    public int ammo = 30;
    public int maxAmmo = 30;
    public int waveNumber = 0;
    public float gameClock = 0f;
    public Vector2Int selectedTile = new Vector2Int(-1, -1);
    public string selectedZone = "";

    void Awake()
    {
        I = this;
    }

    void Update()
    {
        gameClock += Time.deltaTime;
    }

    public string ClockText()
    {
        int m = (int)(gameClock / 60f);
        int s = (int)(gameClock % 60f);
        return m.ToString("00") + ":" + s.ToString("00");
    }
}
