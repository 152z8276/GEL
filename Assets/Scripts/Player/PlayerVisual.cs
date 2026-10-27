using UnityEngine;

namespace GEL
{
    public class PlayerVisual : MonoBehaviour
    {
        public PlayerAgent Agent;
        public PlayerMotor Motor;

        private Vector3 _lastLocalVelocity;
        private float _bobPhase;

        private Animator _animator;
        private Transform _leftUpperLeg;
        private Transform _leftLowerLeg;
        private Transform _leftFoot;
        private Transform _rightUpperLeg;
        private Transform _rightLowerLeg;
        private Transform _rightFoot;
        private Quaternion _baseLeftUpper;
        private Quaternion _baseLeftLower;
        private Quaternion _baseLeftFoot;
        private Quaternion _baseRightUpper;
        private Quaternion _baseRightLower;
        private Quaternion _baseRightFoot;
        private bool _elasticoActive;

        private void Awake()
        {
            if (Agent == null) Agent = GetComponentInParent<PlayerAgent>();
            if (Motor == null) Motor = GetComponentInParent<PlayerMotor>();
            _animator = GetComponentInChildren<Animator>();

            if (_animator != null)
            {
                _leftUpperLeg = _animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                _leftLowerLeg = _animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                _rightUpperLeg = _animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                _rightLowerLeg = _animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
            }
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

            if (Motor != null && Motor.Weight != null)
            {
                pitch += Motor.Weight.Weight.y * 4f;
                roll -= Motor.Weight.Weight.x * 12f;
            }

            pitch = Mathf.Clamp(pitch, -15f, 15f);
            roll = Mathf.Clamp(roll, -18f, 18f);

            Quaternion target = Quaternion.Euler(pitch, 0f, roll);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target, 10f * dt);

            _bobPhase += localVelocity.magnitude * 2.2f * dt;
            float bob = Mathf.Sin(_bobPhase) * 0.018f * Mathf.Clamp01(localVelocity.magnitude / 7.5f);
            transform.localPosition = new Vector3(0f, bob, 0f);

            ApplyElasticoPose();
        }

        private void ApplyElasticoPose()
        {
            if (Motor == null)
                return;

            if (Motor.IsElasticoActive)
            {
                if (!_elasticoActive)
                {
                    _elasticoActive = true;
                    CaptureLegPose();
                }

                float phase = Mathf.Clamp01(Motor.ElasticoPhase);
                float kick = Mathf.Sin(phase * Mathf.PI);
                int side = Motor.ElasticoSide;

                Transform upper = side > 0 ? _rightUpperLeg : _leftUpperLeg;
                Transform lower = side > 0 ? _rightLowerLeg : _leftLowerLeg;
                Transform foot = side > 0 ? _rightFoot : _leftFoot;
                Quaternion baseUpper = side > 0 ? _baseRightUpper : _baseLeftUpper;
                Quaternion baseLower = side > 0 ? _baseRightLower : _baseLeftLower;
                Quaternion baseFoot = side > 0 ? _baseRightFoot : _baseLeftFoot;

                if (upper != null)
                    upper.localRotation = baseUpper * Quaternion.Euler(
                        -58f * kick, 0f, side * 20f * kick);
                if (lower != null)
                    lower.localRotation = baseLower * Quaternion.Euler(62f * kick, 0f, 0f);
                if (foot != null)
                    foot.localRotation = baseFoot * Quaternion.Euler(18f * kick, 0f, 0f);
            }
            else if (_elasticoActive)
            {
                _elasticoActive = false;
                RestoreLegPose();
            }
        }

        private void CaptureLegPose()
        {
            if (_leftUpperLeg != null) _baseLeftUpper = _leftUpperLeg.localRotation;
            if (_leftLowerLeg != null) _baseLeftLower = _leftLowerLeg.localRotation;
            if (_leftFoot != null) _baseLeftFoot = _leftFoot.localRotation;
            if (_rightUpperLeg != null) _baseRightUpper = _rightUpperLeg.localRotation;
            if (_rightLowerLeg != null) _baseRightLower = _rightLowerLeg.localRotation;
            if (_rightFoot != null) _baseRightFoot = _rightFoot.localRotation;
        }

        private void RestoreLegPose()
        {
            if (_leftUpperLeg != null) _leftUpperLeg.localRotation = _baseLeftUpper;
            if (_leftLowerLeg != null) _leftLowerLeg.localRotation = _baseLeftLower;
            if (_leftFoot != null) _leftFoot.localRotation = _baseLeftFoot;
            if (_rightUpperLeg != null) _rightUpperLeg.localRotation = _baseRightUpper;
            if (_rightLowerLeg != null) _rightLowerLeg.localRotation = _baseRightLower;
            if (_rightFoot != null) _rightFoot.localRotation = _baseRightFoot;
        }
    }
}
