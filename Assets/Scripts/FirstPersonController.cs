using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento en primera persona: WASD para caminar, mouse para mirar.
// Cuando se abre una ventana (tienda, resumen) el mouse se libera para poder hacer clic.
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public Transform cameraPivot;
    public float moveSpeed = 3.5f;
    public float lookSensitivity = 0.12f;
    public float minPitch = -75f, maxPitch = 80f;

    CharacterController cc;
    float pitch = 15f; // empieza mirando un poco hacia abajo
    float vy;

    void Awake() => cc = GetComponent<CharacterController>();

    void Update()
    {
        var hm = HouseManager.Instance;
        bool ui = hm != null && hm.IsUIOpen;
        SetCursor(!ui);
        if (ui) return;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 d = mouse.delta.ReadValue() * lookSensitivity;
            transform.Rotate(0f, d.x, 0f);
            pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);
        }
        if (cameraPivot) cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);

        var kb = Keyboard.current;
        Vector2 i = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed) i.y += 1;
            if (kb.sKey.isPressed) i.y -= 1;
            if (kb.dKey.isPressed) i.x += 1;
            if (kb.aKey.isPressed) i.x -= 1;
        }
        Vector3 move = transform.right * i.x + transform.forward * i.y;
        if (move.sqrMagnitude > 1f) move.Normalize();
        move *= moveSpeed;
        vy = cc.isGrounded ? -1f : vy - 20f * Time.deltaTime;
        move.y = vy;
        cc.Move(move * Time.deltaTime);
    }

    static void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void OnDisable() => SetCursor(false);
}
