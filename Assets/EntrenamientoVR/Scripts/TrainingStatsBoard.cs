using TMPro;
using UnityEngine;
namespace CuatroPatasVR.Training
{
    public sealed class TrainingStatsBoard : MonoBehaviour
    {
        [SerializeField] private TrainingSession session;
        [SerializeField] private Transform leftPanel;
        [SerializeField] private Transform rightPanel;
        [SerializeField] private TMP_Text leftText;
        [SerializeField] private TMP_Text rightText;
        private float nextRefresh;
        private void LateUpdate()
        {
            bool visible = !session.AwaitingModeChoice && session.IsCalibrated;
            leftPanel.gameObject.SetActive(visible);
            rightPanel.gameObject.SetActive(visible);
            if (!visible) return;
            Place(leftPanel, -2.4f);
            Place(rightPanel, 2.4f);
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.15f;
            leftText.text = $"TIEMPO ACTIVO\n{TrainingSession.FormatTime(session.ActiveSeconds)}\n\nRACHA ACTUAL\n{session.Streak}";
            string accuracy = session.HasResolvedTargets ? $"{session.Accuracy:0}%" : "--";
            rightText.text = $"GOLPES   {session.Hits}\n\nACIERTOS   {accuracy}\n\nESQUIVAS   {session.Dodges}\n\nMEJOR RACHA   {session.BestStreak}";
        }
        private void Place(Transform panel, float side)
        {
            panel.position = session.StandingHead + session.Forward * 4.8f + session.Right * side;
            panel.rotation = Quaternion.LookRotation(panel.position - session.StandingHead);
        }
    }
}
