using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using WSMGameStudio.Splines;
using System.Text.RegularExpressions;
using UnityEngine.Profiling;
using System.Collections.Generic;

[CustomEditor(typeof(Spline))]
public class SplineInspector : Editor
{
    private const int _lineSteps = 10;

    private Spline _spline;
    private Transform _splineTransform;
    private Quaternion _handleRotation;
    private const float _handleSize = 0.04f;
    private const float _pickSize = 0.06f;
    private int _selectedIndex = -1;
    private List<int> _multiSelectedPoints = new List<int>();

    private Color _splineColor = Color.white;

    private Object _operationTarget;

    bool IsFirstPoint
    {
        get { return (_selectedIndex == 0); }
    }

    bool IsLastPoint
    {
        get { return (_selectedIndex == (_spline.ControlPointCount - 1)); }
    }

    bool IsHandle
    {
        get { return (_selectedIndex > 0 && _selectedIndex % 3 != 0); }
    }

    bool NoPointSelected
    {
        get { return _selectedIndex < 0 || _selectedIndex > (_spline.ControlPointCount - 1); }
    }

    private int _selectedMenuIndex = 0;
    private string[] _toolbarMenuOptions = new[] { "Curve", "Spline", "Handles", "Terrain", "Scene View" };
    private GUIStyle _menuBoxStyle;

    private GUIContent _btnAddNewCurve = new GUIContent("Add Curve", "Adds a new curve at the end of the spline");
    private GUIContent _btnRemoveCurve = new GUIContent("Remove Curve", "Removes the last spline curve");

    private GUIContent _btnResetCurve = new GUIContent("Straight Line", "Change the last curve to a straight line");
    private GUIContent _btnTurnRight = new GUIContent("Right", "Turns the last spline curve to the right");
    private GUIContent _btnTurnLeft = new GUIContent("Left", "Turns the last spline curve to the left");
    private GUIContent _btnTurnUpwards = new GUIContent("Up", "Turns the last spline curve upwards");
    private GUIContent _btnTurnDownwards = new GUIContent("Down", "Turns the last spline curve downwards");

    private GUIContent _btnSubdivideCurve = new GUIContent("Subdivide Curve", "Insert a new curve between the current selected curve and the next one");
    private GUIContent _btnDissolveCurve = new GUIContent("Dissolve Curve", "Dissolve selected curve");

    private GUIContent _btnSplitSpline = new GUIContent("Split Spline", "Split spline into two");
    private GUIContent _btnMergeSplines = new GUIContent("Merge Splines", "Merge two splines into one");

    private GUIContent _btnConnectTarget = new GUIContent("Connect Target", "Connect target to the end of this spline");
    private GUIContent _btnBridgeGap = new GUIContent("Bridge Gap", "Creates a new curve to close the gap between this and target spline");
    private GUIContent _btnCreateParallelSpline = new GUIContent("Create Parallel Spline", "Creates a parallel spline");
    private GUIContent _btnRevertSpline = new GUIContent("Revert Spline", "Reverts the spline direction");

    private GUIContent _btnResetNormals = new GUIContent("Reset Normals", "Resets all control points normals");
    private GUIContent _btnResetSpline = new GUIContent("Reset Spline", "Restarts spline from scratch");
    private GUIContent _btnDeleteOrientedPoints = new GUIContent("Delete Oriented Points", "Deletes the oriented points (for testing purposes only)");

    private GUIContent _btnFlatten = new GUIContent("Flatten", "Resets all control points height values, making a flat spline");
    private GUIContent _btnAppendSpline = new GUIContent("Append Spline", "Connect a new spline at the end of the current one");

    private GUIContent _btnTerraform = new GUIContent("Terraform", "Adjust the terrain along the spline");
    private GUIContent _btnPaintTerrain = new GUIContent("Paint Terrain", "Paint terrain along the spline");
    private GUIContent _btnBackupTerrains = new GUIContent("Backup Terrains", "Create or update terrains backups");
    private GUIContent _btnRestoreTerrains = new GUIContent("Restore Terrains", "Restore terrains backups (if any)");
    private GUIContent _btnSelectedNodesToTerrain = new GUIContent("Selected Nodes to Terrain", "Project selected nodes to terrains");
    private GUIContent _btnNodesToTerrain = new GUIContent("Nodes to Terrain", "Project all nodes to terrains");

    private static Color[] _alignmentColors = {
        Color.white,
        Color.yellow,
        Color.cyan,
        Color.red
    };

    //Caching for performance
    private Vector3 _segmentStartPoint;
    private Vector3 _bezierHandle1;
    private Vector3 _bezierHandle2;
    private Vector3 _segmentEndPoint;

    private Vector3 _previewStartPoint;
    private Vector3 _previewHandle1;
    private Vector3 _previewHandle2;
    private Vector3 _previewEndPoint;

