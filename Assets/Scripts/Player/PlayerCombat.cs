using FishNet.Object;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private PlayerState _playerState;
    public Transform BulletSpawnPoint;
    public GameObject BulletPrefab;
    
    public float Cooldown = 0.4f;
    private float _lastShotTime;
    private Camera _mainCamera;

    public override void OnStartNetwork()
    {
        if (base.Owner.IsLocalClient) _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!base.IsOwner || !_playerState.IsAlive.Value) return;

        if (Input.GetKeyDown(KeyCode.C) && Cursor.lockState == CursorLockMode.Locked)
            _playerState.RequestRandomColorServerRpc();

        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector3 shootDir = Input.GetMouseButton(1) && _mainCamera != null ? _mainCamera.transform.forward : transform.forward;
            ShootServerRpc(BulletSpawnPoint.position, shootDir);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 spawnPos, Vector3 direction)
    {
        if (!_playerState.IsAlive.Value || _playerState.Ammo.Value <= 0 || Time.time < _lastShotTime + Cooldown) return;

        _lastShotTime = Time.time;
        _playerState.Ammo.Value--;

        GameObject bullet = Instantiate(BulletPrefab, spawnPos, Quaternion.LookRotation(direction));
        base.ServerManager.Spawn(bullet, base.Owner); // FishNet сам назначит OwnerId
    }
}