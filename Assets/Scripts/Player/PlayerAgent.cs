using UnityEngine;

namespace GEL
{
    public enum FootSide
    {
        Left,
        Right
    }

    [RequireComponent(typeof(Rigidbody))]
    public class PlayerAgent : MonoBehaviour
    {
        public bool IsHuman;
        public bool ControlEnabled = true;
        public Transform BallFoot;
        public Transform BallToe;
        public WeightSystem Weight;
        public float TackleAbility = 0.75f;
        public float BallControlAbility = 0.75f;
        public float ShotAbility = 0.8f;
        public float DribbleAbility = 0.75f;
        public float DefenseAbility = 0.75f;
        public float TackleRange = 1.6f;
        public float StunTimer { get; private set; }
        public float TackleCooldownTimer { get; private set; }
        public float TackleLockTimer { get; private set; }
        public float TackleActionTimer { get; private set; }
        public float DribbleImmuneTimer { get; private set; }
        public Rigidbody Rb { get; private set; }

        public bool CanTackle =>
            ControlEnabled &&
            StunTimer <= 0f &&
            TackleCooldownTimer <= 0f &&
            TackleLockTimer <= 0f &&
            TackleActionTimer <= 0f;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody>();

            if (IsHuman)
                ControlEnabled = true;
        }

        private void Update()
        {
            if (StunTimer > 0f)
                StunTimer = Mathf.Max(0f, StunTimer - Time.deltaTime);

            if (TackleCooldownTimer > 0f)
                TackleCooldownTimer = Mathf.Max(0f, TackleCooldownTimer - Time.deltaTime);

            if (TackleLockTimer > 0f)
                TackleLockTimer = Mathf.Max(0f, TackleLockTimer - Time.deltaTime);

            if (TackleActionTimer > 0f)
                TackleActionTimer = Mathf.Max(0f, TackleActionTimer - Time.deltaTime);

            if (DribbleImmuneTimer > 0f)
                DribbleImmuneTimer = Mathf.Max(0f, DribbleImmuneTimer - Time.deltaTime);
        }

        public void Stun(float seconds)
        {
            StunTimer = Mathf.Max(StunTimer, seconds);
        }

        public void StartTackleCooldown(float seconds)
        {
            TackleCooldownTimer = Mathf.Max(TackleCooldownTimer, seconds);
        }

        public void BlockTackle(float seconds)
        {
            TackleLockTimer = Mathf.Max(TackleLockTimer, seconds);
        }

        public void BlockDribble(float seconds)
        {
            DribbleImmuneTimer = Mathf.Max(DribbleImmuneTimer, seconds);
        }

        public void LockTackleAction(float seconds)
        {
            TackleActionTimer = Mathf.Max(TackleActionTimer, seconds);
            StopMotion();
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            StunTimer = 0f;
            TackleCooldownTimer = 0f;
            TackleLockTimer = 0f;
            TackleActionTimer = 0f;
            DribbleImmuneTimer = 0f;

            if (Rb != null)
            {
                Rb.velocity = Vector3.zero;
                Rb.angularVelocity = Vector3.zero;
                Rb.position = position;
                Rb.rotation = rotation;
            }

            transform.position = position;
            transform.rotation = rotation;

            PlayerMotor motor = GetComponent<PlayerMotor>();
            if (motor != null)
                motor.SyncBodyYawToTransform();
        }

        public void StopMotion()
        {
            if (Rb == null) return;
            Rb.velocity = new Vector3(0f, Rb.velocity.y, 0f);
            Rb.angularVelocity = Vector3.zero;
        }
    }
}
