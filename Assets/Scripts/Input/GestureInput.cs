using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GEL
{
    public class GestureInput : MonoBehaviour
    {
        private enum GestureMode
        {
            None,
            Shot,
            Dribble
        }

        public PlayerMotor Player;
        public Camera Cam;
        public LineRenderer Trail;
        public float MinDragPixels = 24f;
        public float MinDribblePixels = 10f;
        public float MaxPowerPixels = 320f;
        public float BallHitRadius = 95f;
        public float MinShotDistance = 8f;
        public float MaxShotDistance = 30f;
        public float GesturePlaneDistance = 12f;
        public float CurveGain = 1.0f;
        public float VerticalClimbScale = 2.5f;

        public string LastResult { get; private set; } = "Drag to shoot";
        public bool IsDragging => _dragging;
        public bool IsShotDrag => _dragging && _mode == GestureMode.Shot;
        public bool IsDribbleDrag => _dragging && _mode == GestureMode.Dribble;

        private bool _dragging;
        private GestureMode _mode = GestureMode.None;
        private int _touchId = -1;
        private Vector2 _lastScreen;
        private Vector2 _startScreen;
        private float _startTime;
        private bool _hasLastWorld;
        private Vector3 _lastWorld;
        private readonly List<Vector3> _worldPoints = new List<Vector3>();
        private readonly List<Vector2> _screenPoints = new List<Vector2>();
        private Coroutine _clearRoutine;

        private void Update()
        {
            if (Player == null) return;
            if (Cam == null) Cam = Camera.main;

            if (Input.GetKeyDown(KeyCode.F))
            {
                Vector3 direction = Cam != null
                    ? Vector3.ProjectOnPlane(Cam.transform.forward, Vector3.up).normalized
                    : Vector3.right;

                bool success = Player.TryKick(direction, 0.8f, 0f, false);
                LastResult = success ? "Test shot" : "No ball";
            }

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
            {
                bool tackleSuccess = Player.TryTackle();
                LastResult = tackleSuccess ? "Tackle won" : "Tackle failed";
            }

            if (Input.touchCount > 0)
                HandleTouches();
            else
                HandleMouse();
        }

        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0))
                BeginDrag(Input.mousePosition);
            else if (_dragging && Input.GetMouseButton(0))
                ContinueDrag(Input.mousePosition);
            else if (_dragging && Input.GetMouseButtonUp(0))
                EndDrag(Input.mousePosition);
        }

        private void HandleTouches()
        {
            if (!_dragging)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase != TouchPhase.Began) continue;
                    if (MobileControls.Instance != null &&
                        MobileControls.Instance.IsTouchOnJoystick(touch.position))
                        continue;

                    BeginDrag(touch.position);
                    _touchId = touch.fingerId;
                    break;
                }

                return;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId != _touchId) continue;

                if (touch.phase == TouchPhase.Moved ||
                    touch.phase == TouchPhase.Stationary)
                {
                    ContinueDrag(touch.position);
                }
                else if (touch.phase == TouchPhase.Ended ||
                         touch.phase == TouchPhase.Canceled)
                {
                    EndDrag(touch.position);
                    _touchId = -1;
                }

                return;
            }

            // The gesture finger disappeared (focus loss / cancel).
            EndDrag(_lastScreen);
            _touchId = -1;
        }

        private void BeginDrag(Vector2 screenPosition)
        {
            _dragging = true;
            _startScreen = screenPosition;
            _lastScreen = screenPosition;
            _startTime = Time.unscaledTime;
            _hasLastWorld = false;
            _worldPoints.Clear();
            _screenPoints.Clear();

            _mode = IsBallStart(screenPosition) ? GestureMode.Shot : GestureMode.Dribble;

            if (Player != null)
            {
                Player.SetGestureLock(true, _mode == GestureMode.Dribble);
                if (_mode == GestureMode.Shot)
                    Player.BeginAim();
            }

            if (Trail != null)
            {
                Trail.positionCount = 0;
                Trail.enabled = true;
                Color trailColor = _mode == GestureMode.Shot
                    ? new Color(0.2f, 0.72f, 1f, 1f)
                    : new Color(1f, 0.82f, 0.16f, 1f);
                Trail.startColor = trailColor;
                Trail.endColor = trailColor;
            }

            AddTrailPoint(screenPosition);
        }

        private bool IsBallStart(Vector2 screenPosition)
        {
            if (Player == null || Player.Ball == null || Cam == null) return false;
            if (!Player.HasBall()) return false;

            Vector2 ballScreen = Cam.WorldToScreenPoint(Player.Ball.transform.position);
            float projectedRadius = Mathf.Max(BallHitRadius, Screen.height * 0.07f);

            Vector3 center = Player.Ball.transform.position;
            Vector3 edge = center + Cam.transform.right * Player.Ball.transform.localScale.x * 0.5f;
            Vector2 edgeScreen = Cam.WorldToScreenPoint(edge);
            projectedRadius = Mathf.Max(projectedRadius, Vector2.Distance(ballScreen, edgeScreen));

            return Vector2.Distance(screenPosition, ballScreen) <= projectedRadius;
        }

        private void ContinueDrag(Vector2 screenPosition)
        {
            _lastScreen = screenPosition;
            AddTrailPoint(screenPosition);
        }

        private void OnDisable()
        {
            _touchId = -1;
            if (Player != null)
                Player.SetGestureLock(false);
        }

        private void EndDrag(Vector2 screenPosition)
        {
            _dragging = false;
            AddTrailPoint(screenPosition);

            Vector2 delta = screenPosition - _startScreen;
            float minDrag = _mode == GestureMode.Dribble
                ? Mathf.Max(MinDribblePixels, Screen.height * 0.012f)
                : Mathf.Max(MinDragPixels, Screen.height * 0.02f);
            if (delta.magnitude < minDrag)
            {
                LastResult = "Tap";
                if (Player != null)
                {
                    Player.EndAim();
                    Player.SetGestureLock(false);
                }
                RestartTrailClear(0.15f);
                return;
            }

            if (_mode == GestureMode.Dribble)
            {
                // The screen gesture direction is the movement direction. Use the
                // camera's horizontal axes directly; no raycast or world projection
                // is involved. Screen-up = camera forward, screen-right = camera right.
                Vector3 forward = Cam != null
                    ? Vector3.ProjectOnPlane(Cam.transform.forward, Vector3.up)
                    : (Player != null ? Player.transform.forward : Vector3.forward);
                Vector3 right = Cam != null
                    ? Vector3.ProjectOnPlane(Cam.transform.right, Vector3.up)
                    : (Player != null ? Player.transform.right : Vector3.right);

                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                if (forward.sqrMagnitude < 0.001f)
                    forward = Player != null ? Player.transform.forward : Vector3.forward;
                if (right.sqrMagnitude < 0.001f)
                    right = Player != null ? Player.transform.right : Vector3.right;

                Vector2 screenDirection = delta.normalized;
                Vector3 dribbleDirection =
                    forward * screenDirection.y +
                    right * screenDirection.x;
                dribbleDirection.y = 0f;

                bool accepted = Player != null && Player.TryDribble(dribbleDirection);
                if (Player != null) Player.SetGestureLock(false);
                LastResult = !accepted
                    ? "Busy"
                    : (Player.LastActionWasDefensiveBurst
                        ? "Burst"
                        : (Player.LastDribbleSucceeded ? "Beat!" : "Dribble"));

                _mode = GestureMode.None;
                RestartTrailClear(0.25f);
                return;
            }

            // Anchor the gesture at the ball's screen position, so the fitted curve
            // starts at the ball even if the player began the drag elsewhere.
            Vector3 ballWorld = Player != null && Player.Ball != null
                ? Player.Ball.transform.position
                : (Player != null ? Player.transform.position : Vector3.zero);

            Vector2 ballScreen = Cam != null
                ? (Vector2)Cam.WorldToScreenPoint(ballWorld)
                : (Player != null ? (Vector2)Player.transform.position : Vector2.zero);

            List<Vector2> anchoredPoints = new List<Vector2>(_screenPoints.Count + 1);
            if (_screenPoints.Count > 0)
            {
                Vector2 offset = ballScreen - _screenPoints[0];
                for (int i = 0; i < _screenPoints.Count; i++)
                    anchoredPoints.Add(_screenPoints[i] + offset);
            }
            else
            {
                anchoredPoints.Add(ballScreen);
            }

            // Screen Y-up and world camera-right have opposite perpendicular signs,
            // so a rightward screen arc must become a positive world curve.
            float curve = Mathf.Clamp(
                -FitScreenCurveRatio(anchoredPoints) * CurveGain, -1f, 1f);

            List<Vector3> gesturePoints = new List<Vector3>(anchoredPoints.Count);
            for (int i = 0; i < anchoredPoints.Count; i++)
                gesturePoints.Add(ScreenToGesturePlane(anchoredPoints[i]));

            List<Vector3> shapePoints = gesturePoints.Count >= 2 ? gesturePoints : _worldPoints;
            float shotDistance = -1f;
            Vector3 worldDirection = Cam != null
                ? Vector3.ProjectOnPlane(Cam.transform.forward, Vector3.up).normalized
                : Vector3.right;

            if (shapePoints.Count >= 2)
            {
                Vector3 endWorld = shapePoints[shapePoints.Count - 1];
                Vector3 toEnd = endWorld - ballWorld;
                toEnd.y = 0f;

                if (toEnd.sqrMagnitude > 0.01f)
                {
                    worldDirection = toEnd.normalized;
                    shotDistance = Mathf.Clamp(
                        toEnd.magnitude, MinShotDistance, MaxShotDistance);
                }
            }

            // Final visual correction: compare the raw screen endpoint with the goal's
            // screen position (no spatial projection) and raise/lower the arc height.
            float verticalBias = 0f;
            if (Player != null && Player.AttackGoal != null && Cam != null)
            {
                Vector3 goalScreen3 = Cam.WorldToScreenPoint(Player.AttackGoal.position);
                float normalizedY = (screenPosition.y - goalScreen3.y) /
                                    Mathf.Max(1f, Screen.height);
                verticalBias = Mathf.Clamp(
                    normalizedY * VerticalClimbScale, -0.8f, 1.5f);
            }

            float power = Mathf.Clamp01(delta.magnitude / MaxPowerPixels);
            bool lob = delta.y > 0f && power > 0.85f && Mathf.Abs(curve) < 0.15f;

            bool success = Player != null &&
                           Player.TryKick(
                               worldDirection, power, curve, lob, shotDistance, verticalBias);
            if (Player != null)
            {
                Player.EndAim();
                Player.SetGestureLock(false);
            }

            if (!success)
            {
                LastResult = "No ball";
            }
            else
            {
                LastResult = (lob ? "Lob " : "Shot ") + Mathf.RoundToInt(power * 100f) + "%";
                if (Mathf.Abs(curve) > 0.12f)
                    LastResult += curve > 0f ? " / curve L" : " / curve R";
            }

            RestartTrailClear(0.35f);
        }

        private void AddTrailPoint(Vector2 screenPosition)
        {
            if (Cam == null) return;

            if (_screenPoints.Count < 128)
                _screenPoints.Add(screenPosition);

            if (Trail == null) return;

            Vector3 world = ScreenToGesturePlane(screenPosition);
            if (_hasLastWorld && Vector3.Distance(world, _lastWorld) < 0.05f) return;

            _hasLastWorld = true;
            _lastWorld = world;

            if (_worldPoints.Count < 128)
                _worldPoints.Add(world);

            if (Trail.positionCount < 128)
            {
                Trail.positionCount++;
                Trail.SetPosition(Trail.positionCount - 1, world + Vector3.up * 0.03f);
            }
            else
            {
                for (int i = 1; i < Trail.positionCount; i++)
                    Trail.SetPosition(i - 1, Trail.GetPosition(i));

                Trail.SetPosition(Trail.positionCount - 1, world + Vector3.up * 0.03f);
            }
        }

        private float FitScreenCurveRatio(List<Vector2> points)
        {
            if (points == null || points.Count < 3) return 0f;

            Vector2 start = points[0];
            Vector2 end = points[points.Count - 1];
            Vector2 chord = end - start;
            float length = chord.magnitude;
            if (length < 8f) return 0f;

            Vector2 direction = chord / length;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);

            float numerator = 0f;
            float denominator = 0f;

            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector2 relative = points[i] - start;
                float s = Mathf.Clamp01(Vector2.Dot(relative, direction) / length);
                float offset = Vector2.Dot(relative, perpendicular);
                float basis = 4f * s * (1f - s);
                numerator += offset * basis;
                denominator += basis * basis;
            }

            if (denominator < 0.0001f) return 0f;

            float deviation = numerator / denominator;
            return Mathf.Clamp(deviation / (length * 0.35f), -1f, 1f);
        }

        private float FitCurveRatio(List<Vector3> points)
        {
            if (points == null || points.Count < 3) return 0f;

            Vector3 start = points[0];
            Vector3 end = points[points.Count - 1];
            Vector3 chord = end - start;
            float length = chord.magnitude;

            if (length < 0.5f) return 0f;

            Vector3 direction = chord / length;
            Vector3 perpendicular = Vector3.Cross(Vector3.up, direction).normalized;

            float numerator = 0f;
            float denominator = 0f;

            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector3 relative = points[i] - start;
                float s = Mathf.Clamp01(Vector3.Dot(relative, direction) / length);
                float offset = Vector3.Dot(relative, perpendicular);
                float basis = 4f * s * (1f - s);

                numerator += offset * basis;
                denominator += basis * basis;
            }

            if (denominator < 0.0001f) return 0f;

            float deviation = numerator / denominator;
            return Mathf.Clamp(deviation / (length * 0.35f), -1f, 1f);
        }

        private Vector3 ScreenToGesturePlane(Vector2 screenPosition)
        {
            Ray ray = Cam.ScreenPointToRay(screenPosition);
            Vector3 forward = Cam.transform.forward;
            Vector3 planePoint = Cam.transform.position + forward * GesturePlaneDistance;
            Plane plane = new Plane(-forward, planePoint);

            if (plane.Raycast(ray, out float enter))
                return ray.GetPoint(enter);

            return planePoint;
        }

        private void RestartTrailClear(float seconds)
        {
            if (_clearRoutine != null) StopCoroutine(_clearRoutine);
            _clearRoutine = StartCoroutine(ClearTrailAfter(seconds));
        }

        private IEnumerator ClearTrailAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (Trail != null) Trail.positionCount = 0;
        }
    }
}
