using UnityEngine;

namespace MiniGame
{
    /// <summary>
    /// In-game HUD providing roaming guidance, controls cheatsheet,
    /// and instant camera view toggling (Free Roam 3rd Person vs Cinematic Deck Camera).
    /// </summary>
    public class PlayerRoamHUD : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private GameObject playerFollowCamera;
        [SerializeField] private GameObject cinematicDeckCamera;

        private bool isPlayerView = true;
        private GUIStyle boxStyle;
        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle keyStyle;

        private void Update()
        {
            // Press 'C' or 'V' to toggle cameras
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.V))
            {
                ToggleCamera();
            }
        }

        public void ToggleCamera()
        {
            isPlayerView = !isPlayerView;
            if (playerFollowCamera != null)
                playerFollowCamera.SetActive(isPlayerView);
            if (cinematicDeckCamera != null)
                cinematicDeckCamera.SetActive(!isPlayerView);
        }

        private void OnGUI()
        {
            InitStyles();

            // Top-left controls overlay
            float panelWidth = 260f;
            float panelHeight = 150f;
            Rect panelRect = new Rect(18f, 18f, panelWidth, panelHeight);

            GUI.Box(panelRect, GUIContent.none, boxStyle);

            GUILayout.BeginArea(new Rect(panelRect.x + 14f, panelRect.y + 12f, panelWidth - 28f, panelHeight - 24f));

            GUILayout.Label("DINOSAUR PARK ROAM", headerStyle);
            GUILayout.Space(6f);

            DrawControlRow("WASD / Arrows", "Move Character");
            DrawControlRow("Shift", "Sprint / Run");
            DrawControlRow("Space", "Jump");
            DrawControlRow("Mouse", "360° Look");
            DrawControlRow("C Key", isPlayerView ? "Switch to Deck Cam" : "Switch to Player");

            GUILayout.EndArea();

            // Camera toggle button at bottom-right
            float btnW = 190f;
            float btnH = 36f;
            Rect btnRect = new Rect(Screen.width - btnW - 20f, Screen.height - btnH - 20f, btnW, btnH);
            string btnLabel = isPlayerView ? "🔭 View: Observation Deck" : "🚶 View: Free Roam Player";
            if (GUI.Button(btnRect, btnLabel))
            {
                ToggleCamera();
            }
        }

        private void DrawControlRow(string key, string desc)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(key, keyStyle, GUILayout.Width(95f));
            GUILayout.Label(desc, labelStyle);
            GUILayout.EndHorizontal();
        }

        private void InitStyles()
        {
            if (boxStyle != null) return;

            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.08f, 0.10f, 0.12f, 0.85f));
            bgTex.Apply();

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = bgTex;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            headerStyle.normal.textColor = new Color(0.95f, 0.78f, 0.25f);

            keyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            keyStyle.normal.textColor = new Color(0.4f, 0.85f, 1.0f);

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11
            };
            labelStyle.normal.textColor = new Color(0.88f, 0.90f, 0.92f);
        }
    }
}
