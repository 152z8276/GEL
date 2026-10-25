using UnityEngine;

namespace GEL
{
    public class PlayerVisual : MonoBehaviour
    {
        public PlayerAgent Agent;
        public PlayerMotor Motor;

        private Vector3 _lastLocalVelocity;
        private float _bobPhase;

        private void Awake()
        {
            if (Agent == null) Agent = GetComponentInParent<PlayerAgent>();
            if (Motor == null) Motor = GetComponentInParent<PlayerMotor>();
        }

        private void LateUpdate()
        {
            if (Agent == null || Agent.Rb == null || transform.parent == null) return;

            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            Vector3 localVelocity = transform.parent.InverseTransformDirection(Agent.Rb.velocity);
            Vector3 localAcceleration = (localVelocity - _lastLocalVelocity) / dt;
            _lastLocalVelocity = localVelocity;

            float pitch = Mathf.Clamp(localAcceleration.z * 0.3f, -10f, 10f);
            float roll = Mathf.Clamp(-localAcceleration.x * 0.35f, -12f, 12f);

            if (Motor != null && Motor.IsBraking)
                pitch += 8f;

            if (Motor != null && Motor.IsTurning)
                roll += 8f * Mathf.Sign(localVelocity.x + 0.001f);

            Quaternion target = Quaternion.Euler(pitch, 0f, roll);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target, 10f * dt);

            _bobPhase += localVelocity.magnitude * 2.2f * dt;
            float bob = Mathf.Sin(_bobPhase) * 0.018f * Mathf.Clamp01(localVelocity.magnitude / 7.5f);
            transform.localPosition = new Vector3(0f, bob, 0f);
        }
    }
}
