using System.Collections;
using Unity.Collections; // Нужно для FixedString32Bytes
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using TMPro;

public class PlayerNetworkTest : NetworkBehaviour
{[Header("Сетевые переменные")]
    private NetworkVariable<Color> _netColor = new NetworkVariable<Color>(Color.white);
    public NetworkVariable<int> Health = new NetworkVariable<int>(100);
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);
    
    // НОВОЕ: Сетевая переменная для ника
    public NetworkVariable<FixedString32Bytes> Nickname = new NetworkVariable<FixedString32Bytes>(
        "", 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    [Header("Ссылки")]
    private Renderer _renderer;
    public TextMeshProUGUI PlayerStatsText;
    public Transform BulletSpawnPoint;
    public GameObject BulletPrefab;

    [Header("Настройки камеры и управления")]
    public float MouseSensitivity = 3f;
    public float NormalCameraDistance = 5f;
    public float AimCameraDistance = 2f;
    public Vector2 PitchMinMax = new Vector2(-40, 85);
    
    private Camera _mainCamera;
    private float _yaw;
    private float _pitch;
    private float _currentCameraDistance;

    [Header("Движение")]
    public float MoveSpeed = 5f;[Header("Рывок (Dash)")]
    public float DashSpeed = 20f;
    public float DashDuration = 0.2f;
    public float DashCooldown = 1.5f;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private Vector3 _dashDirection;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        _netColor.OnValueChanged += OnColorChanged;
        Health.OnValueChanged += OnStatsChanged;
        Score.OnValueChanged += OnStatsChanged;
        Nickname.OnValueChanged += OnNicknameChanged; // Подписка на смену ника

        _renderer.material.color = _netColor.Value;
        UpdateUI();

        if (IsOwner)
        {
            // НОВОЕ: Отправляем наш ник из меню на сервер
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);

            _mainCamera = Camera.main;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _yaw = transform.eulerAngles.y;
            _currentCameraDistance = NormalCameraDistance;
        }
    }

    public override void OnNetworkDespawn()
    {
        _netColor.OnValueChanged -= OnColorChanged;
        Health.OnValueChanged -= OnStatsChanged;
        Score.OnValueChanged -= OnStatsChanged;
        Nickname.OnValueChanged -= OnNicknameChanged;
    }

    // НОВОЕ: Серверный метод для установки ника
    [ServerRpc]
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{OwnerClientId}" : nickname.Trim();
        Nickname.Value = safeValue;
    }

    private void OnColorChanged(Color oldColor, Color newColor) => _renderer.material.color = newColor;
    private void OnStatsChanged(int oldValue, int newValue) => UpdateUI();
    private void OnNicknameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue) => UpdateUI();

    private void UpdateUI()
    {
        if (PlayerStatsText != null)
        {
            // Теперь выводим реальный никнейм
            PlayerStatsText.text = $"{Nickname.Value}\nHP: {Health.Value}\nScore: {Score.Value}";
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleCursorLock();
        HandleCameraInput();
        HandleMovement();
        HandleInput();
    }

    private void LateUpdate()
    {
        if (!IsOwner || _mainCamera == null) return;

        Vector3 targetCenter = transform.position + Vector3.up * 1.5f;
        Quaternion camRotation = Quaternion.Euler(_pitch, _yaw, 0);

        bool isAiming = Input.GetMouseButton(1) && Cursor.lockState == CursorLockMode.Locked;
        
        float targetDistance = isAiming ? AimCameraDistance : NormalCameraDistance;
        _currentCameraDistance = Mathf.Lerp(_currentCameraDistance, targetDistance, Time.deltaTime * 10f);

        Vector3 camPosition = targetCenter - camRotation * Vector3.forward * _currentCameraDistance;

        if (isAiming)
        {
            camPosition += camRotation * Vector3.right * 0.8f;
        }

        _mainCamera.transform.position = camPosition;
        _mainCamera.transform.rotation = camRotation;
    }

    private void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void HandleCameraInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        _yaw += Input.GetAxis("Mouse X") * MouseSensitivity;
        _pitch -= Input.GetAxis("Mouse Y") * MouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, PitchMinMax.x, PitchMinMax.y);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0, vertical).normalized;

        Vector3 moveDir = Vector3.zero;

        if (inputDir.magnitude >= 0.1f)
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

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C) && Cursor.lockState == CursorLockMode.Locked)
        {
            RequestRandomColorServerRpc();
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector3 shootDirection = Input.GetMouseButton(1) ? _mainCamera.transform.forward : transform.forward;
            ShootServerRpc(BulletSpawnPoint.position, shootDirection);
        }
    }

    [ServerRpc]
    private void RequestRandomColorServerRpc()
    {
        _netColor.Value = new Color(Random.value, Random.value, Random.value);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 spawnPos, Vector3 direction)
    {
        GameObject bullet = Instantiate(BulletPrefab, spawnPos, Quaternion.LookRotation(direction));
        BulletNetwork bulletScript = bullet.GetComponent<BulletNetwork>();
        bulletScript.OwnerId = OwnerClientId;
        bullet.GetComponent<NetworkObject>().Spawn();
    }

    public void TakeDamage(int damage, ulong shooterId)
    {
        if (!IsServer) return;

        // ИСПРАВЛЕНИЕ: Ограничиваем здоровье снизу нулем (как просят в задании)
        Health.Value = Mathf.Max(0, Health.Value - damage);
        
        HitFlashClientRpc();

        // Проверяем смерть именно по нулю
        if (Health.Value == 0)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterId, out var shooterClient))
            {
                var shooter = shooterClient.PlayerObject.GetComponent<PlayerNetworkTest>();
                if (shooter != null) shooter.Score.Value += 1;
            }

            Health.Value = 100;
            Vector3 randomSpawnPoint = new Vector3(Random.Range(-5f, 5f), transform.position.y, Random.Range(-5f, 5f));
            ForceRespawnClientRpc(randomSpawnPoint);
        }
    }

    [ClientRpc]
    private void HitFlashClientRpc()
    {
        StartCoroutine(FlashRedRoutine());
    }

    private IEnumerator FlashRedRoutine()
    {
        _renderer.material.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        _renderer.material.color = _netColor.Value;
    }

    [ClientRpc]
    private void ForceRespawnClientRpc(Vector3 spawnPosition)
    {
        if (IsOwner)
        {
            if (TryGetComponent(out NetworkTransform netTransform))
            {
                netTransform.Teleport(spawnPosition, transform.rotation, transform.localScale);
            }
            else
            {
                transform.position = spawnPosition;
            }
        }
    }
}