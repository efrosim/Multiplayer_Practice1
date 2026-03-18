using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour, IDamageable
{
    [SerializeField] private PlayerState _playerState;
    [SerializeField] private Renderer _renderer;
    
    public Transform BulletSpawnPoint;
    public GameObject BulletPrefab;
    private Camera _mainCamera;

    private void Start()
    {
        if (IsOwner) _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsOwner) return;
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
            ShootServerRpc(BulletSpawnPoint.position, shootDirection);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 spawnPos, Vector3 direction)
    {
        GameObject bullet = Instantiate(BulletPrefab, spawnPos, Quaternion.LookRotation(direction));
        if (bullet.TryGetComponent(out NetworkBullet bulletScript))
        {
            bulletScript.OwnerId = OwnerClientId;
        }
        bullet.GetComponent<NetworkObject>().Spawn();
    }

    public void TakeDamage(int damage, ulong shooterId)
    {
        if (!IsServer) return;

        _playerState.Health.Value = Mathf.Max(0, _playerState.Health.Value - damage);
        HitFlashClientRpc();

        if (_playerState.Health.Value == 0)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterId, out var shooterClient))
            {
                var shooterState = shooterClient.PlayerObject.GetComponent<PlayerState>();
                if (shooterState != null) shooterState.Score.Value += 1;
            }

            _playerState.Health.Value = 100;
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
        _renderer.material.color = _playerState.NetColor.Value;
    }

    [ClientRpc]
    private void ForceRespawnClientRpc(Vector3 spawnPosition)
    {
        if (IsOwner)
        {
            if (TryGetComponent(out Unity.Netcode.Components.NetworkTransform netTransform))
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