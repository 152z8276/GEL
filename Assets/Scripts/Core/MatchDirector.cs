using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GEL
{
    public enum MatchState
    {
        Kickoff,
        Playing,
        Goal,
        GameOver
    }

    public class MatchDirector : MonoBehaviour
    {
        public PlayerAgent PlayerAgent;
        public PlayerAgent AIAgent;
        public BallSimulator Ball;
        public PossessionManager Possession;
        public ShoulderCamera CameraRig;
        public Vector3 PlayerSpawn = new Vector3(-6f, 1f, 0f);
        public Vector3 AISpawn = new Vector3(8f, 1f, 0f);
        public Vector3 BallSpawn = new Vector3(0f, 0.16f, 0f);
        public int WinningScore = 3;
        public float MatchLength = 120f;

        public MatchState State { get; private set; } = MatchState.Kickoff;
        public int PlayerScore { get; private set; }
        public int AIScore { get; private set; }
        public float TimeLeft { get; private set; }
        public string Message { get; private set; } = string.Empty;

        private void Awake()
        {
            Physics.gravity = new Vector3(0f, -20f, 0f);
        }

        private void Start()
        {
            TimeLeft = MatchLength;
            ResetPositions();
            State = MatchState.Playing;
        }

        private void Update()
        {
            if (State == MatchState.Playing)
            {
                TimeLeft -= Time.deltaTime;
                if (TimeLeft <= 0f)
                {
                    TimeLeft = 0f;
                    EndMatch();
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
                RestartMatch();
        }

        public void OnGoal(bool playerScored)
        {
            if (State != MatchState.Playing) return;

            if (playerScored) PlayerScore++;
            else AIScore++;

            State = MatchState.Goal;
            Message = playerScored ? "GOAL!" : "AI GOAL!";
            StartCoroutine(GoalRoutine());
        }

        private IEnumerator GoalRoutine()
        {
            yield return new WaitForSecondsRealtime(1.6f);

            if (PlayerScore >= WinningScore || AIScore >= WinningScore)
            {
                EndMatch();
                yield break;
            }

            Message = string.Empty;
            ResetPositions();
            State = MatchState.Playing;
        }

        private void EndMatch()
        {
            State = MatchState.GameOver;

            if (PlayerScore > AIScore) Message = "YOU WIN";
            else if (PlayerScore < AIScore) Message = "YOU LOSE";
            else Message = "DRAW";
        }

        public void RestartMatch()
        {
            StopAllCoroutines();

            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            if (buildIndex >= 0)
            {
                SceneManager.LoadScene(buildIndex);
                return;
            }

            PlayerScore = 0;
            AIScore = 0;
            TimeLeft = MatchLength;
            Message = string.Empty;
            ResetPositions();
            State = MatchState.Playing;
        }

        public void ResetPositions()
        {
            if (PlayerAgent != null)
                PlayerAgent.Teleport(PlayerSpawn, Quaternion.LookRotation(Vector3.right));

            if (AIAgent != null)
                AIAgent.Teleport(AISpawn, Quaternion.LookRotation(Vector3.left));

            if (Ball != null)
                Ball.ResetBall(BallSpawn);

            if (Possession != null)
                Possession.ResetPossession();

            if (CameraRig != null)
                CameraRig.ResetDirection();
        }
    }
}
