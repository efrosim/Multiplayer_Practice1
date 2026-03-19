using Unity.Netcode;
using UnityEngine;

public class HealthPickup : NetworkBehaviour
{
    [SerializeField] private int _healAmount = 40;
    private PickupManager _manager;
    private Vector3 _spawnPosition;

    public void Init(PickupManager manager)
    {
        _manager = manager;
        _spawnPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Только сервер обрабатывает подбор

        // DIP: Зависим от абстракции IHealable, а не от PlayerHealth
        if (other.TryGetComponent(out IHealable healable))
        {
            // Проверяем, нужен ли хил (чтобы не съесть аптечку впустую)
            if (other.TryGetComponent(out PlayerState state))
            {
                if (!state.IsAlive.Value || state.Health.Value >= 100) return;
            }

            healable.Heal(_healAmount);
            _manager.OnPickedUp(_spawnPosition);
            NetworkObject.Despawn(destroy: true);
        }
    }
}