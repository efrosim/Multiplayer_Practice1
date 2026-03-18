using Unity.Netcode.Components;
using UnityEngine;

// Наследуемся от стандартного NetworkTransform
public class ClientNetworkTransform : NetworkTransform
{
    // Переопределяем метод который определяет кто главный
    protected override bool OnIsServerAuthoritative()
    {
        // Возвращаем false, чтобы разрешить Клиенту управлять этим объектом
        return false;
    }
}