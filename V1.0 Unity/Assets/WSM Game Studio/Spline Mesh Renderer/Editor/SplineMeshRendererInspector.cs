using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WSMGameStudio.Splines;

[CustomEditor(typeof(SplineMeshRenderer))]
public class SplineMeshRendererInspector : Editor
{
    private SplineMeshRenderer _splineMeshRenderer;

    private GUIStyle _menuBoxStyle;

    private GUIContent _btnPrintMeshDetails = new GUIContent("Print Mesh Details", "Prints the generated mesh details on console window when Realtime Mesh Generation is selected");
    private GUIContent _btnCreateMesh = new GUIContent("Generate Mesh", "Manually generates the mesh when Manual Mesh Generation is selected");
    private GUIContent _btnBakeMesh = new GUIContent("Bake Mesh", "Exports the generated mesh as a prefab (Mesh Baker Window)");
    private GUIContent _btnConnectNewRenderer = new GUIContent("Connect New Renderer", string.Format("Connects a new Mesh Renderer at the end of the current one. Usefull to improve performance with occlusion culling.", System.Environment.NewLine));
    private GUIContent _btnSpawnCaps = new GUIContent("Spawn Caps", "Spawn or updates start and end caps instances");

    private SerializedProperty _spline;
    private SerializedProperty _meshGenerationProfile;
    private SerializedProperty _meshGenerationMethod;
    private SerializedProperty _meshOffset;
    private SerializedProperty _autoSplit;
    private SerializedProperty _verticesLimit;
    private SerializedProperty _startCap;
    private SerializedProperty _endCap;

    /// <summary>
    /// Caching Serialized Properties for better performance
    /// </summary>
    private void OnEnable()
    {
        _spline = serializedObject.FindProperty("_spline");
        _meshGenerationProfile = serializedObject.FindProperty("_meshGenerationProfile");
        _meshGenerationMethod = serializedObject.FindProperty("_meshGenerationMethod");
        _meshOffset = serializedObject.FindProperty("_meshOffset");
        _autoSplit = serializedObject.FindProperty("_autoSplit");
        _verticesLimit = serializedObject.FindProperty("_verticesLimit");
        _startCap = serializedObject.FindProperty("_startCap");
        _endCap = serializedObject.FindProperty("_endCap");
    }

    /// <summary>
    /// Draw spline on editor
    /// </summary>
    private void OnSceneGUI()
    {
        try
        {
            _splineMeshRenderer = (SplineMeshRenderer)target;

            if (_splineMeshRenderer.Spline != null && _splineMeshRenderer.Spline.Theme != null)
            {
                Handles.BeginGUI();

                int posX = _splineMeshRenderer.Spline.Theme.supportsVerticalBuilding ? _splineMeshRenderer.Spline.Theme.button8PosX : _splineMeshRenderer.Spline.Theme.button6PosX;
                int posY = _splineMeshRenderer.Spline.Theme.supportsVerticalBuilding ? _splineMeshRenderer.Spline.Theme.button8PosY : _splineMeshRenderer.Spline.Theme.button6PosY;

                if (_splineMeshRenderer.MeshGenerationMethod == MeshGenerationMethod.Manual)
                {
                    // Scene Window buttons
                    if (GUI.Button(new Rect(posX, posY, _splineMeshRenderer.Spline.Theme.buttonSize, _splineMeshRenderer.Spline.Theme.buttonSize), _splineMeshRenderer.Spline.Theme.MeshIcon))
                    {
                        _splineMeshRenderer.ExtrudeMesh();
                        MarkSceneAlteration();
                    }
                    // Add icons to inspector buttons
                    _btnCreateMesh.image = _splineMeshRenderer.Spline.Theme.SmallMeshIcon;
                }
                else if (_splineMeshRenderer.MeshGenerationMethod == MeshGenerationMethod.Realtime)
                {
                    // Scene Window buttons
                    if (GUI.Button(new Rect(posX, posY, _splineMeshRenderer.Spline.Theme.buttonSize, _splineMeshRenderer.Spline.Theme.buttonSize), _splineMeshRenderer.Spline.Theme.LogIcon))
                    {
                        _splineMeshRenderer.PrintMeshDetails();
                    }
                    // Add icons to inspector buttons
                    _btnPrintMeshDetails.image = _splineMeshRenderer.Spline.Theme.SmallLogIcon;
                }

                Handles.EndGUI();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    /// <summary>
    /// Custom inspector
    /// </summary>
    public override void OnInspectorGUI()
    {
        try
        {
            //base.OnInspectorGUI();
            _splineMeshRenderer = (SplineMeshRenderer)target;

            //Set up the box style if null
            if (_menuBoxStyle == null)
            {
                _menuBoxStyle = new GUIStyle(GUI.skin.box);
                _menuBoxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                _menuBoxStyle.fontStyle = FontStyle.Bold;
                _menuBoxStyle.alignment = TextAnchor.UpperLeft;
            }

            GUILayout.BeginVertical(_menuBoxStyle);

            #region Mesh Generation Settings

            GUILayout.Label("MESH GENERATION SETTINGS", EditorStyles.boldLabel);

            serializedObject.Update();
            EditorGUILayout.PropertyField(_spline, false);
            EditorGUILayout.PropertyField(_meshGenerationProfile, false);
            EditorGUILayout.PropertyField(_meshGenerationMethod, false);
            EditorGUILayout.PropertyField(_meshOffset, false);
            EditorGUILayout.PropertyField(_autoSplit, false);
            EditorGUILayout.PropertyField(_verticesLimit, false);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_startCap, false);
            EditorGUILayout.PropertyField(_endCap, false);
            GUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();

            GUILayout.Label("MESH OPERATIONS", EditorStyles.boldLabel);


            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            if (_splineMeshRenderer.MeshGenerationMethod != MeshGenerationMethod.Manual)
            {
                if (GUILayout.Button(_btnPrintMeshDetails))
                {
                    _splineMeshRenderer.PrintMeshDetails();
                }
            }
            else
            {
                if (GUILayout.Button(_btnCreateMesh))
                {
                    _splineMeshRenderer.ExtrudeMesh();
                    MarkSceneAlteration();
                }
            }

            if (GUILayout.Button(_btnConnectNewRenderer))
            {
                Selection.activeGameObject = _splineMeshRenderer.ConnectNewRenderer();
                MarkSceneAlteration();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button(_btnBakeMesh))
            {
                BakeMeshWindow.ShowWindow();
            }

            if (GUILayout.Button(_btnSpawnCaps))
            {
                _splineMeshRenderer.SpawnCaps();
                MarkSceneAlteration();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            #endregion

            GUILayout.EndVertical();
        }
        catch (System.Exception ex)
        {
            if (ex.GetType() != typeof(ExitGUIException)) // ExitGUIException is used by the Unity GUI System as a flag to stop the GUI Loop
                Debug.LogError(ex.Message);
        }
    }

    /// <summary>
    /// Show player the scene needs to be saved
    /// </summary>
    private void MarkSceneAlteration()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(_splineMeshRenderer);
            EditorUtility.SetDirty(_splineMeshRenderer.Spline); //Spline must also be saved to update serialized references

            if (_splineMeshRenderer.GeneratedMeshes != null)
                foreach (var meshObj in _splineMeshRenderer.GeneratedMeshes)
                    EditorUtility.SetDirty(meshObj.GetGameObject);

            if (_splineMeshRenderer.GeneratedColliders != null)
                foreach (var colliderObj in _splineMeshRenderer.GeneratedColliders)
                    EditorUtility.SetDirty(colliderObj.GetGameObject);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
