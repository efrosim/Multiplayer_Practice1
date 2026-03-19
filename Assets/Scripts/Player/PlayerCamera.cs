using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    public float MouseSensitivity = 3f;
    public float NormalCameraDistance = 5f;
    public float AimCameraDistance = 2f;
    public Vector2 PitchMinMax = new Vector2(-40, 85);
    
    private Camera _mainCamera;
    private float _yaw;
    private float _pitch;
    private float _currentCameraDistance;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false; // Полностью отключаем скрипт для чужих игроков
            return;
        }

        _mainCamera = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _yaw = transform.eulerAngles.y;
        _currentCameraDistance = NormalCameraDistance;
    }

    private void Update()
    {
        HandleCursorLock();
        HandleCameraInput();
    }

    private void LateUpdate()
    {
        if (_mainCamera == null) return;

        Vector3 targetCenter = transform.position + Vector3.up * 1.5f;
        Quaternion camRotation = Quaternion.Euler(_pitch, _yaw, 0);
        bool isAiming = Input.GetMouseButton(1) && Cursor.lockState == CursorLockMode.Locked;
        
        float targetDistance = isAiming ? AimCameraDistance : NormalCameraDistance;
        _currentCameraDistance = Mathf.Lerp(_currentCameraDistance, targetDistance, Time.deltaTime * 10f);

        Vector3 camPosition = targetCenter - camRotation * Vector3.forward * _currentCameraDistance;
        if (isAiming) camPosition += camRotation * Vector3.right * 0.8f;

        _mainCamera.transform.position = camPosition;
        _mainCamera.transform.rotation = camRotation;
    }

    private void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }
    }

    private void HandleCameraInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        _yaw += Input.GetAxis("Mouse X") * MouseSensitivity;
        _pitch -= Input.GetAxis("Mouse Y") * MouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, PitchMinMax.x, PitchMinMax.y);
    }
}