using System.Collections;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerUI : NetworkBehaviour
{
    [SerializeField] private PlayerState _playerState;
    public TextMeshProUGUI PlayerStatsText;

    public override void OnStartNetwork()
    {
        _playerState.UIUpdateNeeded += UpdateUI;
        _playerState.AliveStateChanged += OnAliveChanged;

        UpdateUI();
    }

    public override void OnStopNetwork()
    {
        _playerState.UIUpdateNeeded -= UpdateUI;
        _playerState.AliveStateChanged -= OnAliveChanged;
    }

    private void OnAliveChanged(bool isAlive)
    {
        UpdateUI();
        if (!isAlive && base.IsOwner) StartCoroutine(RespawnTimerRoutine());
    }

    private void UpdateUI()
    {
        if (PlayerStatsText != null && _playerState.IsAlive.Value)
        {
            PlayerStatsText.text = $"{_playerState.Nickname.Value}\nHP: {_playerState.Health.Value} | Ammo: {_playerState.Ammo.Value}\nScore: {_playerState.Score.Value}";
        }
    }

    private IEnumerator RespawnTimerRoutine()
    {
        int timer = 3;
        while (timer > 0 && !_playerState.IsAlive.Value)
        {
            if (PlayerStatsText != null) 
                PlayerStatsText.text = $"DEAD\nRespawning in {timer}...";
            
            yield return new WaitForSeconds(1f);
            timer--;
        }
    }
}