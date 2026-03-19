using FishNet.Object;
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
        if (!base.IsServerInitialized) return;

        if (other.TryGetComponent(out IHealable healable))
        {
            if (other.TryGetComponent(out PlayerState state))
            {
                if (!state.IsAlive.Value || state.Health.Value >= 100) return;
            }

            healable.Heal(_healAmount);
            _manager.OnPickedUp(_spawnPosition);
            base.Despawn();
        }
    }
}