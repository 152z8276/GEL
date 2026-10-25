using UnityEngine;

namespace GEL
{
    public class ShoulderCamera : MonoBehaviour
    {
        public Transform Target;
        public PossessionManager Possession;
        public BallSimulator Ball;
        public PlayerAgent PlayerAgent;

        public Vector3 Offset = new Vector3(-3.0f, 1.35f, -1.1f);
        public Vector3 LookOffset = new Vector3(3.5f, -0.55f, 0f);
        public Vector3 CloseOffset = new Vector3(-1.7f, 1.35f, -0.6f);

        public float FollowSharpness = 6f;
        public float CloseFollowSharpness = 14f;
        public float ZoomSpeed = 12f;
        public float YawSpeed = 900f;
        public float SlowThreshold = 0.9f;
        public float NormalFov = 58f;
        public float DuelFov = 42f;

        [Header("Jitter damping")]
        public float PositionSmoothTime = 0.045f;
        public float RotationSharpness = 35f;
        public float YawSmoothTime = 0.035f;
        public float FeedForwardSmoothTime = 0.12f;

        private Camera _camera;
        private float _zoom;
        private float _baseYaw;
        private float _targetYaw;
        private Vector3 _positionVelocity;
        private float _yawVelocity;
        private Vector3 _velocityFeed;
        private Vector3 _velocityFeedVelocity;

        // At yaw 0 the rig sits behind the player on -X and looks toward +X.
        private Quaternion PlanarRotation => Quaternion.Euler(0f, _baseYaw + 90f, 0f);
        public Vector3 PlanarForward => PlanarRotation * Vector3.forward;
        public Vector3 PlanarRight => PlanarRotation * Vector3.right;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (Possession == null) Possession = FindObjectOfType<PossessionManager>();
            if (Ball == null) Ball = FindObjectOfType<BallSimulator>();
            if (PlayerAgent == null && Target != null) PlayerAgent = Target.GetComponent<PlayerAgent>();
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            // Camera heading is always the player body heading, with a tiny
            // angular damp to remove fixed-step rotation jitter.
            _targetYaw = Mathf.Repeat(Target.eulerAngles.y - 90f, 360f);

            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            _baseYaw = Mathf.SmoothDampAngle(
                _baseYaw, _targetYaw, ref _yawVelocity, YawSmoothTime,
                Mathf.Infinity, dt);

            float targetZoom = Time.timeScale < SlowThreshold ? 1f : 0f;
            _zoom = Mathf.MoveTowards(_zoom, targetZoom, ZoomSpeed * dt);

            Vector3 targetFeed = GetPlayerVelocity() * 0.12f;
            _velocityFeed = Vector3.SmoothDamp(
                _velocityFeed, targetFeed, ref _velocityFeedVelocity,
                FeedForwardSmoothTime, Mathf.Infinity, dt);

            ComputePose(_baseYaw, out Vector3 offset, out Vector3 lookPoint);

            Vector3 desiredPosition = Target.position + offset;
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref _positionVelocity,
                PositionSmoothTime, Mathf.Infinity, dt);

            Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            float rotationT = 1f - Mathf.Exp(-RotationSharpness * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);

            if (_camera != null)
                _camera.fieldOfView = Mathf.Lerp(NormalFov, DuelFov, _zoom);
        }

        private void ComputePose(float cameraYaw, out Vector3 offset, out Vector3 lookPoint)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, cameraYaw, 0f);

            Vector3 normalOffset = yawRotation * Offset;
            Vector3 lowOffset = yawRotation * CloseOffset;
            Vector3 duelOffset = Vector3.Lerp(lowOffset, normalOffset, 0.15f);
            offset = Vector3.Lerp(normalOffset, duelOffset, _zoom);

            if (Ball != null && _zoom > 0f)
            {
                float ballOffsetY = Ball.transform.position.y - Target.position.y;
                offset.y = Mathf.Lerp(offset.y, ballOffsetY, _zoom);
            }

            Vector3 normalLook = Target.position + yawRotation * LookOffset;

            Vector3 foot = Target.position + Target.forward * 0.3f;
            foot.y = 0.15f;
            Vector3 ballPosition = Ball != null ? Ball.transform.position : foot;

            Vector3 feetFocus = Vector3.Lerp(foot, ballPosition, 0.3f);
            feetFocus += _velocityFeed;
            feetFocus.y = Mathf.Max(0.35f, feetFocus.y + 0.2f);

            Vector3 duelFocus = Vector3.Lerp(feetFocus, normalLook, 0.3f);
            lookPoint = Vector3.Lerp(normalLook, duelFocus, _zoom);

            if (Ball != null && _zoom > 0f)
            {
                // At ball height, aim above the ball so the goal and both players'
                // feet stay in frame while the ball sits in the foreground.
                Vector3 bulletLook = Ball.transform.position +
                                     Target.forward * 3.5f +
                                     Vector3.up * 0.9f;
                lookPoint = Vector3.Lerp(lookPoint, bulletLook, _zoom);
            }
        }

        private Vector3 GetPlayerVelocity()
        {
            if (PlayerAgent == null || PlayerAgent.Rb == null) return Vector3.zero;

            Vector3 velocity = PlayerAgent.Rb.velocity;
            velocity.y = 0f;
            return velocity;
        }

        public void Snap()
        {
            if (Target == null) return;

            ComputePose(_baseYaw, out Vector3 offset, out Vector3 lookPoint);

            _positionVelocity = Vector3.zero;
            _yawVelocity = 0f;
            _velocityFeed = Vector3.zero;
            _velocityFeedVelocity = Vector3.zero;

            transform.position = Target.position + offset;
            transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
        }

        public void ResetDirection()
        {
            _targetYaw = Target != null ? Mathf.Repeat(Target.eulerAngles.y - 90f, 360f) : 0f;
            _baseYaw = _targetYaw;
            _zoom = 0f;
            Snap();
        }
    }
}
