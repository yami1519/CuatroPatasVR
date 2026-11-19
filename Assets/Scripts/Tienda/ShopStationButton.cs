using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CuatroPatasVR.Shop
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    [AddComponentMenu("Cuatro Patas/Tienda/Boton del puesto")]
    public sealed class ShopStationButton : MonoBehaviour
    {
        [SerializeField] private UnityEvent onPressed = new UnityEvent();
        private XRSimpleInteractable interactable;
        private void Awake() => interactable = GetComponent<XRSimpleInteractable>();
        private void OnEnable() => interactable.selectEntered.AddListener(Selected);
        private void OnDisable() => interactable.selectEntered.RemoveListener(Selected);
        private void Selected(SelectEnterEventArgs args) => Press();
        public void Press() => onPressed.Invoke();
    }
}
