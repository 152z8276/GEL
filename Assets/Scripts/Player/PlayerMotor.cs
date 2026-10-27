using System.Collections;
using UnityEngine;

namespace GEL
{
    public class PlayerMotor : MonoBehaviour
    {
        public enum LocomotionState
        {
            Idle,
            Run,
            Turn,
            Brake
        }

        public PlayerAgent Agent;
        public PossessionManager Possession;
        public BallSimulator Ball;
        public ShoulderCamera CameraRig;
        public SlowMotionController SlowMotion;
        public WeightSystem Weight;
        public Transform AttackGoal;

        [Header("Locomotion")]
        public float MoveSpeed = 7.5f;
        public float Acceleration = 22f;
        public float Deceleration = 30f;
        public float LowSpeedTurnRate = 720f;
        public float HighSpeedTurnRate = 300f;
        public float LateralGrip = 24f;
        public float TurnAngleThreshold = 90f;
        public float TurnSpeedPenalty = 0.7f;
        public float TurnDuration = 0.18f;
        public float BrakeSpeedThreshold = 1.5f;
        public float BackpedalFactor = 0.6f;
        public float TurnSharpness = 12f;

        [Header("Mind-map steering")]
        public float BodyTurnSpeed = 90f;
        public float MaxTurnPerPress = 270f;
        public float Turn180Duration = 0.67f;

        [Header("Shot Tuning")]
        public float ShotSpeed = 22f;
        public float ShotSpeedDecay = 0.08f;
        public float ShotRange = 30f;
        public float ShotArc = 1.0f;
        public float MaxCurvature = 0.22f;
        public float MaxCurveOffset = 1.8f;
        public float CurveSkill = 1f;

        [Header("Shot Timing")]
        public float ShotWindupTime = 0.35f;
        public float ShotWindupSpeed = 0.8f;
        public float ShotWindupBallSpeed = 1.2f;

        [Header("Dribble")]
        public float DribbleRange = 1.6f;
        public float DribbleInstantOffset = 0.5f;
        public float DribbleBurstDuration = 0.12f;
        public float DribbleRecoveryDuration = 0.05f;
        public float DribbleBurstPeakSpeed = 16f;
        public float DefensiveBurstPeakSpeed = 18f;
        public float DribbleBurstSpeed = 16f;
        public float DribbleCooldown = 0.55f;
        public float DribbleStun = 0.28f;
        public float DribbleTackleBlock = 0.5f;
        public float DribbleImmunity = 1.2f;
        public float DribbleSpeed = 6f;
        public float DribbleAbilityCap = 0.9f;
        public float SpeedGapThreshold = 0.8f;
        public float BodyHeadingDifferentThreshold = 0.85f;

        public float BallControlAbility =>
            DribbleSpeed / Mathf.Max(0.1f, MoveSpeed) * DribbleAbilityCap;

        public Vector2 VirtualInput { get; set; }

        public void SetGestureLean(Vector2 screenDelta)
        {
            if (Weight != null) Weight.SetGestureLean(screenDelta);
        }

        public void ClearGestureLean()
        {
            if (Weight != null) Weight.ClearGestureLean();
        }

        public LocomotionState State { get; private set; } = LocomotionState.Idle;
        public bool IsBraking => State == LocomotionState.Brake;
        public bool IsTurning => State == LocomotionState.Turn;
        public float TurnInput { get; private set; }
        public bool IsS180Turning => _sTurnActive;
        public bool IsDribbleBurst => _dribbleBurstTimer > 0f;
        public bool IsDribbleActionActive => _dribbleBurstTimer > 0f || _dribbleInputLockTimer > 0f;
        public bool DirectionInputLocked =>
            (_gestureLock && _gestureLocksMovement) ||
            (_dribbleInputLockTimer > 0f && _dribbleBurstTimer <= 0f);
        public Vector3 DribbleDirection => _dribbleDir;
        public bool LastDribbleSucceeded { get; private set; }
        public bool LastActionWasDefensiveBurst { get; private set; }
        public DribbleStyle LastDribbleStyle { get; private set; } = DribbleStyle.Simple;
        public float ElasticoPoseDuration = 0.34f;
        public bool IsElasticoActive => _elasticoTimer > 0f;
        public float ElasticoPhase => _elasticoTimer <= 0f
            ? 1f
            : 1f - _elasticoTimer / Mathf.Max(0.01f, ElasticoPoseDuration);
        public int ElasticoSide { get; private set; } = 1;

