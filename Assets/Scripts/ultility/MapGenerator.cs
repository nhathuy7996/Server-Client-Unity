using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{

    [SerializeField] GameObject mapContainer;
    [SerializeField] List<GameObject> obstacles = new List<GameObject>();

    [SerializeField] GameObject groundPlane;
    public void GenarateMap(string jsonData)
    {
        Debug.Log("Generate map from data: " + jsonData);
        ClearMap();

        var mapData = JSON.Parse(jsonData);
        var obstaclesData = mapData["obstacles"].AsArray;

        // Instantiate mapContainer if needed
        if (mapContainer == null)
        {
            mapContainer = new GameObject("MapContainer");
        }

        // Set ground plane
        float width = mapData["width"].AsFloat;
        float length = mapData["length"].AsFloat;
        if (groundPlane != null)
        {
            groundPlane.transform.localScale = new Vector3(width, 1, length);
            groundPlane.transform.position = Vector3.zero;
        }

        // Create obstacles
        foreach (JSONNode obstacle in obstaclesData)
        {
            string id = obstacle["id"];
            string shape = obstacle["shape"];
            Vector3 pos = new Vector3(obstacle["position"]["x"].AsFloat, obstacle["position"]["y"].AsFloat, obstacle["position"]["z"].AsFloat);
            Vector3 size = new Vector3(obstacle["size"]["x"].AsFloat, obstacle["size"]["y"].AsFloat, obstacle["size"]["z"].AsFloat);
            GameObject obj = null;
            if (shape == "box")
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }
            else if (shape == "cylinder")
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            }
            if (obj != null)
            {
                obj.transform.position = pos;
                obj.transform.localScale = size;
                obj.name = id;
                obj.transform.parent = mapContainer.transform;
                obstacles.Add(obj);
            }
        }
    }

    public void ClearMap()
    {
        Debug.Log("Clearing map...");

        foreach (GameObject g in this.obstacles)
        {
            Destroy(g);
        }
        this.obstacles.Clear();

        Destroy(mapContainer);
    }
}
