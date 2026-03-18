using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    public float MoveSpeed = 5f;
    public float DashSpeed = 20f;
    public float DashDuration = 0.2f;
    public float DashCooldown = 1.5f;
    
    private float _dashTimer;
    private float _dashCooldownTimer;
    private Vector3 _dashDirection;
    private Camera _mainCamera;

    private void Start()
    {
        if (IsOwner) _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsOwner) return;
        HandleMovement();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0, vertical).normalized;
        Vector3 moveDir = Vector3.zero;

        if (inputDir.magnitude >= 0.1f && _mainCamera != null)
        {
            Vector3 camForward = _mainCamera.transform.forward;
            Vector3 camRight = _mainCamera.transform.right;
            camForward.y = 0; camRight.y = 0;
            camForward.Normalize(); camRight.Normalize();
            moveDir = camForward * inputDir.z + camRight * inputDir.x;
        }

        if (_dashCooldownTimer > 0) _dashCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.LeftShift) && _dashCooldownTimer <= 0 && moveDir != Vector3.zero && Cursor.lockState == CursorLockMode.Locked)
        {
            _dashTimer = DashDuration;
            _dashCooldownTimer = DashCooldown;
            _dashDirection = moveDir;
        }

        float currentSpeed = MoveSpeed;
        Vector3 finalMovement = Vector3.zero;

        if (_dashTimer > 0)
        {
            _dashTimer -= Time.deltaTime;
            currentSpeed = DashSpeed;
            finalMovement = _dashDirection * currentSpeed;
        }
        else
        {
            finalMovement = moveDir * currentSpeed;
        }

        transform.position += finalMovement * Time.deltaTime;

        if (moveDir != Vector3.zero && _dashTimer <= 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
}