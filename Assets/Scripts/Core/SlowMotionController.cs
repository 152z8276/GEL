using UnityEngine;

namespace GEL
{
    public class SlowMotionController : MonoBehaviour
    {
        public MatchDirector Match;
        public PlayerAgent Player;
        public AIController AI;
        public GestureInput Gesture;
        public float SlowScale = 0.25f;
        public float TransitionSpeed = 6f;
        public float DuelDistance = 3f;
        public bool AutoSlowMotion = true;

        public bool IsSlowMotion => Time.timeScale < 0.95f;

        private float _suppressUntil;
        private float _forceUntil;

        public void ForceExit(float suppressSeconds = 0.6f)
        {
            _suppressUntil = Mathf.Max(_suppressUntil, Time.unscaledTime + suppressSeconds);
            _forceUntil = 0f;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        public void ForceEnter(float seconds = 0.6f)
        {
            _suppressUntil = 0f;
            _forceUntil = Mathf.Max(_forceUntil, Time.unscaledTime + seconds);
        }

        private void Awake()
        {
            if (Match == null) Match = FindObjectOfType<MatchDirector>();
            if (Gesture == null) Gesture = FindObjectOfType<GestureInput>();
        }

        private void Start()
        {
            if (Match == null) return;

            if (Player == null) Player = Match.PlayerAgent;

            if (AI == null && Match.AIAgent != null)
                AI = Match.AIAgent.GetComponent<AIController>();
        }

        private void Update()
        {
            bool canSlow = Match == null || Match.State == MatchState.Playing;

            bool shooting = Gesture != null &&
                            Gesture.Player != null &&
                            Gesture.Player.IsShooting;

            // Gesture completion exits bullet time immediately; the shot wind-up
            // then plays in normal time while the ball creeps forward.
            if (shooting)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
                return;
            }

            if (Time.unscaledTime < _suppressUntil)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
                return;
            }

            // Never enter bullet time while the ball is loose. This gate covers manual,
            // aiming and proximity conditions, not just the duel trigger.
            bool hasPossession = Match != null &&
                                 Match.Possession != null &&
                                 Match.Possession.CurrentOwner != null;

            bool manual = Input.GetKey(KeyCode.Space);
            bool aiming = Gesture != null &&
                          Gesture.Player != null &&
                          (Gesture.IsShotDrag || Gesture.Player.IsShooting) &&
                          Gesture.Player.HasBall();
            bool dribbling = (Gesture != null && Gesture.IsDribbleDrag) ||
                             (Gesture != null && Gesture.Player != null &&
                              Gesture.Player.IsDribbleActionActive);
            bool duel = AutoSlowMotion && Player != null && AI != null && AI.Automatic &&
                        !dribbling &&
                        Vector3.Distance(Player.transform.position, AI.transform.position) <= DuelDistance;
            bool forced = Time.unscaledTime < _forceUntil;

            bool slow = canSlow && hasPossession && (manual || aiming || duel || forced);
            float target = slow ? SlowScale : 1f;

            Time.timeScale = Mathf.MoveTowards(Time.timeScale, target, TransitionSpeed * Time.unscaledDeltaTime);
            Time.fixedDeltaTime = 0.02f * Mathf.Max(0.05f, Time.timeScale);
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}
