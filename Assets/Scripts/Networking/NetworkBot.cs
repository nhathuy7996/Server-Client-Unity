using UnityEngine;

namespace Networking
{
    /// <summary>
    /// Represents a bot in the game
    /// Handles smooth interpolation of position from server updates
    /// </summary>
    public class NetworkBot : MonoBehaviour
    {
        [Header("Bot Info")]
        public string botId;

        [Header("Bot Stats")]
        public float hp = 100f;
        public float maxHp = 100f;
        public float dmg = 10f;
        public float speed = 3f;

        [Header("Interpolation Settings")]
        [SerializeField] private float positionLerpSpeed = 10f;
        [SerializeField] private float rotationLerpSpeed = 10f;

        [Header("Reconciliation Settings")]
        [SerializeField] private float snapThreshold = 5f; // Ngưỡng để snap thay vì lerp

        private Vector3 targetPosition;
        private Vector3 targetVelocity;
        private Quaternion targetRotation;

        private void Awake()
        {
            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }

        private void Update()
        {
            // Calculate distance error
            float distanceError = Vector3.Distance(transform.position, targetPosition);

            // Nếu lệch quá xa, snap ngay lập tức
            if (distanceError > snapThreshold)
            {
                transform.position = targetPosition;
            }
            // Nếu lệch nhưng chưa quá xa, lerp smooth
            else if (distanceError > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionLerpSpeed);
            }

            // Smooth rotation based on velocity
            if (targetVelocity.magnitude > 0.01f)
            {
                targetRotation = Quaternion.LookRotation(targetVelocity.normalized);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
            }
        }

        /// <summary>
        /// Initialize bot with server data
        /// </summary>
        public void Initialize(string id, Vector3 position, float hp, float dmg, float speed)
        {
            this.botId = id;
            this.hp = hp;
            this.maxHp = hp;
            this.dmg = dmg;
            this.speed = speed;

            transform.position = position;
            targetPosition = position;
        }

        /// <summary>
        /// Update bot state from server
        /// </summary>
        public void UpdateState(Vector3 position, Vector3 velocity)
        {
            targetPosition = position;
            targetVelocity = velocity;
        }

        /// <summary>
        /// Update only position
        /// </summary>
        public void UpdatePosition(Vector3 position)
        {
            targetPosition = position;
        }

        /// <summary>
        /// Update only velocity
        /// </summary>
        public void UpdateVelocity(Vector3 velocity)
        {
            targetVelocity = velocity;
        }

        /// <summary>
        /// Update bot health
        /// </summary>
        public void UpdateHealth(float health)
        {
            this.hp = health;
        }

        /// <summary>
        /// Check if bot is alive
        /// </summary>
        public bool IsAlive()
        {
            return hp > 0;
        }
    }
}
