using UnityEngine;
using UnityEditor;
using Networking;
using System.IO;

/// <summary>
/// Editor helper để setup multiplayer scene và tạo prefabs tự động
/// </summary>
public class MultiplayerSetupHelper : EditorWindow
{
    private string prefabPath = "Assets/Prefabs/";

    [MenuItem("Tools/Multiplayer/Setup Wizard")]
    static void ShowWindow()
    {
        GetWindow<MultiplayerSetupHelper>("Multiplayer Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Multiplayer Setup Helper", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("Prefab Path:");
        prefabPath = EditorGUILayout.TextField(prefabPath);
        GUILayout.Space(10);

        if (GUILayout.Button("1. Create Player Prefabs", GUILayout.Height(30)))
        {
            CreatePlayerPrefabs();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("2. Setup Current Scene", GUILayout.Height(30)))
        {
            SetupScene();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("3. Complete Setup (All Steps)", GUILayout.Height(40)))
        {
            CreatePlayerPrefabs();
            SetupScene();
        }

        GUILayout.Space(20);
        GUILayout.Label("Quick Actions:", EditorStyles.boldLabel);

        if (GUILayout.Button("Assign Prefabs to NetworkManager"))
        {
            AssignPrefabsToNetworkManager();
        }
    }

    void CreatePlayerPrefabs()
    {
        // Tạo folder nếu chưa có
        if (!Directory.Exists(prefabPath))
        {
            Directory.CreateDirectory(prefabPath);
        }

        // Tạo Local Player Prefab
        GameObject localPlayer = CreateLocalPlayerPrefab();
        string localPath = prefabPath + "LocalPlayer.prefab";
        PrefabUtility.SaveAsPrefabAsset(localPlayer, localPath);
        DestroyImmediate(localPlayer);

        // Tạo Remote Player Prefab
        GameObject remotePlayer = CreateRemotePlayerPrefab();
        string remotePath = prefabPath + "RemotePlayer.prefab";
        PrefabUtility.SaveAsPrefabAsset(remotePlayer, remotePath);
        DestroyImmediate(remotePlayer);

        AssetDatabase.Refresh();

        Debug.Log($"✅ Player prefabs created successfully!");
        Debug.Log($"   - LocalPlayer: {localPath}");
        Debug.Log($"   - RemotePlayer: {remotePath}");

        EditorUtility.DisplayDialog("Success",
            "Player prefabs created successfully!\n\n" +
            $"LocalPlayer: {localPath}\n" +
            $"RemotePlayer: {remotePath}",
            "OK");
    }

    GameObject CreateLocalPlayerPrefab()
    {
        // Create root
        GameObject player = new GameObject("LocalPlayer");

        // Add Character Controller
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.5f;
        controller.center = new Vector3(0, 1, 0);

        // Add PlayerController
        PlayerController playerController = player.AddComponent<PlayerController>();

        // Create body visual
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0, 1, 0);

        // Remove collider from visual (CharacterController handles collision)
        DestroyImmediate(body.GetComponent<CapsuleCollider>());

        // Create green material
        Material greenMat = new Material(Shader.Find("Standard"));
        greenMat.color = new Color(0.2f, 0.8f, 0.2f);
        body.GetComponent<Renderer>().material = greenMat;

        // Assign renderer to controller
        SerializedObject so = new SerializedObject(playerController);
        so.FindProperty("playerRenderer").objectReferenceValue = body.GetComponent<Renderer>();
        so.ApplyModifiedProperties();

        return player;
    }

    GameObject CreateRemotePlayerPrefab()
    {
        // Create root
        GameObject player = new GameObject("RemotePlayer");

        // Add NetworkPlayer
        Networking.NetworkPlayer networkPlayer = player.AddComponent<Networking.NetworkPlayer>();

        // Create body visual
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0, 1, 0);

        // Remove collider (remote players don't need collision)
        DestroyImmediate(body.GetComponent<CapsuleCollider>());

        // Create blue material
        Material blueMat = new Material(Shader.Find("Standard"));
        blueMat.color = new Color(0.2f, 0.4f, 0.8f);
        body.GetComponent<Renderer>().material = blueMat;

        // Assign renderer to network player
        SerializedObject so = new SerializedObject(networkPlayer);
        so.FindProperty("playerRenderer").objectReferenceValue = body.GetComponent<Renderer>();
        so.ApplyModifiedProperties();

        return player;
    }

    void SetupScene()
    {
        // Create Ground
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10);

            // Create simple material
            Material groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = new Color(0.3f, 0.3f, 0.3f);
            ground.GetComponent<Renderer>().material = groundMat;

            Debug.Log("✅ Ground created");
        }

        // Setup Main Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 8, -12);
            mainCam.transform.rotation = Quaternion.Euler(35, 0, 0);
            Debug.Log("✅ Camera positioned");
        }

        // Create or find NetworkManager
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager == null)
        {
            GameObject nmObj = new GameObject("NetworkManager");
            networkManager = nmObj.AddComponent<NetworkManager>();
            Debug.Log("✅ NetworkManager created");
        }

        // Create or find MultiplayerDemo
        MultiplayerDemo demo = FindObjectOfType<MultiplayerDemo>();
        if (demo == null)
        {
            GameObject demoObj = new GameObject("MultiplayerDemo");
            demo = demoObj.AddComponent<MultiplayerDemo>();
            Debug.Log("✅ MultiplayerDemo created");
        }

        // Try to assign prefabs
        AssignPrefabsToNetworkManager();

        EditorUtility.DisplayDialog("Success",
            "Scene setup complete!\n\n" +
            "Objects created:\n" +
            "- Ground (Plane)\n" +
            "- NetworkManager\n" +
            "- MultiplayerDemo\n\n" +
            "Camera has been positioned.\n" +
            "Prefabs have been assigned (if they exist).",
            "OK");
    }

    void AssignPrefabsToNetworkManager()
    {
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager == null)
        {
            Debug.LogWarning("NetworkManager not found in scene!");
            return;
        }

        // Load prefabs
        GameObject localPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "LocalPlayer.prefab");
        GameObject remotePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "RemotePlayer.prefab");

        if (localPrefab == null || remotePrefab == null)
        {
            Debug.LogWarning("Prefabs not found! Create them first using 'Create Player Prefabs' button.");
            return;
        }

        // Assign prefabs
        SerializedObject so = new SerializedObject(networkManager);
        so.FindProperty("localPlayerPrefab").objectReferenceValue = localPrefab;
        so.FindProperty("remotePlayerPrefab").objectReferenceValue = remotePrefab;
        so.ApplyModifiedProperties();

        Debug.Log("✅ Prefabs assigned to NetworkManager");
        EditorUtility.SetDirty(networkManager);
    }
}
