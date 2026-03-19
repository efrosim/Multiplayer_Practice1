using System.Collections;
using FishNet.Object;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour, IDamageable, IHealable
{
    [SerializeField] private PlayerState _playerState;
    [SerializeField] private Renderer _renderer;
    
    private Coroutine _flashCoroutine;

    public override void OnStartNetwork()
    {
        _playerState.NetColor.OnChange += OnColorChanged;
        _playerState.AliveStateChanged += OnAliveChanged;
        
        if (_renderer != null) _renderer.material.color = _playerState.NetColor.Value;
    }

    public override void OnStopNetwork()
    {
        _playerState.NetColor.OnChange -= OnColorChanged;
        _playerState.AliveStateChanged -= OnAliveChanged;
    }

    private void OnColorChanged(Color oldColor, Color newColor, bool asServer)
    {
        // Меняем базовый цвет, только если сейчас не идет мигание урона
        if (_flashCoroutine == null && _renderer != null)
            _renderer.material.color = newColor;
    }

    private void OnAliveChanged(bool isAlive)
    {
        if (_renderer != null) _renderer.enabled = isAlive;
    }

    public void TakeDamage(int damage, int shooterId)
    {
        if (!base.IsServerInitialized || !_playerState.IsAlive.Value) return;

        _playerState.Health.Value = Mathf.Max(0, _playerState.Health.Value - damage);
        HitFlashObserversRpc();

        if (_playerState.Health.Value == 0)
        {
            _playerState.IsAlive.Value = false;

            if (base.ServerManager.Clients.TryGetValue(shooterId, out var shooterClient))
            {
                var shooterState = shooterClient.FirstObject.GetComponent<PlayerState>();
                if (shooterState != null) shooterState.Score.Value += 1;
            }

            StartCoroutine(RespawnRoutine());
        }
    }

    public void Heal(int amount)
    {
        if (!base.IsServerInitialized || !_playerState.IsAlive.Value || _playerState.Health.Value >= 100) return;
        _playerState.Health.Value = Mathf.Min(100, _playerState.Health.Value + amount);
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(3f);

        _playerState.Health.Value = 100;
        _playerState.Ammo.Value = 10;
        
        Vector3 randomSpawnPoint = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
        
        // Делегируем телепортацию компоненту движения
        if (TryGetComponent(out PlayerMovement movement))
        {
            movement.Teleport(randomSpawnPoint);
        }
        
        _playerState.IsAlive.Value = true;
    }

    [ObserversRpc]
    private void HitFlashObserversRpc()
    {
        if (!gameObject.activeInHierarchy) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRedRoutine());
    }

    private IEnumerator FlashRedRoutine()
    {
        if (_renderer != null) _renderer.material.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        if (_renderer != null) _renderer.material.color = _playerState.NetColor.Value;
        _flashCoroutine = null;
    }
}