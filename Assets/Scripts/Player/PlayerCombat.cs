using Unity.Netcode;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private PlayerState _playerState;
    public Transform BulletSpawnPoint;
    public GameObject BulletPrefab;
    
    public float Cooldown = 0.4f;
    private float _lastShotTime;
    private Camera _mainCamera;

    private void Start()
    {
        if (IsOwner) _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsOwner || !_playerState.IsAlive.Value) return; // Мертвые не стреляют
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C) && Cursor.lockState == CursorLockMode.Locked)
        {
            _playerState.RequestRandomColorServerRpc();
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector3 shootDirection = Input.GetMouseButton(1) && _mainCamera != null 
                ? _mainCamera.transform.forward 
                : transform.forward;
                
            // Передаем запрос на сервер
            ShootServerRpc(BulletSpawnPoint.position, shootDirection);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 spawnPos, Vector3 direction, ServerRpcParams rpcParams = default)
    {
        // СЕРВЕРНАЯ ВАЛИДАЦИЯ
        if (!_playerState.IsAlive.Value) return; // 1. Жив ли?
        if (_playerState.Ammo.Value <= 0) return; // 2. Есть ли патроны?
        if (Time.time < _lastShotTime + Cooldown) return; // 3. Прошел ли кулдаун?

        _lastShotTime = Time.time;
        _playerState.Ammo.Value--; // Тратим патрон

        GameObject bullet = Instantiate(BulletPrefab, spawnPos, Quaternion.LookRotation(direction));
        if (bullet.TryGetComponent(out NetworkBullet bulletScript))
        {
            bulletScript.OwnerId = rpcParams.Receive.SenderClientId;
        }
        bullet.GetComponent<NetworkObject>().SpawnWithOwnership(rpcParams.Receive.SenderClientId);
    }
}