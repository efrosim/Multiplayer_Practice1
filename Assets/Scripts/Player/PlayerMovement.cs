using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

public struct MoveData : IReplicateData
{
    public float Horizontal;
    public float Vertical;
    public Vector3 CamForward;
    public Vector3 CamRight;
    public bool Dash;

    private uint _tick;
    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}

public struct ReconcileData : IReconcileData
{
    public Vector3 Position;
    public Quaternion Rotation;
    public float VerticalVelocity;
    public float DashTimer;

    private uint _tick;
    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private PlayerState _playerState;
    private CharacterController _cc;
    
    public float MoveSpeed = 5f;
    public float DashSpeed = 20f;
    public float DashDuration = 0.2f;
    public float Gravity = -9.81f;

    private float _verticalVelocity;
    private float _dashTimer;
    private Vector3 _dashDirection;
    private Camera _mainCamera;

    private void Awake() => _cc = GetComponent<CharacterController>();

    public override void OnStartNetwork()
    {
        if (base.IsServerInitialized)
        {
            Vector3 randomSpawn = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
            Teleport(randomSpawn);
        }

        if (base.Owner.IsLocalClient) 
        {
            _mainCamera = Camera.main;
        }
        else if (!base.IsServerInitialized)
        {
            // Отключаем физику для чужих игроков на клиенте, 
            // чтобы NetworkTransform мог плавно их двигать без сопротивления CharacterController
            _cc.enabled = false;
        }
        
        base.TimeManager.OnTick += OnTick;
        base.TimeManager.OnPostTick += OnPostTick;
    }

    public void Teleport(Vector3 position)
    {
        bool wasEnabled = _cc.enabled;
        _cc.enabled = false;
        transform.position = position;
        _cc.enabled = wasEnabled;
        
        if (base.IsServerInitialized) 
            TeleportObserversRpc(position);
    }[ObserversRpc]
    private void TeleportObserversRpc(Vector3 position)
    {
        if (base.IsServerInitialized) return; 
        
        bool wasEnabled = _cc.enabled;
        _cc.enabled = false;
        transform.position = position;
        _cc.enabled = wasEnabled;
    }

    public override void OnStopNetwork()
    {
        if (base.TimeManager != null) 
        {
            base.TimeManager.OnTick -= OnTick;
            base.TimeManager.OnPostTick -= OnPostTick;
        }
    }

    private void OnTick()
    {
        // ИСПРАВЛЕНО: Только владелец отправляет инпуты.
        // Обозреватели НЕ ДОЛЖНЫ вызывать Replicate(default), иначе они будут стоять на месте!
        if (base.IsOwner)
        {
            Replicate(BuildMoveData());
        }
    }

    private void OnPostTick()
    {
        // ИСПРАВЛЕНО: Только сервер должен создавать данные для сверки
        if (base.IsServerInitialized)
        {
            CreateReconcile();
        }
    }

    public override void CreateReconcile()
    {
        ReconcileData rd = new ReconcileData
        {
            Position = transform.position,
            Rotation = transform.rotation,
            VerticalVelocity = _verticalVelocity,
            DashTimer = _dashTimer
        };
        Reconcile(rd);
    }

    private MoveData BuildMoveData()
    {
        // ИСПРАВЛЕНО: Добавлено .Value к CurrentState
        bool canMove = _playerState.IsAlive.Value && 
                       GameManager.Instance != null && 
                       GameManager.Instance.CurrentState.Value == GameManager.GameState.InProgress;

        if (!canMove) return default;

        MoveData md = new MoveData
        {
            Horizontal = Input.GetAxisRaw("Horizontal"),
            Vertical = Input.GetAxisRaw("Vertical"),
            Dash = Input.GetKey(KeyCode.LeftShift)
        };

        if (_mainCamera != null)
        {
            md.CamForward = _mainCamera.transform.forward;
            md.CamRight = _mainCamera.transform.right;
            md.CamForward.y = 0; md.CamRight.y = 0;
            md.CamForward.Normalize(); md.CamRight.Normalize();
        }

        return md;
    }

    [Replicate]
    private void Replicate(MoveData md, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        float delta = (float)base.TimeManager.TickDelta;
        Vector3 finalMovement = Vector3.zero;

        if (_playerState.IsAlive.Value)
        {
            Vector3 moveDir = md.CamForward * md.Vertical + md.CamRight * md.Horizontal;

            if (md.Dash && _dashTimer <= 0 && moveDir != Vector3.zero)
            {
                _dashTimer = DashDuration;
                _dashDirection = moveDir;
            }

            float currentSpeed = MoveSpeed;

            if (_dashTimer > 0)
            {
                _dashTimer -= delta;
                currentSpeed = DashSpeed;
                finalMovement = _dashDirection * currentSpeed;
            }
            else
            {
                finalMovement = moveDir * currentSpeed;
            }

            if (moveDir != Vector3.zero && _dashTimer <= 0)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, delta * 10f);
            }
        }

        _verticalVelocity += Gravity * delta;
        finalMovement.y = _verticalVelocity;

        // Двигаем только если CharacterController включен (он выключен у обозревателей)
        if (_cc.enabled)
        {
            _cc.Move(finalMovement * delta);
            if (_cc.isGrounded) _verticalVelocity = -2f;
        }
    }

    [Reconcile]
    private void Reconcile(ReconcileData rd, Channel channel = Channel.Unreliable)
    {
        bool wasEnabled = _cc.enabled;
        _cc.enabled = false; 
        transform.position = rd.Position;
        transform.rotation = rd.Rotation;
        _verticalVelocity = rd.VerticalVelocity;
        _dashTimer = rd.DashTimer;
        _cc.enabled = wasEnabled;
    }
}