        public string ConsumeDribbleAnimationTrigger()
        {
            string trigger = _pendingDribbleTrigger;
            _pendingDribbleTrigger = null;
            return trigger;
        }

        public void SetGestureLock(bool locked, bool lockMovement = false)
        {
            _gestureLock = locked;
            _gestureLocksMovement = locked && lockMovement;

            if (!locked) return;

            SyncBodyYawToTransform();
            TurnInput = 0f;
            _turnHeld = false;
            _turnApplied = 0f;
            _sTurnActive = false;

            if (_gestureLocksMovement)
                Agent.StopMotion();
        }

        private Vector3 _planarVelocity;
        private float _kickCooldown;
        private Camera _camera;
        private float _stateTimer;
        private bool _shotPending;
        private Vector3 _pendingDirection;
        private float _pendingPower;
        private float _pendingCurve;
        private float _pendingVerticalBias;
        private float _pendingDistance = -1f;
        private bool _pendingLob;
        private float _dribbleBurstTimer;
        private float _dribbleInputLockTimer;
        private float _elasticoTimer;
        private float _dribbleCooldownTimer;
        private Vector3 _dribbleDir;
        private string _pendingDribbleTrigger;
        private bool _defensiveBurst;
        private bool _gestureLock;
        private bool _gestureLocksMovement;
        private float _bodyYaw;
        private bool _bodyYawInitialized;
        private bool _backHeld;
        private bool _turnHeld;
        private int _turnDirection;
        private float _turnApplied;
        private bool _sTurnActive;
        private float _sTurnElapsed;
        private float _sTurnStartYaw;
        private bool _sBackpedal;
        private bool _tackleAnimationRequested;

        private void Awake()
        {
            if (Agent == null) Agent = GetComponent<PlayerAgent>();
            if (CameraRig == null) CameraRig = FindObjectOfType<ShoulderCamera>();
            if (SlowMotion == null) SlowMotion = FindObjectOfType<SlowMotionController>();

            if (ShotSpeed <= 0f) ShotSpeed = 22f;
            if (ShotSpeedDecay < 0f) ShotSpeedDecay = 0.08f;
            if (ShotRange <= 0f) ShotRange = 30f;
            if (ShotArc <= 0f) ShotArc = 1.2f;
            if (MaxCurvature <= 0f) MaxCurvature = 1f;
            if (MaxCurveOffset <= 0f) MaxCurveOffset = 8f;
            if (CurveSkill <= 0f) CurveSkill = 1f;
            if (ElasticoPoseDuration <= 0f) ElasticoPoseDuration = 0.34f;
            if (ShotWindupTime < 0f) ShotWindupTime = 0.35f;
            if (ShotWindupSpeed <= 0f) ShotWindupSpeed = 0.8f;
            if (ShotWindupBallSpeed <= 0f) ShotWindupBallSpeed = 1.2f;

            if (Acceleration <= 0f) Acceleration = 22f;
            if (Deceleration <= 0f) Deceleration = 30f;
            if (LowSpeedTurnRate <= 0f) LowSpeedTurnRate = 720f;
            if (HighSpeedTurnRate <= 0f) HighSpeedTurnRate = 300f;
            if (LateralGrip <= 0f) LateralGrip = 24f;
            if (TurnAngleThreshold <= 0f) TurnAngleThreshold = 90f;
            if (TurnSpeedPenalty <= 0f) TurnSpeedPenalty = 0.7f;
            if (TurnDuration <= 0f) TurnDuration = 0.18f;
            if (BrakeSpeedThreshold <= 0f) BrakeSpeedThreshold = 1.5f;
            if (BackpedalFactor <= 0f) BackpedalFactor = 0.6f;
            if (BodyTurnSpeed <= 0f) BodyTurnSpeed = 90f;
            if (MaxTurnPerPress <= 0f) MaxTurnPerPress = 270f;
            if (Turn180Duration <= 0f) Turn180Duration = 0.67f;
        }

