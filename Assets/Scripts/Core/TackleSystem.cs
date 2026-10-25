using System.Collections.Generic;
using UnityEngine;

namespace GEL
{
    public static class TackleSystem
    {
        public const float VictimLockSeconds = 2f;
        public const float TacklerCooldownSeconds = 0.8f;
        public const float TackleActionSeconds = 0.35f;

        private class Attempt
        {
            public PlayerAgent Tackler;
            public PlayerAgent Victim;
            public float Remaining;
            public float ClosestDistance;
        }

        private static readonly List<Attempt> Attempts = new List<Attempt>();

        public static bool IsAnyAttemptActive => Attempts.Count > 0;

        public static bool IsAttemptActive(PlayerAgent tackler)
        {
            if (tackler == null) return false;
            for (int i = 0; i < Attempts.Count; i++)
                if (Attempts[i].Tackler == tackler) return true;
            return false;
        }

        public static void CancelAttemptsBy(PlayerAgent tackler)
        {
            if (tackler == null) return;
            for (int i = Attempts.Count - 1; i >= 0; i--)
                if (Attempts[i].Tackler == tackler)
                    Attempts.RemoveAt(i);
        }

        public static bool TryTackle(PlayerAgent tackler, PlayerAgent victim, BallSimulator ball, PossessionManager possession)
        {
            float range = tackler != null ? tackler.TackleRange : 1.6f;
            return TryTackle(tackler, victim, ball, possession, range);
        }

        public static bool TryTackle(
            PlayerAgent tackler,
            PlayerAgent victim,
            BallSimulator ball,
            PossessionManager possession,
            float range)
        {
            if (tackler == null || victim == null || ball == null || possession == null) return false;
            if (tackler == victim) return false;
            if (!tackler.CanTackle) return false;
            if (possession.CurrentOwner != victim) return false;
            if (IsAttemptActive(tackler)) return false;

            range = Mathf.Max(0.1f, range);
            float distance = Vector3.Distance(ToePoint(tackler), ball.transform.position);
            if (distance > range) return false;

            tackler.StartTackleCooldown(TacklerCooldownSeconds);
            tackler.LockTackleAction(TackleActionSeconds);

            Attempts.Add(new Attempt
            {
                Tackler = tackler,
                Victim = victim,
                Remaining = TackleActionSeconds,
                ClosestDistance = distance
            });

            return true;
        }

        public static void Tick(float dt, BallSimulator ball, PossessionManager possession)
        {
            if (ball == null || possession == null) return;

            for (int i = Attempts.Count - 1; i >= 0; i--)
            {
                Attempt attempt = Attempts[i];
                if (attempt.Tackler == null || attempt.Victim == null)
                {
                    Attempts.RemoveAt(i);
                    continue;
                }

                if (possession.CurrentOwner != attempt.Victim)
                {
                    Attempts.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(ToePoint(attempt.Tackler), ball.transform.position);
                attempt.ClosestDistance = Mathf.Min(attempt.ClosestDistance, distance);
                attempt.Remaining -= dt;

                if (attempt.Remaining > 0f) continue;

                Resolve(attempt, ball, possession);
                Attempts.RemoveAt(i);
            }
        }

        private static void Resolve(Attempt attempt, BallSimulator ball, PossessionManager possession)
        {
            PlayerAgent tackler = attempt.Tackler;
            PlayerAgent victim = attempt.Victim;
            if (tackler == null || victim == null || possession.CurrentOwner != victim) return;

            float range = Mathf.Max(0.1f, tackler.TackleRange);
            float closeness = Mathf.Clamp01(1f - attempt.ClosestDistance / range);
            float chance = tackler.TackleAbility * victim.BallControlAbility *
                           closeness * Random.Range(0.8f, 1.2f);
            chance = Mathf.Clamp(chance, 0.05f, 0.9f);

            if (Random.value >= chance)
            {
                tackler.Stun(0.15f);
                return;
            }

            possession.ForceOwner(tackler);

            Vector3 direction = tackler.transform.position - ball.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                ball.Kick(direction.normalized * 3f + Vector3.up * 0.5f, Vector3.zero);

            victim.StopMotion();
            victim.Stun(0.2f);
            victim.BlockTackle(VictimLockSeconds);
        }

        private static Vector3 ToePoint(PlayerAgent agent)
        {
            Transform toe = agent.BallToe != null ? agent.BallToe : agent.BallFoot;
            return toe != null ? toe.position : agent.transform.position;
        }
    }
}
