using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace EVP.Editor
{
    public static class ObstacleCourseTerrainGenerator
    {
        const int HeightmapRes = 513;
        const float TerrainWidth = 500f;
        const float TerrainHeight = 60f;
        const float TerrainLength = 500f;
        const float RoadHalfWidth = 8f;
        const float RoadDepression = 0.3f;

        // Course waypoints in world space (x, y_elevation, z)
        static readonly Vector3[] CourseWaypoints = new Vector3[]
        {
            // S1: Start/Finish area and winding start road
            new Vector3(250, 8, 50),    // 0: START/FINISH
            new Vector3(230, 8, 80),    // 1
            new Vector3(210, 7, 110),   // 2
            new Vector3(195, 6, 140),   // 3
            new Vector3(185, 5, 170),   // 4: approach valley

            // S2: Valley bridge
            new Vector3(175, 5, 195),   // 5: bridge start
            new Vector3(170, 5, 225),   // 6: bridge end (valley below)

            // S3: Steep switchbacks climbing
            new Vector3(160, 10, 250),  // 7: switchback 1 entry
            new Vector3(130, 18, 270),  // 8: hairpin 1 turn
            new Vector3(160, 25, 290),  // 9: hairpin 1 exit
            new Vector3(130, 33, 310),  // 10: hairpin 2 turn
            new Vector3(160, 40, 330),  // 11: hairpin 2 exit
            new Vector3(140, 45, 355),  // 12: hairpin 3 turn

            // S4: Hilltop hairpin at summit
            new Vector3(180, 50, 380),  // 13: summit approach
            new Vector3(220, 50, 395),  // 14: summit peak
            new Vector3(260, 50, 380),  // 15: summit exit

            // S5: Downhill S-curves
            new Vector3(280, 45, 355),  // 16: S-curve 1
            new Vector3(310, 40, 330),  // 17
            new Vector3(280, 35, 305),  // 18: S-curve 2
            new Vector3(310, 30, 280),  // 19
            new Vector3(290, 25, 255),  // 20: S-curve 3
            new Vector3(320, 22, 230),  // 21

            // S6: Long bridge approach and bridge
            new Vector3(330, 20, 200),  // 22: bridge start
            new Vector3(335, 20, 150),  // 23: bridge end

            // S7: Mixed terrain back to start
            new Vector3(330, 15, 120),  // 24
            new Vector3(310, 12, 95),   // 25
            new Vector3(285, 10, 75),   // 26
            new Vector3(260, 8, 55),    // 27: approach finish
            new Vector3(250, 8, 50),    // 28: back to START
        };

        [MenuItem("Tools/Generate Obstacle Course Terrain")]
        public static void Generate()
        {
            // Find or create terrain
            var terrainObj = GameObject.Find("Terrain_ObstacleCourse");
            Terrain terrain;
            if (terrainObj != null)
            {
                terrain = terrainObj.GetComponent<Terrain>();
                if (terrain != null && terrain.terrainData != null)
                    Undo.RecordObject(terrain.terrainData, "Regenerate Obstacle Course Terrain");
            }
            else
            {
                var terrainData = new TerrainData();
                AssetDatabase.CreateAsset(terrainData, "Assets/Terrain/ObstacleCourse.asset");
                terrainObj = Terrain.CreateTerrainGameObject(terrainData);
                terrainObj.name = "Terrain_ObstacleCourse";
                terrain = terrainObj.GetComponent<Terrain>();
                Undo.RegisterCreatedObjectUndo(terrainObj, "Create Obstacle Course Terrain");
            }

            var td = terrain.terrainData;
            td.heightmapResolution = HeightmapRes;
            td.size = new Vector3(TerrainWidth, TerrainHeight, TerrainLength);

            // Generate heightmap
            float[,] heights = new float[HeightmapRes, HeightmapRes];
            GenerateBaseHeights(heights);
            CarveRoadCorridor(heights);
            CarveValleys(heights);
            BuildSwitchbackHillside(heights);
            td.SetHeights(0, 0, heights);

            // Paint splatmap
            SetupTerrainLayers(td);
            PaintSplatmap(td, heights);

            // Position terrain so world origin is at corner
            terrainObj.transform.position = Vector3.zero;

            // Apply URP terrain material if available
            var urpTerrainMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Settings/URPTerrainLit.mat");
            if (urpTerrainMat == null)
            {
                // Try to find any URP terrain material
                var guids = AssetDatabase.FindAssets("t:Material URPTerrain");
                if (guids.Length > 0)
                    urpTerrainMat = AssetDatabase.LoadAssetAtPath<Material>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            if (urpTerrainMat != null)
                terrain.materialTemplate = urpTerrainMat;

            EditorUtility.SetDirty(td);
            AssetDatabase.SaveAssets();
            Debug.Log("[ObstacleCourse] Terrain generated: 500x60x500, heightmap 513");
        }

        static void GenerateBaseHeights(float[,] heights)
        {
            for (int z = 0; z < HeightmapRes; z++)
            {
                for (int x = 0; x < HeightmapRes; x++)
                {
                    float nx = (float)x / HeightmapRes;
                    float nz = (float)z / HeightmapRes;

                    // Gentle rolling Perlin noise base (0-8m range -> 0 to 8/60)
                    float h = Mathf.PerlinNoise(nx * 3f + 0.5f, nz * 3f + 0.5f) * (8f / TerrainHeight);
                    h += Mathf.PerlinNoise(nx * 7f + 10f, nz * 7f + 10f) * (3f / TerrainHeight);

                    // Add general elevation bias: higher in the north (z > 0.5)
                    float northBias = Mathf.Clamp01((nz - 0.4f) * 2f);
                    h += northBias * (15f / TerrainHeight);

                    heights[z, x] = h;
                }
            }
        }

        static void CarveRoadCorridor(float[,] heights)
        {
            // For each road segment between waypoints, flatten terrain along the path
            for (int seg = 0; seg < CourseWaypoints.Length - 1; seg++)
            {
                Vector3 a = CourseWaypoints[seg];
                Vector3 b = CourseWaypoints[seg + 1];
                float segLen = Vector3.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(segLen / 1f));

                for (int s = 0; s <= steps; s++)
                {
                    float t = (float)s / steps;
                    Vector3 p = Vector3.Lerp(a, b, t);
                    float targetH = (p.y - RoadDepression) / TerrainHeight;

                    // Convert world pos to heightmap coords
                    int cx = Mathf.RoundToInt(p.x / TerrainWidth * (HeightmapRes - 1));
                    int cz = Mathf.RoundToInt(p.z / TerrainLength * (HeightmapRes - 1));

                    // Flatten a corridor around this point
                    int radius = Mathf.RoundToInt(RoadHalfWidth / TerrainWidth * (HeightmapRes - 1));
                    int blendRadius = radius + Mathf.RoundToInt(6f / TerrainWidth * (HeightmapRes - 1));

                    for (int dz = -blendRadius; dz <= blendRadius; dz++)
                    {
                        for (int dx = -blendRadius; dx <= blendRadius; dx++)
                        {
                            int hx = cx + dx;
                            int hz = cz + dz;
                            if (hx < 0 || hx >= HeightmapRes || hz < 0 || hz >= HeightmapRes) continue;

                            float dist = Mathf.Sqrt(dx * dx + dz * dz);
                            float normDist = dist / blendRadius;

                            if (dist <= radius)
                            {
                                // Inside road corridor: set to road height
                                heights[hz, hx] = targetH;
                            }
                            else if (dist <= blendRadius)
                            {
                                // Blend zone: smooth transition
                                float blend = (dist - radius) / (blendRadius - radius);
                                blend = blend * blend * (3f - 2f * blend); // smoothstep
                                heights[hz, hx] = Mathf.Lerp(targetH, heights[hz, hx], blend);
                            }
                        }
                    }
                }
            }
        }

        static void CarveValleys(float[,] heights)
        {
            // Valley 1: Under bridge at segment 2 (waypoints 5-6, around z=195-225, x=170-175)
            CarveValley(heights, new Vector2(172, 210), 25f, 15f, 18f);

            // Valley 2: Under bridge at segment 6 (waypoints 22-23, around z=150-200, x=330-335)
            CarveValley(heights, new Vector2(332, 175), 35f, 12f, 16f);
        }

        static void CarveValley(float[,] heights, Vector2 center, float length, float width, float depth)
        {
            int cx = Mathf.RoundToInt(center.x / TerrainWidth * (HeightmapRes - 1));
            int cz = Mathf.RoundToInt(center.y / TerrainLength * (HeightmapRes - 1));
            int radiusX = Mathf.RoundToInt(width / TerrainWidth * (HeightmapRes - 1));
            int radiusZ = Mathf.RoundToInt(length / TerrainLength * (HeightmapRes - 1));
            float depthNorm = depth / TerrainHeight;

            for (int dz = -radiusZ; dz <= radiusZ; dz++)
            {
                for (int dx = -radiusX; dx <= radiusX; dx++)
                {
                    int hx = cx + dx;
                    int hz = cz + dz;
                    if (hx < 0 || hx >= HeightmapRes || hz < 0 || hz >= HeightmapRes) continue;

                    float fx = (float)dx / radiusX;
                    float fz = (float)dz / radiusZ;
                    float ellipse = fx * fx + fz * fz;
                    if (ellipse > 1f) continue;

                    // Smooth valley profile
                    float valleyFactor = 1f - ellipse;
                    valleyFactor = valleyFactor * valleyFactor; // steeper edges
                    heights[hz, hx] -= depthNorm * valleyFactor;
                    heights[hz, hx] = Mathf.Max(0f, heights[hz, hx]);
                }
            }
        }

        static void BuildSwitchbackHillside(float[,] heights)
        {
            // Build up the hillside in the northwest quadrant where switchbacks climb
            // Region: roughly x=120-180, z=240-400
            for (int z = 0; z < HeightmapRes; z++)
            {
                for (int x = 0; x < HeightmapRes; x++)
                {
                    float wx = (float)x / (HeightmapRes - 1) * TerrainWidth;
                    float wz = (float)z / (HeightmapRes - 1) * TerrainLength;

                    // Hillside region
                    if (wx > 100 && wx < 280 && wz > 240 && wz < 420)
                    {
                        // Elevation increases with z (northward climb)
                        float zFactor = Mathf.InverseLerp(240, 400, wz);
                        // Peak centered around x=200
                        float xFactor = 1f - Mathf.Abs(wx - 200) / 100f;
                        xFactor = Mathf.Clamp01(xFactor);

                        float hillHeight = zFactor * xFactor * (50f / TerrainHeight);

                        // Smooth edge blending
                        float edgeX = Mathf.Min(
                            Mathf.InverseLerp(100, 130, wx),
                            Mathf.InverseLerp(280, 250, wx)
                        );
                        float edgeZ = Mathf.Min(
                            Mathf.InverseLerp(240, 260, wz),
                            Mathf.InverseLerp(420, 400, wz)
                        );
                        float edge = Mathf.Min(edgeX, edgeZ);
                        edge = edge * edge * (3f - 2f * edge); // smoothstep

                        heights[z, x] = Mathf.Max(heights[z, x], heights[z, x] + hillHeight * edge);
                    }
                }
            }

            // Re-carve road corridor after building hillside to ensure roads are flat
            CarveRoadCorridor(heights);
        }

        static void SetupTerrainLayers(TerrainData td)
        {
            // Load existing terrain layers from EVP5
            var layers = new List<TerrainLayer>();

            string[] layerPaths = new string[]
            {
                "Assets/Plugins/EVP5/World/Terrain/Terrain Layers/layer_ter_grass002asphalt.terrainlayer",    // 0: grass (base)
                "Assets/Plugins/EVP5/World/Terrain/Terrain Layers/layer_Grass (Hill)asphalt.terrainlayer",    // 1: hill grass
                "Assets/Plugins/EVP5/World/Terrain/Terrain Layers/layer_Grass&Rock.terrainlayer",             // 2: rock
                "Assets/Plugins/EVP5/World/Terrain/Terrain Layers/layer_GoodDirtasphalt.terrainlayer",        // 3: dirt
            };

            foreach (var path in layerPaths)
            {
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (layer != null)
                {
                    layers.Add(layer);
                }
                else
                {
                    Debug.LogWarning($"[ObstacleCourse] Missing terrain layer: {path}");
                    // Create a fallback layer
                    var fallback = new TerrainLayer();
                    fallback.tileSize = new Vector2(15, 15);
                    layers.Add(fallback);
                }
            }

            td.terrainLayers = layers.ToArray();
        }

        static void PaintSplatmap(TerrainData td, float[,] heights)
        {
            int alphamapRes = td.alphamapResolution;
            float[,,] splat = new float[alphamapRes, alphamapRes, 4];

            for (int z = 0; z < alphamapRes; z++)
            {
                for (int x = 0; x < alphamapRes; x++)
                {
                    float nx = (float)x / alphamapRes;
                    float nz = (float)z / alphamapRes;

                    // Sample height and compute slope
                    int hx = Mathf.RoundToInt(nx * (HeightmapRes - 1));
                    int hz = Mathf.RoundToInt(nz * (HeightmapRes - 1));
                    float h = heights[Mathf.Clamp(hz, 0, HeightmapRes - 1),
                                      Mathf.Clamp(hx, 0, HeightmapRes - 1)];
                    float slope = ComputeSlope(heights, hx, hz);

                    // Check if near road
                    float roadDist = DistanceToRoad(nx * TerrainWidth, nz * TerrainLength);

                    // Layer weights: [grass, hill_grass, rock, dirt]
                    float grass = 1f;
                    float hillGrass = 0f;
                    float rock = 0f;
                    float dirt = 0f;

                    // Steep slopes -> rock
                    if (slope > 0.4f)
                    {
                        rock = Mathf.InverseLerp(0.4f, 0.7f, slope);
                    }

                    // Higher elevation -> hill grass
                    float elevation = h * TerrainHeight;
                    if (elevation > 15f)
                    {
                        hillGrass = Mathf.InverseLerp(15f, 30f, elevation) * (1f - rock);
                    }

                    // Near road -> dirt
                    if (roadDist < RoadHalfWidth + 4f)
                    {
                        float dirtFactor = 1f - Mathf.InverseLerp(RoadHalfWidth - 2f, RoadHalfWidth + 4f, roadDist);
                        dirt = dirtFactor;
                    }

                    // Remaining goes to grass
                    grass = Mathf.Max(0, 1f - hillGrass - rock - dirt);

                    // Normalize
                    float total = grass + hillGrass + rock + dirt;
                    if (total > 0)
                    {
                        splat[z, x, 0] = grass / total;
                        splat[z, x, 1] = hillGrass / total;
                        splat[z, x, 2] = rock / total;
                        splat[z, x, 3] = dirt / total;
                    }
                    else
                    {
                        splat[z, x, 0] = 1f;
                    }
                }
            }

            td.SetAlphamaps(0, 0, splat);
        }

        static float ComputeSlope(float[,] heights, int x, int z)
        {
            if (x <= 0 || x >= HeightmapRes - 1 || z <= 0 || z >= HeightmapRes - 1)
                return 0f;

            float dhdx = (heights[z, x + 1] - heights[z, x - 1]) * TerrainHeight;
            float dhdz = (heights[z + 1, x] - heights[z - 1, x]) * TerrainHeight;
            float cellSize = TerrainWidth / (HeightmapRes - 1);
            return Mathf.Sqrt(dhdx * dhdx + dhdz * dhdz) / (2f * cellSize);
        }

        static float DistanceToRoad(float wx, float wz)
        {
            float minDist = float.MaxValue;
            for (int i = 0; i < CourseWaypoints.Length - 1; i++)
            {
                Vector3 a = CourseWaypoints[i];
                Vector3 b = CourseWaypoints[i + 1];
                Vector2 p = new Vector2(wx, wz);
                Vector2 pa = new Vector2(a.x, a.z);
                Vector2 pb = new Vector2(b.x, b.z);
                float dist = DistanceToSegment(p, pa, pb);
                minDist = Mathf.Min(minDist, dist);
            }
            return minDist;
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
            Vector2 closest = a + t * ab;
            return Vector2.Distance(p, closest);
        }
    }
}