        private void FixedUpdate()
        {
            if (Agent == null || Agent.Rb == null) return;
            if (_camera == null) _camera = Camera.main;

            if (!_bodyYawInitialized)
            {
                _bodyYaw = transform.eulerAngles.y;
                _bodyYawInitialized = true;
            }

            if (_dribbleCooldownTimer > 0f)
                _dribbleCooldownTimer = Mathf.Max(0f, _dribbleCooldownTimer - Time.fixedDeltaTime);

            if (_dribbleBurstTimer > 0f)
                _dribbleBurstTimer = Mathf.Max(0f, _dribbleBurstTimer - Time.fixedDeltaTime);

            if (_dribbleInputLockTimer > 0f)
                _dribbleInputLockTimer = Mathf.Max(0f, _dribbleInputLockTimer - Time.fixedDeltaTime);

            if (_elasticoTimer > 0f)
                _elasticoTimer = Mathf.Max(0f, _elasticoTimer - Time.fixedDeltaTime);

            if (_shotPending)
            {
                if (Agent.StunTimer > 0f)
                {
                    CancelPendingShot();
                    return;
                }

                LockShotMotion();
                return;
            }

            UpdateMindMapMovement(Time.fixedDeltaTime);

            if (_kickCooldown > 0f)
                _kickCooldown -= Time.fixedDeltaTime;
        }

        private void UpdateMindMapMovement(float dt)
        {
            if (Agent.StunTimer > 0f || Agent.TackleActionTimer > 0f)
            {
                Agent.StopMotion();
                TurnInput = 0f;
                _turnHeld = false;
                _turnApplied = 0f;
                _sTurnActive = false;
                _backHeld = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
                return;
            }

            if (_dribbleBurstTimer > 0f)
            {
                TurnInput = 0f;
                // Keep the body/camera facing stable during the burst. The burst is
                // a real-time explosive step, not a slow walk in bullet time.
                float burstSpeed = _defensiveBurst
                    ? DefensiveBurstPeakSpeed
                    : DribbleBurstPeakSpeed;
                Agent.Rb.velocity = new Vector3(
                    _dribbleDir.x * burstSpeed,
                    Agent.Rb.velocity.y,
                    _dribbleDir.z * burstSpeed);
                _backHeld = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
                return;
            }

            if (DirectionInputLocked)
            {
                Agent.StopMotion();
                TurnInput = 0f;
                _turnHeld = false;
                _turnApplied = 0f;
                _sTurnActive = false;
                _backHeld = false;
                return;
            }

            Vector2 virtualInput = VirtualInput;
            bool forwardKey = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ||
                              virtualInput.y > 0.2f;
            bool backKey = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ||
                           virtualInput.y < -0.2f;
            bool leftKey = !_gestureLock &&
                           (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ||
                            virtualInput.x < -0.2f);
            bool rightKey = !_gestureLock &&
                            (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ||
                             virtualInput.x > 0.2f);

            if (_sTurnActive)
            {
                UpdateS180Turn(dt);
                TurnInput = 0f;
                UpdateVelocity(0f, dt);
                _backHeld = backKey;
                return;
            }

            bool sPressed = backKey && !_backHeld;
            if (backKey)
            {
                if (!_sBackpedal && sPressed)
                {
                    if (EnemyInFront180())
                        _sBackpedal = true;
                    else
                        StartS180Turn();
                }

                if (_sTurnActive)
                {
                    UpdateS180Turn(dt);
                    TurnInput = 0f;
                    UpdateVelocity(0f, dt);
                    _backHeld = backKey;
                    return;
                }
            }
            else
            {
                _sBackpedal = false;
            }

            int turnDirection = 0;
            if (!backKey)
            {
                turnDirection = (rightKey ? 1 : 0) - (leftKey ? 1 : 0);
                UpdateTurn(turnDirection, dt);
            }
            else
            {
                TurnInput = 0f;
                _turnHeld = false;
                _turnApplied = 0f;
            }

            float move = 0f;
            if (backKey)
            {
                if (_sBackpedal) move = -1f;
            }
            else if (forwardKey || turnDirection != 0)
            {
                // A/D is both a turn and "current key as forward".
                move = 1f;
            }

            UpdateVelocity(move, dt);
            _backHeld = backKey;
        }

