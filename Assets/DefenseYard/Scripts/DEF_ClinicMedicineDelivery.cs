using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

// One authoritative MedicineUnit moves inventory -> cargo -> delivered stock -> Field Aid.
// The cart is the visible courier, never a second inventory or a new craft receipt.
[DisallowMultipleComponent]
public sealed class DEF_ClinicMedicineDelivery : MonoBehaviour
{
    enum Shipment { Empty, Loaded, InTransit, Delivered, Recoverable }
    DEF_ClinicTraining training;
    DEF_ClinicReadiness readiness;
    DEF_ClinicFieldAid aid;
    DEF_ClinicCourierRoute route;
    MED_SupplyCart cart, shipmentCart;
    MED_MedicineUnit unit;
    Shipment state;
    bool busy;
    string defenseFeedback, lastRouteFailure;
    float defenseFeedbackUntil;
    string status = "Unload incoming ingredients first. Medicine uses only 2 Herbs + 1 Water.";
    float nextScan;
    Vector3 handoffStart;
    float handoffAt;
    GameObject clinicControls, defenseControls, cargoVisual;
    MED_WorldPanel clinicPanel, defensePanel;
    MED_ActionButton loadButton, sendButton;
    TextMeshPro loadLabel;
    XRSimpleInteractable useButton;
    Material blue, white;
    public bool Delivered => state == Shipment.Delivered && unit != null && readiness != null && readiness.IsVerifiedMedicine(unit);
    bool Qualified => this != null && isActiveAndEnabled && aid != null && aid.IsQualified;
    Vector3 ClinicBay => route.Route.GetPointAtDistance(0);

