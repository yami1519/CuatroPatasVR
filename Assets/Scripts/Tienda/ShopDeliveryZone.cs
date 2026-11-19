using UnityEngine;

namespace CuatroPatasVR.Shop
{
    [RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("Cuatro Patas/Tienda/Zona de entrega")]
    public sealed class ShopDeliveryZone : MonoBehaviour
    {
        [SerializeField] private ShopSession session;
        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;
        private void OnTriggerStay(Collider other)
        {
            var product = other.GetComponentInParent<ShopProduct>();
            if (session != null && product != null && !product.IsHeld) session.TryDeliver(product);
        }
    }
}
