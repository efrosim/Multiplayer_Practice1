using FishNet.Object;
using UnityEngine;

public class NetworkBullet : NetworkBehaviour
{
    public float Speed = 20f;
    public int Damage = 25;
    public float LifeTime = 3f;

    public override void OnStartNetwork()
    {
        if (base.IsServerInitialized) Invoke(nameof(DestroyBullet), LifeTime);
    }

    private void Update()
    {
        transform.position += transform.forward * Speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized) return;

        if (other.TryGetComponent(out IDamageable damageable))
        {
            // Используем встроенный base.OwnerId для проверки урона по себе
            if (other.TryGetComponent(out NetworkBehaviour netObj) && netObj.OwnerId == base.OwnerId) return;

            damageable.TakeDamage(Damage, base.OwnerId);
            DestroyBullet();
        }
        else if (!other.isTrigger)
        {
            DestroyBullet();
        }
    }

    private void DestroyBullet()
    {
        if (base.IsSpawned) base.Despawn();
    }
}