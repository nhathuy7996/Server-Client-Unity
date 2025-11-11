using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamControl : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] public Transform target; // Player transform

    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -7); // Khoảng cách camera với player
    [SerializeField] private float followSpeed = 5f; // Tốc độ follow
    [SerializeField] private bool useFixedUpdate = true; // Sử dụng FixedUpdate cho smooth hơn

    [Header("Look Settings")]
    [SerializeField] private bool lookAtTarget = true; // Camera có nhìn về player không
    [SerializeField] private float lookSpeed = 3f; // Tốc độ xoay camera

    private Vector3 velocity = Vector3.zero; // Cho SmoothDamp

    // Start is called before the first frame update
    void Start()
    {
        // Tự động tìm player nếu chưa assign
        if (target == null)
        {
            PlayerMove player = FindObjectOfType<PlayerMove>();
            if (player != null)
                target = player.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!useFixedUpdate)
            FollowTarget();
    }

    void FixedUpdate()
    {
        if (useFixedUpdate)
            FollowTarget();
    }

    void FollowTarget()
    {
        if (target == null) return;

        // Tính vị trí target cho camera
        Vector3 targetPosition = target.position + offset;

        // Di chuyển camera mượt mà đến vị trí target
        if (followSpeed > 0)
        {
            // Sử dụng SmoothDamp cho movement tự nhiên hơn
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / followSpeed);
        }
        else
        {
            // Di chuyển trực tiếp (không smooth)
            transform.position = targetPosition;
        }

        // Xoay camera nhìn về player nếu được bật
        if (lookAtTarget)
        {
            Vector3 lookDirection = target.position - transform.position;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                if (lookSpeed > 0)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookSpeed * Time.deltaTime);
                }
                else
                {
                    transform.rotation = targetRotation;
                }
            }
        }
    }

    // Method để thay đổi offset từ script khác
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    // Method để thay đổi target
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
