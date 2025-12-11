using UnityEngine;

namespace Networking
{
    /// <summary>
    /// Represents a remote player in the game
    /// Handles smooth interpolation of position and velocity from server updates
    /// </summary>
    public class NetworkPlayer : MonoBehaviour
    {
        [Header("Player Info")]
        public int playerId;
        private PlayerData playerData;

        [Header("Interpolation Settings")]
        [SerializeField] private float positionLerpSpeed = 10f;
        [SerializeField] private float rotationLerpSpeed = 10f;

        [Header("Reconciliation Settings")]
        [SerializeField] private float maxPositionError = 2f; // Khoảng cách tối đa cho phép sai lệch
        [SerializeField] private float snapThreshold = 5f; // Ngưỡng để snap thay vì lerp

        private Vector3 targetPosition;
        private Vector3 targetVelocity;
        private Quaternion targetRotation;

        // Tracking for synchronization
        private long lastSequenceNumber = 0;
        private long lastTimestamp = 0;


        private void Awake()
        {
            targetPosition = transform.position;
            targetRotation = transform.rotation;

            this.playerData = GetComponentInParent<PlayerData>();

        }

        private void Update()
        {
            // Calculate distance error
            float distanceError = Vector3.Distance(transform.position, targetPosition);

            // Nếu lệch quá xa, snap ngay lập tức
            if (distanceError > snapThreshold)
            {
                transform.position = targetPosition;
                Debug.LogWarning($"[NetworkPlayer {playerId}] Position error too large ({distanceError:F2}), snapping to server position");
            }
            // Nếu lệch nhưng chưa quá xa, lerp smooth
            else if (distanceError > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionLerpSpeed);
            }

            // Smooth rotation
            if (targetVelocity.magnitude > 0.01f)
            {
                targetRotation = Quaternion.LookRotation(targetVelocity.normalized);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
            }
        }

        /// <summary>
        /// Update player state from server (full update)
        /// </summary>
        public void UpdateState(Vector3 position, Vector3 velocity, float health, long sequenceNumber = 0, long timestamp = 0)
        {
            // Chỉ accept update nếu sequence number mới hơn
            if (sequenceNumber > 0 && sequenceNumber < lastSequenceNumber)
            {
                Debug.LogWarning($"[NetworkPlayer {playerId}] Ignoring old update (seq: {sequenceNumber} < {lastSequenceNumber})");
                return;
            }

            lastSequenceNumber = sequenceNumber;
            lastTimestamp = timestamp;

            // Check position error
            float positionError = Vector3.Distance(transform.position, position);
            if (positionError > maxPositionError)
            {
                Debug.LogWarning($"[NetworkPlayer {playerId}] Large position correction: {positionError:F2}m");
            }

            targetPosition = position;
            targetVelocity = velocity;
            this.playerData.health = health;
            this.playerData.velocity = velocity;
        }

        /// <summary>
        /// Update only position (for partial updates)
        /// </summary>
        public void UpdatePosition(Vector3 position, long sequenceNumber = 0)
        {
            if (sequenceNumber > 0 && sequenceNumber < lastSequenceNumber)
            {
                return; // Ignore old updates
            }

            if (sequenceNumber > 0)
            {
                lastSequenceNumber = sequenceNumber;
            }

            targetPosition = position;
        }

        /// <summary>
        /// Force snap to position (for full sync)
        /// </summary>
        public void SnapToPosition(Vector3 position)
        {
            transform.position = position;
            targetPosition = position;
            Debug.Log($"[NetworkPlayer {playerId}] Snapped to position: {position}");
        }

        /// <summary>
        /// Update only velocity (for partial updates)
        /// </summary>
        public void UpdateVelocity(Vector3 velocity)
        {
            targetVelocity = velocity;
            this.playerData.velocity = velocity;
        }

        /// <summary>
        /// Update only health (for partial updates)
        /// </summary>
        public void UpdateHealth(float health)
        {
            this.playerData.health = health;
        }

        /// <summary>
        /// Update only speed (for partial updates)
        /// </summary>
        public void UpdateSpeed(float speed)
        {
            this.playerData.speed = speed;
        }


    }
}
