using UnityEngine;

namespace GEL
{
    public class MobileControls : MonoBehaviour
    {
        public PlayerMotor Player;
        public GestureInput Gesture;
        public TiltInput Tilt;
        public bool ShowInEditor;
        public float JoystickRadius = 110f;
        public float JoystickMargin = 70f;

        public static MobileControls Instance { get; private set; }

        public Vector2 InputVector { get; private set; }

        private int _joystickTouchId = -1;
        private Texture2D _baseTexture;
        private Texture2D _knobTexture;

        private bool Visible => Application.isMobilePlatform || ShowInEditor;

        private void Awake()
        {
            Instance = this;
            CreateTextures();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_baseTexture != null) Destroy(_baseTexture);
            if (_knobTexture != null) Destroy(_knobTexture);
        }

        private void Update()
        {
            if (!Visible || Player == null)
            {
                InputVector = Vector2.zero;
                if (Player != null) Player.VirtualInput = Vector2.zero;
                return;
            }

            bool found = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (_joystickTouchId < 0 && touch.phase == TouchPhase.Began &&
                    IsTouchOnJoystick(touch.position))
                {
                    _joystickTouchId = touch.fingerId;
                    found = true;
                }

                if (touch.fingerId != _joystickTouchId) continue;

                found = true;
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    _joystickTouchId = -1;
                    InputVector = Vector2.zero;
                    break;
                }

                Vector2 delta = touch.position - JoystickCenter;
                float radius = Mathf.Max(1f, JoystickRadius * 0.72f);
                InputVector = Vector2.ClampMagnitude(delta / radius, 1f);
            }

            if (!found && _joystickTouchId >= 0)
            {
                _joystickTouchId = -1;
                InputVector = Vector2.zero;
            }

            Player.VirtualInput = InputVector;
        }

        public bool IsTouchOnJoystick(Vector2 screenPosition)
        {
            if (!Visible) return false;
            return (screenPosition - JoystickCenter).sqrMagnitude <=
                   JoystickRadius * JoystickRadius;
        }

        public bool IsTouchOnUi(Vector2 screenPosition)
        {
            if (!Visible) return false;
            if (IsTouchOnJoystick(screenPosition)) return true;

            Rect tiltRect = TiltButtonRect;
            Rect calRect = CalibrateButtonRect;
            Vector2 guiPoint = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return tiltRect.Contains(guiPoint) || calRect.Contains(guiPoint);
        }

        private Rect TiltButtonRect
        {
            get
            {
                Rect safe = Screen.safeArea;
                return new Rect(safe.xMax - 176f, Screen.height - safe.yMax + 18f, 158f, 46f);
            }
        }

        private Rect CalibrateButtonRect
        {
            get
            {
                Rect safe = Screen.safeArea;
                return new Rect(safe.xMax - 176f, Screen.height - safe.yMax + 72f, 158f, 46f);
            }
        }

        private Vector2 JoystickCenter
        {
            get
            {
                Rect safe = Screen.safeArea;
                return new Vector2(
                    safe.x + JoystickMargin + JoystickRadius,
                    safe.y + JoystickMargin + JoystickRadius);
            }
        }

        private void OnGUI()
        {
            if (!Visible || _baseTexture == null || _knobTexture == null) return;

            Vector2 center = JoystickCenter;
            float radius = JoystickRadius;
            Rect baseRect = new Rect(
                center.x - radius,
                Screen.height - center.y - radius,
                radius * 2f,
                radius * 2f);

            GUI.color = new Color(0.15f, 0.65f, 1f, 0.42f);
            GUI.DrawTexture(baseRect, _baseTexture, ScaleMode.ScaleToFit, true);

            Vector2 knobCenter = center + InputVector * (radius * 0.55f);
            float knobRadius = radius * 0.42f;
            Rect knobRect = new Rect(
                knobCenter.x - knobRadius,
                Screen.height - knobCenter.y - knobRadius,
                knobRadius * 2f,
                knobRadius * 2f);

            GUI.color = new Color(0.45f, 0.95f, 1f, 0.82f);
            GUI.DrawTexture(knobRect, _knobTexture, ScaleMode.ScaleToFit, true);

            GUI.color = Color.white;
            GUI.Label(new Rect(baseRect.x, baseRect.y - 26f, baseRect.width, 22f),
                "MOVE", new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    normal = { textColor = new Color(0.65f, 0.9f, 1f, 0.9f) }
                });

            if (Tilt != null)
            {
                GUI.color = Color.white;
                string tiltLabel = Tilt.Enabled ? "TILT: ON" : "TILT: OFF";
                if (GUI.Button(TiltButtonRect, tiltLabel))
                {
                    Tilt.Enabled = !Tilt.Enabled;
                    if (Tilt.Enabled) Tilt.BeginCalibration();
                }

                if (GUI.Button(CalibrateButtonRect, "CALIBRATE"))
                    Tilt.BeginCalibration();
            }
        }

        private void CreateTextures()
        {
            _baseTexture = CreateCircleTexture(128, false);
            _knobTexture = CreateCircleTexture(128, true);
        }

        private static Texture2D CreateCircleTexture(int size, bool filled)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            float outer = size * 0.5f - 1f;
            float inner = filled ? size * 0.30f : size * 0.40f;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 0f;
                    if (filled)
                    {
                        alpha = Mathf.Clamp01((outer - distance) / 2f);
                    }
                    else
                    {
                        float ring = Mathf.Abs(distance - (outer - 3f));
                        alpha = Mathf.Clamp01((4f - ring) / 3f) * 0.7f;
                        alpha += Mathf.Clamp01((inner - distance) / 2f) * 0.35f;
                    }

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