        private void UpdateTurn(int direction, float dt)
        {
            if (direction == 0)
            {
                _turnHeld = false;
                _turnApplied = 0f;
                TurnInput = 0f;
                return;
            }

            if (!_turnHeld || direction != _turnDirection)
            {
                _turnDirection = direction;
                _turnHeld = true;
                _turnApplied = 0f;
            }

            if (_turnApplied >= MaxTurnPerPress)
            {
                TurnInput = 0f;
                return;
            }

            float remaining = MaxTurnPerPress - _turnApplied;
            float step = direction * BodyTurnSpeed * dt;
            step = Mathf.Sign(step) * Mathf.Min(Mathf.Abs(step), remaining);

            _bodyYaw = Mathf.Repeat(_bodyYaw + step, 360f);
            _turnApplied += Mathf.Abs(step);
            TurnInput = _turnApplied < MaxTurnPerPress ? direction : 0f;
        }

        private void StartS180Turn()
        {
            _sTurnActive = true;
            _sTurnElapsed = 0f;
            _sTurnStartYaw = _bodyYaw;
            _sBackpedal = false;
            TurnInput = 0f;
            _planarVelocity *= 0.25f;
        }

        private void UpdateS180Turn(float dt)
        {
            _sTurnElapsed += dt;
            float t = Mathf.Clamp01(_sTurnElapsed / Mathf.Max(0.05f, Turn180Duration));
            _bodyYaw = Mathf.LerpAngle(_sTurnStartYaw, _sTurnStartYaw + 180f, t);

            if (t >= 1f)
            {
                _sTurnActive = false;
                _bodyYaw = Mathf.Repeat(_bodyYaw, 360f);
                _sBackpedal = true;
            }
        }

        private void UpdateVelocity(float move, float dt)
        {
            Vector3 forward = Quaternion.Euler(0f, _bodyYaw, 0f) * Vector3.forward;
            Agent.Rb.MoveRotation(Quaternion.LookRotation(forward, Vector3.up));

            float speed = _planarVelocity.magnitude;
            bool hasMove = Mathf.Abs(move) > 0.001f;

            if (!hasMove && speed > BrakeSpeedThreshold && State != LocomotionState.Brake)
            {
                State = LocomotionState.Brake;
                _stateTimer = 0.3f;
            }
            else if (!hasMove && speed < 0.2f)
            {
                State = LocomotionState.Idle;
            }
            else if (hasMove)
            {
                State = LocomotionState.Run;
            }

            if (State == LocomotionState.Brake)
            {
                if (hasMove)
                    State = LocomotionState.Run;
                else if (speed < 0.4f)
                    State = LocomotionState.Idle;
            }

            float targetSpeed = HasBall() ? DribbleSpeed : MoveSpeed;
            if (move < 0f) targetSpeed *= BackpedalFactor;

            if (State == LocomotionState.Brake || !hasMove)
                targetSpeed = 0f;

            Vector3 targetVelocity = forward * (move * targetSpeed);

            float forwardSpeed = Vector3.Dot(_planarVelocity, forward);
            Vector3 lateralVelocity = _planarVelocity - forward * forwardSpeed;

            float targetForwardSpeed = Vector3.Dot(targetVelocity, forward);
            float acceleration = targetForwardSpeed > forwardSpeed ? Acceleration : Deceleration;
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, targetForwardSpeed, acceleration * dt);

