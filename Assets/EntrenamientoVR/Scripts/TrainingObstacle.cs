using UnityEngine;
using TMPro;

namespace CuatroPatasVR.Training
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TrainingObstacle : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        private TrainingSession session;
        private Vector3 direction;
        private float speed;
        private float distanceToPlayer;
        private bool duck;
        private int requiredSide;
        private bool evaluated;
        private Rigidbody body;

        public void Initialize(TrainingSession owner, bool isDuck, int side, float distance, float travelSpeed)
        {
            session = owner;
            duck = isDuck;
            requiredSide = side;
            distanceToPlayer = distance;
            speed = travelSpeed;
            direction = -owner.Forward;
            body = GetComponent<Rigidbody>();
            if (label != null)
            {
                label.gameObject.SetActive(!owner.IsFreePlay || !duck);
                label.text = owner.IsFreePlay ? (side < 0 ? "<" : ">") : duck ? "AGACHATE" : side < 0 ? "< IZQUIERDA" : "DERECHA >";
                if (owner.IsFreePlay && !duck) label.fontSize = 3f;
            }
        }
        private void FixedUpdate()
        {
            if (session == null || !session.CanMove) return;
            float step = speed * Time.fixedDeltaTime;
            body.MovePosition(body.position + direction * step);
            distanceToPlayer -= step;
            if (!evaluated && distanceToPlayer <= 0f)
            {
                evaluated = true;
                session.RecordDodge(duck ? session.HasDucked() : session.HasDodged(requiredSide));
            }
            if (distanceToPlayer < -0.6f) Destroy(gameObject);
        }
    }
}
