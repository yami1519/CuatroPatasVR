using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CuatroPatasVR.Shop
{
    [AddComponentMenu("Cuatro Patas/Tienda/Turno de tienda")]
    public sealed class ShopSession : MonoBehaviour
    {
        [Header("Pedidos: arrastra los productos en el orden del turno")]
        [SerializeField] private ShopProductDefinition[] orders;
        [SerializeField, Min(0)] private int payPerOrder = 2;
        [Header("Paciencia de los clientes")]
        [SerializeField, Min(5f)] private float patienceSeconds = 20f;
        [SerializeField, Min(0f)] private float mistakePenaltySeconds = 3f;
        [Header("Paneles de la escena")]
        [SerializeField] private TMP_Text orderText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text resultText;
        [Header("Regreso")]
        [SerializeField] private string homeScene = "casaVR";
        [Header("Eventos para conectar sonido, animación y efectos")]
        [SerializeField] private UnityEvent onCorrectDelivery = new UnityEvent();
        [SerializeField] private UnityEvent onWrongDelivery = new UnityEvent();
        [SerializeField] private UnityEvent onShiftFinished = new UnityEvent();
        private ShopShift shift;
        private bool rewardPaid;
        private bool submittedThisFrame;
        private ShopProductDefinition[] activeOrders;
        private float patienceLeft;
        public bool Working => shift != null && shift.State == ShopShift.Phase.Working;

        private void Awake() => ShowReady();
        private void LateUpdate() => submittedThisFrame = false;

        private void Update()
        {
            if (!Working) return;
            patienceLeft -= Time.deltaTime;
            if (patienceLeft <= 0f) { CustomerLeaves(); return; }
            Set(resultText, $"Paciencia: {PatienceBar()} {Mathf.CeilToInt(patienceLeft)}s");
        }
        private void ShowReady()
        {
            Set(orderText, "TRABAJO EN LA TIENDA");
            Set(statusText, "Pulsa INICIAR TURNO. Lee el pedido y suelta el producto en la bandeja.");
            Set(resultText, $"Cada cliente espera {patienceSeconds:0}s. Si se impacienta, pierdes su pago.");
        }
        public void BeginShift()
        {
            // Un turno por visita: no se puede reiniciar para repetir un pago.
            if (shift != null) return;
            if (orders == null || orders.Length == 0 || payPerOrder < 0 ||
                (long)orders.Length * payPerOrder > int.MaxValue)
            { Set(statusText, "Configura pedidos y pago validos en el Inspector."); return; }
            foreach (var product in orders)
                if (product == null) { Set(statusText, "Hay un producto sin asignar en los pedidos."); return; }
            activeOrders = (ShopProductDefinition[])orders.Clone();
            shift = new ShopShift(activeOrders.Length, payPerOrder);
            shift.Begin();
            patienceLeft = patienceSeconds;
            RefreshOrder();
            Set(statusText, "Agarra el producto correcto y sueltalo dentro de la bandeja.");
        }
        public bool TryDeliver(ShopProduct product)
        {
            if (!Working || submittedThisFrame || product == null || product.Definition == null || product.IsHeld) return false;
            submittedThisFrame = true;
            bool correct = product.Definition == activeOrders[shift.Completed];
            shift.Deliver(correct);
            // Retirar de la bandeja antes de activar cualquier evento del Inspector.
            product.ReturnToShelf();
            if (!correct)
            {
                // Equivocarse cuesta tiempo: el cliente pierde paciencia.
                patienceLeft = Mathf.Max(1f, patienceLeft - mistakePenaltySeconds);
                Set(statusText, $"Ese producto no corresponde (-{mistakePenaltySeconds:0}s). Entrega: {activeOrders[shift.Completed].displayName}");
                onWrongDelivery.Invoke();
                return false;
            }
            if (shift.State == ShopShift.Phase.Finished)
            {
                FinishShift();
                onCorrectDelivery.Invoke();
            }
            else
            {
                patienceLeft = patienceSeconds;
                RefreshOrder();
                Set(statusText, "¡Bien! Siguiente cliente.");
                onCorrectDelivery.Invoke();
            }
            return true;
        }
        private void CustomerLeaves()
        {
            shift.Skip();
            onWrongDelivery.Invoke();
            if (shift.State == ShopShift.Phase.Finished) { FinishShift(); return; }
            patienceLeft = patienceSeconds;
            Set(orderText, $"PEDIDO {shift.Completed + 1} / {shift.Count}\n{activeOrders[shift.Completed].displayName}");
            Set(statusText, $"El cliente se fue impaciente (-{shift.PayPerOrder} monedas). ¡Date prisa!");
        }
        private void FinishShift()
        {
            PayOnce();
            Set(orderText, "TURNO COMPLETADO");
            Set(statusText, $"Entregados: {shift.Paid}/{shift.Count} | Perdidos: {shift.Lost} | Errores: {shift.Mistakes}");
            Set(resultText, $"Pago guardado: {shift.Earned} monedas. Puedes volver a casa.");
            onShiftFinished.Invoke();
        }
        private void PayOnce()
        {
            if (rewardPaid) return;
            rewardPaid = true;
            GameManager.Get().AddCoins(shift.Earned);
        }
        private void RefreshOrder()
        {
            Set(orderText, $"PEDIDO {shift.Completed + 1} / {shift.Count}\n{activeOrders[shift.Completed].displayName}");
        }
        private string PatienceBar()
        {
            const int slots = 10;
            int filled = Mathf.Clamp(Mathf.CeilToInt(slots * patienceLeft / patienceSeconds), 0, slots);
            return new string('█', filled) + new string('░', slots - filled);
        }
        public void ReturnHome()
        {
            if (Working)
            { Set(statusText, "Completa los pedidos antes de volver a casa."); return; }
            if (Application.CanStreamedLevelBeLoaded(homeScene)) SceneManager.LoadScene(homeScene);
            else Set(statusText, $"Agrega la escena {homeScene} al perfil de compilacion.");
        }
        private static void Set(TMP_Text target, string value) { if (target != null) target.text = value; }
    }
}
