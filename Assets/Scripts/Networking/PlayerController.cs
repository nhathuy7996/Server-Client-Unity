using UnityEngine;

namespace Networking
{
    /// <summary>
    /// Controls the local player movement and sends updates to server
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Network Settings")]
        [SerializeField] private float positionUpdateRate = 0.05f; // 20 times per second

        private CharacterController characterController;
        private Vector3 velocity;
        private Vector3 lastSentPosition;
        private Vector3 lastSentVelocity;
        private float lastUpdateTime;

        [Header("Player Stats")]
        public int playerId = -1;
        public float health = 100f;

        [Header("Visual")]
        [SerializeField] private Renderer playerRenderer;
        [SerializeField] private Color localPlayerColor = Color.green;

        public bool IsLocalPlayer { get; private set; }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (playerRenderer != null)
            {
                playerRenderer.material.color = localPlayerColor;
            }
        }

        private void Start()
        {
            lastSentPosition = transform.position;
            lastUpdateTime = Time.time;
        }

        private void Update()
        {
            if (!IsLocalPlayer) return;

            HandleMovement();
            HandleRotation();
            SendPositionUpdate();
        }

        /// <summary>
        /// Initialize as local player
        /// </summary>
        public void InitializeAsLocalPlayer(int id)
        {
            playerId = id;
            IsLocalPlayer = true;

            if (playerRenderer != null)
            {
                playerRenderer.material.color = localPlayerColor;
            }
        }

        /// <summary>
        /// Handle player movement input
        /// </summary>
        private void HandleMovement()
        {
            // Get input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // Calculate movement direction
            Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

            // Apply movement
            if (moveDirection.magnitude >= 0.1f)
            {
                velocity.x = moveDirection.x * moveSpeed;
                velocity.z = moveDirection.z * moveSpeed;
            }
            else
            {
                velocity.x = 0;
                velocity.z = 0;
            }

            // Apply gravity
            if (characterController.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
            }

            // Move character
            characterController.Move(velocity * Time.deltaTime);
        }

        /// <summary>
        /// Handle player rotation based on movement
        /// </summary>
        private void HandleRotation()
        {
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);

            if (horizontalVelocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        /// <summary>
        /// Send position update to server at regular intervals
        /// </summary>
        private void SendPositionUpdate()
        {
            // Check if enough time has passed since last update
            if (Time.time - lastUpdateTime < positionUpdateRate) return;

            // Check if position or velocity has changed significantly
            float positionDelta = Vector3.Distance(transform.position, lastSentPosition);
            float velocityDelta = Vector3.Distance(velocity, lastSentVelocity);

            if (positionDelta > 0.01f || velocityDelta > 0.01f)
            {
                // Send update to server
                NetworkManager manager = NetworkManager.Instant;
                if (manager != null)
                {
                    manager.SendPlayerPosition(transform.position, velocity);
                }

                lastSentPosition = transform.position;
                lastSentVelocity = velocity;
                lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Get current velocity
        /// </summary>
        public Vector3 GetVelocity()
        {
            return velocity;
        }

        /// <summary>
        /// Set player color
        /// </summary>
        public void SetColor(Color color)
        {
            localPlayerColor = color;
            if (playerRenderer != null)
            {
                playerRenderer.material.color = color;
            }
        }
    }
}
