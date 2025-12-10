using UnityEngine;
using Networking;

/// <summary>
/// Component để sync network cho PlayerMove hiện có
/// Attach vào cùng GameObject với PlayerMove
/// </summary>
[RequireComponent(typeof(PlayerMove))]
public class PlayerNetworkSync : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private float positionUpdateRate = 0.05f; // 20 times per second

    private PlayerMove playerMove;
    private PlayerData playerData;
    private Vector3 lastSentPosition;
    private Vector3 lastSentVelocity;
    private float lastUpdateTime;

    [Header("Player Info")]
    public int playerId = -1;
    public bool isLocalPlayer = false;

    private void Awake()
    {
        playerMove = GetComponent<PlayerMove>();
        playerData = GetComponent<PlayerData>();
    }

    private void Start()
    {
        if (playerMove == null)
        {
            Debug.LogError("[PlayerNetworkSync] PlayerMove component not found!");
            enabled = false;
            return;
        }

        lastSentPosition = transform.position;
        lastUpdateTime = Time.time;
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        SendPositionUpdate();
    }

    /// <summary>
    /// Initialize as local player
    /// </summary>
    public void InitializeAsLocalPlayer(int id)
    {
        playerId = id;
        isLocalPlayer = true;

        // Enable PlayerMove for local player
        if (playerMove != null)
            playerMove.enabled = true;

        Debug.Log($"[PlayerNetworkSync] Initialized as local player {id}");
    }

    /// <summary>
    /// Initialize as remote player (disable PlayerMove, only receive sync)
    /// </summary>
    public void InitializeAsRemotePlayer(int id)
    {
        playerId = id;
        isLocalPlayer = false;

        // Disable PlayerMove for remote players
        if (playerMove != null)
            playerMove.enabled = false;

        Debug.Log($"[PlayerNetworkSync] Initialized as remote player {id}");
    }

    /// <summary>
    /// Send position update to server at regular intervals
    /// </summary>
    private void SendPositionUpdate()
    {

        // Check if enough time has passed since last update
        if (Time.time - lastUpdateTime < positionUpdateRate) return;

        // Get current velocity from PlayerData
        Vector3 currentVelocity = playerData != null ? playerData.velocity : Vector3.zero;

        // Check if position or velocity has changed significantly
        float positionDelta = Vector3.Distance(transform.position, lastSentPosition);
        float velocityDelta = Vector3.Distance(currentVelocity, lastSentVelocity);

        if (positionDelta > 0.01f || velocityDelta > 0.01f)
        {
            // Send update to server
            NetworkManager manager = NetworkManager.Instant;
            if (manager != null && manager.IsInGame)
            {
                manager.SendPlayerPosition(transform.position, currentVelocity);
                // Debug.Log($"[PlayerNetworkSync] Sent position: {transform.position}, velocity: {currentVelocity}");
            }
            else
            {
                Debug.LogWarning("[PlayerNetworkSync] Cannot send - NetworkManager not ready or not in game");
            }

            lastSentPosition = transform.position;
            lastSentVelocity = currentVelocity;
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Get current velocity
    /// </summary>
    public Vector3 GetVelocity()
    {
        return playerData != null ? playerData.velocity : Vector3.zero;
    }
}
