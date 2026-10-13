using UnityEngine;

public class DEF_ThreatMeter : MonoBehaviour
{
    public static DEF_ThreatMeter I;
    public float threat = 0f;
    public float perSecond = 0.5f;
    public float maxThreat = 100f;

    void Awake()
    {
        I = this;
    }

    void Update()
    {
        threat = Mathf.Min(maxThreat, threat + perSecond * Time.deltaTime);
    }

    public void AddThreat(float amount)
    {
        threat = Mathf.Min(maxThreat, threat + amount);
    }
}
