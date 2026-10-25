using UnityEngine;
using UnityEngine.InputSystem;

// Jugador del modo correr: 3 carriles, saltar y deslizarse.
// Controles: A/D o flechas = carril | W, flecha arriba o Espacio = saltar | S o flecha abajo = deslizar
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class RunnerPlayer : MonoBehaviour
{
    [Header("Carriles")]
    public float laneDistance = 2f;
    public float laneChangeSpeed = 12f;

    [Header("Salto")]
    public float jumpForce = 9f;
    public float gravity = 28f;

    [Header("Deslizar")]
    public float slideDuration = 0.8f;
    [Tooltip("Hijo con el modelo 3D. Se aplasta al deslizarse (útil mientras no hay animación).")]
    public Transform visual;
    [Tooltip("Desactívalo cuando el personaje tenga animación de deslizarse")]
    public bool squashVisualOnSlide = true;

    [Header("Opcional")]
    [Tooltip("Si lo asignas, necesita los parámetros: Jump (Trigger), Slide (Trigger), Grounded (Bool)")]
    public Animator animator;

    int currentLane = 1; // 0 = izquierda, 1 = centro, 2 = derecha
    float verticalVelocity;
    float groundY;
    bool isGrounded = true;
    bool isSliding;
    float slideTimer;
    bool isDead;

    CapsuleCollider col;
    float originalHeight;
    Vector3 originalCenter;
    Vector3 visualOriginalScale;

    void Awake()
    {
        col = GetComponent<CapsuleCollider>();
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        originalHeight = col.height;
        originalCenter = col.center;
        groundY = transform.position.y;
        if (visual) visualOriginalScale = visual.localScale;
    }

    void Update()
    {
        if (isDead) return;
        if (RunnerManager.Instance != null && !RunnerManager.Instance.IsRunning) return;

        ReadInput();

        Vector3 pos = transform.position;

        // Movimiento lateral suave hacia el carril objetivo
        float targetX = (currentLane - 1) * laneDistance;
        pos.x = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * Time.deltaTime);

        // Salto con gravedad manual
        if (!isGrounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
            pos.y += verticalVelocity * Time.deltaTime;
            if (pos.y <= groundY)
            {
                pos.y = groundY;
                verticalVelocity = 0f;
                isGrounded = true;
            }
        }

        transform.position = pos;

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f) EndSlide();
        }

        if (animator) animator.SetBool("Grounded", isGrounded);
    }

    void ReadInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) ChangeLane(-1);
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) ChangeLane(1);
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Jump();
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Slide();
    }

    void ChangeLane(int dir)
    {
        currentLane = Mathf.Clamp(currentLane + dir, 0, 2);
    }

    void Jump()
    {
        if (!isGrounded) return;
        if (isSliding) EndSlide();
        isGrounded = false;
        verticalVelocity = jumpForce;
        if (RunnerFX.Instance != null) RunnerFX.Instance.Jump();
        if (animator) animator.SetTrigger("Jump");
    }

    void Slide()
    {
        // Si está en el aire, baja rápido (como Subway Surfers)
        if (!isGrounded) verticalVelocity = -jumpForce * 1.5f;

        if (!isSliding && RunnerFX.Instance != null) RunnerFX.Instance.Slide();
        isSliding = true;
        slideTimer = slideDuration;

        // Collider a la mitad, pegado al suelo
        col.height = originalHeight * 0.5f;
        col.center = originalCenter - Vector3.up * (originalHeight * 0.25f);

        if (visual && squashVisualOnSlide) visual.localScale = new Vector3(visualOriginalScale.x, visualOriginalScale.y * 0.5f, visualOriginalScale.z);
        if (animator) animator.SetTrigger("Slide");
    }

    void EndSlide()
    {
        isSliding = false;
        col.height = originalHeight;
        col.center = originalCenter;
        if (visual) visual.localScale = visualOriginalScale;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        var coin = other.GetComponentInParent<Coin>();
        if (coin != null)
        {
            RunnerManager.Instance?.CollectCoin(coin.value);
            if (RunnerFX.Instance != null) RunnerFX.Instance.Coin(coin.transform.position);
            Destroy(coin.gameObject);
            return;
        }

        if (other.GetComponentInParent<Obstacle>() != null)
        {
            isDead = true;
            if (animator) animator.SetTrigger("Death");
            if (RunnerFX.Instance != null) RunnerFX.Instance.Crash();
            RunnerManager.Instance?.GameOver();
        }
    }
}
