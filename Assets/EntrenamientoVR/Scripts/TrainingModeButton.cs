using UnityEngine;
namespace CuatroPatasVR.Training
{
    [RequireComponent(typeof(Collider), typeof(Renderer))]
    public sealed class TrainingModeButton : MonoBehaviour
    {
        [SerializeField] private TrainingSession session;
        [SerializeField] private bool freePlay;
        private MaterialPropertyBlock highlight;
        public bool IsFreePlayButton => freePlay;
        public bool Select()
        {
            if (session == null || !session.CanChooseMode) return false;
            if (freePlay) session.BeginFreePlay(); else session.BeginPractice();
            return true;
        }
        public void SetFocused(bool focused)
        {
            if (highlight == null) highlight = new MaterialPropertyBlock();
            highlight.SetColor("_BaseColor", focused ? new Color(0.29f, 0.43f, 0.30f) : new Color(0.12f, 0.23f, 0.17f));
            GetComponent<Renderer>().SetPropertyBlock(highlight);
        }
        private void OnTriggerEnter(Collider other)
        {
            var hand = other.GetComponentInParent<TrainingHand>();
            if (hand != null && hand.IsTracked) Select();
        }
    }
}
