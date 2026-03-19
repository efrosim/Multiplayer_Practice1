using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerUI : NetworkBehaviour
{
    [SerializeField] private PlayerState _playerState;
    [SerializeField] private Renderer _renderer;
    public TextMeshProUGUI PlayerStatsText;

    public override void OnNetworkSpawn()
    {
        _playerState.NetColor.OnValueChanged += OnColorChanged;
        _playerState.Health.OnValueChanged += OnStatsChanged;
        _playerState.Score.OnValueChanged += OnStatsChanged;
        _playerState.Ammo.OnValueChanged += OnStatsChanged;
        _playerState.Nickname.OnValueChanged += OnNicknameChanged;
        _playerState.IsAlive.OnValueChanged += OnAliveChanged;

        _renderer.material.color = _playerState.NetColor.Value;
        UpdateUI();
    }

    public override void OnNetworkDespawn()
    {
        _playerState.NetColor.OnValueChanged -= OnColorChanged;
        _playerState.Health.OnValueChanged -= OnStatsChanged;
        _playerState.Score.OnValueChanged -= OnStatsChanged;
        _playerState.Ammo.OnValueChanged -= OnStatsChanged;
        _playerState.Nickname.OnValueChanged -= OnNicknameChanged;
        _playerState.IsAlive.OnValueChanged -= OnAliveChanged;
    }

    private void OnColorChanged(Color oldColor, Color newColor) => _renderer.material.color = newColor;
    private void OnStatsChanged(int oldValue, int newValue) => UpdateUI();
    private void OnNicknameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue) => UpdateUI();

    private void OnAliveChanged(bool oldVal, bool isAlive)
    {
        _renderer.enabled = isAlive; // Скрываем/показываем модель
        UpdateUI();
        
        if (!isAlive && IsOwner) StartCoroutine(RespawnTimerRoutine());
    }

    private void UpdateUI()
    {
        if (PlayerStatsText != null)
        {
            if (_playerState.IsAlive.Value)
            {
                PlayerStatsText.text = $"{_playerState.Nickname.Value}\nHP: {_playerState.Health.Value} | Ammo: {_playerState.Ammo.Value}\nScore: {_playerState.Score.Value}";
            }
        }
    }

    private IEnumerator RespawnTimerRoutine()
    {
        int timer = 3;
        while (timer > 0 && !_playerState.IsAlive.Value)
        {
            PlayerStatsText.text = $"DEAD\nRespawning in {timer}...";
            yield return new WaitForSeconds(1f);
            timer--;
        }
    }
}