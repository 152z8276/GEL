using UnityEngine;

namespace GEL
{
    public class FootTouch : MonoBehaviour
    {
        public FootSide Side = FootSide.Right;
        public PlayerAgent Agent;
        public PossessionManager Possession;

        private void Awake()
        {
            if (Agent == null) Agent = GetComponentInParent<PlayerAgent>();
            if (Possession == null) Possession = FindObjectOfType<PossessionManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Possession == null || Agent == null) return;

            BallSimulator ball = other.GetComponentInParent<BallSimulator>();
            if (ball == null) return;

            Possession.TryClaimFromFoot(Agent);
        }
    }
}
