using UnityEngine;

namespace GEL
{
    public class PrototypeHUD : MonoBehaviour
    {
        public MatchDirector Match;
        public PossessionManager Possession;
        public GestureInput Gesture;
        public AIController AI;
        public SlowMotionController SlowMotion;

        private GUIStyle _scoreStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _centerStyle;

        private void Awake()
        {
            if (Match == null) Match = FindObjectOfType<MatchDirector>();
            if (Possession == null) Possession = FindObjectOfType<PossessionManager>();
            if (Gesture == null) Gesture = FindObjectOfType<GestureInput>();
            if (AI == null) AI = FindObjectOfType<AIController>();
            if (SlowMotion == null) SlowMotion = FindObjectOfType<SlowMotionController>();
        }

        private void OnGUI()
        {
            if (Match == null) return;
            EnsureStyles();

            GUI.color = Color.white;
            GUI.Label(new Rect(24f, 18f, 520f, 40f), $"YOU  {Match.PlayerScore} : {Match.AIScore}  AI", _scoreStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 70f, 18f, 160f, 40f), Mathf.CeilToInt(Match.TimeLeft) + "s", _scoreStyle);

            string owner = "Loose ball";
            if (Possession != null && Possession.CurrentOwner != null)
                owner = Possession.CurrentOwner.IsHuman ? "Your ball" : "AI ball";

            GUI.Label(new Rect(24f, 62f, 320f, 30f), owner, _smallStyle);

            string aiState = AI == null ? "AI: -" : (AI.Automatic ? "AI: AUTO" : "AI: OFF");
            GUI.Label(new Rect(24f, 90f, 420f, 30f), aiState + "   (T toggles)", _smallStyle);

            if (SlowMotion != null && SlowMotion.IsSlowMotion)
                GUI.Label(new Rect(Screen.width * 0.5f - 60f, 62f, 180f, 30f), "SLOW-MO", _smallStyle);

            if (Gesture != null)
                GUI.Label(new Rect(24f, Screen.height - 46f, 640f, 30f), "Gesture: " + Gesture.LastResult, _smallStyle);

            GUI.Label(new Rect(24f, Screen.height - 78f, 1100f, 30f),
                "WASD move   |   Drag mouse to shoot   |   Q / RMB tackle   |   Auto slow near AI / while aiming   |   F test shot   |   T AI on/off   |   R restart", _smallStyle);

            if (!string.IsNullOrEmpty(Match.Message))
            {
                GUI.color = new Color(1f, 0.9f, 0.2f);
                GUI.Label(new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.4f, 520f, 80f), Match.Message, _centerStyle);
                GUI.color = Color.white;
            }

            if (Match.State == MatchState.GameOver)
                GUI.Label(new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.55f, 520f, 40f), "Press R to restart", _centerStyle);
        }

        private void EnsureStyles()
        {
            if (_scoreStyle != null) return;

            _scoreStyle = new GUIStyle(GUI.skin.label);
            _scoreStyle.fontSize = 26;
            _scoreStyle.fontStyle = FontStyle.Bold;
            _scoreStyle.normal.textColor = Color.white;

            _smallStyle = new GUIStyle(GUI.skin.label);
            _smallStyle.fontSize = 16;
            _smallStyle.normal.textColor = new Color(0.9f, 0.95f, 0.9f);

            _centerStyle = new GUIStyle(GUI.skin.label);
            _centerStyle.fontSize = 40;
            _centerStyle.fontStyle = FontStyle.Bold;
            _centerStyle.alignment = TextAnchor.MiddleCenter;
            _centerStyle.normal.textColor = Color.white;
        }
    }
}
