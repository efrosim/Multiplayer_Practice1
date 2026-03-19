using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private int _requiredPlayers = 2;
    [SerializeField] private float _matchDuration = 60f;

    public enum GameState { WaitingForPlayers, InProgress, ShowingResults }

    public readonly SyncVar<GameState> CurrentState = new SyncVar<GameState>(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncVar<float> MatchTimer = new SyncVar<float>(0f);

    private float _stateTimer = 0f;

    private void Awake() => Instance = this;

    public override void OnStartServer()
    {
        MatchTimer.Value = _matchDuration;
        _stateTimer = 3f; // Задержка перед стартом
    }

    private void Update()
    {
        if (!base.IsServerInitialized) return;

        // Гарантированно актуальное количество игроков каждый кадр
        ConnectedPlayers.Value = base.ServerManager.Clients.Count;

        // Железобетонный State Machine вместо ненадежных Invoke
        switch (CurrentState.Value)
        {
            case GameState.WaitingForPlayers:
                if (ConnectedPlayers.Value >= _requiredPlayers)
                {
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                    {
                        StartMatch();
                    }
                }
                else
                {
                    _stateTimer = 3f; // Сбрасываем таймер, если кто-то вышел
                }
                break;

            case GameState.InProgress:
                MatchTimer.Value -= Time.deltaTime;
                if (MatchTimer.Value <= 0f)
                {
                    EndMatch();
                }
                break;

            case GameState.ShowingResults:
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    ResetToLobby();
                }
                break;
        }
    }

    private void StartMatch()
    {
        CurrentState.Value = GameState.InProgress;
        MatchTimer.Value = _matchDuration;
        ResetAllPlayers();
        Debug.Log("[Server] Match Started!");
    }

    private void EndMatch()
    {
        CurrentState.Value = GameState.ShowingResults;
        _stateTimer = 5f; // Показываем результаты 5 секунд
        Debug.Log("[Server] Match Ended! Showing results...");
    }

    private void ResetToLobby()
    {
        CurrentState.Value = GameState.WaitingForPlayers;
        MatchTimer.Value = _matchDuration;
        _stateTimer = 3f; // Ждем 3 секунды в лобби перед новым стартом
        ResetAllPlayers();
        Debug.Log("[Server] Returned to Lobby.");
    }

    private void ResetAllPlayers()
    {
        foreach (var conn in base.ServerManager.Clients.Values)
        {
            // ИСПРАВЛЕНО: Перебираем ВСЕ объекты коннекта, чтобы точно найти игрока.
            // Раньше conn.FirstObject мог указывать не на игрока, из-за чего 
            // IsAlive не сбрасывался на true и управление не возвращалось!
            foreach (var nob in conn.Objects)
            {
                if (nob.TryGetComponent(out PlayerState state))
                {
                    state.ResetStats();
                }
                if (nob.TryGetComponent(out PlayerMovement movement))
                {
                    movement.Teleport(new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f)));
                }
            }
        }
    }
}