using UnityEngine;

namespace CuatroPatasVR.Training
{
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public sealed class TrainingTarget : MonoBehaviour
    {
        private Vector3 direction;
        private float speed;
        private float remainingDistance;
        private bool consumed;
        private bool leftHand;
        private TrainingSession session;
        private Rigidbody body;
        public void Initialize(Vector3 travelDirection, float travelSpeed, float distance, TrainingSession owner = null, bool requiresLeftHand = true)
        {
            direction = travelDirection.normalized;
            speed = travelSpeed;
            remainingDistance = distance;
            session = owner;
            leftHand = requiresLeftHand;
            body = GetComponent<Rigidbody>();
        }
        private void FixedUpdate()
        {
            if (consumed || (session != null && !session.CanMove)) return;
            float step = speed * Time.fixedDeltaTime;
            body.MovePosition(body.position + direction * step);
            remainingDistance -= step;
            if (remainingDistance <= 0f)
            {
                consumed = true;
                if (session != null) session.RecordMiss();
                Destroy(gameObject);
            }
        }
        public bool TryHit(bool isLeft, float handSpeed)
        {
            if (consumed || (session != null && !session.CanMove)) return false;
            if (session != null && isLeft != leftHand) { session.Notify("Usa la mano indicada por la flecha"); return false; }
            if (session != null && handSpeed < session.MinimumPunchSpeed) { session.Notify("Haz un golpe, no solo un contacto"); return false; }
            consumed = true;
            GetComponent<Collider>().enabled = false;
            if (session != null) session.RecordHit();
            Destroy(gameObject);
            return true;
        }
        private void OnTriggerEnter(Collider other) => CheckContact(other);
        private void OnTriggerStay(Collider other) => CheckContact(other);
        private void CheckContact(Collider other)
        {
            var hand = other.GetComponentInParent<TrainingHand>();
            if (hand != null && hand.IsTracked) TryHit(hand.IsLeftHand, hand.Speed);
        }
    }
}
