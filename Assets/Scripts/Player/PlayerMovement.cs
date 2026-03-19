using Unity.Netcode;
using UnityEngine;[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private PlayerState _playerState;
    private CharacterController _cc;
    
    public float MoveSpeed = 5f;
    public float Gravity = -9.81f;
    private float _verticalVelocity;
    private Camera _mainCamera;

    private void Awake() => _cc = GetComponent<CharacterController>();

    private void Start()
    {
        if (IsOwner) _mainCamera = Camera.main;
    }

    private void Update()
    {
        // Двигается только владелец и только если жив
        if (!IsOwner || !_playerState.IsAlive.Value) return;
        HandleMovement();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;
        Vector3 moveDir = Vector3.zero;

        if (inputDir.magnitude >= 0.1f && _mainCamera != null)
        {
            Vector3 camForward = _mainCamera.transform.forward;
            Vector3 camRight = _mainCamera.transform.right;
            camForward.y = 0; camRight.y = 0;
            camForward.Normalize(); camRight.Normalize();
            moveDir = camForward * inputDir.z + camRight * inputDir.x;
        }

        _verticalVelocity += Gravity * Time.deltaTime;
        Vector3 finalMovement = moveDir * MoveSpeed;
        finalMovement.y = _verticalVelocity;

        _cc.Move(finalMovement * Time.deltaTime);

        if (_cc.isGrounded) _verticalVelocity = -2f; // Прилипание к земле

        if (moveDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
}