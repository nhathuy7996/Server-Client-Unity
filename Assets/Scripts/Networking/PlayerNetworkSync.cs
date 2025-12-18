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

    [Header("Position Reconciliation")]
    [SerializeField] private float maxPositionError = 2f; // Ngưỡng để correction
    [SerializeField] private float snapThreshold = 5f; // Ngưỡng để snap thay vì lerp
    [SerializeField] private float correctionSpeed = 10f; // Tốc độ correction

    private PlayerMove playerMove;
    private PlayerData playerData;
    private Vector3 lastSentPosition;
    private Vector3 lastSentVelocity;
    private float lastUpdateTime;

    // Server authoritative position
    private Vector3 serverPosition;
    private long lastSequenceNumber = 0;

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

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        // Apply position correction from server (nếu có)
        ApplyPositionCorrection();

        // Send position update to server
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
    /// Nhận server position (authoritative) để correct client position
    /// </summary>
    public void UpdateServerPosition(Vector3 position, long sequenceNumber = 0)
    {
        // Chỉ accept update mới hơn
        if (sequenceNumber > 0 && sequenceNumber < lastSequenceNumber)
        {
            Debug.LogWarning($"[PlayerNetworkSync] Ignoring old position update (seq: {sequenceNumber} < {lastSequenceNumber})");
            return;
        }

        lastSequenceNumber = sequenceNumber;
        serverPosition = position;

        // Check position error
        float positionError = Vector3.Distance(transform.position, serverPosition);

        if (positionError > maxPositionError)
        {
            if (positionError > snapThreshold)
            {
                // Lệch quá xa - snap ngay lập tức
                transform.position = serverPosition;
                Debug.LogWarning($"[PlayerNetworkSync {playerId}] Large position error ({positionError:F2}m), snapping to server position");
            }
            else
            {
                Debug.LogWarning($"[PlayerNetworkSync {playerId}] Position error detected: {positionError:F2}m, correcting...");
            }
        }
    }

    /// <summary>
    /// Apply position correction từ server
    /// </summary>
    private void ApplyPositionCorrection()
    {
        if (!isLocalPlayer) return;

        float positionError = Vector3.Distance(transform.position, serverPosition);

        // Nếu lệch trong ngưỡng cho phép, lerp smooth
        if (positionError > 0.01f && positionError <= maxPositionError)
        {
            transform.position = Vector3.Lerp(transform.position, serverPosition, Time.deltaTime * correctionSpeed);
        }
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

        // Check if velocity has changed significantly
        float velocityDelta = Vector3.Distance(currentVelocity, lastSentVelocity);

        if (velocityDelta > 0.01f)
        {
            // Send velocity update to server (server will calculate position)
            NetworkManager manager = NetworkManager.Instant;
            if (manager != null && manager.IsInGame)
            {
                manager.SendPlayerVelocity(currentVelocity);
                // Debug.Log($"[PlayerNetworkSync] Sent velocity: {currentVelocity}");
            }
            else
            {
                Debug.LogWarning("[PlayerNetworkSync] Cannot send - NetworkManager not ready or not in game");
            }

            lastSentPosition = transform.position;
            lastSentVelocity = currentVelocity;
            lastUpdateTime = Time.time;
        }
        else
        {
            // Debug.LogError($"[PlayerNetworkSync] Velocity delta too small to send: {currentVelocity} - {lastSentVelocity}");
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
