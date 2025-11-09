using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;

    [Header("Transition Settings")]
    [SerializeField] private float crossFadeDuration = 0.2f;
    [SerializeField] private float moveThreshold = 0.1f; // Ngưỡng để xác định đang di chuyển 

    [Header("Movement References")]
    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private Rigidbody rb;

    [Header("Animation Override")]
    [SerializeField] private AnimatorOverrideController overrideController;
    private RuntimeAnimatorController originalController;

    // Animation states
    public enum AnimState
    {
        Idle,
        Attack,
        Run
    }

    private AnimState currentState = AnimState.Idle;
    private float currentSpeed;

    // Start is called before the first frame update
    void Start()
    {
        // Tự động tìm các component nếu chưa assign
        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerMove == null)
            playerMove = GetComponent<PlayerMove>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        // Kiểm tra animator có tồn tại không
        if (animator == null)
        {
            Debug.LogError("Animator component not found on " + gameObject.name);
            enabled = false;
            return;
        }

        // Lưu trữ original controller để có thể khôi phục
        originalController = animator.runtimeAnimatorController;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAnimationState();
    }

    void UpdateAnimationState()
    {
        // Tính tốc độ di chuyển hiện tại
        CalculateMovementSpeed();

        // Xác định state animation cần thiết
        AnimState targetState = DetermineAnimationState();

        // Chuyển đổi animation nếu cần
        if (targetState != currentState)
        {
            ChangeAnimationState(targetState);
        }
    }

    void CalculateMovementSpeed()
    {
        if (rb != null)
        {
            // Sử dụng velocity của rigidbody để tính tốc độ
            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            currentSpeed = horizontalVelocity.magnitude;
        }
        else
        {
            // Fallback: sử dụng PlayerMove nếu có
            currentSpeed = 0f;
        }
    }

    AnimState DetermineAnimationState()
    {
        if (currentSpeed < moveThreshold)
        {
            return AnimState.Idle;
        }
        else
        {
            return AnimState.Run;
        }
    }

    void ChangeAnimationState(AnimState newState)
    {
        // Sử dụng CrossFade để chuyển đổi mượt mà
        animator.CrossFade(newState.ToString(), crossFadeDuration);
        currentState = newState;
    }

    public void SetCustomAnimation(string animationName, float fadeDuration = -1f)
    {
        float duration = fadeDuration >= 0 ? fadeDuration : crossFadeDuration;
        animator.CrossFade(animationName, duration);
    }

    /// <summary>
    /// Thay đổi một clip cụ thể theo tên state
    /// </summary>
    /// <param name="stateName">Tên của animation state</param>
    /// <param name="newClip">Clip mới để thay thế</param>
    public void OverrideSpecificClip(AnimState animStateName, AnimationClip newClip)
    {
        if (newClip == null)
        {
            Debug.LogWarning("New animation clip is null!");
            return;
        }

        // Tạo AnimatorOverrideController nếu chưa có
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(originalController);
        }

        // Lấy danh sách override hiện tại
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        // Tìm và thay thế clip theo tên
        bool found = false;
        for (int i = 0; i < overrides.Count; i++)
        {
            if (overrides[i].Key.name == animStateName.ToString() || overrides[i].Key.name.ToLower().Contains(animStateName.ToString().ToLower()))
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, newClip);
                found = true;
                Debug.Log($"Override {animStateName} clip: {overrides[i].Key.name} -> {newClip.name}");
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning($"Animation state '{animStateName}' not found in animator controller!");
            return;
        }

        // Áp dụng override
        overrideController.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = overrideController;
    }

    /// <summary>
    /// Khôi phục về animator controller gốc
    /// </summary>
    public void RestoreOriginalController()
    {
        if (originalController != null)
        {
            animator.runtimeAnimatorController = originalController;
            overrideController = null;
            Debug.Log("Restored to original animator controller");
        }
    }

    /// <summary>
    /// Lấy danh sách tất cả animation clips hiện tại
    /// </summary>
    /// <returns>Array các AnimationClip</returns>
    public AnimationClip[] GetAllAnimationClips()
    {
        if (animator.runtimeAnimatorController != null)
        {
            return animator.runtimeAnimatorController.animationClips;
        }
        return new AnimationClip[0];
    }

    /// <summary>
    /// Kiểm tra xem có đang sử dụng override controller không
    /// </summary>
    /// <returns>True nếu đang sử dụng override</returns>
    public bool IsUsingOverrideController()
    {
        return overrideController != null && animator.runtimeAnimatorController == overrideController;
    }

}
