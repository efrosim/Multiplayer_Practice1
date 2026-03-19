using System.Collections;
using Unity.Netcode;
using UnityEngine;

// Класс отвечает ТОЛЬКО за здоровье, смерть и возрождение (Single Responsibility)
public class PlayerHealth : NetworkBehaviour, IDamageable, IHealable
{
    [SerializeField] private PlayerState _playerState;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private CharacterController _characterController;

    public void TakeDamage(int damage, ulong shooterId)
    {
        if (!IsServer || !_playerState.IsAlive.Value) return;

        _playerState.Health.Value = Mathf.Max(0, _playerState.Health.Value - damage);
        HitFlashClientRpc();

        if (_playerState.Health.Value == 0)
        {
            _playerState.IsAlive.Value = false; // Игрок умирает

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterId, out var shooterClient))
            {
                var shooterState = shooterClient.PlayerObject.GetComponent<PlayerState>();
                if (shooterState != null) shooterState.Score.Value += 1;
            }

            StartCoroutine(RespawnRoutine());
        }
    }

    public void Heal(int amount)
    {
        if (!IsServer || !_playerState.IsAlive.Value || _playerState.Health.Value >= 100) return;
        _playerState.Health.Value = Mathf.Min(100, _playerState.Health.Value + amount);
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(3f); // Ждем 3 секунды на сервере

        _playerState.Health.Value = 100;
        _playerState.Ammo.Value = 10; // Восстанавливаем патроны при респавне
        
        Vector3 randomSpawnPoint = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
        ForceRespawnClientRpc(randomSpawnPoint);
        
        _playerState.IsAlive.Value = true; // Игрок снова жив
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
    }[ClientRpc]
    private void ForceRespawnClientRpc(Vector3 spawnPosition)
    {
        // CharacterController блокирует телепортацию, его нужно выключить на кадр
        if (_characterController != null) _characterController.enabled = false;
        transform.position = spawnPosition;
        if (_characterController != null) _characterController.enabled = true;
    }
}