            Vector3 targetLateral = targetVelocity - forward * targetForwardSpeed;
            lateralVelocity = Vector3.MoveTowards(lateralVelocity, targetLateral, LateralGrip * dt);

            _planarVelocity = forward * forwardSpeed + lateralVelocity;
            Agent.Rb.velocity = new Vector3(_planarVelocity.x, Agent.Rb.velocity.y, _planarVelocity.z);
        }

        private bool EnemyInFront180()
        {
            if (Possession == null || Possession.Agents == null) return false;

            for (int i = 0; i < Possession.Agents.Length; i++)
            {
                PlayerAgent other = Possession.Agents[i];
                if (other == null || other == Agent) continue;

                Vector3 toEnemy = other.transform.position - transform.position;
                toEnemy.y = 0f;
                if (toEnemy.sqrMagnitude < 0.001f) return true;
                if (Vector3.Dot(transform.forward, toEnemy.normalized) >= 0f)
                    return true;
            }

            return false;
        }

        private void LockShotMotion()
        {
            // Hard-lock to a small positive speed, not a slow deceleration.
            _planarVelocity = _pendingDirection * ShotWindupSpeed;
            Agent.Rb.velocity = new Vector3(_planarVelocity.x, Agent.Rb.velocity.y, _planarVelocity.z);

            Vector3 facing = Quaternion.Euler(0f, _bodyYaw, 0f) * Vector3.forward;
            Agent.Rb.MoveRotation(Quaternion.LookRotation(facing, Vector3.up));
        }

        public bool HasBall()
        {
            return Possession != null && Possession.CurrentOwner == Agent;
        }

        public bool IsShooting => _shotPending;
        public Vector3 ShotDirection => _pendingDirection;

        public void BeginAim()
        {
            if (Possession != null && Agent != null)
                Possession.BeginAim(Agent);
        }

        public void EndAim()
        {
            if (_shotPending) return;
            if (Possession != null)
                Possession.EndAim();
        }

