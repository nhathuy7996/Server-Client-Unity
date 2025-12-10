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

        private Vector3 targetPosition;
        private Vector3 targetVelocity;
        private Quaternion targetRotation;


        private void Awake()
        {
            targetPosition = transform.position;
            targetRotation = transform.rotation;

            this.playerData = GetComponentInParent<PlayerData>();

        }

        private void Update()
        {
            // Smooth interpolation to target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionLerpSpeed);

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
        public void UpdateState(Vector3 position, Vector3 velocity, float health)
        {
            targetPosition = position;
            targetVelocity = velocity;
            this.playerData.health = health;
            this.playerData.velocity = velocity;
        }

        /// <summary>
        /// Update only position (for partial updates)
        /// </summary>
        public void UpdatePosition(Vector3 position)
        {
            targetPosition = position;
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
