using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    public NetworkVariable<Color> NetColor = new NetworkVariable<Color>(Color.white);
    public NetworkVariable<int> Health = new NetworkVariable<int>(100);
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);
    
    // НОВОЕ: Состояние жизни и патроны
    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(true);
    public NetworkVariable<int> Ammo = new NetworkVariable<int>(10);
    
    public NetworkVariable<FixedString32Bytes> Nickname = new NetworkVariable<FixedString32Bytes>(
        "", 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SubmitNicknameServerRpc(MainMenuUI.PlayerNickname);
        }
    }[ServerRpc]
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{OwnerClientId}" : nickname.Trim();
        Nickname.Value = safeValue;
    }

    [ServerRpc]
    public void RequestRandomColorServerRpc()
    {
        NetColor.Value = new Color(Random.value, Random.value, Random.value);
    }
}