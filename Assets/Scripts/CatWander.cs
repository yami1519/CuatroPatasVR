using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

// El gato pasea por los 5 ambientes usando NavMesh (esquiva muebles y pasa por las puertas).
// Si el modelo del gato trae animaciones (Walk / Idle), las reproduce solo.
public class CatWander : MonoBehaviour
{
    public Transform[] points;
    public NavMeshSurface surface;
    public float speed = 1.5f;
    public float waitMin = 2f, waitMax = 5f;

    NavMeshAgent agent;
    Animation anim;
    Animator animator;
    string walkClip, idleClip;
    float wait = 1f;
    float pauseTimer;
    Vector3 lookTarget;

    bool Ready => agent != null && agent.enabled && agent.isOnNavMesh;

    void Start()
    {
        if (surface != null && surface.navMeshData == null) surface.BuildNavMesh();

        agent = GetComponent<NavMeshAgent>();
        if (agent != null && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.enabled = true;
            agent.speed = speed;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.2f;
        }

        // Animaciones del modelo (si existen)
        anim = GetComponentInChildren<Animation>();
        if (anim != null)
        {
            foreach (AnimationState s in anim)
            {
                string n = s.name.ToLower();
                if (walkClip == null && n.Contains("walk")) walkClip = s.name;
                if (idleClip == null && (n.Contains("idle") || n.Contains("sit"))) idleClip = s.name;
                s.wrapMode = WrapMode.Loop;
            }
        }
        animator = GetComponentInChildren<Animator>();
    }

    // Se queda quieto mirando al jugador (cuando lo acarician o le sirven comida)
    public void Pause(float seconds, Vector3 lookAt)
    {
        pauseTimer = Mathf.Max(pauseTimer, seconds);
        lookTarget = lookAt;
        if (Ready) agent.ResetPath();
        wait = seconds;
    }

    void Update()
    {
        var hm = HouseManager.Instance;
        bool ui = hm != null && hm.IsUIOpen;
        if (Ready) agent.isStopped = ui || pauseTimer > 0f;
        if (ui) return;

        bool sick = GameManager.Get().data.catIsSick;
        if (Ready) agent.speed = speed * (sick ? 0.5f : 1f);

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            Vector3 look = lookTarget - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look.normalized), 8f * Time.deltaTime);
            PlayAnim(false);
            return;
        }

        if (!Ready) { PlayAnim(false); return; }

        bool moving = agent.pathPending || (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.05f);
        PlayAnim(moving);

        if (!moving)
        {
            wait -= Time.deltaTime;
            if (wait <= 0f)
            {
                PickTarget();
                wait = Random.Range(waitMin, waitMax);
            }
        }
    }

    void PickTarget()
    {
        Vector3 t = (points != null && points.Length > 0)
            ? points[Random.Range(0, points.Length)].position + new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f))
            : transform.position + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
        if (NavMesh.SamplePosition(t, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    void PlayAnim(bool walking)
    {
        if (anim != null)
        {
            string c = walking ? walkClip : idleClip;
            if (c != null && !anim.IsPlaying(c)) anim.CrossFade(c, 0.2f);
        }
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var p in animator.parameters)
                if (p.name == "Speed" && p.type == AnimatorControllerParameterType.Float)
                    animator.SetFloat("Speed", walking ? 1f : 0f);
        }
    }
}