    private void OnEnable()
    {
        _spline = target as Spline;

        if (_spline.EmbankmentSlope == null || _spline.EmbankmentSlope.length == 0)
            _spline.EmbankmentSlope = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    /// <summary>
    /// Draw spline on editor
    /// </summary>
    private void OnSceneGUI()
    {
        _spline = target as Spline;

        bool multiSelection = (Selection.objects != null && Selection.objects.Length > 1);

        Profiler.BeginSample("Draw Spline on Scene View (WSM)");
        #region Draw Spline on Scene View

        _splineTransform = _splineTransform == null ? _spline.transform : _splineTransform;
        _handleRotation = Tools.pivotRotation == PivotRotation.Local ? _splineTransform.rotation : Quaternion.identity;

        bool startVisible, isHandle1_Visible, isHandle2_Visible, endVisible;

        if (_spline.HandlesVisibility == HandlesVisibility.DebugOrientedPoints && _spline.OrientedPoints != null)
        {
            for (int i = 0; i < _spline.OrientedPoints.Length; i++)
            {
                Vector3 pointPosition = _spline.OrientedPoints[i].Position;
                float size = HandleUtility.GetHandleSize(pointPosition);
                size *= 3f;
                Handles.color = Color.white;
                if (Handles.Button(pointPosition, _handleRotation, (size * _handleSize), (size * _pickSize), Handles.SphereHandleCap))
                {
                    if (_spline.ZoomOnClick)
                        SceneViewZoom(pointPosition, _spline.ZoomLevel);
                }

                //Oriented Points Directions
                Vector3 lineStart = pointPosition;
                Vector3 zAxisEnd = lineStart + (_spline.OrientedPoints[i].Forward) * SplineDefaultValues.DirectionScale;
                Vector3 yAxisEnd = lineStart + (_spline.OrientedPoints[i].Up) * SplineDefaultValues.DirectionScale;
                Vector3 xAxisEnd = lineStart + (_spline.OrientedPoints[i].Right) * SplineDefaultValues.DirectionScale;

                DrawLine(Color.blue, lineStart, zAxisEnd);
                DrawLine(Color.green, lineStart, yAxisEnd);
                DrawLine(Color.red, lineStart, xAxisEnd);
            }
        }
        else
        {
            //Draw spline - 1 section by iteration
            _segmentStartPoint = ShowControlPoint(0, out startVisible);
            for (int i = 1; i < _spline.ControlPointCount; i += 3)
            {
                //Draw control points
                _bezierHandle1 = ShowControlPoint(i, out isHandle1_Visible); //Handle 1
                _bezierHandle2 = ShowControlPoint(i + 1, out isHandle2_Visible); //Handle 2
                _segmentEndPoint = ShowControlPoint(i + 2, out endVisible); //Next Section start

                //Draw handle lines
                Handles.color = Color.red;
                if (isHandle1_Visible) Handles.DrawLine(_segmentStartPoint, _bezierHandle1);
                if (isHandle2_Visible) Handles.DrawLine(_bezierHandle2, _segmentEndPoint);

                _splineColor = _spline.Theme != null ? _spline.Theme.splineColor : Color.white;
                Handles.DrawBezier(_segmentStartPoint, _segmentEndPoint, _bezierHandle1, _bezierHandle2, _splineColor, null, 2f);
                _segmentStartPoint = _segmentEndPoint;
            }

            ShowDirections();
        }
        #endregion
        Profiler.EndSample();

        Profiler.BeginSample("Quick Building Menu (WSM)");
        #region Quick Building Menu
        Handles.BeginGUI();

        if (_spline.Theme == null)
        {
            GUIStyle noThemeSelectedWarningStyle = new GUIStyle(GUI.skin.box);
            noThemeSelectedWarningStyle.alignment = TextAnchor.MiddleLeft;
            noThemeSelectedWarningStyle.fontStyle = FontStyle.Bold;
            noThemeSelectedWarningStyle.normal.textColor = Color.red;

            GUI.Box(new Rect(10, SceneView.lastActiveSceneView.camera.pixelHeight - 60, 350, 50), string.Format("Spline Theme not selected.{0}To unlock the quick access menu, please select a theme under the SCENE VIEW tab in the Inspector.", System.Environment.NewLine), noThemeSelectedWarningStyle);
        }
        else
        {
            // Scene Window buttons
            if (GUI.Button(new Rect(_spline.Theme.button1PosX, _spline.Theme.button1PosY, _spline.Theme.buttonSize, _spline.Theme.buttonSize), _spline.Theme.AddIcon))
                AddCurve();
            if (GUI.Button(new Rect(_spline.Theme.button2PosX, _spline.Theme.button2PosY, _spline.Theme.buttonSize, _spline.Theme.buttonSize), _spline.Theme.DeleteIcon))
                RemoveCurve();
            if (GUI.Button(new Rect(_spline.Theme.button3PosX, _spline.Theme.button3PosY, _spline.Theme.buttonSize, _spline.Theme.buttonSize), _spline.Theme.CurvedArrowLeftIcon))
                TurnCurve_Left();
            if (GUI.Button(new Rect(_spline.Theme.button4PosX, _spline.Theme.button4PosY, _spline.Theme.buttonSize, _spline.Theme.buttonSize), _spline.Theme.CurvedArrowRightIcon))
                TurnCurve_Right();

            if (_spline.Theme.supportsVerticalBuilding)
            {
                if (GUI.Button(new Rect(_spline.Theme.button5PosX, _spline.Theme.button5PosY, _spline.Theme.buttonSize, _spline.Theme.buttonSize), _spline.Theme.CurvedArrowUpIcon))
                    TurnCurve_Upwards();
                if (GUI.Button(new Rect(_spline.Theme.button6PosX, _spline.Theme.button6PosY, _spline.Theme.buttonSize, _spline.Theme.buttonSize), _spline.Theme.CurvedArrowDownIcon))
                    TurnCurve_Downwards();
                if (GUI.Button(new Rect(_spline.Theme.button7PosX, _spline.Theme.button7PosY, _spline.Theme.buttonSize, _spline.Theme.buttonSize), _spline.Theme.ArrowUpIcon))
                    ResetCurve();
            }
            else
            {
                if (GUI.Button(new Rect(_spline.Theme.button5PosX, _spline.Theme.button5PosY, _spline.Theme.buttonSize, _spline.Theme.buttonSize), _spline.Theme.ArrowUpIcon))
                    ResetCurve();
            }

            float currentAngle = _spline.NewCurveAngle;
            GUI.Box(new Rect(_spline.Theme.inputBoxPosX, _spline.Theme.inputBoxPosY, 110, 30), string.Format("Angle: {0}º", currentAngle));
            float newAngle = GUI.HorizontalSlider(new Rect(_spline.Theme.inputBoxPosX + 10, _spline.Theme.inputBoxPosY + 10, 90, 15), currentAngle, 0f, 90f);
            if (newAngle != currentAngle)
                _spline.NewCurveAngle = Mathf.Round(newAngle); //Use only round numbers on the sliders for better user experience

            GUIStyle curveLengthBoxStyle = new GUIStyle(GUI.skin.box);
            curveLengthBoxStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Box(new Rect(_spline.Theme.inputBoxPosX, _spline.Theme.inputBoxPosY + 30, 110, 30), "Length:", curveLengthBoxStyle);
            float currentLength = _spline.NewCurveLength;
            float newLength = currentLength;
            string length = GUI.TextField(new Rect(_spline.Theme.inputBoxPosX + 50, _spline.Theme.inputBoxPosY + 36, 50, 18), _spline.NewCurveLength.ToString());
            length = Regex.Replace(length, "[^0-9]?", "");
            float.TryParse(length, out newLength);
            if (newLength != currentLength)
                _spline.NewCurveLength = newLength;

            // Add icons to inspector buttons
            _btnAddNewCurve.image = _spline.Theme.SmallAddIcon;
            _btnRemoveCurve.image = _spline.Theme.SmallDeleteIcon;
            _btnTurnRight.image = _spline.Theme.SmallCurvedArrowRightIcon;
            _btnTurnLeft.image = _spline.Theme.SmallCurvedArrowLeftIcon;
            _btnTurnUpwards.image = _spline.Theme.SmallCurvedArrowUpIcon;
            _btnTurnDownwards.image = _spline.Theme.SmallCurvedArrowDownIcon;
            _btnResetCurve.image = _spline.Theme.SmallArrowUpIcon;

            /*
             * Shortcut menu
             */
            if (_spline.Theme.showShortcutMenu)
            {
                GUIStyle shortCutmenuStyle = new GUIStyle(GUI.skin.box);
                shortCutmenuStyle.alignment = TextAnchor.MiddleLeft;
                shortCutmenuStyle.fontStyle = FontStyle.Bold;
                shortCutmenuStyle.normal.textColor = Color.black;

                if (multiSelection)
                {
                    shortCutmenuStyle.normal.textColor = Color.red;
                    GUI.Box(new Rect(10, SceneView.lastActiveSceneView.camera.pixelHeight - 40, 350, 30), string.Format("Multi-object Editing Not Supported", System.Environment.NewLine), shortCutmenuStyle);
                }
                else
                {
                    GUI.Box(new Rect(10, SceneView.lastActiveSceneView.camera.pixelHeight - 60, 350, 50), string.Format("SHIFT [Hold]:   Preview new curve{0}SHIFT + RIGHT CLICK:   Add new curve{0}CTRL + LEFT CLICK:   Control points multi-selection", System.Environment.NewLine), shortCutmenuStyle);
                }
            }
        }

        Handles.EndGUI();
        #endregion
        Profiler.EndSample();

        Profiler.BeginSample("Click Events (WSM)");
        #region Click Events
        Event e = Event.current;

        if (!multiSelection && e.shift) //Holding Shift
        {
            //If cursor is on a valid position, draw preview curve
            Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            Vector3 hitPos;
            Vector3 upwardsDirection = _spline.transform.up;

            bool hitSomething = Physics.Raycast(worldRay, out hit);

            if (hitSomething)
            {
                hitPos = hit.point;
            }
            else // Project at imaginary horizontal plane if no hit
            {
                float imaginaryPlaneDistance;

                Vector3 lastPoint = _spline.GetPoint(1f);

                //imaginary horizontal plane (ZX)
                Plane imaginaryPlane = new Plane();
                upwardsDirection = ConfigureImaginaryPlane(ref imaginaryPlane, Vector3.up, new Vector3(0f, lastPoint.y, 0f));
                hitSomething = imaginaryPlane.Raycast(worldRay, out imaginaryPlaneDistance);

                if (!hitSomething)
                {
                    //imaginary vertical plane (ZY)
                    upwardsDirection = ConfigureImaginaryPlane(ref imaginaryPlane, Vector3.right, new Vector3(lastPoint.x, 0f, 0f));
                    hitSomething = imaginaryPlane.Raycast(worldRay, out imaginaryPlaneDistance);
                }
                //imaginary vertical plane (XY) NOT SUPPORTED

                hitPos = worldRay.GetPoint(imaginaryPlaneDistance);
            }

            if (hitSomething)
            {
                if (e.type == EventType.MouseDown && e.button == 1)
                    AddCurve(hitPos, upwardsDirection); // Shift + Right Click
                else
                    PreviewCurve(hitPos, upwardsDirection); // Holding Shift
            }
        }

        #endregion
        Profiler.EndSample();
    }

    private static Vector3 ConfigureImaginaryPlane(ref Plane imaginaryPlane, Vector3 inNormal, Vector3 inPoint)
    {
        imaginaryPlane.SetNormalAndPosition(inNormal, inPoint);
        return imaginaryPlane.normal;
    }

    /// <summary>
    /// Preview curve on Inspector
    /// </summary>
    /// <param name="point"></param>
    private void PreviewCurve(Vector3 point, Vector3 upwardsDirection)
    {
        _splineColor = Color.magenta; //_spline.Theme != null ? _spline.Theme.splineColor : Color.white;
        _spline.PreviewQuadraticBezierCurve(point, out _previewStartPoint, out _previewHandle1, out _previewHandle2, out _previewEndPoint, upwardsDirection);
        Handles.DrawBezier(_previewStartPoint, _previewEndPoint, _previewHandle1, _previewHandle2, _splineColor, null, 2f);
        Repaint(); //Force editor to repaint curve preview
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
    /// 
    /// </summary>
    /// <param name="lineColor"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineEnd"></param>
    private void DrawLine(Color lineColor, Vector3 lineStart, Vector3 lineEnd)
    {
        Profiler.BeginSample("DrawLine Method (WSM)");

        Handles.color = lineColor;
        Handles.DrawLine(lineStart, lineEnd);

        Profiler.EndSample();
    }

    /// <summary>
    /// Show spline points forward direction on Editor
    /// </summary>
    private void ShowDirections()
    {
        Profiler.BeginSample("ShowDirections Method (WSM)");

        Vector3 lineStart = _spline.GetPoint(0f);
        Vector3 lineEnd = lineStart + _spline.GetDirection(0f) * SplineDefaultValues.DirectionScale;
        Handles.DrawLine(lineStart, lineEnd);
        int steps = SplineDefaultValues.StepsPerCurve * _spline.CurveCount;
        for (int i = 1; i <= steps; i++)
        {
            lineStart = _spline.GetPoint(i / (float)steps);
            lineEnd = lineStart + _spline.GetDirection(i / (float)steps) * SplineDefaultValues.DirectionScale;
            DrawLine(Color.blue, lineStart, lineEnd);
        }

        Profiler.EndSample();
    }

    /// <summary>
    /// Show spline point handles on Editor
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private Vector3 ShowControlPoint(int index, out bool visible)
    {
        Profiler.BeginSample("ShowControlPoint Method (WSM)");

        bool isBezierHandle = false;
        bool isSelected = false;
        visible = CheckControlPointVisibility(index, out isSelected, out isBezierHandle);

        Vector3 pointPosition = _splineTransform.TransformPoint(_spline.GetControlPointPosition(index));
        Vector3 pointNormal = _spline.GetControlPointNormal(index);
        Vector3 pointDirection = _spline.GetDirection(index); //World space
        Quaternion pointRotation = _spline.GetControlPointRotation(index);

        if (visible)
        {
            float size = HandleUtility.GetHandleSize(pointPosition);
            if (index == 0)
                size *= 5f;
            else
                size *= 3f;

            if (isBezierHandle) //Handles uses selected mode colors
                Handles.color = _alignmentColors[(int)_spline.GetHandlesAlignment(index)];
            else
                Handles.color = (isSelected || _multiSelectedPoints.Contains(index)) ? Color.yellow : Color.green; //Yellow if selected, green otherwise

            //Button used to select handle/controlPoint
            if (Handles.Button(pointPosition, _handleRotation, (size * _handleSize), (size * _pickSize), Handles.SphereHandleCap))
            {
                //First point position and rotation cannot be edited
                if (Event.current.control && !isBezierHandle && _selectedIndex > 0)
                {
                    if (!_multiSelectedPoints.Contains(index))
                    {
                        _selectedIndex = index;
                        _multiSelectedPoints.Add(_selectedIndex);
                    }
                    else
                        _multiSelectedPoints.Remove(index);
                }
                else
                {
                    _selectedIndex = index;
                    _multiSelectedPoints.Clear();
                    if (!isBezierHandle && _selectedIndex > 0) _multiSelectedPoints.Add(_selectedIndex);
                }

                Repaint();

                if (_spline.ZoomOnClick)
                    SceneViewZoom(pointPosition, _spline.ZoomLevel);
            }

            //Is selected handle/controlPoint
            if (isSelected && index > 0) //First control point cannot be moved or rotated. It must be at origin for spline consistency
            {
                if (Tools.current == Tool.Rotate && !IsHandle) //Rotation handle
                {
                    EditorGUI.BeginChangeCheck();
                    pointRotation = Handles.RotationHandle(pointRotation, pointPosition);
                    if (EditorGUI.EndChangeCheck())
                    {
                        pointNormal = pointRotation * Vector3.up;
                        Undo.RecordObject(_spline, "Rotate Point");
                        if (_multiSelectedPoints.Count > 1) // Multiple points rotation
                        {
                            foreach (int pointIndex in _multiSelectedPoints)
                            {
                                _spline.SetControlPointNormal(pointIndex, pointNormal);
                                _spline.SetControlPointRotation(pointIndex, pointRotation);
                            }
                        }
                        else // Single point rotation
                        {
                            _spline.SetControlPointNormal(index, pointNormal);
                            _spline.SetControlPointRotation(index, pointRotation);
                        }
                        MarkSceneAlteration(_spline);
                    }
                }
                else //Position handle
                {
                    EditorGUI.BeginChangeCheck();
                    pointPosition = Handles.PositionHandle(pointPosition, _handleRotation);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Vector3 oldLocalPos = _spline.GetControlPointPosition(index);
                        Vector3 newLocalPos = _splineTransform.InverseTransformPoint(pointPosition);
                        Vector3 movementVector = newLocalPos - oldLocalPos;
                        Vector3 auxPos;

                        Undo.RecordObject(_spline, "Move Point");
                        _spline.SetControlPointPosition(index, newLocalPos);

                        foreach (int pointIndex in _multiSelectedPoints)
                        {
                            if (pointIndex == index || index == 0) continue; //ignore selected point (already moved) and first point (origin)
                            auxPos = _spline.GetControlPointPosition(pointIndex) + movementVector;
                            _spline.SetControlPointPosition(pointIndex, auxPos);
                        }

                        MarkSceneAlteration(_spline);
                    }
                }
            }
        }

        Profiler.EndSample();
        return pointPosition;
    }

    /// <summary>
    /// Check if controlpoint or handle should be visible
    /// </summary>
    /// <param name="index"></param>
    /// <param name="isSelected"></param>
    /// <param name="isBezierHandle"></param>
    /// <returns></returns>
    private bool CheckControlPointVisibility(int index, out bool isSelected, out bool isBezierHandle)
    {
        isSelected = (_selectedIndex == index);
        isBezierHandle = !(index == 0 || ((index) % 3 == 0));

        if (isBezierHandle && _spline.GetHandlesAlignment(index) == BezierHandlesAlignment.Automatic)
            return false;

        if (_spline.HandlesVisibility == HandlesVisibility.ShowAllHandles)
            return true;

        if (_spline.HandlesVisibility == HandlesVisibility.ShowOnlyActiveHandles)
        {
            if (isBezierHandle)
            {
                bool selectedIsBezierHandle = !(_selectedIndex == 0 || ((_selectedIndex) % 3 == 0));
                bool isBeforeSelected = (index == (_selectedIndex - 1));
                bool isAfterSelected = (index == (_selectedIndex + 1));

                if (isSelected)
                    return true;
                else if (!selectedIsBezierHandle && (isBeforeSelected || isAfterSelected)) //Current is before or after selected
                    return true;
                else if (selectedIsBezierHandle && (_selectedIndex == index - 2 || _selectedIndex == index + 2))
                    return true;
            }
            else //Control points are always visible, only handles can be invisible
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Draw inspector elements and UI
    /// </summary>
    public override void OnInspectorGUI()
    {
        //base.DrawDefaultInspector();
        _spline = target as Spline;

        //Set up the box style if null
        if (_menuBoxStyle == null)
        {
            _menuBoxStyle = new GUIStyle(GUI.skin.box);
            _menuBoxStyle.normal.textColor = GUI.skin.label.normal.textColor;
            _menuBoxStyle.fontStyle = FontStyle.Bold;
            _menuBoxStyle.alignment = TextAnchor.UpperLeft;
        }

        GUILayout.BeginVertical(_menuBoxStyle);

        EditorGUI.BeginChangeCheck();
        _selectedMenuIndex = GUILayout.Toolbar(_selectedMenuIndex, _toolbarMenuOptions);
        if (EditorGUI.EndChangeCheck())
        {
            GUI.FocusControl(null);
        }

        if (_selectedMenuIndex == (int)SplineInspectorMenu.Curve)
        {
            Profiler.BeginSample("Curve Settings Inspector (WSM)");
            #region Curve Settings

            /*
             * Curve Settings
             */
            GUILayout.Label("CURVE SETTINGS", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool loop = EditorGUILayout.Toggle("Close Loop", _spline.Loop);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Toggle Loop");
                _spline.Loop = loop;
                MarkSceneAlteration(_spline);
            }

            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("New Curve Length");
            int newCurveLength = EditorGUILayout.IntField((int)_spline.NewCurveLength, GUILayout.MaxWidth(100));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Changed Curve Length");
                _spline.NewCurveLength = newCurveLength;
                MarkSceneAlteration(_spline);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("New Curve Angle");
            float newCurveAngle = EditorGUILayout.Slider(_spline.NewCurveAngle, 0f, 90f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Changed Curve Angle");
                _spline.NewCurveAngle = newCurveAngle;
                MarkSceneAlteration(_spline);
            }
            GUILayout.EndHorizontal();

            /*
             * Curve Operations
             */
            GUILayout.Label("CURVE OPERATIONS", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            if (GUILayout.Button(_btnAddNewCurve))
            {
                AddCurve();
            }

            if (GUILayout.Button(_btnTurnLeft))
            {
                TurnCurve_Left();
            }

            if (GUILayout.Button(_btnTurnUpwards))
            {
                TurnCurve_Upwards();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button(_btnRemoveCurve))
            {
                RemoveCurve();
            }

            if (GUILayout.Button(_btnTurnRight))
            {
                TurnCurve_Right();
            }

            if (GUILayout.Button(_btnTurnDownwards))
            {
                TurnCurve_Downwards();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_btnResetCurve))
            {
                ResetCurve();
            }

            GUILayout.EndHorizontal();

            //Ignore first/last controlpoints and bezier handles

            GUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(NoPointSelected || IsLastPoint || IsHandle))
            {
                if (GUILayout.Button(_btnSubdivideCurve))
                {
                    Undo.RecordObject(_spline, "Subdivide Curve");
                    _spline.SubdivideCurve(_selectedIndex);
                    MarkSceneAlteration(_spline);
                }
            }

            using (new EditorGUI.DisabledScope(NoPointSelected || IsFirstPoint || IsLastPoint || IsHandle))
            {
                if (GUILayout.Button(_btnDissolveCurve))
                {
                    Undo.RecordObject(_spline, "Dissolve Curve");
                    _spline.DissolveCurve(_selectedIndex);
                    MarkSceneAlteration(_spline);
                }
                GUILayout.EndHorizontal();
            }
            #endregion
            Profiler.EndSample();
        }
        else if (_selectedMenuIndex == (int)SplineInspectorMenu.Spline)
        {
            Profiler.BeginSample("Spline Settings Inspector (WSM)");
            #region Spline Settings

            /*
             * Spline Settings
             */
            GUILayout.Label("SPLINE SETTINGS", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool staticSpline = EditorGUILayout.Toggle("Static", _spline.StaticSpline);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Static Spline Property");
                _spline.StaticSpline = staticSpline;
                MarkSceneAlteration(_spline);
            }

            EditorGUI.BeginChangeCheck();
            Vector3Axis flattenAxis = (Vector3Axis)EditorGUILayout.EnumPopup("Flatten Axis", _spline.FlattenAxis);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Flatten Axis Changed");
                _spline.FlattenAxis = flattenAxis;
                MarkSceneAlteration(_spline);
            }

            EditorGUI.BeginChangeCheck();
            Vector2 parallelSplineDirection = EditorGUILayout.Vector2Field("Parallel Spline Direction", _spline.ParallelSplineDirection);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Parallel Spline Direction Changed");
                _spline.ParallelSplineDirection = parallelSplineDirection;
                MarkSceneAlteration(_spline);
            }

            /*
             * Spline Operations
             */
            GUILayout.Label("SPLINE OPERATIONS", EditorStyles.boldLabel);
            
            _operationTarget = EditorGUILayout.ObjectField("Operation Target", _operationTarget, typeof(GameObject), true);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            using (new EditorGUI.DisabledScope(NoPointSelected || IsFirstPoint || IsLastPoint || IsHandle))
            {
                if (GUILayout.Button(_btnSplitSpline))
                {
                    Undo.RecordObject(_spline, "Split Spline");
                    _spline.SplitSpline(_selectedIndex);
                    MarkSceneAlteration(_spline);
                }
            }

            using (new EditorGUI.DisabledScope(_operationTarget == null))
            {
                if (GUILayout.Button(_btnConnectTarget))
                {
                    Undo.RecordObject(_spline, "Connect Target");
                    _spline.ConnectTarget((GameObject)_operationTarget);
                    MarkSceneAlteration(_spline);
                    MarkSceneAlteration(_operationTarget);
                }
            }

            if (GUILayout.Button(_btnAppendSpline))
            {
                Undo.RecordObject(_spline, "Append");
                Selection.activeGameObject = _spline.AppendSpline();
                MarkSceneAlteration(_spline);
            }

            if (GUILayout.Button(_btnResetSpline))
            {
                bool dialogResult = EditorUtility.DisplayDialog("Spline Reset Confirmation", "Are you sure you want to reset this spline?", "Reset", "Cancel");

                if (dialogResult)
                {
                    Undo.RecordObject(_spline, "Reset");
                    _spline.Reset();
                    MarkSceneAlteration(_spline); 
                }
            }

            if (GUILayout.Button(_btnCreateParallelSpline))
            {
                Undo.RecordObject(_spline, "Create Parallel Spline");
                _spline.CreateParallelSpline();
                MarkSceneAlteration(_spline);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            using (new EditorGUI.DisabledScope(_operationTarget == null))
            {
                if (GUILayout.Button(_btnMergeSplines))
                {
                    int dialogResult = EditorUtility.DisplayDialogComplex("Merging Confirmation", string.Format("What do you want to do with the merging target?{0}{0}WARNING: Target deletion cannot be undone", System.Environment.NewLine), "Delete", "Disable", "Cancel");

                    bool mergingCanceled = (dialogResult == 2);
                    if (!mergingCanceled)
                    {
                        bool deleteTarget = (dialogResult == 0);

                        Undo.RecordObject(_spline, "Merge Spline");
                        _spline.Merge((GameObject)_operationTarget, deleteTarget);
                        _operationTarget = null;
                        MarkSceneAlteration(_spline);
                    }
                }

                if (GUILayout.Button(_btnBridgeGap))
                {
                    Undo.RecordObject(_spline, "Bridge Gap");
                    _spline.BridgeGap((GameObject)_operationTarget);
                    MarkSceneAlteration(_spline);
                }
            }

            if (GUILayout.Button(_btnFlatten))
            {
                Undo.RecordObject(_spline, "Flatten");
                _spline.Flatten();
                MarkSceneAlteration(_spline);
            }

            if (GUILayout.Button(_btnResetNormals))
            {
                bool dialogResult = EditorUtility.DisplayDialog("Reset Normals Confirmation", "Are you sure you want to reset all control points normals?", "Reset", "Cancel");

                if (dialogResult)
                {
                    Undo.RecordObject(_spline, "Reset Normals");
                    _spline.ResetNormals();
                    MarkSceneAlteration(_spline); 
                }
            }

            if (GUILayout.Button(_btnRevertSpline))
            {
                Undo.RecordObject(_spline, "Revert");
                _spline.RevertSpline();
                MarkSceneAlteration(_spline);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            #endregion
            Profiler.EndSample();
        }
        else if (_selectedMenuIndex == (int)SplineInspectorMenu.Handles)
        {
            Profiler.BeginSample("Handle Settings Inspector (WSM)");
            #region Handles Settings
            /*
            * Handles Settings
            */
            GUILayout.Label("HANDLES SETTINGS", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            HandlesVisibility selectedHandlesVisibility = (HandlesVisibility)EditorGUILayout.EnumPopup("Handles Visibility", _spline.HandlesVisibility);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Handle Visibility Changed");
                _spline.HandlesVisibility = selectedHandlesVisibility;
                MarkSceneAlteration(_spline);
            }

            EditorGUI.BeginChangeCheck();
            float autoHandleSpacing = EditorGUILayout.Slider("Automatic Handles Spacing", _spline.AutoHandleSpacing, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Automatic Handles Spacing");
                _spline.AutoHandleSpacing = autoHandleSpacing;
                MarkSceneAlteration(_spline);
            }

            /*
             * Selected Handle
             */
            if (_selectedIndex >= 0 && _selectedIndex < _spline.ControlPointCount)
            {
                if (_spline.HandlesVisibility != HandlesVisibility.DebugOrientedPoints)
                    DrawSelectedPointInspector();
                else
                    GUILayout.Label(string.Format("Debuging Mode{0}Handle editing not allowed", System.Environment.NewLine), EditorStyles.boldLabel);
            }
            else
            {
                if (_spline.HandlesVisibility != HandlesVisibility.DebugOrientedPoints)
                    GUILayout.Label(string.Format("No Handle Selected{0}Click on a handle to select it", System.Environment.NewLine), EditorStyles.boldLabel);
                else
                    GUILayout.Label(string.Format("Debuging Mode{0}Handle editing not allowed", System.Environment.NewLine), EditorStyles.boldLabel);
            }

            if (_spline.HandlesVisibility == HandlesVisibility.DebugOrientedPoints)
            {
                if (GUILayout.Button(_btnDeleteOrientedPoints))
                    DeleteOrientedPoints();
            }
            #endregion

            Profiler.EndSample();
        }
        else if (_selectedMenuIndex == (int)SplineInspectorMenu.Terrain)
        {
            Profiler.BeginSample("Terrain Settings Inspector (WSM)");
            #region TERRAIN SETTINGS
            /*
             * Terrain Settings
             */
            GUILayout.Label("TERRAIN SETTINGS", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool followTerrain = EditorGUILayout.Toggle("Follow Terrain", _spline.FollowTerrain);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Follow Terrain Property");
                _spline.FollowTerrain = followTerrain;
                MarkSceneAlteration(_spline);
            }

            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Terrain Check Distance");
            float terrainCheckDistance = EditorGUILayout.FloatField(_spline.TerrainCheckDistance, GUILayout.MaxWidth(100));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Terrain Check Distance");
                _spline.TerrainCheckDistance = Mathf.Abs(terrainCheckDistance);
                MarkSceneAlteration(_spline);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("TERRAFORMING SETTINGS", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Terraforming Width");
            int terraformingWidth = EditorGUILayout.IntField(_spline.TerraformingWidth, GUILayout.MaxWidth(100));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Terraforming Width");
                _spline.TerraformingWidth = terraformingWidth;
                MarkSceneAlteration(_spline);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Embankment Width");
            int terraformingEmbankment = EditorGUILayout.IntField(_spline.EmbankmentWidth, GUILayout.MaxWidth(100));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Terraforming Embankment");
                _spline.EmbankmentWidth = terraformingEmbankment;
                MarkSceneAlteration(_spline);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Embankment Slope");
            AnimationCurve embankmentSlope = EditorGUILayout.CurveField(_spline.EmbankmentSlope);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Embankment Slope");
                _spline.EmbankmentSlope = embankmentSlope;
                MarkSceneAlteration(_spline);
            }
            GUILayout.EndHorizontal();

            // GUILayout.BeginHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Terraforming Texture");
            Texture2D terraformingTexture = (Texture2D)EditorGUILayout.ObjectField(_spline.TerraformingTexture, typeof(Texture2D), false, GUILayout.Width(70), GUILayout.Height(70));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Terraforming Texture");
                _spline.TerraformingTexture = terraformingTexture;
                MarkSceneAlteration(_spline);
            }
            GUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            float minTextureBlending = _spline.MinTextureBlending;
            float maxTextureBlending = _spline.MaxTextureBlending;
            EditorGUILayout.MinMaxSlider("Texture Blending", ref minTextureBlending, ref maxTextureBlending, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Texture Blending");
                _spline.MinTextureBlending = minTextureBlending;
                _spline.MaxTextureBlending = maxTextureBlending;
                MarkSceneAlteration(_spline);
            }

            /*
             * Terrain Operations
             */
            GUILayout.Label("TERRAIN OPERATIONS", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            using (new EditorGUI.DisabledScope(_spline.FollowTerrain))
            {
                if (GUILayout.Button(_btnTerraform))
                {
                    bool dialogResult = EditorUtility.DisplayDialog("Terraforming Confirmation", string.Format("WARNING: Terraforming is a destructive operation. Creating a terrain backup before terraforming is highly recommended{0}{0}Are you sure you want to proceed?", System.Environment.NewLine), "Proceed", "Cancel");

                    if (dialogResult)
                    {
                        Undo.RecordObject(_spline, "Adjust Terrain to Spline");
                        _spline.Terraform();
                        MarkSceneAlteration(_spline);
                    }
                }
            }

            if (GUILayout.Button(_btnBackupTerrains))
            {
                bool dialogResult = EditorUtility.DisplayDialog("Backup Confirmation", string.Format("Are you sure you want to update your terrain backups?{0}{0}WARNING: Any previous backup data will be lost", System.Environment.NewLine), "Proceed", "Cancel");

                if (dialogResult)
                {
                    Undo.RecordObject(_spline, "Backup Terrains");
                    _spline.BackupTerrains();
                    MarkSceneAlteration(_spline);
                }
            }

            using (new EditorGUI.DisabledScope(_multiSelectedPoints == null || _multiSelectedPoints.Count == 0))
            {
                if (GUILayout.Button(_btnSelectedNodesToTerrain))
                {
                    Undo.RecordObject(_spline, "Project Nodes On Surface");

                    for (int i = 0; i < _multiSelectedPoints.Count; i++)
                    {
                        _spline.ProjectNodeOnSurface(_multiSelectedPoints[i]);
                    }

                    MarkSceneAlteration(_spline);
                } 
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            using (new EditorGUI.DisabledScope(_spline.TerraformingTexture == null))
            {
                if (GUILayout.Button(_btnPaintTerrain))
                {
                    bool dialogResult = EditorUtility.DisplayDialog("Paint Terrain Confirmation", string.Format("Creating a terrain backup before painting is highly recommended{0}{0}Are you sure you want to proceed?", System.Environment.NewLine), "Proceed", "Cancel");

                    if (dialogResult)
                    {
                        Undo.RecordObject(_spline, "Paint Terrain along Spline");
                        _spline.Terraform(false, true);
                        MarkSceneAlteration(_spline);
                    }
                }
            }

            if (GUILayout.Button(_btnRestoreTerrains))
            {
                bool dialogResult = EditorUtility.DisplayDialog("Backup Restoration", string.Format("Are you sure you want to restore your terrain backups?{0}{0}WARNING: Current terrain data will be lost", System.Environment.NewLine), "Proceed", "Cancel");

                if (dialogResult)
                {
                    Undo.RecordObject(_spline, "Restore Terrains");
                    _spline.RestoreTerrains();
                    MarkSceneAlteration(_spline);
                }
            }

            if (GUILayout.Button(_btnNodesToTerrain))
            {
                Undo.RecordObject(_spline, "Project Nodes On Surface");
                _spline.ProjectNodesOnSurface();
                MarkSceneAlteration(_spline);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            #endregion
            Profiler.EndSample();
        }
        else if (_selectedMenuIndex == (int)SplineInspectorMenu.SceneView)
        {
            Profiler.BeginSample("Scene View Settings Inspector (WSM)");
            #region SCENE VIEW SETTINGS

            GUILayout.Label("UI THEME", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            SMR_Theme theme = (SMR_Theme)EditorGUILayout.ObjectField("Theme", _spline.Theme, typeof(SMR_Theme), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Changed Theme");
                _spline.Theme = theme;
                MarkSceneAlteration(_spline);
            }

            if (_spline.Theme != null)
            {
                #region EDITOR FEATURES
                GUILayout.Label("EDITOR FEATURES", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                bool zoomEnabled = EditorGUILayout.Toggle("Enable Zoom on Click", _spline.ZoomOnClick);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Zoom On Click Changed");
                    _spline.ZoomOnClick = zoomEnabled;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int zoomLevel = EditorGUILayout.IntField("Zoom Level", _spline.ZoomLevel);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Zoom Level Changed");
                    _spline.ZoomLevel = zoomLevel;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                bool supportsVerticalBuilding = EditorGUILayout.Toggle("Supports Vertical Building", _spline.SupportsVerticalBuilding);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Supports Vertical Building Changed");
                    _spline.SupportsVerticalBuilding = supportsVerticalBuilding;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                Color splineColor = EditorGUILayout.ColorField("Spline Color", _spline.SplineColor);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Spline Color Changed");
                    _spline.SplineColor = splineColor;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                bool showShortcutMenu = EditorGUILayout.Toggle("Show Shortcut Menu", _spline.ShowShortcutMenu);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Show Shortcut Menu Changed");
                    _spline.ShowShortcutMenu = showShortcutMenu;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                #endregion

                #region EDITOR UI
                GUILayout.Label("EDITOR UI", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                int btnSize = EditorGUILayout.IntField("Button Size", _spline.Theme.buttonSize);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button Size Changed");
                    _spline.Theme.buttonSize = btnSize;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical();
                EditorGUI.BeginChangeCheck();
                int inputBoxPosX = EditorGUILayout.IntField("Input Box Pos X", _spline.Theme.inputBoxPosX);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Input Box Pos X Changed");
                    _spline.Theme.inputBoxPosX = inputBoxPosX;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn1PosX = EditorGUILayout.IntField("Button 1 Pos X", _spline.Theme.button1PosX);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 1 Pos X Changed");
                    _spline.Theme.button1PosX = btn1PosX;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn2PosX = EditorGUILayout.IntField("Button 2 Pos X", _spline.Theme.button2PosX);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 2 Pos X Changed");
                    _spline.Theme.button2PosX = btn2PosX;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn3PosX = EditorGUILayout.IntField("Button 3 Pos X", _spline.Theme.button3PosX);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 3 Pos X Changed");
                    _spline.Theme.button3PosX = btn3PosX;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn4PosX = EditorGUILayout.IntField("Button 4 Pos X", _spline.Theme.button4PosX);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 4 Pos X Changed");
                    _spline.Theme.button4PosX = btn4PosX;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn5PosX = EditorGUILayout.IntField("Button 5 Pos X", _spline.Theme.button5PosX);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 5 Pos X Changed");
                    _spline.Theme.button5PosX = btn5PosX;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn6PosX = EditorGUILayout.IntField("Button 6 Pos X", _spline.Theme.button6PosX);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 6 Pos X Changed");
                    _spline.Theme.button6PosX = btn6PosX;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn7PosX = EditorGUILayout.IntField("Button 7 Pos X", _spline.Theme.button7PosX);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 7 Pos X Changed");
                    _spline.Theme.button7PosX = btn7PosX;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn8PosX = EditorGUILayout.IntField("Button 8 Pos X", _spline.Theme.button8PosX);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 8 Pos X Changed");
                    _spline.Theme.button8PosX = btn8PosX;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                EditorGUI.BeginChangeCheck();
                int inputBoxPosY = EditorGUILayout.IntField("Input Box Pos Y", _spline.Theme.inputBoxPosY);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Input Box Pos Y Changed");
                    _spline.Theme.inputBoxPosY = inputBoxPosY;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn1PosY = EditorGUILayout.IntField("Button 1 Pos Y", _spline.Theme.button1PosY);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 1 Pos Y Changed");
                    _spline.Theme.button1PosY = btn1PosY;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn2PosY = EditorGUILayout.IntField("Button 2 Pos Y", _spline.Theme.button2PosY);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 2 Pos Y Changed");
                    _spline.Theme.button2PosY = btn2PosY;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn3PosY = EditorGUILayout.IntField("Button 3 Pos Y", _spline.Theme.button3PosY);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 3 Pos Y Changed");
                    _spline.Theme.button3PosY = btn3PosY;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn4PosY = EditorGUILayout.IntField("Button 4 Pos Y", _spline.Theme.button4PosY);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 4 Pos Y Changed");
                    _spline.Theme.button4PosY = btn4PosY;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn5PosY = EditorGUILayout.IntField("Button 5 Pos Y", _spline.Theme.button5PosY);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 5 Pos Y Changed");
                    _spline.Theme.button5PosY = btn5PosY;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn6PosY = EditorGUILayout.IntField("Button 6 Pos Y", _spline.Theme.button6PosY);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 6 Pos Y Changed");
                    _spline.Theme.button6PosY = btn6PosY;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn7PosY = EditorGUILayout.IntField("Button 7 Pos Y", _spline.Theme.button7PosY);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 7 Pos Y Changed");
                    _spline.Theme.button7PosY = btn7PosY;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }

                EditorGUI.BeginChangeCheck();
                int btn8PosY = EditorGUILayout.IntField("Button 8 Pos Y", _spline.Theme.button8PosY);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Button 8 Pos Y Changed");
                    _spline.Theme.button8PosY = btn8PosY;
                    MarkSceneAlteration(_spline);
                    MarkAssetAlteration(_spline.Theme);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                #endregion
            }

            GUIStyle style = new GUIStyle();
            style.fontStyle = FontStyle.BoldAndItalic;
            style.normal.textColor = Color.blue;
            GUILayout.Label("Obs: Customized settings will be persisted on selected theme", style);
            #endregion
            Profiler.EndSample();
        }

        GUILayout.EndVertical();
    }

    #region CURVE OPERATIONS

    /// <summary>
    /// Add new curve
    /// </summary>
    private void AddCurve()
    {
        Undo.RecordObject(_spline, "Add Curve");
        _spline.AddCurve();
        MarkSceneAlteration(_spline);
    }

    /// <summary>
    /// Add new curve based on target point
    /// </summary>
    /// <param name="point"></param>
    private void AddCurve(Vector3 point, Vector3 upwardsDirection)
    {
        Undo.RecordObject(_spline, "Add Curve");
        _spline.AddCurve(point, upwardsDirection);
        MarkSceneAlteration(_spline);
    }

    /// <summary>
    /// Remove last curve
    /// </summary>
    private void RemoveCurve()
    {
        Undo.RecordObject(_spline, "Remove Curve");
        _spline.RemoveCurve();
        MarkSceneAlteration(_spline);
    }

    /// <summary>
    /// Reset last curve
    /// </summary>
    private void ResetCurve()
    {
        Undo.RecordObject(_spline, "Reset Curve");
        _spline.ResetLastCurve();
        MarkSceneAlteration(_spline);
    }

    /// <summary>
    /// Turn curve 45º right
    /// </summary>
    private void TurnCurve_Right()
    {
        Undo.RecordObject(_spline, "Turn Curve Right");
        _spline.ShapeCurve(_splineTransform.right);
        MarkSceneAlteration(_spline);
    }

    /// <summary>
    /// Turn curve 45º left
    /// </summary>
    private void TurnCurve_Left()
    {
        Undo.RecordObject(_spline, "Turn Curve Left");
        _spline.ShapeCurve(-_splineTransform.right);
        MarkSceneAlteration(_spline);
    }

    /// <summary>
    /// Turn curve 45º upward
    /// </summary>
    private void TurnCurve_Upwards()
    {
        Undo.RecordObject(_spline, "Turn Curve Upwards");
        _spline.ShapeCurve(_splineTransform.up);
        MarkSceneAlteration(_spline);
    }

    /// <summary>
    /// Turn curve 45º downwards
    /// </summary>
    private void TurnCurve_Downwards()
    {
        Undo.RecordObject(_spline, "Turn Curve Downwards");
        _spline.ShapeCurve(-_splineTransform.up);
        MarkSceneAlteration(_spline);
    }

    #endregion

    private void DeleteOrientedPoints()
    {
        Undo.RecordObject(_spline, "Delete Oriented Points");
        _spline.DeleteOrientedPoints();
        MarkSceneAlteration(_spline);
    }

    /// <summary>
    /// Draw inpector elements form selected spline point
    /// </summary>
    private void DrawSelectedPointInspector()
    {
        GUILayout.Label("SELECTED HANDLE", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(IsFirstPoint))
        {
            EditorGUI.BeginChangeCheck();
            Vector3 point = EditorGUILayout.Vector3Field("Position", _spline.GetControlPointPosition(_selectedIndex));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Move Point");
                _spline.SetControlPointPosition(_selectedIndex, point);
                MarkSceneAlteration(_spline);
            }
        }

        using (new EditorGUI.DisabledScope(IsHandle))
        {
            EditorGUI.BeginChangeCheck();
            Vector3 normal = EditorGUILayout.Vector3Field("Normal", _spline.GetControlPointNormal(_selectedIndex));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Changed Point Normal");
                _spline.SetControlPointNormal(_selectedIndex, normal);
                MarkSceneAlteration(_spline);
            }
        }

        // Starting from v2.1 point rotation is read only. Point rotation is now calculated automatically based on point normal
        //using (new EditorGUI.DisabledScope(true))
        //{
        //    EditorGUI.BeginChangeCheck();
        //    Vector3 rotation = EditorGUILayout.Vector3Field("Rotation", Convert.QuaternionToVector3(_spline.GetControlPointRotation(_selectedIndex)));
        //    if (EditorGUI.EndChangeCheck())
        //    {
        //        Undo.RecordObject(_spline, "Rotate Point");
        //        _spline.SetControlPointRotation(_selectedIndex, Convert.Vector3ToQuaternion(rotation));
        //        MarkSceneAlteration(_spline);
        //    }
        //}

        EditorGUI.BeginChangeCheck();
        BezierHandlesAlignment handleAlignment = (BezierHandlesAlignment)EditorGUILayout.EnumPopup("Handles Alignment", _spline.GetHandlesAlignment(_selectedIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_spline, "Change Handle Alignment");
            _spline.SetHandlesAlignment(_selectedIndex, handleAlignment, true);
            MarkSceneAlteration(_spline);
        }
    }

    /// <summary>
    /// Show player the scene needs to be saved
    /// </summary>
    private void MarkSceneAlteration(Object target)
    {
        Profiler.BeginSample("MarkSceneAlteration Method (WSM)");

        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        Profiler.EndSample();
    }

    /// <summary>
    /// Force saving assets loaded from disk, like scriptable objects for example
    /// </summary>
    /// <param name="target"></param>
    private void MarkAssetAlteration(Object target)
    {
        Profiler.BeginSample("MarkAssetAlteration Method (WSM)");

        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Profiler.EndSample();
    }
}