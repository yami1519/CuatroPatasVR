using System.Collections.Generic;
using UnityEngine;

namespace CuatroPatasVR.Training
{
    public sealed class TrainingSpawner : MonoBehaviour
    {
        [SerializeField] private Transform playerCamera;
        [SerializeField] private TrainingSession session;
        [SerializeField] private TrainingTarget targetPrefab;
        [SerializeField] private TrainingTarget alternatePrefab;
        [SerializeField] private TrainingObstacle duckPrefab;
        [SerializeField] private TrainingObstacle sidePrefab;
        [SerializeField, Min(1f)] private float spawnDistance = 3f;
        private enum ActionKind { None, PunchLeft, PunchRight, Duck, StepLeft, StepRight }
        private readonly List<GameObject> active = new List<GameObject>();
        private readonly List<ActionKind> movementBag = new List<ActionKind>();
        private int sequence;
        private float nextSpawn;
        private bool awaitingGap;
        private bool hasPlan;
        private ActionKind plannedKind;
        private float plannedSpeed;
        private float plannedDistance;
        private float plannedSpawnTime;
        private float lastArrival;
        private ActionKind lastKind;
        private ActionKind lastMovement;
        private int freePunchesRemaining;
        private bool lastLeft;
        private int repeatedHand;

        private static bool Movement(ActionKind kind) => kind == ActionKind.Duck || kind == ActionKind.StepLeft || kind == ActionKind.StepRight;
        private void Update()
        {
            active.RemoveAll(item => item == null);
            if (session == null || !session.CanMove || playerCamera == null || targetPrefab == null) return;
            if (session.IsFreePlay) UpdateFreePlay(); else UpdatePractice();
        }
        private void UpdateFreePlay()
        {
            if (!hasPlan) PlanFreeAction();
            if (Time.time < plannedSpawnTime || active.Count >= 3) return;
            if (Movement(plannedKind))
                foreach (var item in active)
                    if (item.GetComponent<TrainingObstacle>() != null) return;
            Spawn(plannedKind, plannedDistance, plannedSpeed);
            // Schedule by arrival, not by waiting for every object to disappear.
            // Blocks and movement cues can be visible together without requiring simultaneous actions.
            lastArrival = Time.time + plannedDistance / plannedSpeed;
            lastKind = plannedKind;
            hasPlan = false;
        }
        private void PlanFreeAction()
        {
            plannedKind = ChooseFreeAction();
            plannedSpeed = Movement(plannedKind) ? session.ObstacleSpeed : session.TargetSpeed;
            plannedDistance = Mathf.Max(plannedSpeed, 4f * Random.Range(0.9f, 1.05f));
            float reaction = plannedDistance / plannedSpeed;
            float gap = Movement(plannedKind) || Movement(lastKind)
                ? Random.Range(session.FreeMovementClearance, session.FreeMovementClearance + 0.18f)
                : Random.Range(session.FreeMinimumInterval, session.FreeMaximumInterval);
            plannedSpawnTime = Mathf.Max(Time.time, lastArrival + gap - reaction);
            hasPlan = true;
        }
        private ActionKind ChooseFreeAction()
        {
            if (freePunchesRemaining <= 0)
            {
                if (Random.value >= 0.2f)
                {
                    if (movementBag.Count == 0)
                    {
                        movementBag.Add(ActionKind.Duck);movementBag.Add(ActionKind.StepLeft);movementBag.Add(ActionKind.StepRight);
                        for (int i = movementBag.Count - 1; i > 0; i--)
                        { int j = Random.Range(0, i + 1);var value = movementBag[i];movementBag[i] = movementBag[j];movementBag[j] = value; }
                        if (movementBag[movementBag.Count - 1] == lastMovement)
                        { var value = movementBag[0];movementBag[0] = movementBag[movementBag.Count - 1];movementBag[movementBag.Count - 1] = value; }
                    }
                    lastMovement = movementBag[movementBag.Count - 1];movementBag.RemoveAt(movementBag.Count - 1);
                    freePunchesRemaining = Random.Range(2, 5);
                    return lastMovement;
                }
                freePunchesRemaining = Random.Range(2, 5);
            }
            freePunchesRemaining--;
            bool left = repeatedHand >= 2 ? !lastLeft : Random.value < 0.5f;
            repeatedHand = left == lastLeft ? repeatedHand + 1 : 1;
            lastLeft = left;
            return left ? ActionKind.PunchLeft : ActionKind.PunchRight;
        }
        private void UpdatePractice()
        {
            if (active.Count > 0) return;
            if (awaitingGap) { nextSpawn = Time.time + (session.Level == 4 ? 0.3f : 0.7f);awaitingGap = false; }
            if (Time.time < nextSpawn) return;
            bool duck = session.Level == 2 ? sequence % 3 == 2 : session.Level == 3 ? sequence % 3 == 1 : session.Level == 4 && sequence % 5 == 2;
            bool side = session.Level == 3 ? sequence % 3 == 2 : session.Level == 4 && sequence % 5 == 4;
            int cycleLength = session.Level == 3 ? 3 : 5;
            var kind = duck ? ActionKind.Duck : side ? ((sequence / cycleLength) % 2 == 0 ? ActionKind.StepLeft : ActionKind.StepRight) : sequence % 2 == 0 ? ActionKind.PunchLeft : ActionKind.PunchRight;
            Spawn(kind, spawnDistance, Movement(kind) ? session.ObstacleSpeed : session.TargetSpeed);
            sequence++;
            awaitingGap = true;
        }
        private void Spawn(ActionKind kind, float distance, float speed)
        {
            Vector3 spawn = session.StandingHead + session.Forward * distance;
            if (Movement(kind))
            {
                bool duck = kind == ActionKind.Duck;
                int requiredSide = kind == ActionKind.StepLeft ? -1 : 1;
                Vector3 position = spawn + Vector3.down * (duck ? 0.15f : 0.3f);
                if (!duck) position -= session.Right * requiredSide * 0.32f;
                var obstacle = Instantiate(duck ? duckPrefab : sidePrefab, position, Quaternion.LookRotation(session.Forward));
                obstacle.Initialize(session, duck, requiredSide, distance, speed);
                active.Add(obstacle.gameObject);
                session.ShowInstruction(duck ? "AGACHATE al llegar la barra" : requiredSide < 0 ? "ESQUIVA A LA IZQUIERDA" : "ESQUIVA A LA DERECHA");
            }
            else
            {
                bool left = kind == ActionKind.PunchLeft;
                var prefab = left || alternatePrefab == null ? targetPrefab : alternatePrefab;
                Vector3 position = spawn + Vector3.down * 0.25f;
                if (session.IsFreePlay) position += session.Right * (left ? -0.32f : 0.32f);
                var target = Instantiate(prefab, position, Quaternion.LookRotation(session.Forward));
                target.Initialize(-session.Forward, speed, distance + 0.75f, session, left);
                active.Add(target.gameObject);
                session.ShowInstruction(left ? "Golpea con la MANO IZQUIERDA" : "Golpea con la MANO DERECHA");
            }
        }
        public void ClearActive()
        {
            foreach (var item in active) if (item != null) { item.SetActive(false);Destroy(item); }
            active.Clear();movementBag.Clear();
            sequence = 0;nextSpawn = lastArrival = 0f;
            awaitingGap = hasPlan = false;
            lastKind = lastMovement = ActionKind.None;
            repeatedHand = 0;freePunchesRemaining = Random.Range(2, 5);
        }
#if UNITY_EDITOR
        public void PreviewPunch(bool left)
        {
            if (session == null || !session.UsingDesktopPreview || !session.CanMove) return;
            TrainingTarget nearest = null;float nearestDistance = float.PositiveInfinity;
            foreach (var item in active)
            {
                if (item == null) continue;
                var target = item.GetComponent<TrainingTarget>();if (target == null) continue;
                float distance = Vector3.Dot(item.transform.position - session.StandingHead, session.Forward);
                if (distance < nearestDistance) { nearestDistance = distance;nearest = target; }
            }
            if (nearest != null) nearest.TryHit(left, 1f);
        }
#endif
    }
}
