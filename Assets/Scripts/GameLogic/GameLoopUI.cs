using System.Linq;
using FishNet;
using TMPro;
using UnityEngine;

public class GameLoopUI : MonoBehaviour
{
    [Header("Панели")]
    public GameObject LobbyPanel;
    public GameObject GamePanel;
    public GameObject ResultsPanel;[Header("Тексты")]
    public TextMeshProUGUI LobbyText;
    public TextMeshProUGUI TimerText;
    public TextMeshProUGUI ResultsText;

    private void Update()
    {
        // Если GameManager еще не проснулся
        if (GameManager.Instance == null)
        {
            HideAll();
            return;
        }

        // Проверяем, подключились ли мы как клиент (или запустили ли сервер)
        bool isNetworkStarted = false;
        if (InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Started) isNetworkStarted = true;
        if (InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started) isNetworkStarted = true;

        // Если мы просто висим в главном меню — прячем игровой UI
        if (!isNetworkStarted)
        {
            HideAll();
            return;
        }

        // УБРАЛИ ПРОВЕРКУ IsSpawned! Просто читаем стейт напрямую.
        var state = GameManager.Instance.CurrentState.Value;

        if (LobbyPanel != null) LobbyPanel.SetActive(state == GameManager.GameState.WaitingForPlayers);
        if (GamePanel != null) GamePanel.SetActive(state == GameManager.GameState.InProgress);
        if (ResultsPanel != null) ResultsPanel.SetActive(state == GameManager.GameState.ShowingResults);

        if (state == GameManager.GameState.WaitingForPlayers && LobbyText != null)
        {
            LobbyText.text = $"Ожидание игроков: {GameManager.Instance.ConnectedPlayers.Value} / 2";
        }
        else if (state == GameManager.GameState.InProgress && TimerText != null)
        {
            TimerText.text = $"Время: {Mathf.CeilToInt(GameManager.Instance.MatchTimer.Value)}";
        }
        else if (state == GameManager.GameState.ShowingResults && ResultsText != null)
        {
            UpdateResultsText();
        }
    }

    private void HideAll()
    {
        if (LobbyPanel != null && LobbyPanel.activeSelf) LobbyPanel.SetActive(false);
        if (GamePanel != null && GamePanel.activeSelf) GamePanel.SetActive(false);
        if (ResultsPanel != null && ResultsPanel.activeSelf) ResultsPanel.SetActive(false);
    }

    private void UpdateResultsText()
    {
        PlayerState[] players = FindObjectsByType<PlayerState>(FindObjectsSortMode.None);
        var sortedPlayers = players.OrderByDescending(p => p.Score.Value).ToList();

        string res = "РЕЗУЛЬТАТЫ МАТЧА:\n\n";
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            res += $"{i + 1}. {sortedPlayers[i].Nickname.Value} - {sortedPlayers[i].Score.Value} убийств\n";
        }
        if (ResultsText != null) ResultsText.text = res;
    }
}