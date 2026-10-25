using UnityEngine;

namespace GEL
{
    public class AIController : MonoBehaviour
    {
        public PlayerAgent Agent;
        public PossessionManager Possession;
        public BallSimulator Ball;
        public PlayerAgent Human;
        public Transform AttackGoal;
        public Transform DefendGoal;
        public float MoveSpeed = 6.8f;
        public float Acceleration = 18f;
        public float TurnSharpness = 5f;
        public float ReactionTime = 0.18f;
        public float TackleRange = 1.6f;
        public float ShootRange = 8f;

        [Header("Shot")]
        public float ShotAbility = 1f;
        public float ShotSpeed = 20f;
        public float ShotDistance = 26f;

        [Header("Debug")]
        public bool Automatic = true;
        public bool ShowDebug = true;
        public KeyCode ToggleKey = KeyCode.T;

        private Vector3 _planarVelocity;
        private float _tackleCooldown;
        private float _shootCooldown;
        private Vector3 _debugTarget;
        private bool _hasDebugTarget;
        private Vector3 _cachedTarget;
        private float _reactionTimer;
        private bool _hasCachedTarget;
        private PlayerAnimationDriver _animationDriver;

        private void Awake()
        {
            if (Agent == null) Agent = GetComponent<PlayerAgent>();
            _animationDriver = GetComponentInChildren<PlayerAnimationDriver>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                Automatic = !Automatic;

                if (Agent != null)
                    Agent.ControlEnabled = Automatic;

                if (!Automatic && Possession != null)
                    Possession.Release(Agent);
            }
            else if (Agent != null && Agent.ControlEnabled != Automatic)
            {
                Agent.ControlEnabled = Automatic;
            }
        }

        private void FixedUpdate()
        {
            if (Agent == null || Agent.Rb == null || Ball == null || Possession == null) return;

            if (!Automatic)
            {
                Agent.Rb.velocity = new Vector3(0f, Agent.Rb.velocity.y, 0f);
                Agent.Rb.angularVelocity = Vector3.zero;
                _planarVelocity = Vector3.zero;
                _hasDebugTarget = false;
                _hasCachedTarget = false;
                return;
            }

            if (Agent.StunTimer > 0f || Agent.TackleActionTimer > 0f)
            {
                Agent.StopMotion();
                _planarVelocity = Vector3.zero;
                _hasCachedTarget = false;
                return;
            }

            _tackleCooldown = Mathf.Max(0f, _tackleCooldown - Time.fixedDeltaTime);
            _shootCooldown = Mathf.Max(0f, _shootCooldown - Time.fixedDeltaTime);

            Vector3 target;

            if (Possession.CurrentOwner == Agent)
            {
                Vector3 goal = AttackGoal != null ? AttackGoal.position : new Vector3(-18f, 0f, 0f);
                target = new Vector3(goal.x + 1.5f, transform.position.y, Mathf.Clamp(Ball.transform.position.z, -2.5f, 2.5f));

                if (transform.position.x < goal.x + ShootRange && _shootCooldown <= 0f)
                    Shoot(goal);
            }
            else if (Human != null && Possession.CurrentOwner == Human)
            {
                Vector3 ownGoal = DefendGoal != null ? DefendGoal.position : new Vector3(18f, 0f, 0f);
                Vector3 toGoal = ownGoal - Ball.transform.position;
                toGoal.y = 0f;
                target = Ball.transform.position + toGoal.normalized * 1.1f;

                // The defender reads the attacker's weight one reaction beat late.
                // This gives a visible committed lean that a feint can exploit.
                if (Human.Weight != null)
                {
                    Vector3 lean = Human.Weight.WorldLeanDirection;
                    target += lean * 0.9f;
                }

                float distance = Vector3.Distance(transform.position, Ball.transform.position);
                if (distance < TackleRange && _tackleCooldown <= 0f)
                {
                    bool started = TackleSystem.TryTackle(Agent, Human, Ball, Possession, TackleRange);
                    _tackleCooldown = started ? 1.2f : 0.8f;
                    if (started && _animationDriver != null)
                        _animationDriver.TriggerTackle();
                }
            }
            else
            {
                target = Ball.transform.position;
            }

            target.y = transform.position.y;

            // Defender reacts one beat late: re-evaluate the steering target only every ReactionTime.
            if (!_hasCachedTarget)
            {
                _cachedTarget = target;
                _hasCachedTarget = true;
                _reactionTimer = ReactionTime;
            }
            else
            {
                _reactionTimer -= Time.fixedDeltaTime;
                if (_reactionTimer <= 0f)
                {
                    _cachedTarget = target;
                    _reactionTimer = ReactionTime;
                }
            }

            target = _cachedTarget;
            _debugTarget = target;
            _hasDebugTarget = true;

            if (ShowDebug)
                Debug.DrawLine(transform.position + Vector3.up, target + Vector3.up, Color.yellow);

            MoveTowards(target);
        }

        private void MoveTowards(Vector3 target)
        {
            Vector3 delta = target - transform.position;
            delta.y = 0f;

            Vector3 desired = delta.sqrMagnitude > 0.04f ? delta.normalized * MoveSpeed : Vector3.zero;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, desired, Acceleration * Time.fixedDeltaTime);
            Agent.Rb.velocity = new Vector3(_planarVelocity.x, Agent.Rb.velocity.y, _planarVelocity.z);

            if (desired.sqrMagnitude > 0.01f)
            {
                Quaternion rotation = Quaternion.LookRotation(desired, Vector3.up);
                Agent.Rb.MoveRotation(Quaternion.Slerp(Agent.Rb.rotation, rotation, TurnSharpness * Time.fixedDeltaTime));
            }
        }

        private void Shoot(Vector3 goal)
        {
            _shootCooldown = 1.8f;

            Vector3 start = Ball.transform.position;
            Vector3 target = new Vector3(goal.x, 0f, Random.Range(-1.2f, 1.2f));
            Vector3 direction = target - start;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
            direction.Normalize();

            float chord = Mathf.Max(8f, ShotDistance);
            float maxDeviation = Mathf.Min(6f, chord * chord / 8f);
            float deviation = PickShotCurve() * maxDeviation;

            Vector3 end = start + direction * chord;
            end.y = start.y;
            Vector3 midpoint = (start + end) * 0.5f;
            Vector3 perpendicular = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 control = midpoint + perpendicular * (2f * deviation);
            control.y = start.y + 0.9f;

            Ball.LaunchCurve(start, control, end, ShotSpeed, 0.08f);
            Possession.Release(Agent);
        }

        private float PickShotCurve()
        {
            float ability = Mathf.Max(0.1f, ShotAbility);
            float peak = Mathf.Clamp(0.6f * ability, 0.3f, 0.85f);
            float sideTotal = (1f - peak) * 0.5f;
            float small = sideTotal * 0.75f;
            float large = sideTotal * 0.25f;

            float roll = Random.value;

            if (roll < large) return -1f;
            roll -= large;
            if (roll < small) return -0.5f;
            roll -= small;
            if (roll < peak) return 0f;
            roll -= peak;
            if (roll < small) return 0.5f;
            return 1f;
        }

        public void SetAutomatic(bool value)
        {
            Automatic = value;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, TackleRange);

            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, ShootRange);

            if (_hasDebugTarget)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up, _debugTarget + Vector3.up);
            }
        }
    }
}
