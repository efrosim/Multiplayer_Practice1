using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    // Новый синтаксис FishNet V4
    public readonly SyncVar<Color> NetColor = new SyncVar<Color>(Color.white);
    public readonly SyncVar<int> Health = new SyncVar<int>(100);
    public readonly SyncVar<int> Score = new SyncVar<int>(0);
    public readonly SyncVar<int> Ammo = new SyncVar<int>(10);
    public readonly SyncVar<string> Nickname = new SyncVar<string>("");
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);

    public event Action UIUpdateNeeded;
    public event Action<bool> AliveStateChanged;

    private void Awake()
    {
        // Подписываемся на изменения
        NetColor.OnChange += OnColorChanged;
        Health.OnChange += OnStateChanged;
        Score.OnChange += OnStateChanged;
        Ammo.OnChange += OnStateChanged;
        Nickname.OnChange += OnStateChanged;
        IsAlive.OnChange += OnAliveChanged;
    }

    public override void OnStartNetwork()
    {
        if (base.Owner.IsLocalClient)
        {
            SetNicknameServerRpc(MainMenuUI.PlayerNickname);
        }
    }

    [ServerRpc]
    private void SetNicknameServerRpc(string nickname)
    {
        Nickname.Value = string.IsNullOrWhiteSpace(nickname) ? $"Player_{base.OwnerId}" : nickname.Trim();
    }

    [ServerRpc]
    public void RequestRandomColorServerRpc()
    {
        NetColor.Value = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
    }

    private void OnColorChanged(Color oldVal, Color newVal, bool asServer) => UIUpdateNeeded?.Invoke();
    private void OnStateChanged(int oldVal, int newVal, bool asServer) => UIUpdateNeeded?.Invoke();
    private void OnStateChanged(string oldVal, string newVal, bool asServer) => UIUpdateNeeded?.Invoke();
    
    private void OnAliveChanged(bool oldVal, bool newVal, bool asServer)
    {
        UIUpdateNeeded?.Invoke();
        AliveStateChanged?.Invoke(newVal);
    }
}