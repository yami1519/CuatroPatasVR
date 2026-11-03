using UnityEngine;
namespace CuatroPatasVR.Training
{
    [RequireComponent(typeof(Collider), typeof(Renderer))]
    public sealed class TrainingModeButton : MonoBehaviour
    {
        [SerializeField] private TrainingSession session;
        [SerializeField] private bool freePlay;
        [SerializeField] private Animator animator;
        public bool IsFreePlayButton => freePlay;
        public bool Select()
        {
            if (session == null || !session.CanChooseMode) return false;
            if (freePlay) session.BeginFreePlay(); else session.BeginPractice();
            return true;
        }
        public void SetFocused(bool focused)
        {
            if (animator != null) animator.SetBool("Focused", focused);
        }
        private void OnTriggerEnter(Collider other)
        {
            var hand = other.GetComponentInParent<TrainingHand>();
            if (hand != null && hand.IsTracked) Select();
        }
    }
}
