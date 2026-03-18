using Unity.Netcode;
using UnityEngine;

public class BulletNetwork : NetworkBehaviour
{
    public float Speed = 10f;
    public int Damage = 25;
    public float LifeTime = 3f;
    
    public ulong OwnerId; // Кто выстрелил

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Уничтожаем пулю через LifeTime секунд, если она никуда не попала
            Invoke(nameof(DestroyBullet), LifeTime);
        }
    }

    private void Update()
    {
        // Пуля летит вперед
        transform.position += transform.forward * Speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Урон считает только сервер

        // Проверяем, попали ли мы в игрока
        if (other.TryGetComponent(out PlayerNetworkTest hitPlayer))
        {
            // Не наносим урон самому себе
            if (hitPlayer.OwnerClientId == OwnerId) return;

            hitPlayer.TakeDamage(Damage, OwnerId);
            DestroyBullet();
        }
        else if (!other.isTrigger) // Если попали в стену (не триггер)
        {
            DestroyBullet();
        }
    }

    private void DestroyBullet()
    {
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(); // Удаляем по сети
        }
    }
}