using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEditor;
using WSMGameStudio.Splines;

[CanEditMultipleObjects]
[CustomEditor(typeof(SplineFollower))]
public class SplineFollowerInspector : Editor
{
    private SplineFollower _splineFollower;

    private int _selectedMenuIndex = 0;
    private string[] _toolbarMenuOptions = new[] { "Follower Settings", "Linked Followers" };
    private GUIStyle _menuBoxStyle;

    private GUIContent _btnMoveToStart = new GUIContent("Move to Start Position", "Move object to start position along the spline");
    private GUIContent _btnRestart = new GUIContent("Restart Follower", "Restarts follower to its initial position");

    private SerializedProperty _splines;
    private SerializedProperty _speed;
    private SerializedProperty _speedUnit;
    private SerializedProperty _followerBehaviour;
    private SerializedProperty _customStartPosition;
    private SerializedProperty _applySplineRotation;
    private SerializedProperty _cycleEndStops;
    private SerializedProperty _cycleStopTime;
    private SerializedProperty _visualizePathOnEditor;
    private SerializedProperty _followersOffset;
    private SerializedProperty _linkedFollowersBehaviour;
    private SerializedProperty _linkedFollowers;

    private void OnEnable()
    {
        _splines = serializedObject.FindProperty("splines");
        _speed = serializedObject.FindProperty("speed");
        _speedUnit = serializedObject.FindProperty("speedUnit");
        _followerBehaviour = serializedObject.FindProperty("followerBehaviour");
        _customStartPosition = serializedObject.FindProperty("customStartPosition");
        _applySplineRotation = serializedObject.FindProperty("applySplineRotation");
        _cycleEndStops = serializedObject.FindProperty("cycleEndStops");
        _cycleStopTime = serializedObject.FindProperty("cycleStopTime");
        _visualizePathOnEditor = serializedObject.FindProperty("visualizePathOnEditor");
        _followersOffset = serializedObject.FindProperty("followersOffset");
        _linkedFollowersBehaviour = serializedObject.FindProperty("linkedFollowersBehaviour");
        _linkedFollowers = serializedObject.FindProperty("linkedFollowers");
    }

    public override void OnInspectorGUI()
    {
        //base.DrawDefaultInspector();
        _splineFollower = target as SplineFollower;

        //Set up the box style if null
        if (_menuBoxStyle == null)
        {
            _menuBoxStyle = new GUIStyle(GUI.skin.box);
            _menuBoxStyle.normal.textColor = GUI.skin.label.normal.textColor;
            _menuBoxStyle.fontStyle = FontStyle.Bold;
            _menuBoxStyle.alignment = TextAnchor.UpperLeft;
        }

        GUILayout.BeginVertical(_menuBoxStyle);

        serializedObject.Update();

        _selectedMenuIndex = GUILayout.Toolbar(_selectedMenuIndex, _toolbarMenuOptions);
        if (_selectedMenuIndex == (int)FollowerInspectorMenu.FollowerSettings)
        {
            #region FOLLOWER SETTINGS
            /*
             * Follower Settings
             */
            GUILayout.Label("FOLLOWER SETTINGS", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_splines, true);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_speed, false);
            EditorGUILayout.PropertyField(_speedUnit, GUIContent.none, false, GUILayout.MaxWidth(50));
            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(_followerBehaviour, false);
            EditorGUILayout.PropertyField(_customStartPosition, false);
            EditorGUILayout.PropertyField(_applySplineRotation, false);
            EditorGUILayout.PropertyField(_cycleEndStops, false);
            EditorGUILayout.PropertyField(_cycleStopTime, false);
            EditorGUILayout.PropertyField(_visualizePathOnEditor, false);

            #endregion
        }
        else if (_selectedMenuIndex == (int)FollowerInspectorMenu.LinkedFollowers)
        {
            #region LINKED FOLLOWERS
            EditorGUILayout.PropertyField(_followersOffset, false);
            EditorGUILayout.PropertyField(_linkedFollowersBehaviour, false);
            EditorGUILayout.PropertyField(_linkedFollowers, true);
            #endregion
        }

        serializedObject.ApplyModifiedProperties();

        #region FOLLOWER OPERATIONS
        /*
         * Follower Operations
         */
        GUILayout.Label("FOLLOWER OPERATIONS", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        if (Selection.gameObjects != null)
        {
            if (GUILayout.Button(_btnMoveToStart))
            {
                foreach (var selected in Selection.gameObjects)
                {
                    SplineFollower follower = selected.GetComponent<SplineFollower>();
                    if (follower != null)
                    {
                        follower.MoveToStartPosition();
                        MarkSceneAlteration(selected);
                    }
                }
            }

            if (GUILayout.Button(_btnRestart))
            {
                foreach (var selected in Selection.gameObjects)
                {
                    SplineFollower follower = selected.GetComponent<SplineFollower>();
                    if (follower != null)
                    {
                        follower.RestartFollower();
                        MarkSceneAlteration(selected);
                    }
                }
            }
        }

        GUILayout.EndHorizontal();
        #endregion

        GUILayout.EndVertical();
    }

    private void OnSceneGUI()
    {
        _splineFollower = target as SplineFollower;

        if (_splineFollower.visualizePathOnEditor)
        {
            Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local ? _splineFollower.transform.rotation : Quaternion.identity;

            OrientedPoint[][] followerPath = _splineFollower.NormalizedOrientedPoints;
            for (int splineIndex = 0; splineIndex < followerPath.Length; splineIndex++)
            {
                for (int pointIndex = 0; pointIndex < followerPath[splineIndex].Length; pointIndex++)
                {
                    Vector3 pointPosition = followerPath[splineIndex][pointIndex].Position;
                    float size = HandleUtility.GetHandleSize(pointPosition);
                    size *= 3f;
                    Handles.color = Color.white;
                    if (Handles.Button(pointPosition, handleRotation, (size * 0.04f), (size * 0.06f), Handles.SphereHandleCap))
                    {
                        SceneViewZoom(pointPosition, 10f);
                    }

                    //Oriented Points Directions
                    Vector3 lineStart = pointPosition;
                    Vector3 zAxisEnd = lineStart + (followerPath[splineIndex][pointIndex].Forward) * SplineDefaultValues.DirectionScale;
                    Vector3 yAxisEnd = lineStart + (followerPath[splineIndex][pointIndex].Up) * SplineDefaultValues.DirectionScale;
                    Vector3 xAxisEnd = lineStart + (followerPath[splineIndex][pointIndex].Right) * SplineDefaultValues.DirectionScale;

                    DrawLine(Color.blue, lineStart, zAxisEnd);
                    DrawLine(Color.green, lineStart, yAxisEnd);
                    DrawLine(Color.red, lineStart, xAxisEnd);
                }
            }
        }
    }

    /// <summary>
    /// Draw a line of the selected color
    /// </summary>
    /// <param name="lineColor"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineEnd"></param>
    private void DrawLine(Color lineColor, Vector3 lineStart, Vector3 lineEnd)
    {
        Handles.color = lineColor;
        Handles.DrawLine(lineStart, lineEnd);
    }

    /// <summary>
    /// Zoom on selected point
    /// </summary>
    /// <param name="point"></param>
    /// <param name="zoomLevel"></param>
    private static void SceneViewZoom(Vector3 point, float zoomLevel)
    {
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.Frame(new Bounds(point, Vector3.one * zoomLevel), false);
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
