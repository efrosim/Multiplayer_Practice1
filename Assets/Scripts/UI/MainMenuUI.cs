using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Элементы")]
    public TMP_InputField NicknameInput;
    public Button HostButton;
    public Button ClientButton;
    public GameObject MenuPanel;
    
    public static string PlayerNickname { get; private set; } = "Player";

    private void Start()
    {
        HostButton.onClick.AddListener(StartAsHost);
        ClientButton.onClick.AddListener(StartAsClient);
    }

    public void StartAsHost()
    {
        SaveNickname();
        // В FishNet Хост = Сервер + Клиент
        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();
        MenuPanel.SetActive(false);
    }

    public void StartAsClient()
    {
        SaveNickname();
        InstanceFinder.ClientManager.StartConnection();
        MenuPanel.SetActive(false);
    }

    private void SaveNickname()
    {
        string rawValue = NicknameInput != null ? NicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
    }
}