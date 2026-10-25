using UnityEngine;

namespace GEL
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class BallSimulator : MonoBehaviour
    {
        public Rigidbody Rb;
        public float MagnusCoefficient = 0.18f;
        public float SpinDecay = 1.2f;
        public float MaxSpeed = 30f;
        public float ResetHeight = -2f;
        public float RestHeight = 0.16f;
        public float BallisticGravity = 20f;

        public Vector3 Spin { get; private set; }
        public bool IsFollowingCurve => _curveActive;

        private bool _curveActive;
        private Vector3 _curveP0;
        private Vector3 _curveP1;
        private Vector3 _curveP2;
        private float _curveLength;
        private float _curveT;
        private float _curveSpeed;
        private float _curveDecay;
        private float _curveStartTime;

        private bool _ballisticActive;
        private Vector3 _ballisticStart;
        private Vector3 _ballisticGoal;
        private float _ballisticPathLength;
        private float _ballisticSpeed;
        private float _ballisticGravity;
        private float _ballisticTime;
        private float _ballisticFlightTime;

        private void Reset()
        {
            Rb = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            if (Rb == null) Rb = GetComponent<Rigidbody>();
            if (RestHeight <= 0f) RestHeight = 0.16f;
        }

        private void FixedUpdate()
        {
            if (Rb == null) return;

            if (_ballisticActive)
            {
                UpdateBallistic();
                if (_ballisticActive) return;
            }
            else if (_curveActive)
            {
                UpdateCurve();
                if (!_curveActive) return;
            }
            else
            {
                if (Rb.position.y > 0.3f && Spin.sqrMagnitude > 0.01f)
                {
                    Vector3 magnus = Vector3.Cross(Spin, Rb.velocity) * MagnusCoefficient;
                    Rb.AddForce(magnus, ForceMode.Acceleration);
                }

                Spin = Vector3.Lerp(Spin, Vector3.zero, SpinDecay * Time.fixedDeltaTime);

                if (Rb.velocity.magnitude > MaxSpeed)
                    Rb.velocity = Rb.velocity.normalized * MaxSpeed;
            }

            if (Rb.position.y < ResetHeight || Mathf.Abs(Rb.position.x) > 21f || Mathf.Abs(Rb.position.z) > 13f)
                ResetBall(new Vector3(0f, RestHeight, 0f));
        }

        private void UpdateCurve()
        {
            float dt = Time.fixedDeltaTime;
            _curveSpeed *= Mathf.Exp(-_curveDecay * dt);
            _curveT += (_curveSpeed * dt) / Mathf.Max(0.1f, _curveLength);

            if (_curveT >= 1f)
            {
                StopCurve();
                return;
            }

            Vector3 target = CurvePoint(_curveT);
            Vector3 tangent = CurveTangent(_curveT).normalized;
            Rb.velocity = tangent * _curveSpeed + (target - Rb.position) * 8f;
        }

        public void LaunchBallistic(Vector3 start, Vector3 goal, float curve, float speed, float gravity)
        {
            if (Rb == null) return;

            StopCurve();
            StopBallistic();

            Vector3 flat = goal - start;
            flat.y = 0f;
            float chord = Mathf.Max(0.1f, flat.magnitude);
            Vector3 direction = flat.sqrMagnitude > 0.0001f ? flat / chord : Vector3.forward;
            Vector3 perpendicular = Vector3.Cross(Vector3.up, direction).normalized;
            float lateral = Mathf.Clamp(curve, -1f, 1f) * Mathf.Min(8f, chord * 0.35f);

            _ballisticStart = start;
            _ballisticGoal = goal;
            _curveP0 = start;
            _curveP2 = goal;
            _curveP1 = (start + goal) * 0.5f + perpendicular * lateral;
            _curveP1.y = start.y;

            _ballisticPathLength = EstimateLength(_curveP0, _curveP1, _curveP2);
            _ballisticSpeed = Mathf.Clamp(speed, 4f, MaxSpeed);
            _ballisticGravity = Mathf.Max(1f, gravity);
            _ballisticTime = 0f;
            _ballisticFlightTime = 2f * _ballisticSpeed / _ballisticGravity;
            _ballisticActive = true;

            Rb.useGravity = false;
            Rb.position = start;
            Rb.velocity = Vector3.zero;
            Rb.angularVelocity = Vector3.zero;
            Rb.WakeUp();
        }

        private void UpdateBallistic()
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            _ballisticTime += dt;

            float horizontalDistance = _ballisticSpeed * _ballisticTime;
            float s = Mathf.Clamp01(horizontalDistance / Mathf.Max(0.1f, _ballisticPathLength));

            Vector3 target = CurvePoint(s);
            float height = _ballisticStart.y +
                           _ballisticSpeed * _ballisticTime -
                           0.5f * _ballisticGravity * _ballisticTime * _ballisticTime;
            target.y = height;

            Vector3 delta = target - Rb.position;
            Rb.velocity = delta / dt;

            if (_ballisticTime >= _ballisticFlightTime || target.y <= RestHeight)
            {
                float horizontalX = target.x;
                float horizontalZ = target.z;
                StopBallistic();
                Rb.position = new Vector3(horizontalX, Mathf.Max(RestHeight, target.y), horizontalZ);
                Rb.velocity = new Vector3(
                    (target.x - Rb.position.x) / dt,
                    0f,
                    (target.z - Rb.position.z) / dt);
            }
        }

        public void StopBallistic()
        {
            _ballisticActive = false;
            if (Rb != null) Rb.useGravity = true;
        }

        public void LaunchCurve(Vector3 start, Vector3 control, Vector3 end, float speed, float decay)
        {
            if (Rb == null) return;

            StopBallistic();
            _curveP0 = start;
            _curveP1 = control;
            _curveP2 = end;
            _curveLength = EstimateLength(start, control, end);
            _curveT = 0f;
            _curveSpeed = speed;
            _curveDecay = decay;
            _curveStartTime = Time.time;
            _curveActive = true;

            Rb.useGravity = false;
            Rb.velocity = CurveTangent(0f).normalized * speed;
            Rb.angularVelocity = Vector3.zero;
            Rb.WakeUp();
        }

        public void StopCurve()
        {
            _curveActive = false;
            if (Rb != null) Rb.useGravity = true;
        }

        public void Kick(Vector3 velocity, Vector3 spin)
        {
            if (Rb == null) return;

            StopBallistic();
            StopCurve();
            Rb.velocity = velocity;
            Spin = spin;
            Rb.WakeUp();
        }

        public void ResetBall(Vector3 position)
        {
            if (Rb == null) return;

            StopBallistic();
            StopCurve();
            Spin = Vector3.zero;
            Rb.velocity = Vector3.zero;
            Rb.angularVelocity = Vector3.zero;
            Rb.position = position;
            transform.position = position;
            Rb.WakeUp();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_ballisticActive && _ballisticTime > 0.08f)
                StopBallistic();

            if (_curveActive && Time.time - _curveStartTime > 0.08f)
                StopCurve();
        }

        private Vector3 CurvePoint(float t)
        {
            float u = 1f - t;
            return u * u * _curveP0 + 2f * u * t * _curveP1 + t * t * _curveP2;
        }

        private Vector3 CurveTangent(float t)
        {
            return 2f * (1f - t) * (_curveP1 - _curveP0) + 2f * t * (_curveP2 - _curveP1);
        }

        private float EstimateLength(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            const int samples = 24;
            float length = 0f;
            Vector3 previous = p0;

            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                float u = 1f - t;
                Vector3 point = u * u * p0 + 2f * u * t * p1 + t * t * p2;
                length += Vector3.Distance(previous, point);
                previous = point;
            }

            return Mathf.Max(0.1f, length);
        }
    }
}
