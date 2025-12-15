using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    Vector3 _moveDirection;

    Rigidbody _rb;
    PlayerData _playerData;

    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _playerData = GetComponent<PlayerData>();
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();

    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void HandleInput()
    {
        Vector2 joystickInput = JoyStick.Instant.GetJoyVector();
        _moveDirection = new Vector3(joystickInput.x, 0, joystickInput.y); ;
    }

    void MovePlayer()
    {
        Vector3 velocity = _moveDirection * moveSpeed;
        velocity.y = _rb.velocity.y; // Preserve vertical velocity (e.g., gravity)

        _playerData.Velocity = velocity;
        _rb.velocity = velocity;
    }
}
