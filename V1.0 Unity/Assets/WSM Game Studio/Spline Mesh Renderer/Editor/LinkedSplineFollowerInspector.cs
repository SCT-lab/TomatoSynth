using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEngine;

namespace WSMGameStudio.Splines
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LinkedSplineFollower))]
    public class LinkedSplineFollowerInspector : Editor
    {
        private GUIStyle _menuBoxStyle;

        SerializedProperty _followingOffset;

        private void OnEnable()
        {
            _followingOffset = serializedObject.FindProperty("followingOffset");
        }

        public override void OnInspectorGUI()
        {
            //base.DrawDefaultInspector();
            //Set up the box style if null
            if (_menuBoxStyle == null)
            {
                _menuBoxStyle = new GUIStyle(GUI.skin.box);
                _menuBoxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                _menuBoxStyle.fontStyle = FontStyle.Bold;
                _menuBoxStyle.alignment = TextAnchor.UpperLeft;
            }

            GUILayout.BeginVertical(_menuBoxStyle);

            GUILayout.Label("LINKED FOLLOWER SETTINGS", EditorStyles.boldLabel);

            serializedObject.Update();
            float offset = _followingOffset.floatValue;
            EditorGUILayout.PropertyField(_followingOffset, new GUIContent("Following Offset"));
            if (offset != _followingOffset.floatValue) _followingOffset.floatValue = Mathf.Abs(_followingOffset.floatValue);
            serializedObject.ApplyModifiedProperties();
            
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Show player the scene needs to be saved
        /// </summary>
        private void MarkSceneAlteration(Object target)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(target);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }
    } 
}
