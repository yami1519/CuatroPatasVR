using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

namespace CuatroPatasVR.Shop
{
    // XR arranca solo mientras la escena TiendaVR esta activa, preservando las escenas de escritorio.
    // (Reemplaza al antiguo TrainingXRSession del modulo de entrenamiento, ya eliminado.)
    public sealed class ShopXRSession : MonoBehaviour
    {
        private XRManagerSettings manager;
        private bool ownsSession;

        private IEnumerator Start()
        {
            if (Application.isBatchMode) yield break;
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                Debug.LogWarning("TiendaVR: vista previa sin visor en macOS. Valida OpenXR y controladores en el dispositivo de destino.");
                yield break;
            }
            manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null || manager.activeLoader != null) yield break;
            ownsSession = true;
            yield return manager.InitializeLoader();
            if (manager.activeLoader != null) manager.StartSubsystems();
            else Debug.LogWarning("TiendaVR: no se encontro un runtime OpenXR activo.");
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            Release();
        }

        private void Release()
        {
            if (!ownsSession || manager == null) return;
            manager.StopSubsystems();
            manager.DeinitializeLoader();
            ownsSession = false;
        }
    }
}
