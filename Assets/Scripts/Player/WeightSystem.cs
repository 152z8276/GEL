using UnityEngine;

namespace GEL
{
    public enum WeightState
    {
        Balanced,
        Lean,
        Commit,
        Recover
    }

    public class WeightSystem : MonoBehaviour
    {
        public PlayerAgent Agent;
        public PlayerMotor Motor;

        public float MaxLean = 0.75f;
        public float LeanSmooth = 12f;
        public float RecoverTime = 0.22f;

        public WeightState State { get; private set; } = WeightState.Balanced;
        public Vector2 Weight { get; private set; }
        public Vector2 Tilt { get; private set; }
        public float LeanAmount => Weight.magnitude;
        public float Balance => 1f - Mathf.Clamp01(LeanAmount);
        public Vector3 CommitDirection { get; private set; }
        public Vector3 LastLeanDirection { get; private set; }

        private Vector2 _target;
        private Vector2 _gestureLean;
        private bool _gestureActive;
        private float _recoverTimer;

        private void Awake()
        {
            if (Agent == null) Agent = GetComponent<PlayerAgent>();
            if (Motor == null) Motor = GetComponent<PlayerMotor>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_recoverTimer > 0f)
            {
                _recoverTimer = Mathf.Max(0f, _recoverTimer - dt);
                if (_recoverTimer <= 0f)
                    State = WeightState.Balanced;
                else
                    State = WeightState.Recover;
            }

            Vector2 target = _target;
            if (!_gestureActive)
                _gestureLean = Vector2.zero;

            target = _gestureLean + Tilt;
            target = Vector2.ClampMagnitude(target, MaxLean);

            float blend = 1f - Mathf.Exp(-LeanSmooth * dt);
            Weight = Vector2.Lerp(Weight, target, blend);
            if (Weight.sqrMagnitude < 0.0004f) Weight = Vector2.zero;

            if (_recoverTimer <= 0f)
            {
                if (Weight.magnitude > 0.04f)
                    State = WeightState.Lean;
                else if (State == WeightState.Commit)
                    State = WeightState.Balanced;
            }
        }

        public void SetGestureLean(Vector2 screenDelta)
        {
            float strength = Mathf.Clamp01(screenDelta.magnitude / 180f);
            if (strength <= 0.01f)
            {
                ClearGestureLean();
                return;
            }

            _gestureActive = true;
            _gestureLean = screenDelta.normalized * strength * MaxLean;
            if (_recoverTimer <= 0f && State != WeightState.Commit)
                State = WeightState.Lean;
        }

        public void ClearGestureLean()
        {
            _gestureActive = false;
            _gestureLean = Vector2.zero;
        }

        public void SetTilt(Vector2 tilt)
        {
            Tilt = Vector2.ClampMagnitude(tilt, 1f) * MaxLean;
        }

        public void Commit(Vector3 worldDirection)
        {
            LastLeanDirection = WorldLeanDirection;
            CommitDirection = worldDirection;
            _gestureActive = false;
            _gestureLean = Vector2.zero;
            _recoverTimer = RecoverTime;
            State = WeightState.Commit;
        }

        public Vector3 WorldLeanDirection
        {
            get
            {
                if (Agent == null || Weight.sqrMagnitude < 0.0001f)
                    return Vector3.zero;

                Vector3 right = Agent.transform.right;
                Vector3 forward = Agent.transform.forward;
                right.y = 0f;
                forward.y = 0f;
                right.Normalize();
                forward.Normalize();
                return (right * Weight.x + forward * Weight.y).normalized;
            }
        }

        public float GetFeintAdvantage(PlayerAgent defender, Vector3 exitDirection)
        {
            Vector3 leanDirection = WorldLeanDirection;
            if (leanDirection.sqrMagnitude < 0.01f)
                leanDirection = LastLeanDirection;
            if (defender == null || leanDirection.sqrMagnitude < 0.01f)
                return 0f;

            Vector3 defenderVelocity = defender.Rb != null
                ? defender.Rb.velocity
                : Vector3.zero;
            defenderVelocity.y = 0f;

            if (defenderVelocity.sqrMagnitude < 0.09f)
                return 0f;

            float defenderBuysLean = Vector3.Dot(
                defenderVelocity.normalized,
                leanDirection.normalized);
            float exitAwayFromLean = 1f - Vector3.Dot(
                exitDirection.normalized,
                leanDirection.normalized);

            return Mathf.Clamp01(defenderBuysLean * Mathf.Clamp01(exitAwayFromLean));
        }
    }
}
