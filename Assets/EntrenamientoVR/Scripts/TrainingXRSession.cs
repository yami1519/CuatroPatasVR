using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

namespace CuatroPatasVR.Training
{
    // XR starts only while the training scene is active, preserving the existing desktop scenes.
    public sealed class TrainingXRSession : MonoBehaviour
    {
        private XRManagerSettings manager;
        private bool ownsSession;

        private IEnumerator Start()
        {
            if (Application.isBatchMode) yield break;
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                Debug.LogWarning("EntrenamientoVR: vista previa sin visor en macOS. Valida OpenXR y controladores en el dispositivo de destino.");
                yield break;
            }
            manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null || manager.activeLoader != null) yield break;
            ownsSession = true;
            yield return manager.InitializeLoader();
            if (manager.activeLoader != null) manager.StartSubsystems();
            else Debug.LogWarning("EntrenamientoVR: no se encontró un runtime OpenXR activo.");
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
