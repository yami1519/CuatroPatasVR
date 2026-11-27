using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace CuatroPatasVR.Comun
{
    [RequireComponent(typeof(CharacterController))]
    public class CatModeController : MonoBehaviour
    {
        [Header("Identidad y Spawn")]
        [Tooltip("0 = Gato 1 (Nina), 1 = Gato 2 (Teddy), 2 = Gato 3 (Milo)")]
        [SerializeField] private int catIndex = 0;
        [SerializeField] private Transform playerSpawnPoint;

        [Header("Paseo cuando NO eres este gato jugador")]
        [Tooltip("Si está marcado, camina solo cuando no controlas a este gato. Si está desmarcado, se queda quieto.")]
        [SerializeField] private bool roamWhenNotControlled = false;

        [Header("Ojos y Cámara del Gato")]
        [SerializeField] private Transform eyeAnchor;
        [SerializeField] private Camera catEyeCamera;
        [SerializeField] private Camera[] otherCamerasToDisable;

        [Header("Movimiento Jugador (PC)")]
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float lookSensitivity = 0.15f;
        [SerializeField] private float minPitch = -75f;
        [SerializeField] private float maxPitch = 75f;
        [SerializeField] private float gravity = -20f;

        [Header("Paseo Automático")]
        [SerializeField] private float roamSpeed = 1.4f;
        [SerializeField] private float roamTurnSpeed = 90f;
        [SerializeField] private float changeDirEvery = 2.5f;
        [SerializeField] private float obstacleCheckDist = 0.8f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        private CharacterController cc;
        private NavMeshAgent navAgent;
        private CatWander catWander;
        private float yaw;
        private float pitch;
        private float verticalVelocity;
        private float roamTimer;
        private Vector3 roamDir;
        private bool wasPlayerActive;

        private bool IsThisCatPlayer
        {
            get
            {
                var gm = GameManager.Get();
                return gm != null && gm.data != null &&
                       gm.data.role == PlayerRole.CatPlayer &&
                       gm.data.selectedCatIndex == catIndex;
            }
        }

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            navAgent = GetComponent<NavMeshAgent>();
            catWander = GetComponent<CatWander>();
            yaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            ApplyModeState(forceTeleport: true);
            PickNewRoamDirection();
            Invoke(nameof(SyncRoleOnSceneLoad), 0.05f);
        }

        private void SyncRoleOnSceneLoad()
        {
            ApplyModeState(forceTeleport: IsThisCatPlayer);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && catIndex == 0)
            {
                if (kb.f1Key.wasPressedThisFrame)
                {
                    var gm = GameManager.Get();
                    if (gm != null) gm.SetRoleAndCat(PlayerRole.Caregiver, 0);
                }

                if (kb.f2Key.wasPressedThisFrame)
                {
                    var gm = GameManager.Get();
                    if (gm != null) gm.SetRoleAndCat(PlayerRole.CatPlayer, 0);
                }
            }

            bool isPlayerNow = IsThisCatPlayer;
            if (isPlayerNow != wasPlayerActive)
            {
                ApplyModeState(forceTeleport: isPlayerNow);
            }

            if (isPlayerNow)
            {
                UpdatePlayerControl();
            }
            else if (roamWhenNotControlled)
            {
                UpdateRoamAI();
            }
            else
            {
                MoveWithGravity(Vector3.zero);
            }
        }

        private void LateUpdate()
        {
            if (IsThisCatPlayer)
            {
                UpdateEyeCameraTransform();
            }
        }

        private void ApplyModeState(bool forceTeleport)
        {
            bool isPlayer = IsThisCatPlayer;
            wasPlayerActive = isPlayer;

            var gm = GameManager.Get();
            bool anyCatIsPlayer = gm != null && gm.data != null && gm.data.role == PlayerRole.CatPlayer;

            if (isPlayer)
            {
                if (catWander != null) catWander.enabled = false;
                if (navAgent != null)
                {
                    if (navAgent.enabled && navAgent.isOnNavMesh) navAgent.ResetPath();
                    navAgent.enabled = false;
                }

                if (forceTeleport && playerSpawnPoint != null)
                {
                    cc.enabled = false;
                    transform.SetPositionAndRotation(
                        playerSpawnPoint.position,
                        Quaternion.Euler(0f, playerSpawnPoint.eulerAngles.y, 0f)
                    );
                    cc.enabled = true;
                }

                yaw = transform.eulerAngles.y;
                pitch = 0f;

                if (otherCamerasToDisable != null)
                {
                    foreach (var cam in otherCamerasToDisable)
                    {
                        if (cam != null && cam != catEyeCamera)
                            cam.enabled = false;
                    }
                }

                if (catEyeCamera != null)
                {
                    catEyeCamera.gameObject.SetActive(true);
                    catEyeCamera.enabled = true;
                }

                UpdateEyeCameraTransform();
            }
            else
            {
                // Apaga solo la cámara de ESTE gato no seleccionado
                if (catEyeCamera != null)
                    catEyeCamera.enabled = false;

                // Solo reactiva las cámaras del modo cuidador si NINGÚN gato está jugando como CatPlayer
                if (!anyCatIsPlayer && otherCamerasToDisable != null)
                {
                    foreach (var cam in otherCamerasToDisable)
                    {
                        if (cam != null)
                            cam.enabled = true;
                    }
                }

                if (navAgent != null && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    navAgent.enabled = true;
                }

                if (catWander != null && navAgent != null && navAgent.enabled)
                    catWander.enabled = true;

                if (!anyCatIsPlayer)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        private void UpdateEyeCameraTransform()
        {
            if (catEyeCamera == null) return;

            Vector3 targetPos = eyeAnchor != null
                ? eyeAnchor.position
                : transform.position + transform.forward * 0.35f + Vector3.up * 0.28f;

            catEyeCamera.transform.position = targetPos;
            catEyeCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void UpdatePlayerControl()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                var delta = mouse.delta.ReadValue();
                yaw += delta.x * lookSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, minPitch, maxPitch);
            }

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);

            Vector3 wishDir = (transform.right * x + transform.forward * z);
            if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

            MoveWithGravity(wishDir * moveSpeed);
        }

        private void UpdateRoamAI()
        {
            roamTimer -= Time.deltaTime;

            Vector3 origin = transform.position + Vector3.up * 0.25f;
            bool blocked = Physics.Raycast(origin, transform.forward, obstacleCheckDist, obstacleMask, QueryTriggerInteraction.Ignore);

            if (blocked || roamTimer <= 0f)
            {
                PickNewRoamDirection();
            }

            Quaternion targetRot = Quaternion.LookRotation(roamDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, roamTurnSpeed * Time.deltaTime);

            MoveWithGravity(transform.forward * roamSpeed);
        }

        private void PickNewRoamDirection()
        {
            roamTimer = UnityEngine.Random.Range(changeDirEvery * 0.7f, changeDirEvery * 1.4f);
            float angle = UnityEngine.Random.Range(0f, 360f);
            roamDir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;
        }

        private void MoveWithGravity(Vector3 horizontalVelocity)
        {
            if (cc.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z) * Time.deltaTime;
            cc.Move(motion);
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}