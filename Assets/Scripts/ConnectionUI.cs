using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionUI : MonoBehaviour
{[Header("UI Элементы")]
    public TMP_InputField NicknameInput; // Поле для ввода ника
    public Button HostButton;
    public Button ClientButton;
    public GameObject MenuPanel; // Панель меню, чтобы скрыть её после старта

    // Статическая переменная, чтобы игрок мог забрать свой ник при спавне
    public static string PlayerNickname { get; private set; } = "Player";

    private void Start()
    {
        // Привязываем кнопки к методам
        HostButton.onClick.AddListener(StartAsHost);
        ClientButton.onClick.AddListener(StartAsClient);
    }

    public void StartAsHost()
    {
        SaveNickname();
        NetworkManager.Singleton.StartHost();
        MenuPanel.SetActive(false); // Прячем меню
    }

    public void StartAsClient()
    {
        SaveNickname();
        NetworkManager.Singleton.StartClient();
        MenuPanel.SetActive(false); // Прячем меню
    }

    private void SaveNickname()
    {
        // Если поле пустое, задаем стандартное имя
        string rawValue = NicknameInput != null ? NicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
    }
}