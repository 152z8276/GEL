using UnityEngine;

namespace GEL
{
    public class PossessionManager : MonoBehaviour
    {
        public BallSimulator Ball;
        public PlayerAgent[] Agents;
        public float ControlRadius = 1.6f;
        public float MaxControlSpeed = 9f;
        public float MinDribbleDistance = 0.5f;
        public float DribbleDistance = 1.0f;
        public float DribbleFrequency = 1.4f;
        public float DribbleSpring = 70f;
        public float DribbleMaxSpeed = 12f;
        public float AimLockRadius = 3f;
        public float LoosePickupRadius = 1.0f;

        private PlayerAgent _lastDribbleOwner;
        private float _lastDribbleYaw;

        public PlayerAgent CurrentOwner { get; private set; }

        private float _releaseTimer;
        private PlayerAgent _aimLock;
        private Collider _ballCollider;
        private PlayerAgent _ignoredOwner;

        private void Awake()
        {
            if (AimLockRadius <= 0f) AimLockRadius = 3f;
            if (MinDribbleDistance < 0f) MinDribbleDistance = 0.5f;
            if (DribbleDistance <= MinDribbleDistance) DribbleDistance = 1.0f;
            if (DribbleFrequency <= 0f) DribbleFrequency = 1.4f;
            if (DribbleSpring <= 0f) DribbleSpring = 70f;
            if (DribbleMaxSpeed <= 0f) DribbleMaxSpeed = 12f;
        }

        private void FixedUpdate()
        {
            if (Ball == null || Ball.Rb == null || Agents == null) return;

            if (_ballCollider == null)
                _ballCollider = Ball.GetComponent<Collider>();

            TackleSystem.Tick(Time.fixedDeltaTime, Ball, this);

            SyncOwnerCollision(CurrentOwner);

            if (_releaseTimer > 0f)
                _releaseTimer -= Time.fixedDeltaTime;

            if (_aimLock != null)
            {
                bool closeEnough = Vector3.Distance(_aimLock.transform.position, Ball.transform.position) <= AimLockRadius;

                if (_aimLock.StunTimer <= 0f && closeEnough)
                {
                    CurrentOwner = _aimLock;
                    SyncOwnerCollision(CurrentOwner);
                    ApplyDribble(_aimLock);
                    return;
                }

                _aimLock = null;
            }

            // The current carrier keeps the ball until a tackle or a deliberate release.
            // Proximity alone no longer steals possession.
            if (CurrentOwner != null)
            {
                PlayerMotor ownerMotor = CurrentOwner.GetComponent<PlayerMotor>();
                if (ownerMotor != null && ownerMotor.IsDribbleActionActive)
                {
                    // A dribble burst can briefly exceed the normal speed/distance
                    // keep thresholds. Lock ownership through the gesture so the
                    // ball never goes loose at the moment of the move.
                    ApplyDribble(CurrentOwner);
                    return;
                }

                bool canKeep = CurrentOwner.ControlEnabled &&
                               CurrentOwner.StunTimer <= 0f &&
                               Ball.Rb.velocity.magnitude <= MaxControlSpeed &&
                               Vector3.Distance(CurrentOwner.transform.position, Ball.transform.position) <= ControlRadius * 1.5f;

                if (canKeep)
                {
                    ApplyDribble(CurrentOwner);
                    return;
                }

                CurrentOwner = null;
                SyncOwnerCollision(null);
            }

            // Loose-ball magnet: only when no tackle action is in progress and no one
            // owns the ball. The toe is used as the proximity reference.
            if (_releaseTimer > 0f || TackleSystem.IsAnyAttemptActive) return;

            PlayerAgent best = null;
            float bestDistance = LoosePickupRadius;

            Vector3 ballFlat = Ball.transform.position;
            ballFlat.y = 0f;

            for (int i = 0; i < Agents.Length; i++)
            {
                PlayerAgent candidate = Agents[i];
                if (candidate == null || !candidate.ControlEnabled) continue;
                if (candidate.StunTimer > 0f || candidate.TackleActionTimer > 0f) continue;

                // Use the player's planar body distance for loose-ball pickup, not the
                // animated toe. This claims before the capsule shoves the ball away.
                Vector3 point = candidate.transform.position;
                point.y = 0f;
                float distance = Vector3.Distance(point, ballFlat);
                if (distance > bestDistance) continue;

                best = candidate;
                bestDistance = distance;
            }

            if (best == null) return;

            CurrentOwner = best;
            SyncOwnerCollision(CurrentOwner);
            ApplyDribble(best);
        }

        public bool TryClaimFromFoot(PlayerAgent agent)
        {
            if (agent == null || Ball == null) return false;
            if (CurrentOwner != null || _aimLock != null || _releaseTimer > 0f) return false;
            if (agent.StunTimer > 0f || !agent.ControlEnabled) return false;

            CurrentOwner = agent;
            _releaseTimer = 0f;
            SyncOwnerCollision(agent);
            ApplyDribble(agent);
            return true;
        }

