using UnityEngine;

public class DEF_GameState : MonoBehaviour
{
    public static DEF_GameState I;

    public float playerHealth = 150f;
    public float maxHealth = 150f;
    public int ammo = 60;
    public int maxAmmo = 60;
    public int scrap = 220;
    public int maxScrap = 999;
    public int waveNumber = 0;
    public float gameClock = 0f;
    public Vector2Int selectedTile = new Vector2Int(-1, -1);
    public string selectedZone = "";

    public event System.Action<Vector3, float> BaseHit;
    public void DamageBase(float damage, Vector3 source)
    {
        if (playerHealth <= 0 || damage <= 0) return;
        playerHealth = Mathf.Max(0, playerHealth - damage);
        BaseHit?.Invoke(source, damage);
    }

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
