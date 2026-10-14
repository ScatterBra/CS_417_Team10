using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Deadzone.Power.EditorTools
{
    /// <summary>
    /// Generates every Power Station asset (textures, materials, prefabs, build defs) and wires the
    /// PWR scene from the placeholder markers the designer placed. Safe to run again: it rebuilds
    /// the assets and the "PWR_Station" root, and only deactivates the designer's markers.
    /// </summary>
    public static class PWR_StationBuilder
    {
        const string ArtRoot = "Assets/Art/PWR";
        const string TexDir = ArtRoot + "/Textures";
        const string MatDir = ArtRoot + "/Materials";
        const string PrefabDir = "Assets/Prefabs/PWR";
        const string DefDir = PrefabDir + "/Defs";
        const string ModelDir = "Assets/Models/PWR";
        const string XROriginPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string StationRootName = "PWR_Station";

        // Grid covering the user's 15 x 15 m floor (floor top at y = 0.05).
        static readonly Vector3 GridOrigin = new Vector3(-7.5f, 0.05f, -7.5f);
        static readonly Vector2Int GridSize = new Vector2Int(15, 15);
        static readonly Vector2Int FloodlightCell = new Vector2Int(4, 5);
        static readonly Vector2Int BankHintCell = new Vector2Int(4, 4);

        // Power chain speed: generator output, generator -> bank, bank -> charger and the charger's intake
        // all run at this many units per 0.5 s tick. 5.5 = 11/s, so a full charge (1000) takes ~90 s.
        const float PowerPerTick = 5.5f;
        // Energy one unit of a fuel stat burns into; the burn rate follows the output so fuel lasts as
        // many units of energy as before (1 coal = 25 fuel = 150 energy).
        const float EnergyPerFuelStat = 6f;

        // ------------------------------------------------------------------ menu

        [MenuItem("Tools/PWR/Build Assets + Setup Scene")]
        public static void BuildAll()
        {
            BuildAssets();
            SetupScene();
        }

        [MenuItem("Tools/PWR/Build Assets Only")]
        public static void BuildAssets()
        {
            EnsureFolder(TexDir);
            EnsureFolder(MatDir);
            EnsureFolder(DefDir);
            var p = BuildPrefabs();
            BuildDefs(p);
            AssetDatabase.SaveAssets();
            Debug.Log("[PWR] Assets built.");
        }

        // ------------------------------------------------------------------ textures & materials

        class Mats
        {
            public Material concrete, belt, rail, coal, metal, steel, warm, chargedBattery, buttonIdle, buttonRunning,
                teal, binMat, chuteMat, copper, glass, darkMetal, arrow, rings, gridLines, ghost;
        }

        static Texture2D MakeTexture(string name, int size, Func<float, float, Color> pixel, TextureWrapMode wrap)
        {
            string path = $"{TexDir}/{name}.png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, pixel((x + 0.5f) / size, (y + 0.5f) / size));
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = wrap;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material SaveMaterial(Material mat, string name)
        {
            string path = $"{MatDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = mat.shader;
                existing.CopyPropertiesFromMaterial(mat);
                existing.shaderKeywords = mat.shaderKeywords;
                UnityEngine.Object.DestroyImmediate(mat);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static Material Lit(string name, Color color, float metallic = 0f, float smoothness = 0.3f, Color? emission = null,
            Texture2D baseMap = null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            if (baseMap != null) mat.SetTexture("_BaseMap", baseMap);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", emission.Value);
            }
            return SaveMaterial(mat, name);
        }

        static Material Scroll(string name, Texture2D tex, Color color, Vector2 speed, Vector2 tiling)
        {
            var shader = Shader.Find("PWR/UVScroll");
            if (shader == null) throw new Exception("Shader PWR/UVScroll not found (Assets/Art/PWR/Shaders).");
            var mat = new Material(shader);
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", tiling);
            mat.SetColor("_BaseColor", color);
            mat.SetVector("_ScrollSpeed", new Vector4(speed.x, speed.y, 0f, 0f));
            return SaveMaterial(mat, name);
        }

        static Mats CreateMats()
        {
            var arrowTex = MakeTexture("PWR_ArrowChevron", 64, (u, v) =>
            {
                float chevron = 0.62f - Mathf.Abs(u - 0.5f) * 0.9f;
                bool on = Mathf.Abs(v - chevron) < 0.11f && Mathf.Abs(u - 0.5f) < 0.42f;
                return on ? new Color(1f, 0.8f, 0.15f, 1f) : new Color(0f, 0f, 0f, 0f);
            }, TextureWrapMode.Repeat);
            var ringTex = MakeTexture("PWR_BlastRings", 64, (u, v) =>
            {
                float band = Mathf.Repeat(v * 3f, 1f);
                float a = Mathf.Clamp01(1f - Mathf.Abs(band - 0.5f) * 4f);
                return new Color(1f, 1f, 1f, a);
            }, TextureWrapMode.Repeat);
            var gridTex = MakeTexture("PWR_GridLines", 64, (u, v) =>
            {
                bool line = u < 0.04f || u > 0.96f || v < 0.04f || v > 0.96f;
                return line ? new Color(0.3f, 0.9f, 1f, 0.7f) : new Color(0.3f, 0.9f, 1f, 0.04f);
            }, TextureWrapMode.Repeat);
            var white = MakeTexture("PWR_White", 4, (u, v) => Color.white, TextureWrapMode.Repeat);

            return new Mats
            {
                concrete = Lit("PWR_Concrete", new Color(0.55f, 0.55f, 0.53f)),
                belt = Lit("PWR_BeltBase", new Color(0.12f, 0.12f, 0.13f), 0.2f, 0.4f),
                rail = Lit("PWR_BeltRail", new Color(0.85f, 0.65f, 0.1f), 0.5f, 0.5f),
                coal = Lit("PWR_Coal", new Color(0.06f, 0.06f, 0.06f), 0f, 0.6f),
                metal = Lit("PWR_ScrapMetal", new Color(0.5f, 0.52f, 0.55f), 0.8f, 0.35f),
                steel = Lit("PWR_Steel", new Color(0.7f, 0.72f, 0.75f), 0.9f, 0.6f),
                darkMetal = Lit("PWR_DarkMetal", new Color(0.18f, 0.19f, 0.2f), 0.7f, 0.4f),
                warm = Lit("PWR_BulbWarm", new Color(1f, 0.92f, 0.7f), 0f, 0.8f, new Color(2.5f, 2.1f, 1.2f)),
                chargedBattery = Lit("PWR_ChargedBattery", Color.white, 0f, 0.4f, new Color(0.15f, 0.9f, 0.25f),
                    AssetDatabase.LoadAssetAtPath<Texture2D>($"{ModelDir}/Textures/Battery_Material_AlbedoTransparency.png")),
                buttonIdle = Lit("PWR_ButtonIdle", new Color(0.85f, 0.15f, 0.1f), 0.2f, 0.6f, new Color(1.2f, 0.15f, 0.1f)),
                buttonRunning = Lit("PWR_ButtonRunning", new Color(0.2f, 0.9f, 0.3f), 0.2f, 0.6f, new Color(0.3f, 1.6f, 0.4f)),
                teal = Lit("PWR_Electrolyte", new Color(0.1f, 0.6f, 0.6f), 0.2f, 0.7f),
                binMat = Lit("PWR_Bin", new Color(0.22f, 0.3f, 0.38f), 0.5f, 0.4f),
                chuteMat = Lit("PWR_Chute", new Color(0.9f, 0.75f, 0.1f), 0.4f, 0.4f),
                copper = Lit("PWR_CargoCopper", new Color(0.8f, 0.45f, 0.2f), 0.6f, 0.5f),
                glass = Lit("PWR_CargoGlass", new Color(0.6f, 0.85f, 0.95f), 0.1f, 0.9f),
                arrow = Scroll("PWR_BeltArrows", arrowTex, Color.white, new Vector2(0f, -0.8f), Vector2.one),
                rings = Scroll("PWR_BlastRing", ringTex, new Color(2f, 1.2f, 0.4f, 1f), new Vector2(0f, -1.5f), new Vector2(1f, 1f)),
                gridLines = Scroll("PWR_GridOverlay", gridTex, Color.white, Vector2.zero, new Vector2(GridSize.x, GridSize.y)),
                ghost = Scroll("PWR_Ghost", white, new Color(0.2f, 1f, 0.3f, 0.35f), Vector2.zero, Vector2.one),
            };
        }

        // ------------------------------------------------------------------ prefab helpers

        static GameObject Root(string name) => new GameObject(name);

        static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale,
            Material mat, bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Model(string fbxName, Transform parent, Vector3 localPos, Vector3 localEuler, float scale)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{fbxName}.fbx");
            if (asset == null) throw new Exception($"Model {fbxName}.fbx not found in {ModelDir}");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = localEuler;
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        static Bounds LocalBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var b = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (var r in renderers)
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            // root is at the origin with identity rotation while building, so world == local.
            return b;
        }

        static BoxCollider FitBox(GameObject root)
        {
            var b = LocalBounds(root);
            var box = root.AddComponent<BoxCollider>();
            box.center = b.center;
            box.size = b.size;
            return box;
        }

        static T Add<T>(GameObject go) where T : Component => go.AddComponent<T>();

        static void MakeGrabbable(GameObject go, float mass, bool velocityTracking = false)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var grab = go.AddComponent<XRGrabInteractable>();
            grab.useDynamicAttach = true;
            grab.throwOnDetach = true;
            if (velocityTracking) grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            go.AddComponent<PWR_Grabbable>();
        }

        static PWR_StatLabel Label(GameObject go, string title, float height, params string[] entries)
        {
            var l = go.AddComponent<PWR_StatLabel>();
            l.title = title;
            l.entries = entries;
            l.offset = new Vector3(0f, height, 0f);
            return l;
        }

        static void Tutorial(GameObject go, string id, string message, float radius, float height)
        {
            var t = go.AddComponent<PWR_TutorialTrigger>();
            t.tutorialId = id;
            t.message = message;
            t.radius = radius;
            t.offset = new Vector3(0f, height, 0f);
        }

        static PWR_Interactable Interactable(GameObject go, string verb)
        {
            var i = go.AddComponent<PWR_Interactable>();
            i.verb = verb;
            return i;
        }

        static T Save<T>(GameObject go, string name) where T : UnityEngine.Object
        {
            string path = $"{PrefabDir}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            if (typeof(T) == typeof(GameObject)) return prefab as T;
            return prefab.GetComponent(typeof(T)) as T;
        }

        static GameObject Save(GameObject go, string name) => Save<GameObject>(go, name);

        // ------------------------------------------------------------------ prefabs

        class Prefabs
        {
            public GameObject bulb, floodlight, chargedCell, charger, bank, coalGen, oilGen, pad, belt,
                coalChunk, barrel, crate, scrap, wrench, batteryItem, bulbItem, cargoCopper, cargoGlass,
                bin, tank, workbench, chute;
            public Mats mats;
        }

        static Prefabs BuildPrefabs()
        {
            var m = CreateMats();
            var p = new Prefabs { mats = m };

            // --- Bulb: separate instance so its BulbLife reaching zero destroys it (Death).
            {
                // Light bulb model (0.077 x 0.12 m, pivot at the base) centred on the mount; the glass
                // uses the emissive warm material so the lamp reads as lit.
                var go = Root("PWR_Bulb");
                var model = Model("PWR_LightBulb", go.transform, new Vector3(0f, -0.06f * 2.3f, 0f), Vector3.zero, 2.3f);
                var r = model.GetComponentInChildren<Renderer>();
                r.sharedMaterials = r.sharedMaterials.Select(mat => mat != null && mat.name.Contains("Glass") ? m.warm : mat).ToArray();
                var stats = Add<PWR_StatBlock>(go);
                var life = stats.AddStat("BulbLife", 100f, 0f, 100f);
                life.usePassiveFloor = true;
                life.passiveFloor = 5f;
                Add<PWR_DeathOnZero>(go).statName = "BulbLife";
                var label = Label(go, "Bulb", 0.35f, "BulbLife%");
                label.width = 0.35f;
                p.bulb = Save(go, "PWR_Bulb");
            }

            // --- Floodlight (pre-placed). Brightness drains; the battery bank refills it.
            {
                // Tripod work light model: 1.22 m tall, twin lamp heads at ~1.07 m facing +Z.
                var go = Root("PWR_Floodlight");
                Model("PWR_FloodLight", go.transform, Vector3.zero, Vector3.zero, 1f);
                // The replaceable bulb sits between the two lamp heads, in front of the pole.
                var mount = new GameObject("BulbMount").transform;
                mount.SetParent(go.transform, false);
                mount.localPosition = new Vector3(0f, 1.07f, 0.14f);
                mount.localScale = Vector3.one * 0.65f;
                var bulb = (GameObject)PrefabUtility.InstantiatePrefab(p.bulb, mount);
                bulb.transform.localPosition = Vector3.zero;
                var lightGo = new GameObject("Lamp");
                lightGo.transform.SetParent(go.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 1.07f, 0.3f);
                var lamp = lightGo.AddComponent<Light>();
                lamp.type = LightType.Point;
                lamp.range = 16f;
                lamp.color = new Color(1f, 0.92f, 0.78f);
                lamp.intensity = 0f;
                lamp.shadows = LightShadows.None;
                var socketGo = new GameObject("BulbSocket");
                socketGo.transform.SetParent(go.transform, false);
                socketGo.transform.localPosition = new Vector3(0f, 1.07f, 0.12f);
                var sc = socketGo.AddComponent<BoxCollider>();
                sc.size = new Vector3(0.6f, 0.5f, 0.6f);
                sc.isTrigger = true;
                socketGo.AddComponent<PWR_ItemSocket>().accepts = PWR_ResourceType.LightBulb;
                FitBox(go);
                var stats = Add<PWR_StatBlock>(go);
                stats.AddStat("Brightness", 0f, 0f, 10f, -2f);
                var grid = Add<PWR_GridBuilding>(go);
                grid.buildingId = "Floodlight";
                var node = Add<PWR_PowerNode>(go);
                node.role = PWR_PowerRole.Sink;
                node.energyStat = "Brightness";
                node.maxReceivePerTick = 2f;
                var fl = Add<PWR_Floodlight>(go);
                fl.lamp = lamp;
                fl.maxIntensity = 7f;
                fl.bulbMount = mount;
                fl.bulbPrefab = p.bulb;
                fl.bulb = bulb.GetComponent<PWR_StatBlock>();
                Interactable(go, "Install bulb");
                Label(go, "Floodlight", 1.75f, "Brightness%");
                p.floodlight = Save(go, "PWR_Floodlight");
            }

            // --- Physical items.
            {
                // Same battery as PWR_BatteryItem, glowing green once charged.
                var go = Root("PWR_ChargedCell");
                var model = Model("PWR_Battery", go.transform, new Vector3(0f, -0.104f * 0.5f, 0f), Vector3.zero, 0.5f);
                model.GetComponentInChildren<Renderer>().sharedMaterial = m.chargedBattery;
                FitBox(go);
                MakeGrabbable(go, 0.6f);
                var item = Add<PWR_ItemObject>(go);
                item.resource = PWR_ResourceType.Battery;
                item.countsAsResource = false;
                Label(go, "Charged Cell", 0.35f).width = 0.35f;
                p.chargedCell = Save(go, "PWR_ChargedCell");
            }
            {
                // Battery model (0.19 x 0.21 x 0.5 m, pivot at the base) at half size, centred.
                var go = Root("PWR_BatteryItem");
                Model("PWR_Battery", go.transform, new Vector3(0f, -0.104f * 0.5f, 0f), Vector3.zero, 0.5f);
                FitBox(go);
                MakeGrabbable(go, 0.6f);
                Add<PWR_ItemObject>(go).resource = PWR_ResourceType.Battery;
                p.batteryItem = Save(go, "PWR_BatteryItem");
            }
            {
                // Light bulb model at 1.5x so it is easy to grab in VR, centred.
                var go = Root("PWR_LightBulbItem");
                Model("PWR_LightBulb", go.transform, new Vector3(0f, -0.06f * 1.5f, 0f), Vector3.zero, 1.5f);
                FitBox(go);
                MakeGrabbable(go, 0.3f);
                Add<PWR_ItemObject>(go).resource = PWR_ResourceType.LightBulb;
                p.bulbItem = Save(go, "PWR_LightBulbItem");
            }

            // --- Cell charger (pre-placed, wall-mounted HEV model).
            {
                var go = Root("PWR_CellCharger");
                Model("PWR_CellCharger_001", go.transform, Vector3.zero, Vector3.zero, 1f);
                FitBox(go);
                // Same battery model as the item, lying across the charger's face.
                var slot = Model("PWR_Battery", go.transform, new Vector3(0f, 0.45f - 0.104f * 0.5f, 0.16f), new Vector3(0f, 90f, 0f), 0.5f);
                slot.name = "SlottedBattery";
                var eject = new GameObject("EjectPoint").transform;
                eject.SetParent(go.transform, false);
                eject.localPosition = new Vector3(0f, 0.3f, 0.45f);
                var socketGo = new GameObject("BatterySocket");
                socketGo.transform.SetParent(go.transform, false);
                socketGo.transform.localPosition = new Vector3(0f, 0.5f, 0.25f);
                var sc = socketGo.AddComponent<BoxCollider>();
                sc.size = new Vector3(0.7f, 0.9f, 0.5f);
                sc.isTrigger = true;
                socketGo.AddComponent<PWR_ItemSocket>().accepts = PWR_ResourceType.Battery;
                var stats = Add<PWR_StatBlock>(go);
                stats.AddStat("Progress", 0f, 0f, 1000f);
                var grid = Add<PWR_GridBuilding>(go);
                grid.buildingId = "CellCharger";
                grid.snapOnStart = false;
                var node = Add<PWR_PowerNode>(go);
                node.role = PWR_PowerRole.Sink;
                node.energyStat = "Progress";
                node.maxReceivePerTick = PowerPerTick;
                node.acceptingPower = false;
                var ch = Add<PWR_CellCharger>(go);
                ch.slottedBatteryVisual = slot;
                ch.chargedCellPrefab = p.chargedCell;
                ch.ejectPoint = eject;
                Interactable(go, "Insert battery");
                Label(go, "Cell Charger", 1.35f, "Progress%");
                Tutorial(go, "charger", "Insert a Battery here. Charge it to 100% to complete the station.", 2.5f, 1.9f);
                p.charger = Save(go, "PWR_CellCharger");
            }

            // --- Battery bank (player-built, free, one only). Starts with 20% charge.
            {
                var go = Root("PWR_BatteryBank");
                Model("PWR_BatteryBank", go.transform, Vector3.zero, Vector3.zero, 1f);
                FitBox(go);
                var stats = Add<PWR_StatBlock>(go);
                stats.AddStat("Charge", 100f, 0f, 500f);
                Add<PWR_GridBuilding>(go).buildingId = "BatteryBank";
                var node = Add<PWR_PowerNode>(go);
                node.role = PWR_PowerRole.Storage;
                node.energyStat = "Charge";
                node.transferPerTick = PowerPerTick;
                Label(go, "Battery Bank", 1.4f, "Charge%");
                p.bank = Save(go, "PWR_BatteryBank");
            }

            // --- Generators (player-built, free, one of each).
            p.coalGen = BuildGenerator(m, "PWR_CoalGenerator", "PWR_Generator", "Coal Generator", "CoalGenerator",
                "CoalLevel", PWR_ResourceType.Coal, 25f, "coal_gen",
                "Coal Generator: pull the trigger on it to load Coal. It only burns Coal and only charges a Battery Bank within 2 cells.");
            p.oilGen = BuildGenerator(m, "PWR_OilGenerator", "PWR_Generator_1", "Oil Generator", "OilGenerator",
                "OilLevel", PWR_ResourceType.RefinedOil, 40f, "oil_gen",
                "Oil Generator: pull the trigger on it to load Refined Oil. It only burns Refined Oil and only charges a Battery Bank within 2 cells.");

            // --- Concrete pad (foundation, free).
            {
                var go = Root("PWR_ConcretePad");
                Prim(PrimitiveType.Cube, go.transform, "Slab", new Vector3(0f, 0.03f, 0f), new Vector3(0.98f, 0.06f, 0.98f), m.concrete);
                var g = Add<PWR_GridBuilding>(go);
                g.buildingId = "ConcretePad";
                g.isFoundation = true;
                g.stackHeight = 0.06f;
                p.pad = Save(go, "PWR_ConcretePad");
            }

            // --- Conveyor belt (needs a pad, 1 Scrap). Arrow strip uses the scrolling UV material.
            {
                var go = Root("PWR_ConveyorBelt");
                Prim(PrimitiveType.Cube, go.transform, "Base", new Vector3(0f, 0.04f, 0f), new Vector3(0.8f, 0.08f, 1f), m.belt);
                Prim(PrimitiveType.Cube, go.transform, "RailL", new Vector3(-0.44f, 0.07f, 0f), new Vector3(0.06f, 0.1f, 1f), m.rail, false);
                Prim(PrimitiveType.Cube, go.transform, "RailR", new Vector3(0.44f, 0.07f, 0f), new Vector3(0.06f, 0.1f, 1f), m.rail, false);
                var strip = Prim(PrimitiveType.Quad, go.transform, "ArrowStrip", new Vector3(0f, 0.082f, 0f), new Vector3(0.7f, 1f, 1f), m.arrow, false);
                strip.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
                strip.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Add<PWR_GridBuilding>(go).buildingId = "ConveyorBelt";
                var belt = Add<PWR_ConveyorBelt>(go);
                belt.surfaceHeight = 0.085f;
                belt.speed = 0.8f;
                p.belt = Save(go, "PWR_ConveyorBelt");
            }

            // --- Coal chunk (from shattered crates; picking it up harvests it).
            {
                var go = Root("PWR_CoalChunk");
                Prim(PrimitiveType.Cube, go.transform, "Lump", Vector3.zero, new Vector3(0.16f, 0.12f, 0.14f), m.coal);
                MakeGrabbable(go, 0.5f);
                Interactable(go, "Pick up");
                var h = Add<PWR_Harvestable>(go);
                h.mode = PWR_Harvestable.Mode.OnPickup;
                h.resource = PWR_ResourceType.Coal;
                p.coalChunk = Save(go, "PWR_CoalChunk");
            }

            // --- Oil barrel (wild). Grab + trigger draws crude oil; empty barrel disappears.
            {
                var go = Root("PWR_OilBarrel");
                var drum = Model("PWR_FuelBarrel_01", go.transform, Vector3.zero, Vector3.zero, 1f);
                // Convex hull of the drum: flat bottom so it stands upright (a capsule would tip over).
                var hull = drum.AddComponent<MeshCollider>();
                hull.convex = true;
                MakeGrabbable(go, 6f);
                Interactable(go, "Draw oil");
                var stats = Add<PWR_StatBlock>(go);
                stats.AddStat("RemainingOil", 18f, 0f, 18f);
                var h = Add<PWR_Harvestable>(go);
                h.mode = PWR_Harvestable.Mode.OnUse;
                h.resource = PWR_ResourceType.CrudeOil;
                h.statName = "RemainingOil";
                Add<PWR_DeathOnZero>(go).statName = "RemainingOil";
                Label(go, "Oil Barrel", 1.4f, "RemainingOil");
                Tutorial(go, "oil_barrel", "Oil Barrel: grab it and pull the trigger to draw Crude Oil.", 2.2f, 1.85f);
                p.barrel = Save(go, "PWR_OilBarrel");
            }

            // --- Wooden crate (wild). Wrench strikes lower Integrity, then it shatters into coal.
            {
                var go = Root("PWR_WoodCrate");
                Model("PWR_FuelCrate_01", go.transform, Vector3.zero, Vector3.zero, 0.8f);
                FitBox(go);
                var stats = Add<PWR_StatBlock>(go);
                stats.AddStat("Integrity", 2f, 0f, 2f);
                var s = Add<PWR_Shatterable>(go);
                s.piecePrefab = p.coalChunk;
                s.pieceCount = 4;
                Interactable(go, "Smash");
                Label(go, "Wooden Crate", 1.2f, "Integrity");
                Tutorial(go, "crate", "Wooden Crate: hit it with the Wrench (on the workbench) to break it into Coal.", 2.2f, 1.65f);
                p.crate = Save(go, "PWR_WoodCrate");
            }

            // --- Scrap (wild, 20 scattered). Picking it up harvests it.
            {
                var go = Root("PWR_Scrap");
                Prim(PrimitiveType.Cube, go.transform, "Plate", Vector3.zero, new Vector3(0.24f, 0.05f, 0.16f), m.metal);
                Prim(PrimitiveType.Cube, go.transform, "Bent", new Vector3(0.05f, 0.04f, 0f), new Vector3(0.08f, 0.06f, 0.14f), m.metal, false);
                MakeGrabbable(go, 0.5f);
                Interactable(go, "Pick up");
                var h = Add<PWR_Harvestable>(go);
                h.mode = PWR_Harvestable.Mode.OnPickup;
                h.resource = PWR_ResourceType.Scrap;
                Tutorial(go, "scrap", "Scrap: pick it up. You spend Scrap to build conveyor belts.", 1.8f, 0.8f);
                p.scrap = Save(go, "PWR_Scrap");
            }

            // --- Wrench.
            {
                // Pipe wrench model: 0.33 m along Z, jaws at +Z, lying flat with its pivot at the base.
                var go = Root("PWR_Wrench");
                var model = Model("PWR_Wrench", go.transform, new Vector3(0f, -0.02f, 0f), Vector3.zero, 1f);
                var hull = model.AddComponent<MeshCollider>();
                hull.convex = true;
                var head = new GameObject("Head");
                head.transform.SetParent(go.transform, false);
                head.transform.localPosition = new Vector3(0f, 0f, 0.12f);
                MakeGrabbable(go, 1f, true);
                Interactable(go, "Strike");
                Add<PWR_Wrench>(go).head = head.transform;
                p.wrench = Save(go, "PWR_Wrench");
            }

            // --- Cargo packages.
            p.cargoCopper = BuildCargo("PWR_Cargo_CopperWire", PWR_ResourceType.CopperWire, m.copper);
            p.cargoGlass = BuildCargo("PWR_Cargo_Glass", PWR_ResourceType.Glass, m.glass);

            // --- Receiving bin.
            {
                var go = Root("PWR_ReceivingBin");
                Prim(PrimitiveType.Cube, go.transform, "Floor", new Vector3(0f, 0.04f, 0f), new Vector3(0.9f, 0.08f, 0.9f), m.binMat);
                Prim(PrimitiveType.Cube, go.transform, "WallN", new Vector3(0f, 0.3f, 0.43f), new Vector3(0.9f, 0.5f, 0.04f), m.binMat);
                Prim(PrimitiveType.Cube, go.transform, "WallS", new Vector3(0f, 0.3f, -0.43f), new Vector3(0.9f, 0.5f, 0.04f), m.binMat);
                Prim(PrimitiveType.Cube, go.transform, "WallE", new Vector3(0.43f, 0.3f, 0f), new Vector3(0.04f, 0.5f, 0.9f), m.binMat);
                Prim(PrimitiveType.Cube, go.transform, "WallW", new Vector3(-0.43f, 0.3f, 0f), new Vector3(0.04f, 0.5f, 0.9f), m.binMat);
                var trig = go.AddComponent<BoxCollider>();
                trig.isTrigger = true;
                trig.center = new Vector3(0f, 0.55f, 0f);
                trig.size = new Vector3(1.5f, 1.1f, 1.5f);
                var stats = Add<PWR_StatBlock>(go);
                stats.AddStat("Stock", 0f, 0f, 99f);
                Add<PWR_ReceivingBin>(go);
                Interactable(go, "Collect");
                Label(go, "Receiving Bin", 1.1f, "Stock");
                Tutorial(go, "receiving_bin", "Receiving Bin: packages from the conveyor land here. Pull the trigger on it to collect them.", 2.2f, 1.6f);
                p.bin = Save(go, "PWR_ReceivingBin");
            }

            // --- Electrolyte tank (50 units, never empties in one level).
            {
                var go = Root("PWR_ElectrolyteTank");
                Prim(PrimitiveType.Cylinder, go.transform, "Tank", new Vector3(0f, 0.3f, 0f), new Vector3(0.32f, 0.3f, 0.32f), m.teal);
                Prim(PrimitiveType.Cylinder, go.transform, "Valve", new Vector3(0f, 0.63f, 0f), new Vector3(0.08f, 0.04f, 0.08f), m.steel, false);
                var stats = Add<PWR_StatBlock>(go);
                stats.AddStat("RemainingElectrolyte", 50f, 0f, 50f);
                var h = Add<PWR_Harvestable>(go);
                h.mode = PWR_Harvestable.Mode.OnUse;
                h.resource = PWR_ResourceType.Electrolyte;
                h.statName = "RemainingElectrolyte";
                Interactable(go, "Collect");
                Label(go, "Electrolyte Tank", 0.95f, "RemainingElectrolyte");
                Tutorial(go, "electrolyte", "Electrolyte Tank: pull the trigger on it to collect Electrolyte for batteries.", 2.2f, 1.3f);
                p.tank = Save(go, "PWR_ElectrolyteTank");
            }

            // --- Workbench (pre-placed).
            {
                var go = Root("PWR_Workbench");
                var model = Model("PWR_Refinery", go.transform, Vector3.zero, Vector3.zero, 0.8f);
                foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                var b = LocalBounds(go);
                var output = new GameObject("OutputPoint").transform;
                output.SetParent(go.transform, false);
                output.localPosition = new Vector3(b.center.x, b.min.y + b.size.y * 0.75f, b.center.z);
                var anchor = new GameObject("PanelAnchor").transform;
                anchor.SetParent(go.transform, false);
                anchor.localPosition = new Vector3(b.center.x, 1.55f, b.max.z + 0.05f);
                anchor.localEulerAngles = new Vector3(0f, 180f, 0f);
                var stats = Add<PWR_StatBlock>(go);
                stats.AddStat("CraftProgress", 0f, 0f, 100f);
                var wb = Add<PWR_Workbench>(go);
                wb.outputPoint = output;
                wb.panelAnchor = anchor;
                wb.recipes = new List<PWR_Recipe>
                {
                    new PWR_Recipe { name = "Refine Oil", inputs = { new PWR_ResourceAmount(PWR_ResourceType.CrudeOil, 3) }, output = PWR_ResourceType.RefinedOil },
                    new PWR_Recipe { name = "Make Fuse", inputs = { new PWR_ResourceAmount(PWR_ResourceType.CopperWire, 2) }, output = PWR_ResourceType.Fuse },
                    new PWR_Recipe { name = "Assemble Battery", inputs = { new PWR_ResourceAmount(PWR_ResourceType.Fuse, 1), new PWR_ResourceAmount(PWR_ResourceType.Electrolyte, 1) }, output = PWR_ResourceType.Battery, spawnPrefab = p.batteryItem },
                    new PWR_Recipe { name = "Make Light Bulb", inputs = { new PWR_ResourceAmount(PWR_ResourceType.Glass, 1), new PWR_ResourceAmount(PWR_ResourceType.CopperWire, 2) }, output = PWR_ResourceType.LightBulb, spawnPrefab = p.bulbItem },
                };
                Tutorial(go, "workbench", "Workbench: refine oil and craft Fuses, Batteries and Light Bulbs. Point at a recipe and pull the trigger.", 3f, 2.4f);
                p.workbench = Save(go, "PWR_Workbench");
            }

            // --- Delivery chute.
            {
                var go = Root("PWR_DeliveryChute");
                Prim(PrimitiveType.Cube, go.transform, "Hopper", new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 0.5f, 0.5f), m.chuteMat);
                Prim(PrimitiveType.Cube, go.transform, "Mouth", new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 0.1f, 0.4f), m.darkMetal, false);
                // Big mushroom button on top of the hopper: red until the player starts the conveyor.
                var button = Prim(PrimitiveType.Cylinder, go.transform, "StartButton", new Vector3(0f, 1.18f, 0f),
                    new Vector3(0.18f, 0.03f, 0.18f), m.buttonIdle, false);
                var output = new GameObject("OutputPoint").transform;
                output.SetParent(go.transform, false);
                output.localPosition = new Vector3(0f, 0.2f, 0f);
                var chute = Add<PWR_DeliveryChute>(go);
                chute.outputPoint = output;
                chute.copperWirePrefab = p.cargoCopper.GetComponent<PWR_Cargo>();
                chute.glassPrefab = p.cargoGlass.GetComponent<PWR_Cargo>();
                chute.startButton = button.GetComponent<Renderer>();
                chute.buttonIdle = m.buttonIdle;
                chute.buttonRunning = m.buttonRunning;
                Interactable(go, "Start conveyor");
                var label = Label(go, "Delivery Chute", 1.6f);
                chute.label = label;
                Tutorial(go, "chute", "Delivery Chute: supplies arrive here at random times. Build a conveyor from here to the Receiving Bin, " +
                    "then point at the chute and pull the trigger to press its red button and start the deliveries.", 3f, 2.1f);
                p.chute = Save(go, "PWR_DeliveryChute");
            }

            return p;
        }

        static GameObject BuildGenerator(Mats m, string prefabName, string model, string display, string id,
            string fuelStat, PWR_ResourceType fuel, float fuelPerUnit, string tutorialId, string tutorial)
        {
            var go = Root(prefabName);
            Model(model, go.transform, Vector3.zero, Vector3.zero, 1f);
            var box = FitBox(go);
            var lightGo = new GameObject("RunningLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, box.size.y + 0.2f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.15f);
            light.range = 2.5f;
            light.intensity = 1.5f;
            light.enabled = false;
            var stats = Add<PWR_StatBlock>(go);
            stats.AddStat(fuelStat, 0f, 0f, 100f);
            stats.AddStat("Energy", 0f, 0f, 50f);
            Add<PWR_GridBuilding>(go).buildingId = id;
            var node = Add<PWR_PowerNode>(go);
            node.role = PWR_PowerRole.Source;
            node.energyStat = "Energy";
            node.transferPerTick = PowerPerTick;
            var gen = Add<PWR_Generator>(go);
            gen.displayName = display;
            gen.fuelStat = fuelStat;
            gen.fuelResource = fuel;
            gen.fuelPerUnit = fuelPerUnit;
            gen.energyPerSecond = PowerPerTick / node.tickInterval;
            gen.burnPerSecond = gen.energyPerSecond / EnergyPerFuelStat;
            gen.blastMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/PWR_BlastRing.mat");
            gen.runningLight = light;
            Interactable(go, "Refuel");
            Label(go, display, box.size.y + 0.5f, fuelStat, "Energy");
            Tutorial(go, tutorialId, tutorial, 2.5f, box.size.y + 1.0f);
            return Save(go, prefabName);
        }

        static GameObject BuildCargo(string name, PWR_ResourceType type, Material mat)
        {
            var go = Root(name);
            Prim(PrimitiveType.Cube, go.transform, "Box", Vector3.zero, Vector3.one * 0.22f, mat);
            MakeGrabbable(go, 0.5f);
            go.GetComponent<Rigidbody>().isKinematic = true;
            var stats = Add<PWR_StatBlock>(go);
            stats.AddStat("PathProgress", 0f, 0f, 1000f);
            var cargo = Add<PWR_Cargo>(go);
            cargo.resource = type;
            cargo.halfHeight = 0.11f;
            return Save(go, name);
        }

        // ------------------------------------------------------------------ build defs

        static PWR_BuildableDef Def(string file, string display, GameObject prefab, int maxCount, string foundation,
            params PWR_ResourceAmount[] cost)
        {
            string path = $"{DefDir}/{file}.asset";
            var def = AssetDatabase.LoadAssetAtPath<PWR_BuildableDef>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<PWR_BuildableDef>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.displayName = display;
            def.prefab = prefab.GetComponent<PWR_GridBuilding>();
            def.maxCount = maxCount;
            def.requiredFoundationId = foundation;
            def.cost = cost.ToList();
            var tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var b = LocalBounds(tmp);
            UnityEngine.Object.DestroyImmediate(tmp);
            def.ghostSize = new Vector3(Mathf.Max(0.2f, b.size.x), Mathf.Max(0.05f, b.size.y), Mathf.Max(0.2f, b.size.z));
            def.ghostCenter = b.center;
            EditorUtility.SetDirty(def);
            return def;
        }

        static List<PWR_BuildableDef> builtDefs;

        static void BuildDefs(Prefabs p)
        {
            builtDefs = new List<PWR_BuildableDef>
            {
                Def("PWR_Build_BatteryBank", "Battery Bank", p.bank, 1, ""),
                Def("PWR_Build_CoalGenerator", "Coal Generator", p.coalGen, 1, ""),
                Def("PWR_Build_OilGenerator", "Oil Generator", p.oilGen, 1, ""),
                Def("PWR_Build_ConcretePad", "Concrete Pad", p.pad, 0, ""),
                Def("PWR_Build_ConveyorBelt", "Conveyor Belt", p.belt, 0, "ConcretePad", new PWR_ResourceAmount(PWR_ResourceType.Scrap, 1)),
            };
        }

        static List<PWR_BuildableDef> LoadDefs() => new[]
            {
                "PWR_Build_BatteryBank", "PWR_Build_CoalGenerator", "PWR_Build_OilGenerator",
                "PWR_Build_ConcretePad", "PWR_Build_ConveyorBelt",
            }.Select(n => AssetDatabase.LoadAssetAtPath<PWR_BuildableDef>($"{DefDir}/{n}.asset"))
            .Where(d => d != null).ToList();

        // ------------------------------------------------------------------ scene

        [MenuItem("Tools/PWR/Setup Scene Only")]
        public static void SetupScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == StationRootName).ToList())
                UnityEngine.Object.DestroyImmediate(old);
            var all = AllObjects(scene);
            GameObject Find(string name) => all.FirstOrDefault(g => g.name == name);

            GameObject Load(string n) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{n}.prefab");
            var mats = new
            {
                grid = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/PWR_GridOverlay.mat"),
                ghost = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/PWR_Ghost.mat"),
            };

            var root = new GameObject(StationRootName);
            SceneManager_Move(root, scene);
            Transform Group(string n)
            {
                var t = new GameObject(n).transform;
                t.SetParent(root.transform, false);
                return t;
            }
            var systems = Group("Systems");
            var machines = Group("Machines");
            var props = Group("Props");
            var markers = Group("Markers");
            var lighting = Group("Lighting");

            // --- grid
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(systems, false);
            gridGo.transform.position = GridOrigin;
            var grid = gridGo.AddComponent<PWR_GridManager>();
            grid.cellSize = 1f;
            grid.gridSize = GridSize;
            grid.neighborRange = 2;
            var overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            overlay.name = "GridOverlay";
            UnityEngine.Object.DestroyImmediate(overlay.GetComponent<Collider>());
            overlay.transform.SetParent(gridGo.transform, false);
            overlay.transform.localPosition = new Vector3(GridSize.x * 0.5f, 0.012f, GridSize.y * 0.5f);
            overlay.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            overlay.transform.localScale = new Vector3(GridSize.x, GridSize.y, 1f);
            overlay.GetComponent<Renderer>().sharedMaterial = mats.grid;
            grid.overlay = overlay;
            Vector3 CellCenter(Vector2Int c) => GridOrigin + new Vector3(c.x + 0.5f, 0f, c.y + 0.5f);

            // --- core systems
            var sys = new GameObject("StationSystems");
            sys.transform.SetParent(systems, false);
            sys.AddComponent<PWR_PlayerResources>();
            var build = sys.AddComponent<PWR_BuildSystem>();
            build.buildables = LoadDefs();
            build.ghostMaterial = mats.ghost;
            sys.AddComponent<PWR_TutorialManager>();
            sys.AddComponent<PWR_PlayerRig>();
            sys.AddComponent<PWR_BuildInput>();
            sys.AddComponent<PWR_InteractionInput>();
            sys.AddComponent<PWR_DesktopRig>();
            sys.AddComponent<PWR_WristPanel>();

            // --- player rig + event system
            var xrOrigin = all.FirstOrDefault(g => g.GetComponent<Unity.XR.CoreUtils.XROrigin>() != null);
            if (xrOrigin == null)
            {
                var rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(XROriginPath);
                xrOrigin = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset, scene);
            }
            xrOrigin.transform.position = new Vector3(0f, 0.05f, 1.5f);
            // Quest Link reports a floor-level (Unbounded) space; with "Not Specified" the rig would keep its
            // 1.36 m camera offset on top of the player's real height. Floor mode drops the offset.
            var originComp = xrOrigin.GetComponent<Unity.XR.CoreUtils.XROrigin>();
            originComp.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
            xrOrigin.transform.rotation = Quaternion.Euler(0f, -146f, 0f);
            var oldCamera = all.FirstOrDefault(g => g.name == "Main Camera" && g.transform.parent == null);
            if (oldCamera != null) oldCamera.SetActive(false);
            if (!all.Any(g => g.GetComponent<EventSystem>() != null))
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
                SceneManager_Move(es, scene);
            }

            // --- machines replacing the designer's markers
            var chargerMarker = Find("PWR_CellCharger_1");
            var charger = Place(Load("PWR_CellCharger"), machines, chargerMarker);
            var floodlight = Place(Load("PWR_Floodlight"), machines, null);
            floodlight.transform.position = CellCenter(FloodlightCell);
            // Aim the lamp heads at the middle of the room.
            Vector3 toCenter = CellCenter(new Vector2Int(7, 7)) - floodlight.transform.position;
            toCenter.y = 0f;
            floodlight.transform.rotation = Quaternion.LookRotation(toCenter);
            Deactivate(Find("照明灯"));
            var workbench = Place(Load("PWR_Workbench"), props, Find("PWR_Refinery"), keepScale: false);
            var bin = Place(Load("PWR_ReceivingBin"), props, Find("收货箱"), keepScale: false);
            var tank = Place(Load("PWR_ElectrolyteTank"), props, Find("电解液罐"), keepScale: false);
            var chute = Place(Load("PWR_DeliveryChute"), props, Find("出货口"), keepScale: false);
            // The chute's output must sit over the grid cell where the first belt goes.
            var chuteOut = chute.GetComponent<PWR_DeliveryChute>().outputPoint;
            Vector2Int chuteCell = new Vector2Int(
                Mathf.FloorToInt(chute.transform.position.x - GridOrigin.x),
                Mathf.FloorToInt(chute.transform.position.z - GridOrigin.z));
            chuteOut.position = CellCenter(chuteCell) + Vector3.up * 0.2f;
            chute.transform.position = new Vector3(GridOrigin.x + 0.05f, GridOrigin.y, CellCenter(chuteCell).z);
            chuteOut.position = CellCenter(chuteCell) + Vector3.up * 0.2f;

            // Crafted items, the wrench and the electrolyte tank go on the actual tabletop, found by
            // casting down onto the bench model (its front half is floor clutter, not table).
            var wb = workbench.GetComponent<PWR_Workbench>();
            Physics.SyncTransforms();
            if (FindTabletop(workbench, out Vector3 table, out Vector3 tableAxis))
            {
                wb.outputPoint.position = table + Vector3.up * 0.2f;
                var tankT = tank.transform;
                tankT.position = SurfaceAt(table - tableAxis * 0.6f, workbench) ?? tankT.position;
            }
            var wrench = (GameObject)PrefabUtility.InstantiatePrefab(Load("PWR_Wrench"), props);
            wrench.transform.position = (SurfaceAt(wb.outputPoint.position + tableAxis * 0.6f, workbench)
                                         ?? wb.outputPoint.position) + Vector3.up * 0.08f;

            // Layout references the player now builds themselves.
            Deactivate(Find("PWR_BatteryBank"));
            Deactivate(Find("PWR_Generator"));
            Deactivate(Find("PWR_Generator_1"));

            // --- battery bank tutorial between the charger and floodlight. No floor marker: the build
            // preview shows the 5x5 area the bank reaches, so any cell that covers both works.
            var hint = new GameObject("BatteryBankHint");
            hint.transform.SetParent(markers, false);
            hint.transform.position = CellCenter(BankHintCell);
            var ht = hint.AddComponent<PWR_TutorialTrigger>();
            ht.tutorialId = "battery_bank";
            ht.message = "Build the Battery Bank near the Charger and Floodlight. Its power reaches 2 cells in every direction " +
                         "(the orange area while placing). It still holds a little charge.";
            ht.radius = 6f;
            ht.offset = new Vector3(0f, 1.6f, 0f);

            // --- wild spawning
            var spawner = new GameObject("WildSpawner").AddComponent<PWR_WildSpawner>();
            spawner.transform.SetParent(systems, false);
            spawner.spawnPoints = all.Where(g => g.name.StartsWith("油桶刷新点") || g.name.StartsWith("木箱刷新点"))
                .Select(g => g.transform).ToList();
            foreach (var sp in spawner.spawnPoints) sp.gameObject.SetActive(true);
            spawner.barrelPrefab = Load("PWR_OilBarrel");
            spawner.cratePrefab = Load("PWR_WoodCrate");
            spawner.scrapPrefab = Load("PWR_Scrap");
            spawner.barrelCount = 2;
            spawner.crateCount = 3;
            spawner.scrapCount = 30; // chute-to-bin route needs ~21 belts (1 Scrap each)
            spawner.scrapZones = new List<BoxCollider>
            {
                Zone(markers, "ScrapZone_A", new Vector3(0.5f, 0.05f, 4.2f), new Vector3(4f, 0.1f, 2f)),
                Zone(markers, "ScrapZone_B", new Vector3(-5.5f, 0.05f, 3.5f), new Vector3(2.5f, 0.1f, 3f)),
                Zone(markers, "ScrapZone_C", new Vector3(2.5f, 0.05f, -4.5f), new Vector3(3f, 0.1f, 2f)),
            };

            // --- lighting
            var emergency = new List<Light>
            {
                RedLight(lighting, "EmergencyLight_A", new Vector3(-6.8f, 2.7f, -6.8f)),
                RedLight(lighting, "EmergencyLight_B", new Vector3(6.6f, 2.7f, 6.6f)),
                RedLight(lighting, "EmergencyLight_C", new Vector3(-6.8f, 2.7f, 6.6f)),
            };
            var director = sys.AddComponent<PWR_LevelDirector>();
            director.floodlight = floodlight.GetComponent<PWR_Floodlight>();
            director.charger = charger.GetComponent<PWR_CellCharger>();
            director.emergencyLights = emergency;
            director.bankHint = hint;
            var sun = Find("Directional Light");
            if (sun != null && sun.TryGetComponent(out Light sunLight)) director.sceneLightsToDim = new List<Light> { sunLight };

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PWR] Scene setup complete.");
        }

        static void SceneManager_Move(GameObject go, UnityEngine.SceneManagement.Scene scene) =>
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);

        static List<GameObject> AllObjects(UnityEngine.SceneManagement.Scene scene)
        {
            var list = new List<GameObject>();
            foreach (var r in scene.GetRootGameObjects())
                foreach (var t in r.GetComponentsInChildren<Transform>(true)) list.Add(t.gameObject);
            return list;
        }

        static GameObject Place(GameObject prefab, Transform parent, GameObject marker, bool keepScale = true)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (marker != null)
            {
                go.transform.SetPositionAndRotation(marker.transform.position, marker.transform.rotation);
                Deactivate(marker);
            }
            return go;
        }

        /// <summary>Center of the bench's flat top (hits between 0.7 and 1.1 m) and its long axis.</summary>
        static bool FindTabletop(GameObject bench, out Vector3 center, out Vector3 axis)
        {
            center = bench.transform.position;
            axis = bench.transform.right;
            var bounds = new Bounds(bench.transform.position, Vector3.zero);
            foreach (var c in bench.GetComponentsInChildren<Collider>()) bounds.Encapsulate(c.bounds);
            var hits = new List<Vector3>();
            for (float x = bounds.min.x; x <= bounds.max.x; x += 0.1f)
            for (float z = bounds.min.z; z <= bounds.max.z; z += 0.1f)
            {
                var p = SurfaceAt(new Vector3(x, 0f, z), bench);
                if (p.HasValue && p.Value.y > 0.7f && p.Value.y < 1.1f) hits.Add(p.Value);
            }
            if (hits.Count < 5) return false;
            var sum = Vector3.zero;
            foreach (var h in hits) sum += h;
            center = sum / hits.Count;
            var extentX = hits.Max(h => h.x) - hits.Min(h => h.x);
            var extentZ = hits.Max(h => h.z) - hits.Min(h => h.z);
            axis = extentX > extentZ ? Vector3.right : Vector3.forward;
            // Snap to an actual hit near the centroid so it is not over a gap.
            var c0 = center;
            center = hits.OrderBy(h => (h - c0).sqrMagnitude).First();
            return true;
        }

        static Vector3? SurfaceAt(Vector3 xz, GameObject only)
        {
            var origin = new Vector3(xz.x, 4f, xz.z);
            foreach (var hit in Physics.RaycastAll(origin, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore)
                         .OrderBy(h => h.distance))
                if (hit.collider.transform.IsChildOf(only.transform)) return hit.point;
            return null;
        }

        static void Deactivate(GameObject go)
        {
            if (go != null) go.SetActive(false);
        }

        static BoxCollider Zone(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.enabled = false;
            return box;
        }

        static Light RedLight(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.12f, 0.08f);
            l.range = 9f;
            l.intensity = 2.5f;
            l.shadows = LightShadows.None;
            return l;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