        public bool TryDribble(Vector3 direction, DribbleStyle style = DribbleStyle.Simple)
        {
            if (Agent == null || Possession == null) return false;
            LastDribbleStyle = style;
            if (_shotPending || Agent.StunTimer > 0f || _dribbleCooldownTimer > 0f) return false;
            if (Agent.TackleActionTimer > 0f) return false;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
            direction.Normalize();

            LastActionWasDefensiveBurst = !HasBall();
            if (LastActionWasDefensiveBurst)
            {
                LastDribbleSucceeded = false;
                if (Weight != null)
                    Weight.Commit(direction);
                BeginBurst(direction, true);
                return true;
            }

            LastDribbleSucceeded = false;

            PlayerAgent defender = null;
            float bestDistance = DribbleRange;

            if (Possession.Agents != null)
            {
                for (int i = 0; i < Possession.Agents.Length; i++)
                {
                    PlayerAgent other = Possession.Agents[i];
                    if (other == null || other == Agent) continue;

                    Vector3 toOther = other.transform.position - transform.position;
                    toOther.y = 0f;
                    float distance = toOther.magnitude;
                    if (distance > bestDistance) continue;
                    if (toOther.sqrMagnitude > 0.001f &&
                        Vector3.Dot(transform.forward, toOther.normalized) < -0.2f)
                        continue;

                    defender = other;
                    bestDistance = distance;
                }
            }

            if (defender != null && defender.DribbleImmuneTimer <= 0f)
            {
                float headingDot = Vector3.Dot(
                    transform.forward, defender.transform.forward);
                bool differentHeading = headingDot < BodyHeadingDifferentThreshold;
                float feintAdvantage = Weight != null
                    ? Weight.GetFeintAdvantage(defender, direction)
                    : 0f;
                if (style == DribbleStyle.Feint || style == DribbleStyle.DoubleTouch ||
                    style == DribbleStyle.Elastico)
                    feintAdvantage = Mathf.Max(feintAdvantage, 0.4f);
                bool success;

                if (differentHeading || feintAdvantage > 0.25f)
                {
                    success = true;
                }
                else
                {
                    float dribblerSpeed = DribbleSpeed;
                    float defenderSpeed = defender.Rb != null
                        ? defender.Rb.velocity.magnitude
                        : 0f;
                    float speedGap = defenderSpeed - dribblerSpeed;

                    float ability = Mathf.Max(0.2f, BallControlAbility);
                    float speedTolerance = SpeedGapThreshold / ability;
                    success = speedGap <= speedTolerance;
                }

                if (success)
                {
                    defender.Stun(DribbleStun);
                    defender.BlockTackle(DribbleTackleBlock);
                    defender.BlockDribble(DribbleImmunity);
                    TackleSystem.CancelAttemptsBy(defender);
                    LastDribbleSucceeded = true;
                }
            }

            if (Weight != null)
                Weight.Commit(direction);

            if (style == DribbleStyle.Roulette)
            {
                _pendingDribbleTrigger = "Spin";
            }
            else if (style == DribbleStyle.Feint)
            {
                _pendingDribbleTrigger = Weight != null && Weight.Weight.x >= 0f
                    ? "FeintRight"
                    : "FeintLeft";
            }
            else if (style == DribbleStyle.DoubleTouch)
            {
                _pendingDribbleTrigger = "Dribble";
            }
            else if (style == DribbleStyle.StepOver)
            {
                _pendingDribbleTrigger = "StepOver";
            }
            else if (style == DribbleStyle.Elastico)
            {
                ElasticoSide = Vector3.Dot(direction, transform.right) >= 0f ? 1 : -1;
                _elasticoTimer = ElasticoPoseDuration;
            }

            BeginBurst(direction, false);

            return true;
        }

        private void BeginBurst(Vector3 direction, bool defensive)
        {
            _defensiveBurst = defensive;

            float safeOffset = GetSafeBurstDistance(direction, DribbleInstantOffset);
            if (safeOffset > 0.01f)
            {
                Agent.Rb.position += direction * safeOffset;
                Physics.SyncTransforms();

                if (!defensive && Ball != null && HasBall())
                    Ball.Rb.position += direction * safeOffset;
            }

            _dribbleDir = direction;
            _dribbleBurstTimer = DribbleBurstDuration;
            _dribbleInputLockTimer = DribbleBurstDuration + DribbleRecoveryDuration;
            _dribbleCooldownTimer = DribbleCooldown;

            if (SlowMotion == null)
                SlowMotion = FindObjectOfType<SlowMotionController>();
            if (SlowMotion != null)
                SlowMotion.ForceExit(0.35f);
        }

        private float GetSafeBurstDistance(Vector3 direction, float desiredDistance)
        {
            if (desiredDistance <= 0f) return 0f;

            CapsuleCollider capsule = Agent.GetComponent<CapsuleCollider>();
            if (capsule == null) return desiredDistance;

            Vector3 center = Agent.transform.TransformPoint(capsule.center);
            float half = Mathf.Max(0.01f, capsule.height * 0.5f - capsule.radius);
            Vector3 up = Agent.transform.up;
            Vector3 p1 = center + up * half;
            Vector3 p2 = center - up * half;
            float radius = Mathf.Max(0.05f, capsule.radius * 0.9f);

            RaycastHit[] hits = Physics.CapsuleCastAll(
                p1, p2, radius, direction, desiredDistance,
                ~0, QueryTriggerInteraction.Ignore);

            float safe = desiredDistance;
            Collider ballCollider = Ball != null ? Ball.GetComponent<Collider>() : null;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i].collider;
                if (hit == null) continue;
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                if (ballCollider != null && hit == ballCollider) continue;

