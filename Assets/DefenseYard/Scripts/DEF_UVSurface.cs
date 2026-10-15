using UnityEngine;

// Shared material, per-instance UV phase. Used for the reload strip and radio screen.
public class DEF_UVSurface : MonoBehaviour
{
    public enum Signal { Reload, Radio }
    public Signal signal;
    public Renderer surface;
    public DEF_Turret turret;
    MaterialPropertyBlock properties;
    public float Phase { get; private set; }
    public float Distortion { get; private set; }
    public Color readyColor = new Color(0.2f, 0.9f, 0.75f);
    public Color activeColor = new Color(1f, 0.46f, 0.1f);
    void Update()
    {
        if (surface == null) return;
        if (properties == null) properties = new MaterialPropertyBlock();
        Phase += Time.deltaTime * (signal == Signal.Reload ? 1.2f : 0.18f);
        float activity = signal == Signal.Reload && turret != null ? Mathf.Clamp01(turret.ShotCooldownRemaining / turret.cooldown) :
            DEF_WaveManager.I != null ? DEF_WaveManager.I.threat / DEF_WaveManager.I.maxThreat : 0;
        if (signal == Signal.Radio && DEF_WaveManager.I != null && DEF_WaveManager.I.phase == DEF_WaveManager.Phase.Spawning) activity = Mathf.Max(activity, 0.7f);
        Distortion = signal == Signal.Radio ? 0.06f + activity * 0.1f : 0;
        properties.SetFloat("_Phase", Phase);
        properties.SetFloat("_Distortion", Distortion);
        properties.SetColor("_Tint", Color.Lerp(readyColor, activeColor, activity));
        surface.SetPropertyBlock(properties);
    }
}
