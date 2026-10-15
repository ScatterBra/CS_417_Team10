using UnityEngine;
using System.Collections.Generic;

// The saved sound prefab owns its AudioSource; this component controls playback and lifetime.
public class DEF_Audio : MonoBehaviour
{
    public AudioSource source;
    float remaining;
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    void Update()
    {
        remaining -= Time.unscaledDeltaTime;
        if (remaining <= 0f) Destroy(gameObject);
    }
    public static AudioClip Get(string name)
    {
        if (cache.TryGetValue(name, out AudioClip clip)) return clip;
        clip = Resources.Load<AudioClip>("Audio/" + name);
        cache[name] = clip;
        return clip;
    }
    public static void Play(string name, Vector3 pos, float vol = 1f, float pitch = 1f)
    {
        PlayClip(name, pos, vol, pitch, true);
    }
    public static void PlayUI(string name, float vol = 0.6f)
    {
        PlayClip(name, Vector3.zero, vol, 1f, false);
    }
    static void PlayClip(string name, Vector3 position, float volume, float pitch, bool spatial)
    {
        var clip = Get(name);
        if (clip == null) return;
        var prefab = Resources.Load<DEF_Audio>("Prefabs/DEF_Sound");
        if (prefab == null) { Debug.LogError("Sound prefab is missing."); return; }
        var sound = Instantiate(prefab, position, Quaternion.identity);
        sound.name = spatial ? "Sfx" : "SfxUI";
        sound.source.clip = clip;
        sound.source.volume = volume;
        sound.source.pitch = spatial ? pitch * Random.Range(.94f, 1.06f) : 1f;
        sound.source.spatialBlend = spatial ? 1f : 0f;
        sound.remaining = clip.length / Mathf.Abs(sound.source.pitch) + .2f;
        sound.source.Play();
    }
}
