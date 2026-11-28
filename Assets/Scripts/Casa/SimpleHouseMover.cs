using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento simple con WASD (solo se usa si no se encontró el PlayerArmature de Starter Assets).
[RequireComponent(typeof(CharacterController))]
public class SimpleHouseMover : MonoBehaviour
{
    public float speed = 4f;
    CharacterController cc;
    float vy;

    void Awake() => cc = GetComponent<CharacterController>();

    void Update()
    {
        var kb = Keyboard.current;
        Vector2 i = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed) i.y += 1;
            if (kb.sKey.isPressed) i.y -= 1;
            if (kb.dKey.isPressed) i.x += 1;
            if (kb.aKey.isPressed) i.x -= 1;
        }
        Vector3 move = new Vector3(i.x, 0f, i.y).normalized * speed;
        if (move.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(move);
        vy = cc.isGrounded ? -1f : vy - 20f * Time.deltaTime;
        move.y = vy;
        cc.Move(move * Time.deltaTime);
    }
}
