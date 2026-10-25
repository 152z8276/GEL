using UnityEngine;

namespace GEL
{
    public class PlayerAnimationDriver : MonoBehaviour
    {
        public Animator Animator;
        public PlayerAgent Agent;
        public PlayerMotor Motor;

        private bool _wasShooting;

        private void Awake()
        {
            if (Animator == null) Animator = GetComponentInChildren<Animator>();
            if (Agent == null) Agent = GetComponentInParent<PlayerAgent>();
            if (Motor == null) Motor = GetComponentInParent<PlayerMotor>();

            if (Animator != null)
                Animator.applyRootMotion = false;
        }

        public void TriggerTackle()
        {
            if (Animator != null && Animator.runtimeAnimatorController != null)
                Animator.SetTrigger("Tackle");
        }

        private void Update()
        {
            if (Animator == null || Animator.runtimeAnimatorController == null) return;
            if (Agent == null || Agent.Rb == null) return;

            // Stun has no animation clip yet: freeze the current frame for the stun window.
            if (Agent.StunTimer > 0f)
            {
                Animator.speed = 0f;
                return;
            }

            Animator.speed = 1f;

            Vector3 planarVelocity = Agent.Rb.velocity;
            planarVelocity.y = 0f;
            Animator.SetFloat("Speed", planarVelocity.magnitude, 0.08f, Time.deltaTime);

            Vector3 localVelocity = transform.parent.InverseTransformDirection(planarVelocity);
            bool backward = localVelocity.z < -0.2f && planarVelocity.sqrMagnitude > 0.04f;

            Animator.SetBool("Backward", backward);
            Animator.SetBool("S180", Motor != null && Motor.IsS180Turning);
            Animator.SetBool("Braking", Motor != null && Motor.IsBraking);

            bool shooting = Motor != null && Motor.IsShooting;
            if (shooting && !_wasShooting)
                Animator.SetTrigger("Shoot");
            _wasShooting = shooting;

            if (Motor != null && Motor.ConsumeTackleAnimation())
                Animator.SetTrigger("Tackle");
        }
    }
}
