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
        _playerState.Nickname.OnValueChanged += OnNicknameChanged;

        _renderer.material.color = _playerState.NetColor.Value;
        UpdateUI();
    }

    public override void OnNetworkDespawn()
    {
        _playerState.NetColor.OnValueChanged -= OnColorChanged;
        _playerState.Health.OnValueChanged -= OnStatsChanged;
        _playerState.Score.OnValueChanged -= OnStatsChanged;
        _playerState.Nickname.OnValueChanged -= OnNicknameChanged;
    }

    private void OnColorChanged(Color oldColor, Color newColor) => _renderer.material.color = newColor;
    private void OnStatsChanged(int oldValue, int newValue) => UpdateUI();
    private void OnNicknameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue) => UpdateUI();

    private void UpdateUI()
    {
        if (PlayerStatsText != null)
        {
            PlayerStatsText.text = $"{_playerState.Nickname.Value}\nHP: {_playerState.Health.Value}\nScore: {_playerState.Score.Value}";
        }
    }
}