                if (hits[i].distance <= 0.001f)
                    return 0f;

                safe = Mathf.Min(safe, hits[i].distance - 0.04f);
            }

            return Mathf.Clamp(safe, 0f, desiredDistance);
        }

        public bool TryTackle()
        {
            if (_shotPending || Agent == null || Possession == null || Ball == null) return false;

            PlayerAgent victim = Possession.CurrentOwner;
            if (victim == null || victim == Agent) return false;

            bool started = TackleSystem.TryTackle(Agent, victim, Ball, Possession, Agent.TackleRange);
            if (started)
                _tackleAnimationRequested = true;
            return started;
        }

        public bool ConsumeTackleAnimation()
        {
            bool requested = _tackleAnimationRequested;
            _tackleAnimationRequested = false;
            return requested;
        }

        public void SyncBodyYawToTransform()
        {
            _bodyYaw = transform.eulerAngles.y;
            _bodyYawInitialized = true;
            _backHeld = false;
            _turnHeld = false;
            _turnApplied = 0f;
            _sTurnActive = false;
            _sBackpedal = false;
        }

        public bool TryKick(
            Vector3 direction,
            float power,
            float curve,
            bool lob,
            float targetDistance = -1f,
            float verticalBias = 0f)
        {
            if (_shotPending) return false;
            if (!HasBall() || Agent == null || Agent.StunTimer > 0f || _kickCooldown > 0f) return false;
            if (Ball == null || Possession == null) return false;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
            direction.Normalize();

            _pendingDirection = direction;
            _pendingPower = Mathf.Clamp01(power);
            _pendingCurve = curve;
            _pendingLob = lob;
            _pendingVerticalBias = verticalBias;
            _pendingDistance = targetDistance > 0f
                ? Mathf.Clamp(targetDistance, 8f, 30f)
                : 30f;
            _shotPending = true;

            // Keep the ball glued to the player during the short wind-up.
            Possession.BeginAim(Agent);
            StartCoroutine(ShotRoutine());
            return true;
        }

        private IEnumerator ShotRoutine()
        {
            if (ShotWindupTime > 0f)
                yield return new WaitForSecondsRealtime(ShotWindupTime);

            if (!_shotPending) yield break;

            _shotPending = false;

            if (!HasBall() || Ball == null || Possession == null)
            {
                if (Possession != null) Possession.EndAim();
                yield break;
            }

            LaunchShot();
        }

        private void LaunchShot()
        {
            float chord = Mathf.Max(8f, _pendingDistance);
            float maxDeviation = Mathf.Min(MaxCurveOffset, MaxCurvature * chord * chord / 8f);
            float deviation = Mathf.Clamp(_pendingCurve, -1f, 1f) * maxDeviation * CurveSkill;
            deviation += Random.Range(-0.04f, 0.04f) * maxDeviation;
            deviation = Mathf.Clamp(deviation, -maxDeviation, maxDeviation);

            Vector3 start = Ball.transform.position;
            Vector3 end = start + _pendingDirection * chord;
            end.y = start.y;

            Vector3 midpoint = (start + end) * 0.5f;
            Vector3 perpendicular = Vector3.Cross(Vector3.up, _pendingDirection).normalized;
            Vector3 control = midpoint + perpendicular * (2f * deviation);
            control.y = start.y +
                        Mathf.Lerp(0.6f, ShotArc, _pendingPower) * (_pendingLob ? 2.2f : 1f) +
                        _pendingVerticalBias;

            Ball.LaunchCurve(start, control, end, ShotSpeed, ShotSpeedDecay);
            Possession.Release(Agent);
            _kickCooldown = 0.35f;
            Agent.Rb.MoveRotation(Quaternion.LookRotation(_pendingDirection, Vector3.up));
        }

        private void CancelPendingShot()
        {
            _shotPending = false;
            if (Possession != null)
                Possession.EndAim();
        }
    }
}
