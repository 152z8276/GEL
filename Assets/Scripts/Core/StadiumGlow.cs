using UnityEngine;

namespace GEL
{
    public class StadiumGlow : MonoBehaviour
    {
        public Material[] Materials;
        public float PulseSpeed = 1.15f;
        public float PulseAmount = 0.45f;
        public float ScrollSpeed = 0.06f;

        private Color[] _baseEmission;
        private bool _ready;

        private void Start()
        {
            if (Materials == null || Materials.Length == 0) return;

            _baseEmission = new Color[Materials.Length];
            for (int i = 0; i < Materials.Length; i++)
            {
                Material material = Materials[i];
                if (material == null) continue;

                _baseEmission[i] = material.HasProperty("_EmissionColor")
                    ? material.GetColor("_EmissionColor")
                    : Color.black;

                material.EnableKeyword("_EMISSION");
            }

            _ready = true;
        }

        private void Update()
        {
            if (!_ready) return;

            float time = Time.time;
            for (int i = 0; i < Materials.Length; i++)
            {
                Material material = Materials[i];
                if (material == null) continue;

                float phase = time * PulseSpeed + i * 1.7f;
                float pulse = 0.62f + 0.38f * Mathf.Sin(phase);
                float strength = 0.8f + PulseAmount * pulse;

                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", _baseEmission[i] * strength);

                if (material.HasProperty("_MainTex"))
                {
                    Vector2 offset = material.mainTextureOffset;
                    offset.x = Mathf.Repeat(time * ScrollSpeed + i * 0.13f, 1f);
                    material.mainTextureOffset = offset;
                }
            }
        }
    }
}
