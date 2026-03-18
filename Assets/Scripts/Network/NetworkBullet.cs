using Unity.Netcode;
using UnityEngine;

public class NetworkBullet : NetworkBehaviour
{
    public float Speed = 10f;
    public int Damage = 25;
    public float LifeTime = 3f;
    
    public ulong OwnerId;

    public override void OnNetworkSpawn()
    {
        if (IsServer) Invoke(nameof(DestroyBullet), LifeTime);
    }

    private void Update()
    {
        transform.position += transform.forward * Speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // Зависим от интерфейса, а не от конкретного класса игрока
        if (other.TryGetComponent(out IDamageable damageable))
        {
            // Проверка на урон самому себе
            if (other.TryGetComponent(out NetworkBehaviour netObj) && netObj.OwnerClientId == OwnerId) 
                return;

            damageable.TakeDamage(Damage, OwnerId);
            DestroyBullet();
        }
        else if (!other.isTrigger)
        {
            DestroyBullet();
        }
    }

    private void DestroyBullet()
    {
        if (NetworkObject.IsSpawned) NetworkObject.Despawn();
    }
}