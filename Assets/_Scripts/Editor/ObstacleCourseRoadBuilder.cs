using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

namespace EVP.Editor
{
    public static class ObstacleCourseRoadBuilder
    {
        // Same waypoints as terrain generator, grouped by segment
        static readonly Vector3[][] Segments = new Vector3[][]
        {
            // S1: Winding start road
            new Vector3[]
            {
                new Vector3(250, 8, 50),
                new Vector3(230, 8, 80),
                new Vector3(210, 7, 110),
                new Vector3(195, 6, 140),
                new Vector3(185, 5, 170),
            },
            // S2: Valley bridge approach
            new Vector3[]
            {
                new Vector3(185, 5, 170),
                new Vector3(175, 5, 195),
                new Vector3(170, 5, 225),
            },
            // S3: Steep switchbacks
            new Vector3[]
            {
                new Vector3(170, 5, 225),
                new Vector3(160, 10, 250),
                new Vector3(130, 18, 270),
                new Vector3(160, 25, 290),
                new Vector3(130, 33, 310),
                new Vector3(160, 40, 330),
                new Vector3(140, 45, 355),
            },
            // S4: Hilltop hairpin
            new Vector3[]
            {
                new Vector3(140, 45, 355),
                new Vector3(180, 50, 380),
                new Vector3(220, 50, 395),
                new Vector3(260, 50, 380),
            },
            // S5: Downhill S-curves
            new Vector3[]
            {
                new Vector3(260, 50, 380),
                new Vector3(280, 45, 355),
                new Vector3(310, 40, 330),
                new Vector3(280, 35, 305),
                new Vector3(310, 30, 280),
                new Vector3(290, 25, 255),
                new Vector3(320, 22, 230),
            },
            // S6: Long bridge
            new Vector3[]
            {
                new Vector3(320, 22, 230),
                new Vector3(330, 20, 200),
                new Vector3(335, 20, 150),
            },
            // S7: Mixed terrain back to start
            new Vector3[]
            {
                new Vector3(335, 20, 150),
                new Vector3(330, 15, 120),
                new Vector3(310, 12, 95),
                new Vector3(285, 10, 75),
                new Vector3(260, 8, 55),
                new Vector3(250, 8, 50),
            },
        };

        static readonly string[] SegmentNames = new string[]
        {
            "Road_S1_WindingStart",
            "Road_S2_ValleyBridge",
            "Road_S3_Switchbacks",
            "Road_S4_HilltopHairpin",
            "Road_S5_DownhillCurves",
            "Road_S6_LongBridge",
            "Road_S7_MixedTerrain",
        };

        [MenuItem("Tools/Build Obstacle Course Roads")]
        public static void Build()
        {
            // Find or create parent
            var parent = GameObject.Find("CourseLayout");
            if (parent == null)
            {
                parent = new GameObject("CourseLayout");
                Undo.RegisterCreatedObjectUndo(parent, "Create CourseLayout");
            }

            // Load road material
            var roadMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Materials/Road_Asphalt.mat");
            if (roadMat == null)
            {
                Debug.LogWarning("[ObstacleCourse] Road_Asphalt.mat not found, roads will use default material");
            }

            // Sample terrain heights if terrain exists
            Terrain terrain = null;
            var terrainObj = GameObject.Find("Terrain_ObstacleCourse");
            if (terrainObj != null)
                terrain = terrainObj.GetComponent<Terrain>();

            for (int seg = 0; seg < Segments.Length; seg++)
            {
                BuildSegment(seg, parent.transform, roadMat, terrain);
            }

            Debug.Log($"[ObstacleCourse] Built {Segments.Length} road segments under CourseLayout");
        }

        static void BuildSegment(int index, Transform parent, Material mat, Terrain terrain)
        {
            string name = SegmentNames[index];

            // Delete existing if present
            var existing = parent.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            go.transform.position = Vector3.zero;

            // Add required components (SplineExtrude auto-adds MeshFilter + MeshRenderer)
            var splineContainer = go.AddComponent<SplineContainer>();
            var splineExtrude = go.AddComponent<SplineExtrude>();
            var meshCollider = go.AddComponent<MeshCollider>();
            var meshFilter = go.GetComponent<MeshFilter>();
            var meshRenderer = go.GetComponent<MeshRenderer>();

            // Configure material
            if (mat != null)
                meshRenderer.sharedMaterial = mat;

            // Set shape to Road via SerializedObject (Shape property is internal)
            var so = new SerializedObject(splineExtrude);
            var shapeProp = so.FindProperty("m_Shape");
            if (shapeProp != null)
            {
                shapeProp.managedReferenceValue = new UnityEngine.Splines.ExtrusionShapes.Road();
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Build spline from waypoints
            var spline = new Spline();
            Vector3[] waypoints = Segments[index];

            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector3 wp = waypoints[i];

                // Sample terrain height + offset if terrain available
                float y = wp.y + 0.05f;
                if (terrain != null)
                {
                    float terrainH = terrain.SampleHeight(wp);
                    // Use the higher of planned elevation or terrain + offset
                    y = Mathf.Max(y, terrainH + 0.05f);
                }

                // Compute tangent direction from neighbors
                Vector3 tangentDir = Vector3.forward;
                if (i > 0 && i < waypoints.Length - 1)
                {
                    tangentDir = (waypoints[i + 1] - waypoints[i - 1]).normalized;
                }
                else if (i == 0 && waypoints.Length > 1)
                {
                    tangentDir = (waypoints[1] - waypoints[0]).normalized;
                }
                else if (i == waypoints.Length - 1 && waypoints.Length > 1)
                {
                    tangentDir = (waypoints[i] - waypoints[i - 1]).normalized;
                }

                // Scale tangent by distance to neighbors for smooth curves
                float tangentScale = 0f;
                if (i > 0)
                    tangentScale += Vector3.Distance(waypoints[i], waypoints[i - 1]) * 0.33f;
                if (i < waypoints.Length - 1)
                    tangentScale += Vector3.Distance(waypoints[i], waypoints[i + 1]) * 0.33f;
                if (i > 0 && i < waypoints.Length - 1)
                    tangentScale *= 0.5f;

                Vector3 tangent = tangentDir * tangentScale;

                // Compute knot rotation from tangent direction
                Quaternion rotation = Quaternion.LookRotation(tangentDir, Vector3.up);

                var knot = new BezierKnot(
                    new float3(wp.x, y, wp.z),
                    new float3(-tangent.x, -tangent.y, -tangent.z),
                    new float3(tangent.x, tangent.y, tangent.z),
                    new quaternion(rotation.x, rotation.y, rotation.z, rotation.w)
                );
                spline.Add(knot);
            }

            // Replace the default spline with our course spline
            splineContainer.Spline = spline;

            // Configure SplineExtrude to match existing pattern
            splineExtrude.Container = splineContainer;
            splineExtrude.RebuildOnSplineChange = true;
            splineExtrude.Sides = 8;
            splineExtrude.SegmentsPerUnit = 4;
            splineExtrude.Capped = true;
            splineExtrude.Radius = 5f;
            splineExtrude.Range = new Vector2(0, 1);

            // Force rebuild the mesh
            splineExtrude.Rebuild();

            // Update collider
            if (meshFilter.sharedMesh != null)
            {
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }

            EditorUtility.SetDirty(go);
        }
    }
}