    void Awake() { training = GetComponent<DEF_ClinicTraining>(); readiness = GetComponent<DEF_ClinicReadiness>(); aid = GetComponent<DEF_ClinicFieldAid>(); }
    void Update()
    {
        if (unit != null && (training == null || training.patient == null || !training.patient.IsCurrent(unit))) ClearForFullReset();
        if (unit != null && (state == Shipment.Loaded || state == Shipment.InTransit) &&
            (shipmentCart == null || !shipmentCart.isActiveAndEnabled))
        {
            if (shipmentCart != null) shipmentCart.HoldForMedicine = false;
            state = Shipment.Recoverable; shipmentCart = null;
            status = "Courier unavailable. Your Medicine is retained: RETURN MEDICINE or SEND with the next empty cart.";
        }
        if (Qualified && Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + .5f;
            cart = null;
            foreach (var c in FindObjectsByType<MED_SupplyCart>(FindObjectsSortMode.None))
                if (c.gameObject.scene == gameObject.scene) { cart = c; if (c.State == MED_SupplyCart.CartState.Waiting) break; }
            if (cart != null && cart.InboundPath != null)
            {
                route = DEF_ClinicCourierRoute.Ensure(training, cart.InboundPath);
                if (route != null && route.Route != null) cart.ConfigureDefenseExport(route.Route);
                else if (route != null && route.FailureReason != lastRouteFailure)
                { lastRouteFailure = route.FailureReason; Message("Medicine delivery route unavailable: " + lastRouteFailure + ". No Medicine spent."); }
            }
            if (route != null && route.Route != null && route.DefenseTerminal != null) EnsureControls();
        }
        UpdateDeliveredPackage();
        RefreshControls();
    }
    bool AtClinic() => route != null && route.Route != null && MED_AreaGate.PlayerInside && MED_PlayerProximity.IsNear(ClinicBay, 4f);
    public void ShowUseFeedback(string message)
    {
        if (message == defenseFeedback && Time.unscaledTime < defenseFeedbackUntil) return;
        defenseFeedback = message; defenseFeedbackUntil = Time.unscaledTime + DEF_UIPresentationPolicy.ReadSeconds(message);
    }
    public bool AtDefense() => route != null && route.DefenseTerminal != null && MED_PlayerProximity.IsNear(route.DefenseTerminal.position, 3.5f);
    bool CanUseCart() => cart != null && cart.isActiveAndEnabled && cart.State == MED_SupplyCart.CartState.Waiting && cart.CargoRemaining.Value < .5f;
    void Message(string text) { status = text; MED_PlayerHUD.ShowMessage(text); }
    public bool LoadOrReturn()
    {
        if (busy || !Qualified || !AtClinic()) return false;
        if (cart != null && cart.MedicineTripBlocked)
        {
            busy = true;
            try
            {
                bool cargoAffected = state == Shipment.InTransit && shipmentCart == cart;
                if (cargoAffected) { state = Shipment.Recoverable; shipmentCart = null; }
                if (cart.CancelStalledMedicineTrip())
                {
                    if (this == null || !Qualified) return false;
                    if (cargoAffected) HideCargo();
                    Message(cargoAffected ? "Cancelled stalled delivery. Medicine retained: RETURN MEDICINE, or SEND with the next empty cart." : "Cancelled stalled empty cart. Delivered Medicine is unchanged; normal incoming ingredients resume.");
                    return true;
                }
                if (cargoAffected) { state = Shipment.InTransit; shipmentCart = cart; }
                return false;
            }
            finally { busy = false; }
        }
        if (state == Shipment.Loaded || state == Shipment.Recoverable) return ReturnMedicine();
        if (state != Shipment.Empty) { Message("One Medicine shipment at a time. Use the delivered dose at Defense first."); return false; }
        if (route == null || route.Route == null) { Message("Delivery route is unavailable. No Medicine spent."); return false; }
        if (!CanUseCart()) { Message("Wait for incoming ingredients, then UNLOAD all remaining ingredients before loading Medicine."); return false; }
        if (!cart.CanDispatch(route.Route)) { Message("Cart is not ready for the delivery route. No Medicine spent."); return false; }
        busy = true;
        try
        {
            bool accepted = training.patient.TransferMedicine(crafted =>
            {
                unit = crafted; state = Shipment.Loaded; shipmentCart = cart; shipmentCart.HoldForMedicine = true;
            }, true, readiness.IsVerifiedMedicine);
            if (!accepted) { Message("Craft another Medicine: 2 Herbs + 1 Water. Keep it in your inventory, not REFILL stock."); return false; }
            if (this == null || !Qualified || unit == null) return false;
            ShowCargoOnCart(); Message("Loaded 1 Medicine. Incoming ingredients are separate. Grip SEND TO DEFENSE when ready."); return true;
        }
        finally { busy = false; }
    }
    bool ReturnMedicine()
    {
        busy = true;
        try
        {
            bool returned = training.patient.ReceiveMedicine(() =>
            {
                var result = unit; unit = null; state = Shipment.Empty;
                if (shipmentCart != null) shipmentCart.HoldForMedicine = false;
                shipmentCart = null; return result;
            });
            if (!returned) { Message("Cannot return Medicine yet. Cargo is retained; try again."); return false; }
            if (this == null || !Qualified) return false;
            HideCargo(); Message("Medicine returned to your inventory. No delivery was counted."); return true;
        }
        finally { busy = false; }
    }
    public bool Send()
    {
        if (busy || !Qualified || !AtClinic()) return false;
        if ((state != Shipment.Loaded && state != Shipment.Recoverable) || unit == null) { Message("LOAD MEDICINE first. SEND never creates or spends another dose."); return false; }
        if (!readiness.IsVerifiedMedicine(unit)) { Message("This cargo is not valid for the current run. No dispatch."); return false; }
        if (route == null || route.Route == null || !CanUseCart() || !cart.CanDispatch(route.Route))
        { Message("Wait for an empty cart at the Clinic bay. Your loaded Medicine is retained."); return false; }
        busy = true;
        try
        {
            shipmentCart = cart; shipmentCart.HoldForMedicine = true;
            state = Shipment.InTransit;
            if (!shipmentCart.TryDispatch(route.Route, Arrived))
            { state = Shipment.Loaded; Message("Cart could not depart. Medicine is still loaded; try SEND again."); return false; }
            if (this == null || !Qualified || unit == null) return false;
            ShowCargoOnCart(); Message("Medicine sent to Defense. Follow the visible cart; Field Aid is available only after it arrives."); return true;
        }
        finally { busy = false; }
    }
    void Arrived(MED_SupplyCart source)
    {
        if (this == null || state != Shipment.InTransit || source != shipmentCart || unit == null || !Qualified ||
            route == null || route.Route == null || source.State != MED_SupplyCart.CartState.AtDefense ||
            Vector3.Distance(source.transform.position, route.Route.GetPointAtDistance(route.Route.TotalLength)) > .3f || !readiness.IsVerifiedMedicine(unit)) return;
        state = Shipment.Delivered; shipmentCart = null;
        DockDeliveredPackage(); status = "Delivered: 1 Medicine in the outpost medical-supply bin. Grip the package for Field Aid.";
    }
    public bool TryConsumeDelivered(Action<MED_MedicineUnit> activate)
    {
        if (busy || !Qualified || !Delivered || !AtDefense() || activate == null) return false;
        busy = true;
        try
        {
            var delivered = unit;
            // Caller is prevalidated and only sets the movement boost's fields; no inventory/event callbacks.
            activate(delivered);
            unit = null; state = Shipment.Empty; HideCargo(); status = "Delivered Medicine used for Field Aid. Craft and send another when needed.";
            return true;
        }
        finally { busy = false; }
    }
    public void ClearForFullReset()
    {
        if (shipmentCart != null) shipmentCart.HoldForMedicine = false;
        unit = null; shipmentCart = null; state = Shipment.Empty; HideCargo();
        status = "New run: complete Clinic self-treatment before sending Medicine.";
    }
    void EnsureControls()
    {
        if (blue == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) shader = Shader.Find("Standard");
            blue = new Material(shader); blue.color = new Color(.12f, .45f, .7f);
            white = new Material(shader); white.color = Color.white;
        }
        if (clinicControls == null)
        {
            clinicControls = new GameObject("Outgoing Clinic Medicine controls"); clinicControls.transform.SetParent(transform, false);
            clinicControls.transform.position = ClinicBay + new Vector3(1.5f, 0, 0);
            loadButton = MakeClinicButton(clinicControls.transform, "LOAD MEDICINE", new Vector3(0, 1.1f, 0), out loadLabel); loadButton.Pressed += OnLoad;
            sendButton = MakeClinicButton(clinicControls.transform, "SEND TO DEFENSE", new Vector3(0, .78f, 0), out _); sendButton.Pressed += OnSend;
            clinicPanel = Panel(clinicControls.transform, "OUTGOING MEDICINE", new Vector3(0, 1.6f, .03f));
        }
        if (defenseControls == null)
        {
            defenseControls = new GameObject("Delivered Clinic Medicine at Defense"); defenseControls.transform.SetParent(route.DefenseTerminal, false);
            defenseControls.transform.localPosition = new Vector3(1.15f, 0, 0);
            // Fixed open medical bin, outside the courier footprint. It never grants inventory.
            BinPart("Medical supply bin pedestal", new Vector3(0, .33f, 0), new Vector3(.55f, .66f, .5f));
            BinPart("Medical supply bin base", new Vector3(0, .72f, 0), new Vector3(.8f, .12f, .7f));
            BinPart("Medical supply bin left", new Vector3(-.38f, .85f, 0), new Vector3(.06f, .25f, .7f));
            BinPart("Medical supply bin right", new Vector3(.38f, .85f, 0), new Vector3(.06f, .25f, .7f));
            BinPart("Medical supply bin back", new Vector3(0, .85f, .32f), new Vector3(.8f, .25f, .06f));
            defensePanel = Panel(defenseControls.transform, "OUTPOST MEDICAL SUPPLIES", new Vector3(0, 1.6f, .03f));
        }
    }
    MED_ActionButton MakeClinicButton(Transform root, string text, Vector3 position, out TextMeshPro label)
    { return Button(root, text, position, out label).AddComponent<MED_ActionButton>(); }
    GameObject Button(Transform root, string text, Vector3 position, out TextMeshPro label)
    {
        var cap = GameObject.CreatePrimitive(PrimitiveType.Cube); cap.name = text; cap.transform.SetParent(root, false);
        cap.transform.localPosition = position; cap.transform.localScale = new Vector3(.7f, .22f, .08f); cap.GetComponent<Renderer>().sharedMaterial = blue;
        var go = new GameObject(text + " label"); go.transform.SetParent(root, false); go.transform.localPosition = position + Vector3.back * .045f;
        label = go.AddComponent<TextMeshPro>(); label.text = text; label.rectTransform.sizeDelta = new Vector2(.65f, .2f);
        label.fontSize = 1.4f; label.enableAutoSizing = true; label.fontSizeMin = .7f; label.fontSizeMax = 1.4f;
        label.alignment = TextAlignmentOptions.Center; label.color = Color.white; label.raycastTarget = false;
        return cap;
    }
    MED_WorldPanel Panel(Transform root, string title, Vector3 position)
    {
        var panel = MED_WorldPanel.Create(title, new Vector2(1.3f, .65f), 1.1f, .75f, Color.white, new Color(.15f, .7f, 1));
        panel.Root.transform.SetParent(root, false); panel.Root.transform.localPosition = position;
        foreach (var collider in panel.Root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        panel.Title.raycastTarget = panel.Body.raycastTarget = false;
        return panel;
    }
    void OnLoad() => LoadOrReturn();
    void OnSend() => Send();
    void OnUse(SelectEnterEventArgs _) { if (aid != null) aid.TryActivate(); }
    void RefreshControls()
    {
        if (clinicControls != null) clinicControls.SetActive(Qualified);
        if (defenseControls != null) defenseControls.SetActive(Qualified);
        if (loadLabel != null) loadLabel.text = cart != null && cart.MedicineTripBlocked ? "CANCEL STALLED CART" : state == Shipment.Loaded || state == Shipment.Recoverable ? "RETURN MEDICINE" : "LOAD MEDICINE";
        if (loadButton != null) loadButton.SetAvailable(Qualified && (cart != null && cart.MedicineTripBlocked || state == Shipment.Loaded || state == Shipment.Recoverable || (state == Shipment.Empty && CanUseCart())));
        if (sendButton != null) sendButton.SetAvailable(Qualified && (state == Shipment.Loaded || state == Shipment.Recoverable) && CanUseCart());
        if (clinicPanel != null)
        {
            string text = state == Shipment.InTransit && shipmentCart != null ? shipmentCart.TravelStatus : status;
            if (state == Shipment.Empty && cart != null && cart.CargoRemaining.Value > .5f) text = "Incoming ingredients: " + Mathf.CeilToInt(cart.CargoRemaining.Value) + " left. UNLOAD INGREDIENTS. Then LOAD 1 crafted Medicine and SEND TO DEFENSE.";
            clinicPanel.SetText("OUTGOING MEDICINE", text);
        }
        if (defensePanel != null) defensePanel.SetText("OUTPOST MEDICAL SUPPLIES", Time.unscaledTime < defenseFeedbackUntil ? defenseFeedback : Delivered ? "Medicine in bin: 1\nGrip the package for FIELD AID: +15% movement for 60s across all yards. Crafting unchanged." :
            state == Shipment.InTransit && shipmentCart != null ? shipmentCart.TravelStatus + ". If blocked, clear the route or cancel at Clinic." :
            aid != null && aid.IsBoostActive ? "FIELD AID active: " + Mathf.CeilToInt(aid.SecondsRemaining) + "s left. No stacking." : "No Medicine delivered. Craft a dose at Clinic, LOAD MEDICINE, then SEND TO DEFENSE.");
    }
    void ShowCargoOnCart()
    {
        HideCargo(); if (shipmentCart == null) return;
        cargoVisual = GameObject.CreatePrimitive(PrimitiveType.Cube); cargoVisual.name = "One authenticated Medicine shipment";
        cargoVisual.transform.SetParent(shipmentCart.transform, false); cargoVisual.transform.localPosition = Vector3.up * .8f;
        cargoVisual.transform.localScale = new Vector3(.5f, .4f, .5f); cargoVisual.GetComponent<Renderer>().sharedMaterial = blue;
        var collider = cargoVisual.GetComponent<Collider>(); collider.enabled = false;
        var labelObject = new GameObject("Medicine package label"); labelObject.transform.SetParent(cargoVisual.transform, false);
        labelObject.transform.localPosition = new Vector3(0, 0, -.51f);
        var label = labelObject.AddComponent<TextMeshPro>(); label.text = "+ MEDICINE"; label.fontSize = 2;
        label.rectTransform.sizeDelta = new Vector2(.95f, .8f); label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
    }
    void BinPart(string name, Vector3 position, Vector3 size)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name;
        part.transform.SetParent(defenseControls.transform, false); part.transform.localPosition = position; part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = white;
        part.GetComponent<Collider>().enabled = false; // Presentation only; do not obstruct courier or rays.
    }
    void DockDeliveredPackage()
    {
        if (!Delivered || defenseControls == null || cargoVisual == null) return;
        handoffStart = cargoVisual.transform.position; handoffAt = Time.time;
        cargoVisual.transform.SetParent(defenseControls.transform, true);
        useButton = cargoVisual.AddComponent<XRSimpleInteractable>();
        useButton.selectEntered.AddListener(OnUse);
        useButton.enabled = false;
    }
    void UpdateDeliveredPackage()
    {
        if (!Delivered || cargoVisual == null || defenseControls == null) return;
        var destination = defenseControls.transform.TransformPoint(new Vector3(0, 1, 0));
        float t = Mathf.Clamp01((Time.time - handoffAt) / .65f);
        cargoVisual.transform.position = Vector3.Lerp(handoffStart, destination, t) + Vector3.up * (.2f * Mathf.Sin(t * Mathf.PI));
        cargoVisual.transform.rotation = defenseControls.transform.rotation;
        if (t >= 1 && useButton != null) { cargoVisual.GetComponent<Collider>().enabled = true; useButton.enabled = true; }
    }
    void HideCargo()
    {
        if (useButton != null) useButton.selectEntered.RemoveListener(OnUse);
        useButton = null;
        if (cargoVisual != null) { cargoVisual.SetActive(false); if (Application.isPlaying) Destroy(cargoVisual); else DestroyImmediate(cargoVisual); }
        cargoVisual = null;
    }
    void OnDisable()
    {
        if (clinicControls != null) clinicControls.SetActive(false);
        if (defenseControls != null) defenseControls.SetActive(false);
    }
    void OnDestroy()
    {
        if (loadButton != null) loadButton.Pressed -= OnLoad;
        if (sendButton != null) sendButton.Pressed -= OnSend;
        if (useButton != null) useButton.selectEntered.RemoveListener(OnUse);
        if (shipmentCart != null) shipmentCart.HoldForMedicine = false;
        clinicPanel?.Destroy(); defensePanel?.Destroy(); HideCargo();
        if (clinicControls != null) Destroy(clinicControls); if (defenseControls != null) Destroy(defenseControls);
        if (blue != null) Destroy(blue); if (white != null) Destroy(white);
    }
}