        private void ApplyDribble(PlayerAgent agent)
        {
            // The ball orbits on the ground around the player center, between the
            // 0.5m and 1.0m radius circles, on the toe-direction line.
            // During the shot wind-up the ball stays owned by the player but creeps
            // forward slowly instead of being launched at gesture release.
            PlayerMotor motor = agent.GetComponent<PlayerMotor>();
            if (motor != null && motor.IsShooting)
            {
                Vector3 shotDirection = motor.ShotDirection;
                Ball.Rb.velocity = new Vector3(
                    shotDirection.x * motor.ShotWindupBallSpeed,
                    0f,
                    shotDirection.z * motor.ShotWindupBallSpeed);
                Ball.Rb.angularVelocity = Vector3.Lerp(Ball.Rb.angularVelocity, Vector3.zero, 0.25f);
                _lastDribbleOwner = agent;
                _lastDribbleYaw = agent.transform.eulerAngles.y;
                return;
            }

            if (motor != null && motor.IsDribbleBurst)
            {
                Vector3 burstTarget = agent.transform.position + motor.DribbleDirection * 1.2f;
                burstTarget.y = Ball.RestHeight;
                Vector3 burstVelocity = (burstTarget - Ball.Rb.position) * 100f;
                // The burst is a short real-time explosion; keep the ball glued to
                // the move even though it exceeds the normal dribble speed.
                Ball.Rb.velocity = Vector3.ClampMagnitude(burstVelocity, 24f);
                Ball.Rb.angularVelocity = Vector3.Lerp(Ball.Rb.angularVelocity, Vector3.zero, 0.25f);
                _lastDribbleOwner = agent;
                _lastDribbleYaw = agent.transform.eulerAngles.y;
                return;
            }

            Vector3 bodyDirection = Vector3.ProjectOnPlane(agent.transform.forward, Vector3.up);
            if (bodyDirection.sqrMagnitude < 0.0001f)
                bodyDirection = agent.transform.forward;
            bodyDirection.Normalize();

            float phase = Time.time * Mathf.PI * 2f * DribbleFrequency +
                          (agent.IsHuman ? 0f : 0.7f);
            float normalized = (Mathf.Sin(phase) + 1f) * 0.5f;
            float distance = Mathf.Lerp(MinDribbleDistance, DribbleDistance, normalized);

            // The ball orbits on the ground between the 0.5m and 1.0m circles,
            // always on the line from player center along the body-forward direction.
            Vector3 center = agent.transform.position;
            center.y = Ball.RestHeight;
            Vector3 target = center + bodyDirection * distance;
            target.y = Ball.RestHeight;

            float yaw = agent.transform.eulerAngles.y;
            bool ownerChanged = _lastDribbleOwner != agent;
            bool snapped = false;

            if (!ownerChanged && Mathf.Abs(Mathf.DeltaAngle(_lastDribbleYaw, yaw)) > 12f)
            {
                Ball.Rb.position = target;
                Ball.Rb.velocity = Vector3.zero;
                snapped = true;
            }

            _lastDribbleOwner = agent;
            _lastDribbleYaw = yaw;

            Vector3 desired = (target - Ball.Rb.position) * DribbleSpring;
            Ball.Rb.velocity = snapped ? Vector3.zero : Vector3.ClampMagnitude(desired, DribbleMaxSpeed);
            Ball.Rb.angularVelocity = Vector3.Lerp(Ball.Rb.angularVelocity, Vector3.zero, 0.25f);
        }

        private void SyncOwnerCollision(PlayerAgent owner)
        {
            if (_ignoredOwner == owner || _ballCollider == null) return;

            if (_ignoredOwner != null)
            {
                Collider oldCollider = _ignoredOwner.GetComponent<Collider>();
                if (oldCollider != null)
                    Physics.IgnoreCollision(_ballCollider, oldCollider, false);
            }

            _ignoredOwner = owner;

            if (_ignoredOwner != null)
            {
                Collider newCollider = _ignoredOwner.GetComponent<Collider>();
                if (newCollider != null)
                    Physics.IgnoreCollision(_ballCollider, newCollider, true);
            }
        }

        public void BeginAim(PlayerAgent agent)
        {
            if (agent == null || Ball == null) return;

            if (CurrentOwner == agent || CurrentOwner == null)
            {
                _aimLock = agent;
                CurrentOwner = agent;
                _releaseTimer = 0f;
                SyncOwnerCollision(CurrentOwner);
            }
        }

        public void EndAim()
        {
            _aimLock = null;
        }

        public void ForceOwner(PlayerAgent agent)
        {
            _aimLock = null;
            CurrentOwner = agent;
            _releaseTimer = 0f;
            SyncOwnerCollision(CurrentOwner);
        }

        public void Release(PlayerAgent agent)
        {
            if (_aimLock == agent) _aimLock = null;
            if (CurrentOwner == agent) CurrentOwner = null;
            _releaseTimer = 0.35f;
            SyncOwnerCollision(CurrentOwner);
        }

        public void ResetPossession()
        {
            CurrentOwner = null;
            _aimLock = null;
            _releaseTimer = 0f;
            SyncOwnerCollision(null);
        }
    }
}
