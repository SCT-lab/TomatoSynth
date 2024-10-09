using UnityEditor;
using UnityEngine;

namespace WSMGameStudio.About
{
    public class WSM_AboutWindow : EditorWindow
    {
        private GUIStyle _menuBoxStyle;

        [MenuItem("WSM Game Studio/About", false, 12)]
        [MenuItem("WSM Game Studio/Support Request", false, 11)]
        public static void ShowWindow()
        {
            WSM_AboutWindow currentWindow = GetWindow<WSM_AboutWindow>("About WSM Game Studio");
            currentWindow.minSize = new Vector2(580, 400);
        }

        private void OnGUI()
        {
            //Set up the box style if null
            if (_menuBoxStyle == null)
            {
                _menuBoxStyle = new GUIStyle(GUI.skin.box);
                _menuBoxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                _menuBoxStyle.fontStyle = FontStyle.Bold;
                _menuBoxStyle.alignment = TextAnchor.UpperLeft;
            }

            GUILayout.BeginVertical(_menuBoxStyle);

            #region ABOUT
            GUILayout.Label("About", EditorStyles.boldLabel);

            GUILayout.Label(@"Passionate indie game developer
Currently focused on developing good quality assets for Unity 3D.

If you enjoyed the asset, please don't forget to leave a positive review in the asset store.

Thank you very much");

            #endregion

            #region RECOMMENDED ASSETS
            GUILayout.Label("Recommended Assets", EditorStyles.boldLabel);
            if (GUILayout.Button("Train Controller Collection"))
            {
                WSM_ExternalLinks.TrainControllerCollection();
            }

            if (GUILayout.Button("Heavy Machinery Collection"))
            {
                WSM_ExternalLinks.HeavyMachineryCollection();
            }

            if (GUILayout.Button("More Assets"))
            {
                WSM_ExternalLinks.MoreAssets();
            }
            #endregion

            #region SOCIAL MEDIA
            GUILayout.Label("Social Media", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Facebook"))
            {
                WSM_ExternalLinks.Facebook();
            }

            if (GUILayout.Button("Instagram"))
            {
                WSM_ExternalLinks.Instagram();
            }

            if (GUILayout.Button("Twitter"))
            {
                WSM_ExternalLinks.Twitter();
            }

            if (GUILayout.Button("Discord"))
            {
                WSM_ExternalLinks.Discord();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Steam Games"))
            {
                WSM_ExternalLinks.Steam();
            }
            #endregion

            #region SUPPPORT
            GUILayout.Label("Support", EditorStyles.boldLabel);

            GUILayout.Label(string.Format(@"For support requests, please message me at wsmgamestudio@gmail.com
Your request must included:

1) Asset Store Invoice Number
2) Detailed description of the issue (including Unity Editor Version)
3) A fullscreen uncropped screenshot of the issue"));

            if (GUILayout.Button("Support Request"))
            {
                WSM_ExternalLinks.SupportRequest();
            }
            #endregion

            GUILayout.EndVertical();
        }
    }
}
