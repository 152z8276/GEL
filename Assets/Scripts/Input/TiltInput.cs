using UnityEngine;

namespace GEL
{
    public class TiltInput : MonoBehaviour
    {
        public WeightSystem Weight;
        public PlayerMotor Motor;

        public bool Enabled;
        public bool UseDebugKeys = true;
        public KeyCode ToggleKey = KeyCode.Y;
        public KeyCode CalibrateKey = KeyCode.U;

        public float MaxTiltDegrees = 22f;
        public float DeadzoneDegrees = 2.5f;
        public float FilterSharpness = 8f;
        public float CalibrationTime = 1f;
        public float Strength = 1f;
        public bool InvertX;
        public bool InvertY;

        private Vector2 _filtered;
        private Vector2 _neutral;
        private Vector2 _sum;
        private float _calibrationTimer;
        private int _calibrationSamples;
        private bool _calibrating;
        private bool _hasNeutral;
        private float _lastToggleTime;

        private void Awake()
        {
            if (Weight == null) Weight = GetComponent<WeightSystem>();
            if (Motor == null) Motor = GetComponent<PlayerMotor>();
        }

        private void Update()
        {
            if (UseDebugKeys)
            {
                if (Input.GetKeyDown(ToggleKey))
                {
                    Enabled = !Enabled;
                    if (Enabled) BeginCalibration();
                }

                if (Input.GetKeyDown(CalibrateKey))
                    BeginCalibration();
            }

            if (!Enabled || Weight == null || Motor == null)
            {
                if (Weight != null) Weight.SetTilt(Vector2.zero);
                return;
            }

            // Shot aiming and menus should not be affected by the phone tilt.
            if (Motor.IsShooting)
            {
                Weight.SetTilt(Vector2.zero);
                return;
            }

            Vector2 acceleration = new Vector2(
                Input.acceleration.x,
                Input.acceleration.y);

            float blend = 1f - Mathf.Exp(-FilterSharpness * Time.deltaTime);
            _filtered = Vector2.Lerp(_filtered, acceleration, blend);

            if (_calibrating)
            {
                _sum += _filtered;
                _calibrationSamples++;
                _calibrationTimer += Time.deltaTime;
                if (_calibrationTimer >= CalibrationTime)
                {
                    _neutral = _sum / Mathf.Max(1, _calibrationSamples);
                    _calibrating = false;
                    _hasNeutral = true;
                }
                Weight.SetTilt(Vector2.zero);
                return;
            }

            if (!_hasNeutral)
            {
                BeginCalibration();
                Weight.SetTilt(Vector2.zero);
                return;
            }

            float maxDelta = Mathf.Max(0.02f, Mathf.Sin(MaxTiltDegrees * Mathf.Deg2Rad));
            float deadzone = Mathf.Sin(DeadzoneDegrees * Mathf.Deg2Rad);
            Vector2 delta = _filtered - _neutral;

            if (Screen.orientation == ScreenOrientation.LandscapeLeft)
                delta.x = -delta.x;
            else if (Screen.orientation == ScreenOrientation.LandscapeRight)
                delta.y = -delta.y;

            if (InvertX) delta.x = -delta.x;
            if (InvertY) delta.y = -delta.y;

            float magnitude = delta.magnitude;
            if (magnitude <= deadzone)
            {
                Weight.SetTilt(Vector2.zero);
                return;
            }

            delta /= maxDelta;
            delta = Vector2.ClampMagnitude(delta, 1f);
            Weight.SetTilt(delta * Strength);
        }

        public void BeginCalibration()
        {
            _calibrating = true;
            _calibrationTimer = 0f;
            _calibrationSamples = 0;
            _sum = Vector2.zero;
        }

        private void OnDisable()
        {
            if (Weight != null) Weight.SetTilt(Vector2.zero);
        }
    }
}
