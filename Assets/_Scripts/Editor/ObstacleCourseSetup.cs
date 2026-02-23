using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace EVP.Editor
{
    public static class ObstacleCourseSetup
    {
        [MenuItem("Tools/Setup Obstacle Course Scene")]
        public static void Setup()
        {
            // Create and save the new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Directional Light (matching ClownTest) ---
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.957f, 0.839f, 1f);
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.position = new Vector3(0, 50, 0);
            lightGo.transform.eulerAngles = new Vector3(50, -30, 0);

            // --- Organizer empty GameObjects ---
            var courseLayout = new GameObject("CourseLayout");
            var bridges = new GameObject("Bridges");
            var props = new GameObject("Props");

            // --- Create materials ---
            CreateRoadMaterial();
            CreateBridgeMaterial();

            // --- Build bridges ---
            BuildBridge(bridges.transform, "Bridge_Valley",
                new Vector3(172, 5, 210), 30f, 8f, 3);
            BuildBridge(bridges.transform, "Bridge_Long",
                new Vector3(332, 20, 175), 50f, 7f, 5);

            // --- Instantiate vehicle ---
            GameObject vehicle = null;
            var vehiclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Prefabs/Vehicles/L200-Red.prefab");
            if (vehiclePrefab != null)
            {
                vehicle = (GameObject)PrefabUtility.InstantiatePrefab(vehiclePrefab);
                vehicle.transform.position = new Vector3(250, 10, 50);
                vehicle.transform.eulerAngles = new Vector3(0, 0, 0);
            }
            else
            {
                Debug.LogWarning("[ObstacleCourse] L200-Red prefab not found");
            }

            // --- Instantiate camera controller ---
            var cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Prefabs/Vehicle Camera Controller.prefab");
            if (cameraPrefab != null)
            {
                var cameraGo = (GameObject)PrefabUtility.InstantiatePrefab(cameraPrefab);
                // Try to wire camera target to vehicle
                if (vehicle != null)
                {
                    var cameraController = cameraGo.GetComponent<EVP.VehicleCameraController>();
                    if (cameraController != null)
                        cameraController.target = vehicle.transform;
                }
            }
            else
            {
                Debug.LogWarning("[ObstacleCourse] Camera controller prefab not found");
            }

            // --- Create SpawnPoint ---
            var spawnGo = new GameObject("SpawnPoint");
            spawnGo.transform.position = new Vector3(250, 10, 50);
            spawnGo.transform.eulerAngles = new Vector3(0, 0, 0);
            var spawn = spawnGo.AddComponent<VehicleSpawnPoint>();
            if (vehicle != null)
                spawn.vehicle = vehicle.GetComponent<VehicleController>();

            // --- Place ramps ---
            var rampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Plugins/EVP5/World/Ramp/Ramp.prefab");
            if (rampPrefab != null)
            {
                var ramp1 = (GameObject)PrefabUtility.InstantiatePrefab(rampPrefab);
                ramp1.name = "Ramp_Segment1";
                ramp1.transform.position = new Vector3(220, 8, 90);
                ramp1.transform.eulerAngles = new Vector3(0, 150, 0);
                ramp1.transform.SetParent(props.transform, true);

                var ramp2 = (GameObject)PrefabUtility.InstantiatePrefab(rampPrefab);
                ramp2.name = "Ramp_Segment7";
                ramp2.transform.position = new Vector3(310, 12, 90);
                ramp2.transform.eulerAngles = new Vector3(0, 200, 0);
                ramp2.transform.SetParent(props.transform, true);
            }

            // --- Telemetry (inactive) ---
            var telemetryGo = new GameObject("Telemetry");
            var telemetryComponent = telemetryGo.AddComponent<EVP.VehicleTelemetry>();
            if (vehicle != null)
                telemetryComponent.target = vehicle.GetComponent<VehicleController>();
            telemetryGo.SetActive(false);

            // --- Save scene ---
            string scenePath = "Assets/_Scenes/ObstacleCourse.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log("[ObstacleCourse] Scene created and saved at " + scenePath);
            Debug.Log("[ObstacleCourse] Next steps:");
            Debug.Log("  1. Run Tools > Generate Obstacle Course Terrain");
            Debug.Log("  2. Run Tools > Build Obstacle Course Roads");
            Debug.Log("  3. Enter Play mode to test driving");
        }

        static void CreateRoadMaterial()
        {
            string matPath = "Assets/_Materials/Road_Asphalt.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null) return;

            // Find URP Lit shader
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = "Road_Asphalt";

            // Set base map texture
            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Plugins/EVP5/World/Textures/asphalt.png");
            if (baseTex != null)
            {
                mat.SetTexture("_BaseMap", baseTex);
                mat.SetTexture("_MainTex", baseTex); // fallback for Standard
            }

            // Set normal map
            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Plugins/EVP5/World/Textures/asphalt_normal.png");
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }

            // Tiling for road surface
            mat.SetTextureScale("_BaseMap", new Vector2(4, 4));
            mat.SetTextureScale("_MainTex", new Vector2(4, 4));

            EnsureFolder("Assets/_Materials");
            AssetDatabase.CreateAsset(mat, matPath);
            Debug.Log("[ObstacleCourse] Created Road_Asphalt material");
        }

        static void CreateBridgeMaterial()
        {
            string matPath = "Assets/_Materials/Bridge_Concrete.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null) return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = "Bridge_Concrete";
            mat.SetColor("_BaseColor", new Color(0.75f, 0.75f, 0.73f, 1f));
            mat.SetColor("_Color", new Color(0.75f, 0.75f, 0.73f, 1f)); // Standard fallback
            mat.SetFloat("_Smoothness", 0.2f);

            EnsureFolder("Assets/_Materials");
            AssetDatabase.CreateAsset(mat, matPath);
            Debug.Log("[ObstacleCourse] Created Bridge_Concrete material");
        }

        static void BuildBridge(Transform parent, string name, Vector3 center, float length, float width, int pillars)
        {
            var bridgeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Materials/Bridge_Concrete.mat");

            var bridgeRoot = new GameObject(name);
            bridgeRoot.transform.SetParent(parent, false);
            bridgeRoot.transform.position = center;

            // Bridge deck
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.SetParent(bridgeRoot.transform, false);
            deck.transform.localPosition = Vector3.zero;
            deck.transform.localScale = new Vector3(width, 0.3f, length);
            if (bridgeMat != null)
                deck.GetComponent<MeshRenderer>().sharedMaterial = bridgeMat;

            // Guardrails
            float railHeight = 1f;
            float railThickness = 0.15f;
            for (int side = -1; side <= 1; side += 2)
            {
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = side < 0 ? "Guardrail_Left" : "Guardrail_Right";
                rail.transform.SetParent(bridgeRoot.transform, false);
                rail.transform.localPosition = new Vector3(
                    side * (width / 2f - railThickness / 2f),
                    (0.3f / 2f) + (railHeight / 2f),
                    0f);
                rail.transform.localScale = new Vector3(railThickness, railHeight, length);
                if (bridgeMat != null)
                    rail.GetComponent<MeshRenderer>().sharedMaterial = bridgeMat;
            }

            // Support pillars
            float pillarSpacing = length / (pillars + 1);
            float pillarHeight = 15f;
            for (int i = 1; i <= pillars; i++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"Pillar_{i}";
                pillar.transform.SetParent(bridgeRoot.transform, false);
                float zPos = -length / 2f + i * pillarSpacing;
                pillar.transform.localPosition = new Vector3(0, -(pillarHeight / 2f + 0.15f), zPos);
                pillar.transform.localScale = new Vector3(1.5f, pillarHeight, 1.5f);
                if (bridgeMat != null)
                    pillar.GetComponent<MeshRenderer>().sharedMaterial = bridgeMat;
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
