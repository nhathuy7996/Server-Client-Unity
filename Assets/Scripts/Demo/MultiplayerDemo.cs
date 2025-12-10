using UnityEngine;
using Networking;

/// <summary>
/// Example script showing how to use the multiplayer system
/// Attach this to an empty GameObject in your scene
/// </summary>
public class MultiplayerDemo : MonoBehaviour
{
    [Header("UI Display")]
    [SerializeField] private bool showDebugInfo = true;

    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = NetworkManager.Instant;

        if (networkManager == null)
        {
            Debug.LogError("[MultiplayerDemo] NetworkManager not found! Make sure it exists in the scene.");
            return;
        }

        // Automatically connect and join game
        //ConnectToGame();
    }

    /// <summary>
    /// Connect to server and join the game
    /// </summary>
    public void ConnectToGame()
    {
        Debug.Log("[MultiplayerDemo] Connecting to game...");
        networkManager.ConnectAndJoinGame();
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Box("Multiplayer Info");

        if (networkManager != null)
        {
            GUILayout.Label($"Connected: {networkManager.IsConnected}");
            GUILayout.Label($"In Game: {networkManager.IsInGame}");
            GUILayout.Label($"Player ID: {networkManager.LocalPlayerId}");

            GameObject localPlayer = networkManager.LocalPlayerObject;
            if (localPlayer != null)
            {
                Vector3 pos = localPlayer.transform.position;

                // Try to get velocity from either PlayerNetworkSync or PlayerController
                Vector3 vel = Vector3.zero;
                PlayerNetworkSync networkSync = localPlayer.GetComponent<PlayerNetworkSync>();
                if (networkSync != null)
                {
                    vel = networkSync.GetVelocity();
                }
                else
                {
                    PlayerController controller = localPlayer.GetComponent<PlayerController>();
                    if (controller != null)
                        vel = controller.GetVelocity();
                }

                GUILayout.Label($"Position: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
                GUILayout.Label($"Velocity: ({vel.x:F2}, {vel.y:F2}, {vel.z:F2})");
            }

            GUILayout.Space(10);

            if (!networkManager.IsConnected)
            {
                if (GUILayout.Button("Connect to Game"))
                {
                    ConnectToGame();
                }
            }
        }

        GUILayout.EndArea();

        // Draw controls
        GUILayout.BeginArea(new Rect(10, 220, 300, 100));
        GUILayout.Box("Controls");
        GUILayout.Label("WASD or Arrow Keys - Move");
        GUILayout.Label("Player will auto-sync with server");
        GUILayout.EndArea();
    }
}
