using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WSMGameStudio.Splines;

[CustomEditor(typeof(BakedSegment))]
public class BakedSegmentInspector : Editor
{
    private BakedSegment _bakedSegment;

    private GUIStyle _menuBoxStyle;

    private GUIContent _btnConnectTarget = new GUIContent("Connect Target", "Connect target to the end of this spline");

    private SerializedProperty _endPoint;
    private SerializedProperty _operationTarget;

    private void OnEnable()
    {
        _endPoint = serializedObject.FindProperty("_endPoint");
        _operationTarget = serializedObject.FindProperty("_operationTarget");
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        _bakedSegment = (BakedSegment)target;

        //Set up the box style if null
        if (_menuBoxStyle == null)
        {
            _menuBoxStyle = new GUIStyle(GUI.skin.box);
            _menuBoxStyle.normal.textColor = GUI.skin.label.normal.textColor;
            _menuBoxStyle.fontStyle = FontStyle.Bold;
            _menuBoxStyle.alignment = TextAnchor.UpperLeft;
        }

        GUILayout.BeginVertical(_menuBoxStyle);

        GUILayout.Label("SEGMENT SETTINGS", EditorStyles.boldLabel);

        serializedObject.Update();
        EditorGUILayout.PropertyField(_endPoint, false);
        EditorGUILayout.PropertyField(_operationTarget, false);
        serializedObject.ApplyModifiedProperties();

        GUILayout.Label("SEGMENT OPERATIONS", EditorStyles.boldLabel);

        if (GUILayout.Button(_btnConnectTarget))
        {
            _bakedSegment.ConnectTarget();
            MarkSceneAlteration(_bakedSegment);
        }

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
