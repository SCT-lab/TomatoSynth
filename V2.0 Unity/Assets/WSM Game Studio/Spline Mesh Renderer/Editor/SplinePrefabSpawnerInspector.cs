using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WSMGameStudio.Splines;

[CustomEditor(typeof(SplinePrefabSpawner))]
public class SplinePrefabSpawnerInspector : Editor
{
    private SplinePrefabSpawner _splinePrefabSpawner;

    private GUIStyle _menuBoxStyle;

    private GUIContent _btnSpawn = new GUIContent("Spawn", "Spawn instances of the selected prefabs");
    private GUIContent _btnReset = new GUIContent("Reset", "Removes all spawned instances from scene");

    private SerializedProperty _splines;
    private SerializedProperty _spawningMethod;
    private SerializedProperty _instances;
    private SerializedProperty _prefabs;
    private SerializedProperty _spawnOffset;

    private void OnEnable()
    {
        _splines = serializedObject.FindProperty("splines");
        _spawningMethod = serializedObject.FindProperty("spawningMethod");
        _instances = serializedObject.FindProperty("instances");
        _prefabs = serializedObject.FindProperty("prefabs");
        _spawnOffset = serializedObject.FindProperty("spawnOffset");
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        _splinePrefabSpawner = (SplinePrefabSpawner)target;

        //Set up the box style if null
        if (_menuBoxStyle == null)
        {
            _menuBoxStyle = new GUIStyle(GUI.skin.box);
            _menuBoxStyle.normal.textColor = GUI.skin.label.normal.textColor;
            _menuBoxStyle.fontStyle = FontStyle.Bold;
            _menuBoxStyle.alignment = TextAnchor.UpperLeft;
        }

        GUILayout.BeginVertical(_menuBoxStyle);

        #region SPAWNER SETTINGS
        /*
         * Spawner Settings
         */
        GUILayout.Label("SPAWNER SETTINGS", EditorStyles.boldLabel);

        serializedObject.Update();
        EditorGUILayout.PropertyField(_splines, true);
        EditorGUILayout.PropertyField(_spawningMethod, false);
        EditorGUILayout.PropertyField(_instances, false);
        EditorGUILayout.PropertyField(_prefabs, true);
        EditorGUILayout.PropertyField(_spawnOffset, false);
        serializedObject.ApplyModifiedProperties();

        #endregion

        #region SPAWNER OPERATIONS
        /*
         * Spawner Operations
         */
        GUILayout.Label("SPAWNER OPERATIONS", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button(_btnSpawn))
        {
            _splinePrefabSpawner.SpawnPrefabs();
            MarkSceneAlteration();
        }

        if (GUILayout.Button(_btnReset))
        {
            _splinePrefabSpawner.ResetObjects();
            MarkSceneAlteration();
        }

        GUILayout.EndHorizontal(); 
        #endregion

        GUILayout.EndVertical();
    }

    /// <summary>
    /// Show player the scene needs to be saved
    /// </summary>
    private void MarkSceneAlteration()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(_splinePrefabSpawner);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
