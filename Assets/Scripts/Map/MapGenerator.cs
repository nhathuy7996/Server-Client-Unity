using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

/// <summary>
/// MapGenerator - Tạo map và obstacles từ JSON data nhận từ server
/// </summary>
public class MapGenerator : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private GameObject cylinderPrefab;

    [Header("Materials")]
    [SerializeField] private Material obstacleMaterial;
    [SerializeField] private Material groundMaterial;

    [Header("Generated Objects")]
    private GameObject mapContainer;
    private GameObject groundPlane;
    private List<GameObject> spawnedObstacles = new List<GameObject>();

    private void Awake()
    {
        // Create container for map objects
        mapContainer = new GameObject("Map Container");
        mapContainer.transform.SetParent(transform);
    }

    /// <summary>
    /// Generate map from JSON data received from server
    /// </summary>
    public void GenerateMapFromJSON(string jsonData)
    {
        try
        {
            Debug.Log("[MapGenerator] Parsing map data...");
            JSONNode mapJson = JSON.Parse(jsonData);

            // Clear existing map
            ClearMap();

            // Parse map properties
            string mapId = mapJson["id"];
            string mapName = mapJson["name"];
            float width = mapJson["width"].AsFloat;
            float length = mapJson["length"].AsFloat;

            Debug.Log($"[MapGenerator] Generating map: {mapName} (ID: {mapId})");
            Debug.Log($"[MapGenerator] Map size: {width}x{length}");

            // Create ground plane
            CreateGroundPlane(width, length);

            // Parse and spawn obstacles
            JSONArray obstacles = mapJson["obstacles"].AsArray;
            if (obstacles != null)
            {
                Debug.Log($"[MapGenerator] Spawning {obstacles.Count} obstacles...");
                foreach (JSONNode obstacleData in obstacles)
                {
                    SpawnObstacle(obstacleData);
                }
            }

            Debug.Log($"[MapGenerator] Map generation complete! Total obstacles: {spawnedObstacles.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapGenerator] Error generating map: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// Create ground plane for the map
    /// </summary>
    private void CreateGroundPlane(float width, float length)
    {
        if (groundPlane != null)
        {
            Destroy(groundPlane);
        }

        groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundPlane.name = "Ground";
        groundPlane.transform.SetParent(mapContainer.transform);
        groundPlane.transform.position = Vector3.zero;

        // Scale plane (Unity plane is 10x10 by default)
        groundPlane.transform.localScale = new Vector3(width / 10f, 1f, length / 10f);

        // Apply material if available
        if (groundMaterial != null)
        {
            groundPlane.GetComponent<Renderer>().material = groundMaterial;
        }
        else
        {
            // Default gray color
            groundPlane.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f);
        }

        Debug.Log($"[MapGenerator] Created ground plane: {width}x{length}");
    }

    /// <summary>
    /// Spawn a single obstacle from JSON data
    /// </summary>
    private void SpawnObstacle(JSONNode obstacleData)
    {
        try
        {
            string id = obstacleData["id"];
            string shape = obstacleData["shape"];

            // Parse position
            Vector3 position = new Vector3(
                obstacleData["position"]["x"].AsFloat,
                obstacleData["position"]["y"].AsFloat,
                obstacleData["position"]["z"].AsFloat
            );

            // Parse size
            Vector3 size = new Vector3(
                obstacleData["size"]["x"].AsFloat,
                obstacleData["size"]["y"].AsFloat,
                obstacleData["size"]["z"].AsFloat
            );

            // Parse rotation (optional)
            Vector3 rotation = Vector3.zero;
            if (obstacleData["rotation"] != null)
            {
                rotation = new Vector3(
                    obstacleData["rotation"]["x"].AsFloat,
                    obstacleData["rotation"]["y"].AsFloat,
                    obstacleData["rotation"]["z"].AsFloat
                );
            }

            GameObject obstacle = null;

            // Create obstacle based on shape
            if (shape == "box")
            {
                obstacle = CreateBoxObstacle(id, position, size, rotation);
            }
            else if (shape == "cylinder")
            {
                obstacle = CreateCylinderObstacle(id, position, size, rotation);
            }
            else
            {
                Debug.LogWarning($"[MapGenerator] Unknown obstacle shape: {shape}");
                return;
            }

            if (obstacle != null)
            {
                obstacle.transform.SetParent(mapContainer.transform);
                spawnedObstacles.Add(obstacle);
                Debug.Log($"[MapGenerator] Spawned {shape} obstacle: {id} at {position}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapGenerator] Error spawning obstacle: {e.Message}");
        }
    }

    /// <summary>
    /// Create a box-shaped obstacle
    /// </summary>
    private GameObject CreateBoxObstacle(string id, Vector3 position, Vector3 size, Vector3 rotation)
    {
        GameObject box;

        if (boxPrefab != null)
        {
            box = Instantiate(boxPrefab);
        }
        else
        {
            // Create primitive cube
            box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        box.name = $"Obstacle_Box_{id}";
        box.transform.position = position;
        box.transform.localScale = size;
        box.transform.eulerAngles = rotation;

        // Add collider if not present
        if (box.GetComponent<Collider>() == null)
        {
            box.AddComponent<BoxCollider>();
        }

        // Apply material
        ApplyObstacleMaterial(box);

        return box;
    }

    /// <summary>
    /// Create a cylinder-shaped obstacle
    /// </summary>
    private GameObject CreateCylinderObstacle(string id, Vector3 position, Vector3 size, Vector3 rotation)
    {
        GameObject cylinder;

        if (cylinderPrefab != null)
        {
            cylinder = Instantiate(cylinderPrefab);
        }
        else
        {
            // Create primitive cylinder
            cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        }

        cylinder.name = $"Obstacle_Cylinder_{id}";
        cylinder.transform.position = position;

        // For cylinder: size.x = radius, size.y = height
        // Unity cylinder default: height=2, radius=0.5
        float radiusScale = size.x / 0.5f;
        float heightScale = size.y / 2f;
        cylinder.transform.localScale = new Vector3(radiusScale, heightScale, radiusScale);

        cylinder.transform.eulerAngles = rotation;

        // Add collider if not present
        if (cylinder.GetComponent<Collider>() == null)
        {
            cylinder.AddComponent<CapsuleCollider>();
        }

        // Apply material
        ApplyObstacleMaterial(cylinder);

        return cylinder;
    }

    /// <summary>
    /// Apply material to obstacle
    /// </summary>
    private void ApplyObstacleMaterial(GameObject obstacle)
    {
        Renderer renderer = obstacle.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (obstacleMaterial != null)
            {
                renderer.material = obstacleMaterial;
            }
            else
            {
                // Default brown/tan color for obstacles
                renderer.material.color = new Color(0.6f, 0.4f, 0.2f);
            }
        }
    }

    /// <summary>
    /// Clear all spawned map objects
    /// </summary>
    public void ClearMap()
    {
        Debug.Log("[MapGenerator] Clearing map...");

        // Destroy all spawned obstacles
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
        }
        spawnedObstacles.Clear();

        // Destroy ground plane
        if (groundPlane != null)
        {
            Destroy(groundPlane);
            groundPlane = null;
        }
    }

    private void OnDestroy()
    {
        ClearMap();
    }
}
