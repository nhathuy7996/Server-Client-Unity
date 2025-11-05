using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HuynnLib;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Rigidbody rb;

    private Vector3 moveDirection;

    // Start is called before the first frame update
    void Start()
    {
        // Tự động lấy Rigidbody nếu không được assign trong Inspector
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // Đọc input từ joystick
        HandleInput();
    }

    void FixedUpdate()
    {
        // Di chuyển player bằng velocity trong FixedUpdate để đảm bảo physics smooth
        MovePlayer();
        // Xoay player theo hướng di chuyển
        RotatePlayer();
    }

    void HandleInput()
    {
        // Lấy hướng di chuyển từ joystick (chuyển từ Vector2 sang Vector3)
        Vector2 joyInput = JoyStick.Instant.GetJoyVector();
        moveDirection = new Vector3(joyInput.x, 0, joyInput.y);
    }

    void MovePlayer()
    {
        // Áp dụng velocity cho Rigidbody 3D (giữ nguyên Y velocity để không ảnh hưởng gravity)
        Vector3 velocity = new Vector3(moveDirection.x * moveSpeed, rb.velocity.y, moveDirection.z * moveSpeed);
        rb.velocity = velocity;

        if (moveDirection.magnitude > 0.5f)
        {
            Networking.NetworkingPeer.Instant.EmmitEvent("player:onMove",
                "{\"position\":{\"x\":" + transform.position.x + ",\"y\":" + transform.position.y + ",\"z\":" + transform.position.z + "} }");
        }
    }

    void RotatePlayer()
    {
        // Chỉ xoay khi player đang di chuyển
        if (moveDirection.magnitude > 0.1f)
        {
            // Tính toán hướng cần xoay (loại bỏ component Y)
            Vector3 lookDirection = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;

            // Tạo rotation target từ hướng di chuyển
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            // Xoay mượt mà đến target rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    // Getter method cho AnimController
    public float GetMoveSpeed()
    {
        return moveDirection.magnitude;
    }

    public Vector3 GetMoveDirection()
    {
        return moveDirection;
    }
}
