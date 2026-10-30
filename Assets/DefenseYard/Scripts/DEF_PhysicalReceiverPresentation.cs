using UnityEngine;

// Recording-style dark physical intake and contrast instructions; receiver still owns transactions.
[DisallowMultipleComponent]
public sealed class DEF_PhysicalReceiverPresentation : MonoBehaviour
{
    MED_WorldPanel panel;
    DEF_WorkshopAmmoReceiver ammo;
    DEF_WorkshopRepairReceiver repair;
    Material material;
    string observedMessage;
    float feedbackUntil;
    void Awake()
    {
        ammo = GetComponent<DEF_WorkshopAmmoReceiver>();
        repair = GetComponent<DEF_WorkshopRepairReceiver>();
        if (ammo == null && repair == null) { enabled = false; return; }
        panel = MED_WorldPanel.Create("Physical delivery instructions and feedback", new Vector2(1.4f, .8f), 1.1f, .85f,
            new Color(.65f, 1, .85f), new Color(.1f, .85f, .6f));
        panel.Root.transform.SetParent(transform, false);
        panel.Root.transform.localPosition = Vector3.up * .85f;
        panel.Body.enableAutoSizing = true; panel.Body.fontSizeMin = .75f; panel.Body.fontSizeMax = .85f;
        panel.Body.overflowMode = TMPro.TextOverflowModes.Truncate;
        panel.Title.raycastTarget = panel.Body.raycastTarget = false;
        foreach (var collider in panel.Root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        if (ammo != null)
        {
            var box = GetComponent<BoxCollider>();
            var size = box.size; var center = box.center;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader); material.color = new Color(.07f, .25f, .22f);
            Part("Ammo intake base", center + new Vector3(0, -size.y * .5f - .06f, 0), new Vector3(size.x + .12f, .12f, size.z + .12f));
            Part("Ammo intake left rim", center + new Vector3(-size.x * .5f - .04f, -.2f, 0), new Vector3(.08f, .4f, size.z + .12f));
            Part("Ammo intake right rim", center + new Vector3(size.x * .5f + .04f, -.2f, 0), new Vector3(.08f, .4f, size.z + .12f));
            Part("Ammo intake back", center + new Vector3(0, -.2f, size.z * .5f + .04f), new Vector3(size.x, .4f, .08f));
            Part("Ammo intake pedestal", center + new Vector3(0, -.75f, 0), new Vector3(.55f, .5f, .55f));
        }
        GetComponent<DEF_ContentGate>()?.Apply();
    }
    void Part(string name, Vector3 position, Vector3 size)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name;
        part.transform.SetParent(transform, false); part.transform.localPosition = position; part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = material;
        var collider = part.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
    }
    void LateUpdate()
    {
        if (panel == null) return;
        string message = ammo != null ? ammo.LastMessage : repair.LastMessage;
        if (message != observedMessage)
        {
            observedMessage = message;
            feedbackUntil = Time.unscaledTime + DEF_UIPresentationPolicy.ReadSeconds(message);
        }
        // A prior rejection must not permanently replace the live delivery instruction.
        bool feedback = !string.IsNullOrEmpty(message) && Time.unscaledTime < feedbackUntil;
        // Full-health defenses do not accumulate permanent battlefield cards.
        panel.Root.SetActive(repair == null || !RepairAtFullIntegrity() || feedback);
        if (!panel.Root.activeSelf) return;
        bool failedAmmoRun = ammo != null && ((ammo.destination != null && ammo.destination.playerHealth <= 0) || (DEF_WaveManager.I != null && DEF_WaveManager.I.AttemptFailed));
        string body = feedback && !failedAmmoRun ? message : CurrentInstruction();
        panel.SetText(ammo != null ? "AMMO INTAKE" : "REPAIR KIT", body);
        var input = DEF_VRInput.Instance;
        var camera = input != null && input.gameObject.scene == gameObject.scene && DEF_VRInput.Active && input.xrCamera != null ? input.xrCamera : Camera.main;
        if (camera != null) panel.Root.transform.rotation = Quaternion.LookRotation(panel.Root.transform.position - camera.transform.position);
    }
    bool RepairAtFullIntegrity()
    {
        if (repair.target is DEF_Turret turret) return turret.health >= turret.maxHealth;
        if (repair.target is DEF_Barricade barricade) return barricade.health >= barricade.maxHealth;
        if (repair.target is DEF_ShockTrap trap) return trap.health >= trap.maxHealth;
        return false;
    }
    string CurrentInstruction()
    {
        if (ammo != null)
        {
            var destination = ammo.destination;
            if ((destination != null && destination.playerHealth <= 0) || (DEF_WaveManager.I != null && DEF_WaveManager.I.AttemptFailed))
                return "Base lost. Restart or retry before delivering ammo. Your box will not be consumed.";
            if (destination != null && destination.ammo >= destination.maxAmmo) return "Defense ammo is full. Save this box until ammo is used.";
            return "Hold grip to carry. Release a crafted Ammo Box inside this intake." +
                (destination != null ? "\nDefense ammo: " + destination.ammo + "/" + destination.maxAmmo : "");
        }
        float health = -1, maximum = 0;
        if (repair.target is DEF_Turret turret) { health = turret.health; maximum = turret.maxHealth; }
        else if (repair.target is DEF_Barricade barricade) { health = barricade.health; maximum = barricade.maxHealth; }
        else if (repair.target is DEF_ShockTrap trap) { health = trap.health; maximum = trap.maxHealth; }
        if (maximum > 0 && health >= maximum) return "Full integrity. Save your RepairKit for a damaged defense.";
        return "Hold grip to carry. Release a crafted RepairKit here." +
            (health >= 0 ? "\nIntegrity: " + Mathf.CeilToInt(health) + "/" + Mathf.CeilToInt(maximum) : "");
    }
    void OnDestroy() { panel?.Destroy(); if (material != null) Destroy(material); }
}
