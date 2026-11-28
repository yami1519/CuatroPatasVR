using UnityEngine;

namespace CuatroPatasVR.Shop
{
    [CreateAssetMenu(menuName = "Cuatro Patas/Tienda/Producto", fileName = "Producto")]
    public sealed class ShopProductDefinition : ScriptableObject
    {
        [Tooltip("Nombre que aparece en el pedido.")]
        public string displayName = "Comida para gato";
    }
}
