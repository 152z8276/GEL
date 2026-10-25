using UnityEngine;

namespace GEL
{
    public class GoalTrigger : MonoBehaviour
    {
        public MatchDirector Match;
        public bool PlayerScores;

        private void OnTriggerEnter(Collider other)
        {
            if (Match == null) return;

            if (other.GetComponent<BallSimulator>() != null)
                Match.OnGoal(PlayerScores);
        }
    }
}
