using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using WSMGameStudio.TerrainTools;

namespace WSMGameStudio.Splines
{
    [RequireComponent(typeof(LODGroup))] //Fix for Inspector performance issues on Unity 2018
    public class Spline : MonoBehaviour
    {
        [SerializeField] private float _newCurveLength = 15f;
        [Range(0f, 90f)] [SerializeField] private float _newCurveAngle = 90;

        [SerializeField] private SMR_Theme _theme;
        [SerializeField] private Vector3[] _controlPointsPositions; //Local positions
        [SerializeField] private Quaternion[] _controlPointsRotations;
        [SerializeField] private BezierHandlesAlignment[] _modes;
        [SerializeField] private Vector3[] _controlPointsNormals;

        [SerializeField] private bool _loop;
        [SerializeField] private bool _staticSpline = true;

        [SerializeField] private bool _followTerrain = false;
        [SerializeField] private float _terrainCheckDistance = 20f;
        [SerializeField] private int _terraformingWidth = 3;
        [SerializeField] private int _embankmentWidth = 3;
        [SerializeField] private AnimationCurve _embankmentSlope = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] Texture2D _terraformingTexture;
        [SerializeField] private float _minTextureBlending = 0.4f;
        [SerializeField] private float _maxTextureBlending = 0.6f;

        [SerializeField] [Range(0f, 1f)] private float _autoHandleSpacing = 0.33f;
        [SerializeField] private HandlesVisibility _handlesVisibility;
        [SerializeField] private Vector3Axis _flattenAxis = Vector3Axis.y;
        [SerializeField] private Vector2 _parallelSplineDirection = new Vector2(5f, 0f);

        private Transform _transform;
        private Vector3 _lastTransformPosition;
        private Quaternion _lastTransformRotation;
        private OrientedPoint[] _orientedPoints; //This CANNOT be serialized for performance sake, and it doesn't need to. (World positions)

        private float _terraformingStepsCount;
        private float _terraformingCurrentStep;

        #region PROPERTIES

        public Vector3[] ControlPointsPositions
        {
            get { return _controlPointsPositions; }
            set { _controlPointsPositions = value; }
        }

        public Quaternion[] ControlPointsRotations
        {
            get { return _controlPointsRotations; }
            set { _controlPointsRotations = value; }
        }

        public BezierHandlesAlignment[] Modes
        {
            get { return _modes; }
            set { _modes = value; }
        }

        public Vector3[] ControlPointsNormals
        {
            get
            {
                ValidateNormals();

                return _controlPointsNormals;
            }
            set { _controlPointsNormals = value; }
        }

        /// <summary>
        /// Merge the first and last control points creating a continuos loop
        /// </summary>
        public bool Loop
        {
            get { return _loop; }
            set
            {
                if (_loop != value)
                {
                    _loop = value;
                    EnforceLoop();
                }
            }
        }

        public float NewCurveLength
        {
            get { return _newCurveLength; }
            set
            {
                _newCurveLength = Mathf.Abs(value);
                _newCurveLength = _newCurveLength < 1 ? 1 : _newCurveLength;
            }
        }

        public float NewCurveAngle
        {
            get { return _newCurveAngle; }
            set
            {
                _newCurveAngle = (float)Math.Round(Mathf.Abs(value), 2, MidpointRounding.AwayFromZero);
            }
        }

        public bool StaticSpline
        {
            get { return _staticSpline; }
            set { _staticSpline = value; }
        }

        public bool FollowTerrain
        {
            get { return _followTerrain; }
            set { _followTerrain = value; }
        }

        public float TerrainCheckDistance
        {
            get { return _terrainCheckDistance; }
            set { _terrainCheckDistance = value; }
        }

        public int TerraformingWidth
        {
            get { return _terraformingWidth; }
            set { _terraformingWidth = value < 0 ? 0 : value; }
        }

        public int EmbankmentWidth
        {
            get { return _embankmentWidth; }
            set { _embankmentWidth = value < 0 ? 0 : value; }
        }

        public AnimationCurve EmbankmentSlope
        {
            get { return _embankmentSlope; }
            set { _embankmentSlope = value; }
        }

        public Texture2D TerraformingTexture
        {
            get { return _terraformingTexture; }
            set { _terraformingTexture = value; }
        }

        public float MinTextureBlending
        {
            get { return _minTextureBlending; }
            set { _minTextureBlending = Mathf.Clamp(value, 0f, 0.5f); }
        }

        public float MaxTextureBlending
        {
            get { return _maxTextureBlending; }
            set { _maxTextureBlending = Mathf.Clamp(value, 0.5f, 1f); }
        }

        public float AutoHandleSpacing
        {
            get { return _autoHandleSpacing; }
            set
            {
                if (value != _autoHandleSpacing)
                {
                    _autoHandleSpacing = value;
                    UpdateAllHandlesAligment();
                }
            }
        }

        public HandlesVisibility HandlesVisibility
        {
            get { return _handlesVisibility; }
            set { _handlesVisibility = value; }
        }

        public Vector3Axis FlattenAxis
        {
            get { return _flattenAxis; }
            set { _flattenAxis = value; }
        }

        public Vector2 ParallelSplineDirection
        {
            get { return _parallelSplineDirection; }
            set { _parallelSplineDirection = value; }
        }

        public bool ZoomOnClick
        {
            get
            {
                if (_theme != null)
                    return _theme.zoomOnClick;
                else
                    return true;
            }
            set
            {
                if (_theme != null)
                    _theme.zoomOnClick = value;
            }
        }

        public int ZoomLevel
        {
            get
            {
                if (_theme != null)
                    return _theme.zoomLevel;
                else
                    return 1;
            }
            set
            {
                if (_theme != null)
                    _theme.zoomLevel = Math.Abs(value) < 1 ? 1 : Math.Abs(value);
            }
        }

        public Color SplineColor
        {
            get
            {
                if (_theme != null)
                    return _theme.splineColor;
                else
                    return Color.white;
            }
            set
            {
                if (_theme != null)
                    _theme.splineColor = value;
            }
        }

        public bool ShowShortcutMenu
        {
            get
            {
                if (_theme != null)
                    return _theme.showShortcutMenu;
                else
                    return true;
            }
            set
            {
                if (_theme != null)
                    _theme.showShortcutMenu = value;
            }
        }

        public bool SupportsVerticalBuilding
        {
            get
            {
                if (_theme != null)
                    return _theme.supportsVerticalBuilding;
                else
                    return true;
            }
            set
            {
                if (_theme != null)
                    _theme.supportsVerticalBuilding = value;
            }
        }

        public int ControlPointCount
        {
            get { return _controlPointsPositions == null ? 0 : _controlPointsPositions.Length; }
        }

        public int CurveCount
        {
            get { return (_controlPointsPositions == null ? 0 : _controlPointsPositions.Length - 1) / 3; }
        }

        public float Length
        {
            get { return GetTotalDistance(); }
        }

        public OrientedPoint[] OrientedPoints
        {
            get { return _orientedPoints; }
            set { _orientedPoints = value; }
        }

        public SMR_Theme Theme
        {
            get { return _theme; }
            set { _theme = value; }
        }

        private float TerraformingProgress
        {
            get
            {
                return _terraformingStepsCount > 0f ? (_terraformingCurrentStep / _terraformingStepsCount) : 0f;
            }
        }

        #endregion

        private void OnEnable()
        {
            _transform = GetComponent<Transform>();

            _lastTransformPosition = _transform.position;
            _lastTransformRotation = _transform.rotation;

            ValidateNormals();
        }

        /// <summary>
        /// Get control point by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Vector3 GetControlPointPosition(int index)
        {
            if (_controlPointsPositions == null)
                Reset();

            return _controlPointsPositions[index];
        }

        /// <summary>
        /// Get rotation by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Quaternion GetControlPointRotation(int index)
        {
            return _controlPointsRotations[index];
        }

        /// <summary>
        /// Set control point rotation
        /// </summary>
        /// <param name="index"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public void SetControlPointRotation(int index, Quaternion rotation)
        {
            if (index % 3 == 0)
            {
                Quaternion deltaRotation = rotation * Quaternion.Inverse(_controlPointsRotations[index]);
                if (_loop)
                {
                    if (index == 0)
                    {
                        _controlPointsRotations[1] *= deltaRotation;
                        _controlPointsRotations[_controlPointsRotations.Length - 2] *= deltaRotation;
                        _controlPointsRotations[_controlPointsRotations.Length - 1] = rotation;
                    }
                    else if (index == _controlPointsPositions.Length - 1)
                    {
                        _controlPointsRotations[0] = rotation;
                        _controlPointsRotations[1] *= deltaRotation;
                        _controlPointsRotations[index - 1] *= deltaRotation;
                    }
                    else
                    {
                        _controlPointsRotations[index - 1] *= deltaRotation;
                        _controlPointsRotations[index + 1] *= deltaRotation;
                    }
                }
                else
                {
                    if (index > 0)
                    {
                        _controlPointsRotations[index - 1] *= deltaRotation;
                    }
                    if (index + 1 < _controlPointsRotations.Length)
                    {
                        _controlPointsRotations[index + 1] *= deltaRotation;
                    }
                }
            }

            _controlPointsRotations[index] = rotation;
        }

        /// <summary>
        /// Set control point by index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="point"></param>
        public void SetControlPointPosition(int index, Vector3 point)
        {
            if (index % 3 == 0)
            {
                Vector3 deltaPosition = point - _controlPointsPositions[index];
                if (_loop)
                {
                    if (index == 0)
                    {
                        _controlPointsPositions[1] += deltaPosition;
                        _controlPointsPositions[_controlPointsPositions.Length - 2] += deltaPosition;
                        _controlPointsPositions[_controlPointsPositions.Length - 1] = point;
                    }
                    else if (index == _controlPointsPositions.Length - 1)
                    {
                        _controlPointsPositions[0] = point;
                        _controlPointsPositions[1] += deltaPosition;
                        _controlPointsPositions[index - 1] += deltaPosition;
                    }
                    else
                    {
                        _controlPointsPositions[index - 1] += deltaPosition;
                        _controlPointsPositions[index + 1] += deltaPosition;
                    }
                }
                else
                {
                    if (index > 0)
                    {
                        _controlPointsPositions[index - 1] += deltaPosition;
                    }
                    if (index + 1 < _controlPointsPositions.Length)
                    {
                        _controlPointsPositions[index + 1] += deltaPosition;
                    }
                }
            }

            _controlPointsPositions[index] = point;

            if (index % 3 == 0)
            {
                for (int i = (index - 3); i <= (index + 3); i = i + 3)
                {
                    if (i >= 0 && i < _controlPointsPositions.Length)
                    {
                        if (_modes[i / 3] == BezierHandlesAlignment.Automatic || index == i)
                            EnforceHandleAlignment(i);
                    }
                }
            }
            else
                EnforceHandleAlignment(index);
        }

        /// <summary>
        /// Get control point mode by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public BezierHandlesAlignment GetHandlesAlignment(int index)
        {
            return _modes[(index + 1) / 3];
        }

        /// <summary>
        /// Set control point mode
        /// </summary>
        /// <param name="index"></param>
        /// <param name="handleAlignment"></param>
        public void SetHandlesAlignment(int index, BezierHandlesAlignment handleAlignment, bool enforceMode)
        {
            int modeIndex = (index + 1) / 3;
            _modes[modeIndex] = handleAlignment;
            if (_loop)
            {
                if (modeIndex == 0)
                    _modes[_modes.Length - 1] = handleAlignment;
                else if (modeIndex == _modes.Length - 1)
                    _modes[0] = handleAlignment;
            }

            if (enforceMode)
                EnforceHandleAlignment(index);
        }

        /// <summary>
        /// Get control point
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Vector3 GetControlPointNormal(int index)
        {
            ValidateNormals();

            return _controlPointsNormals[(index + 1) / 3];
        }

        /// <summary>
        /// Set control point normal
        /// </summary>
        /// <param name="index"></param>
        /// <param name="normal"></param>
        public void SetControlPointNormal(int index, Vector3 normal)
        {
            ValidateNormals();

            int normalIndex = (index + 1) / 3;
            _controlPointsNormals[normalIndex] = normal.normalized;
            if (_loop)
            {
                if (normalIndex == 0)
                    _controlPointsNormals[_controlPointsNormals.Length - 1] = normal.normalized;
                else if (normalIndex == _controlPointsNormals.Length - 1)
                    _controlPointsNormals[0] = normal.normalized;
            }
        }

        /// <summary>
        /// Update all handles aligment
        /// </summary>
        private void UpdateAllHandlesAligment()
        {
            for (int i = 0; i < _modes.Length; i++)
            {
                EnforceHandleAlignment(i * 3);
            }
        }

        /// <summary>
        /// Make sure the selected control point handles alignment mode is applied
        /// </summary>
        /// <param name="index"></param>
        private void EnforceHandleAlignment(int index)
        {
            int alignmentIndex = (index + 1) / 3;
            BezierHandlesAlignment handleAlignment = _modes[alignmentIndex];

            if (handleAlignment == BezierHandlesAlignment.Free) // Don't align if free mode is selected
                return;

            // Control point index is always in the middle of two handles
            int controlPointIndex = alignmentIndex * 3;

            if (handleAlignment == BezierHandlesAlignment.Automatic)
            {
                int previousControlPointIndex, nextControlPointIndex, previousHandle, nextHandle, lookTargetHandle;
                Vector3 direction, prevDirection, nextDirection;
                float previousNeighbourDistance, nextNeighbourDistance;

                if (controlPointIndex == 0) // First
                {
                    if (_loop)
                    {
                        previousControlPointIndex = _controlPointsPositions.Length - 3;
                        previousHandle = _controlPointsPositions.Length - 2;
                        prevDirection = (_controlPointsPositions[previousControlPointIndex] - _controlPointsPositions[controlPointIndex]).normalized;
                        previousNeighbourDistance = Vector3.Distance(_controlPointsPositions[controlPointIndex], _controlPointsPositions[previousControlPointIndex]);

                        nextControlPointIndex = controlPointIndex + 3;
                        nextHandle = controlPointIndex + 1;
                        nextDirection = (_controlPointsPositions[nextControlPointIndex] - _controlPointsPositions[controlPointIndex]).normalized;
                        nextNeighbourDistance = Vector3.Distance(_controlPointsPositions[controlPointIndex], _controlPointsPositions[nextControlPointIndex]);

                        direction = (nextDirection - prevDirection).normalized;

                        _controlPointsPositions[previousHandle] = _controlPointsPositions[controlPointIndex] - direction * previousNeighbourDistance * _autoHandleSpacing;
                        _controlPointsPositions[nextHandle] = _controlPointsPositions[controlPointIndex] + direction * nextNeighbourDistance * _autoHandleSpacing;
                    }
                    else
                    {
                        nextControlPointIndex = controlPointIndex + 3;
                        nextHandle = controlPointIndex + 1;
                        lookTargetHandle = controlPointIndex + 2;

                        direction = _controlPointsPositions[lookTargetHandle] - _controlPointsPositions[controlPointIndex];
                        direction = direction / direction.magnitude;

                        nextNeighbourDistance = Vector3.Distance(_controlPointsPositions[controlPointIndex], _controlPointsPositions[nextControlPointIndex]);

                        _controlPointsPositions[nextHandle] = _controlPointsPositions[controlPointIndex] + direction * nextNeighbourDistance * _autoHandleSpacing;
                    }
                }
                else if (controlPointIndex == _controlPointsPositions.Length - 1) // Last
                {
                    if (_loop)
                    {
                        previousControlPointIndex = _controlPointsPositions.Length - 3;
                        previousHandle = _controlPointsPositions.Length - 2;
                        prevDirection = (_controlPointsPositions[previousControlPointIndex] - _controlPointsPositions[controlPointIndex]).normalized;
                        previousNeighbourDistance = Vector3.Distance(_controlPointsPositions[controlPointIndex], _controlPointsPositions[previousControlPointIndex]);

                        nextControlPointIndex = 3;
                        nextHandle = 1;
                        nextDirection = (_controlPointsPositions[nextControlPointIndex] - _controlPointsPositions[controlPointIndex]).normalized;
                        nextNeighbourDistance = Vector3.Distance(_controlPointsPositions[controlPointIndex], _controlPointsPositions[nextControlPointIndex]);

                        direction = (nextDirection - prevDirection).normalized;

                        _controlPointsPositions[previousHandle] = _controlPointsPositions[controlPointIndex] - direction * previousNeighbourDistance * _autoHandleSpacing;
                        _controlPointsPositions[nextHandle] = _controlPointsPositions[controlPointIndex] + direction * nextNeighbourDistance * _autoHandleSpacing;
                    }
                    else
                    {
                        previousControlPointIndex = controlPointIndex - 3;
                        previousHandle = controlPointIndex - 1;
                        lookTargetHandle = controlPointIndex - 2;

                        direction = _controlPointsPositions[controlPointIndex] - _controlPointsPositions[lookTargetHandle];
                        direction = direction / direction.magnitude;

                        previousNeighbourDistance = Vector3.Distance(_controlPointsPositions[controlPointIndex], _controlPointsPositions[previousControlPointIndex]);

                        _controlPointsPositions[previousHandle] = _controlPointsPositions[controlPointIndex] - direction * previousNeighbourDistance * _autoHandleSpacing;
                    }
                }
                else // In between
                {
                    previousControlPointIndex = _loop ? LoopIndexAroundArray(_controlPointsPositions, controlPointIndex - 3) : controlPointIndex - 3;
                    previousHandle = _loop ? LoopIndexAroundArray(_controlPointsPositions, controlPointIndex - 1) : controlPointIndex - 1;
                    prevDirection = (_controlPointsPositions[previousControlPointIndex] - _controlPointsPositions[controlPointIndex]).normalized;
                    previousNeighbourDistance = Vector3.Distance(_controlPointsPositions[controlPointIndex], _controlPointsPositions[previousControlPointIndex]);

                    nextControlPointIndex = _loop ? LoopIndexAroundArray(_controlPointsPositions, controlPointIndex + 3) : controlPointIndex + 3;
                    nextHandle = _loop ? LoopIndexAroundArray(_controlPointsPositions, controlPointIndex + 1) : controlPointIndex + 1;
                    nextDirection = (_controlPointsPositions[nextControlPointIndex] - _controlPointsPositions[controlPointIndex]).normalized;
                    nextNeighbourDistance = Vector3.Distance(_controlPointsPositions[controlPointIndex], _controlPointsPositions[nextControlPointIndex]);

                    direction = (nextDirection - prevDirection).normalized;

                    _controlPointsPositions[previousHandle] = _controlPointsPositions[controlPointIndex] - direction * previousNeighbourDistance * _autoHandleSpacing;
                    _controlPointsPositions[nextHandle] = _controlPointsPositions[controlPointIndex] + direction * nextNeighbourDistance * _autoHandleSpacing;
                }
            }
            else // Enforce Aligned and Mirrored modes
            {
                // Don't align if it is start or end of non-loop spline
                if (!_loop && (alignmentIndex == 0 || alignmentIndex == _modes.Length - 1))
                    return;

                int fixedHandleIndex, enforcedHandleIndex;

                //Verifying which handle should be fixed and which should be enforced
                if (index <= controlPointIndex)
                {
                    fixedHandleIndex = controlPointIndex - 1;
                    if (fixedHandleIndex < 0)
                        fixedHandleIndex = _controlPointsPositions.Length - 2;

                    enforcedHandleIndex = controlPointIndex + 1;
                    if (enforcedHandleIndex >= _controlPointsPositions.Length)
                        enforcedHandleIndex = 1;
                }
                else
                {
                    fixedHandleIndex = controlPointIndex + 1;
                    if (fixedHandleIndex >= _controlPointsPositions.Length)
                        fixedHandleIndex = 1;

                    enforcedHandleIndex = controlPointIndex - 1;
                    if (enforcedHandleIndex < 0)
                        enforcedHandleIndex = _controlPointsPositions.Length - 2;
                }

                Vector3 middle = _controlPointsPositions[controlPointIndex];
                Vector3 enforcedTangent = middle - _controlPointsPositions[fixedHandleIndex];

                if (handleAlignment == BezierHandlesAlignment.Aligned)
                {
                    enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, _controlPointsPositions[enforcedHandleIndex]);
                }

                _controlPointsPositions[enforcedHandleIndex] = middle + enforcedTangent;
            }
        }

        /// <summary>
        /// Loop index around array if out of bounds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private int LoopIndexAroundArray<T>(T[] array, int index)
        {
            return (index + array.Length) % array.Length;
        }

        /// <summary>
        /// Get curve starting point index
        /// </summary>
        /// <param name="orientedPointIndex"></param>
        /// <returns></returns>
        public int GetCurveStartingPointIndex(int orientedPointIndex)
        {
            float t = orientedPointIndex / (OrientedPoints.Length - 1f);
            return GetCurveStartingPointIndex(t);
        }

        /// <summary>
        /// Get curve starting point index
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public int GetCurveStartingPointIndex(float t)
        {
            int curveStartIndex;
            if (t >= 1f)
            {
                t = 1f;
                curveStartIndex = _controlPointsPositions.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                curveStartIndex = (int)t;
                t -= curveStartIndex;
                curveStartIndex *= 3;
            }

            return curveStartIndex;
        }

        /// <summary>
        /// Return the index of the closest oriented point
        /// </summary>
        /// <param name="t">Percentual interpolotion value</param>
        /// <returns></returns>
        public int GetClosestOrientedPointIndex(float t, bool alwaysRoundToLowerIndex = true)
        {
            int closest = 0;
            t = Mathf.Clamp01(t);

            if (_orientedPoints == null || _orientedPoints.Length == 0)
                CalculateOrientedPoints(1f);

            if (alwaysRoundToLowerIndex)
                closest = (int)((_orientedPoints.Length - 1) * t);
            else
                closest = Mathf.RoundToInt((_orientedPoints.Length - 1) * t);

            return closest;
        }

        /// <summary>
        /// Get point (world space)
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 GetPoint(float t)
        {
            int curveStartIndex;
            if (t >= 1f)
            {
                t = 1f;
                curveStartIndex = _controlPointsPositions.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                curveStartIndex = (int)t;
                t -= curveStartIndex;
                curveStartIndex *= 3;
            }
            return transform.TransformPoint(Bezier.GetPoint(
                _controlPointsPositions[curveStartIndex], _controlPointsPositions[curveStartIndex + 1], _controlPointsPositions[curveStartIndex + 2], _controlPointsPositions[curveStartIndex + 3], t));
        }

        /// <summary>
        /// Get point on segment
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return Bezier.GetPoint(p0, p1, p2, p3, t);
        }

        /// <summary>
        /// Calculate normal
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 GetNormal(float t)
        {
            int curveStartIndex = GetCurveStartingPointIndex(t);
            int curveEndIndex = curveStartIndex + 3;

            return transform.TransformDirection(Vector3.Lerp(GetControlPointNormal(curveStartIndex), GetControlPointNormal(curveEndIndex), t).normalized);
        }

        /// <summary>
        /// Get normal on segment
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 GetNormal(Vector3 n1, Vector3 n2, float t)
        {
            return transform.TransformDirection(Vector3.Lerp(n1, n2, t).normalized);
        }

        /// <summary>
        /// Get point rotation at spline postion t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Quaternion GetRotation(float t)
        {
            int curveStartIndex;
            if (t >= 1f)
            {
                t = 1f;
                curveStartIndex = _controlPointsRotations.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                curveStartIndex = (int)t;
                t -= curveStartIndex;
                curveStartIndex *= 3;
            }

            return Bezier.GetPointRotation(_controlPointsRotations[curveStartIndex], _controlPointsRotations[curveStartIndex + 1], _controlPointsRotations[curveStartIndex + 2], _controlPointsRotations[curveStartIndex + 3], t);
        }

        /// <summary>
        /// Get velocity
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 GetVelocity(float t)
        {
            int curveStartIndex;
            if (t >= 1f)
            {
                t = 1f;
                curveStartIndex = _controlPointsPositions.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                curveStartIndex = (int)t;
                t -= curveStartIndex;
                curveStartIndex *= 3;
            }

            return GetVelocity(_controlPointsPositions[curveStartIndex], _controlPointsPositions[curveStartIndex + 1], _controlPointsPositions[curveStartIndex + 2], _controlPointsPositions[curveStartIndex + 3], t);
        }

        /// <summary>
        /// Get velocity on spline segment
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 GetVelocity(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            Vector3 firstDerivative = Bezier.GetFirstDerivative(p0, p1, p2, p3, t);
            return transform.TransformPoint(firstDerivative) - transform.position;
        }

        /// <summary>
        /// Get direction
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 GetDirection(float t)
        {
            return GetVelocity(t).normalized;
        }

        /// <summary>
        /// Get direction on spline segment
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 GetDirection(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return GetVelocity(p0, p1, p2, p3, t).normalized;
        }

        /// <summary>
        /// Get a list of spline oriented points based on the number os steps
        /// </summary>
        /// <param name="steps"></param>
        /// <returns></returns>
        public List<OrientedPoint> GetOrientedPoints(int steps)
        {
            List<OrientedPoint> ret = new List<OrientedPoint>();

            float stepPercentage = 1f / steps;
            float t = 0;

            while (t < 1f)
            {
                OrientedPoint orientedPoint = GetOrientedPoint(t);
                ret.Add(orientedPoint);
                t += stepPercentage;
            }

            return ret;
        }

        /// <summary>
        /// Get point position and rotation on spline position t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public OrientedPoint GetOrientedPoint(float t)
        {
            Vector3 position = GetPoint(t);
            Quaternion rotation = GetRotation(t);
            Vector3 direction = GetDirection(t);
            Vector3 normal = GetNormal(t);

            rotation = rotation * Quaternion.LookRotation(direction, normal);
            normal = rotation * Vector3.up;

            return new OrientedPoint(position, rotation, direction, normal);
        }

        /// <summary>
        /// Get position and rotation on a spline segment
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public OrientedPoint GetOrientedPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Quaternion r0, Quaternion r1, Quaternion r2, Quaternion r3, Vector3 n1, Vector3 n2, float t)
        {
            Vector3 position = GetPoint(p0, p1, p2, p3, t);
            Quaternion rotation = Bezier.GetPointRotation(r0, r1, r2, r3, t);
            Vector3 direction = GetDirection(p0, p1, p2, p3, t);
            Vector3 normal = GetNormal(n1, n2, t);

            rotation = rotation * Quaternion.LookRotation(direction, normal);
            normal = rotation * Vector3.up;

            return new OrientedPoint(position, rotation, direction, normal);
        }

        /// <summary>
        /// Reset spline
        /// </summary>
        public void Reset()
        {
            _loop = false;

            _handlesVisibility = HandlesVisibility.ShowOnlyActiveHandles;
            _controlPointsPositions = new Vector3[4];
            _controlPointsRotations = new Quaternion[4];
            _modes = new BezierHandlesAlignment[2];
            _controlPointsNormals = new Vector3[2];

            for (int i = 0; i < _controlPointsPositions.Length; i++)
            {
                _controlPointsPositions[i] = new Vector3(0f, 0f, i * (_newCurveLength / 3));
                _controlPointsRotations[i] = Quaternion.identity;
            }

            for (int i = 0; i < _modes.Length; i++)
            {
                _modes[i] = BezierHandlesAlignment.Aligned;
                _controlPointsNormals[i] = Vector3.up;
            }
        }

        /// <summary>
        /// Reset normals array to default value
        /// </summary>
        public void ResetNormals()
        {
            int normalsCount = _modes.Length;

            _controlPointsNormals = new Vector3[normalsCount];
            ResetNormals(Vector3.up);

            ResetRotations();
        }

        /// <summary>
        /// Set all control points normals to selected value
        /// </summary>
        /// <param name="newNormal"></param>
        private void ResetNormals(Vector3 newNormal)
        {
            for (int i = 0; i < _controlPointsNormals.Length; i++)
            {
                _controlPointsNormals[i] = newNormal;
            }
        }

        /// <summary>
        /// Make sure normals array is not null (backwards compatibility)
        /// </summary>
        private void ValidateNormals()
        {
            if (_controlPointsNormals == null || _controlPointsNormals.Length == 0 || _controlPointsNormals.Length != _modes.Length)
                ResetNormals();
        }

        /// <summary>
        /// Reset all control points rotations
        /// </summary>
        private void ResetRotations()
        {
            ResetRotations(Quaternion.identity);
        }

        /// <summary>
        /// Set all control points rotations to new rotation value
        /// </summary>
        private void ResetRotations(Quaternion newRotation)
        {
            for (int i = 0; i < _controlPointsRotations.Length; i++)
            {
                _controlPointsRotations[i] = newRotation;
            }
        }

        /// <summary>
        /// Add new curve to spline
        /// </summary>
        public void AddCurve()
        {
            ValidateNormals();

            if (_loop)
                _loop = false;

            //Add positions
            Vector3 lastPointPosition = _controlPointsPositions[_controlPointsPositions.Length - 1];
            Vector3 lastPointDirection = transform.InverseTransformDirection(GetDirection(1f)); // Last point direction
            Quaternion lastPointRotation = GetRotation(1f); // Last point rotation

            Array.Resize(ref _controlPointsPositions, _controlPointsPositions.Length + 3);
            Array.Resize(ref _controlPointsRotations, _controlPointsRotations.Length + 3);

            float positionOffset = (_newCurveLength / 3);
            //Add the 3 new control points
            for (int i = 3; i > 0; i--)
            {
                //Calculate new position based on last point direction
                lastPointPosition += (lastPointDirection * positionOffset);
                //Position
                _controlPointsPositions[_controlPointsPositions.Length - i] = lastPointPosition;
                //Rotation
                _controlPointsRotations[_controlPointsRotations.Length - i] = lastPointRotation;
            }

            //Add normal
            Array.Resize(ref _controlPointsNormals, _controlPointsNormals.Length + 1);
            _controlPointsNormals[_controlPointsNormals.Length - 1] = _controlPointsNormals[_controlPointsNormals.Length - 2];

            //Add modes
            Array.Resize(ref _modes, _modes.Length + 1);
            _modes[_modes.Length - 1] = _modes[_modes.Length - 2];
            EnforceHandleAlignment(_controlPointsPositions.Length - 4);

            if (_loop)
            {
                _controlPointsPositions[_controlPointsPositions.Length - 1] = _controlPointsPositions[0];
                _controlPointsRotations[_controlPointsRotations.Length - 1] = _controlPointsRotations[0];
                _modes[_modes.Length - 1] = _modes[0];
                EnforceHandleAlignment(0);
            }
        }

        /// <summary>
        /// Add new curve to spline based on target point
        /// </summary>
        /// <param name="point">world space point</param>
        public void AddCurve(Vector3 point, Vector3 upwardsDirection)
        {
            AddCurve();

            int lastPointIndex = _controlPointsPositions.Length - 1;
            int lastPointHandleIndex = _controlPointsPositions.Length - 2;
            int startPointHandleIndex = _controlPointsPositions.Length - 3;
            int startPointIndex = _controlPointsPositions.Length - 4;

            Vector3 startPointDirection = GetCurveStartDirection(startPointIndex);
            Vector3 startPoint_Right = (Quaternion.LookRotation(startPointDirection, upwardsDirection) * Vector3.right);
            Vector3 startPoint_Up = (Quaternion.LookRotation(startPointDirection, upwardsDirection) * Vector3.up);

            point = transform.InverseTransformPoint(point); //Convert to local position
            _controlPointsPositions[lastPointIndex] = point;

            Vector3 targetPointDirection = (_controlPointsPositions[lastPointIndex] - _controlPointsPositions[startPointIndex]);
            targetPointDirection = (targetPointDirection / targetPointDirection.magnitude);

            float targetAngle = Vector3.SignedAngle(startPointDirection, targetPointDirection, startPoint_Up);

            /*
             *  0º  ->  45º  = back -> left
             *  45º ->  90º+ = left -> forward
             *  0º  -> -45º  = back -> right
             * -45º -> -90º- = right -> forward
             */
            Vector3 dir1 = targetAngle > -45f && targetAngle <= 45f ? -startPointDirection : (targetAngle > 45f ? -startPoint_Right : startPoint_Right);
            Vector3 dir2 = targetAngle > 45f || targetAngle <= -45f ? startPointDirection : (targetAngle > 0f && targetAngle <= 45f ? -startPoint_Right : startPoint_Right);

            float absoluteaAngle = Mathf.Abs(targetAngle);
            float t = absoluteaAngle <= 45f ? (absoluteaAngle / 45f) : (absoluteaAngle - 45f) / 45f;

            Vector3 lastHandleDirection = Vector3.Lerp(dir1, dir2, t);

            float minHandleDistanceRatio = absoluteaAngle <= 45f ? 0.33f : 0.4f;
            float maxHandleDistanceRatio = absoluteaAngle <= 45f ? 0.4f : 0.67f;
            float handleDistanceRatio = Mathf.Lerp(minHandleDistanceRatio, maxHandleDistanceRatio, t);

            float distance = Vector3.Distance(_controlPointsPositions[startPointIndex], _controlPointsPositions[lastPointIndex]);
            _controlPointsPositions[startPointHandleIndex] = _controlPointsPositions[startPointIndex] + startPointDirection * distance * handleDistanceRatio;
            _controlPointsPositions[lastPointHandleIndex] = _controlPointsPositions[lastPointIndex] + lastHandleDirection * distance * handleDistanceRatio;
            _controlPointsRotations[lastPointIndex] = GetRotation(1f);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="startPoint"></param>
        /// <param name="handle1"></param>
        /// <param name="handle2"></param>
        /// <param name="endPoint"></param>
        public void PreviewQuadraticBezierCurve(Vector3 point, out Vector3 startPoint, out Vector3 handle1, out Vector3 handle2, out Vector3 endPoint, Vector3 upwardsDirection)
        {
            int lastPointIndex = _controlPointsPositions.Length - 1;
            int startPointIndex = _controlPointsPositions.Length - 4;

            Vector3 startPointDirection = GetCurveEndDirection(startPointIndex);
            Vector3 startPoint_Right = (Quaternion.LookRotation(startPointDirection, upwardsDirection) * Vector3.right);
            Vector3 startPoint_Up = (Quaternion.LookRotation(startPointDirection, upwardsDirection) * Vector3.up);

            point = transform.InverseTransformPoint(point); //Convert to local position

            Vector3 targetPointDirection = (point - _controlPointsPositions[lastPointIndex]);
            targetPointDirection = (targetPointDirection / targetPointDirection.magnitude);

            float targetAngle = Vector3.SignedAngle(startPointDirection, targetPointDirection, startPoint_Up);

            Vector3 dir1 = targetAngle > -45f && targetAngle <= 45f ? -startPointDirection : (targetAngle > 45f ? -startPoint_Right : startPoint_Right);
            Vector3 dir2 = targetAngle > 45f || targetAngle <= -45f ? startPointDirection : (targetAngle > 0f && targetAngle <= 45f ? -startPoint_Right : startPoint_Right);

            float absoluteaAngle = Mathf.Abs(targetAngle);
            float t = absoluteaAngle <= 45f ? (absoluteaAngle / 45f) : (absoluteaAngle - 45f) / 45f;

            Vector3 lastHandleDirection = Vector3.Lerp(dir1, dir2, t);

            float minHandleDistanceRatio = absoluteaAngle <= 45f ? 0.33f : 0.4f;
            float maxHandleDistanceRatio = absoluteaAngle <= 45f ? 0.4f : 0.67f;
            float handleDistanceRatio = Mathf.Lerp(minHandleDistanceRatio, maxHandleDistanceRatio, t);

            float distance = Vector3.Distance(_controlPointsPositions[lastPointIndex], point);
            handle1 = _controlPointsPositions[lastPointIndex] + startPointDirection * distance * handleDistanceRatio;
            handle2 = point + lastHandleDirection * distance * handleDistanceRatio;

            _transform = _transform == null ? GetComponent<Transform>() : _transform;
            startPoint = _transform.TransformPoint(_controlPointsPositions[lastPointIndex]);
            handle1 = _transform.TransformPoint(handle1);
            handle2 = _transform.TransformPoint(handle2);
            endPoint = _transform.TransformPoint(point);
        }

        /// <summary>
        /// Remove the last curve (Disables loop property)
        /// </summary>
        public void RemoveCurve()
        {
            ValidateNormals();

            if (CurveCount <= 1)
            {
                Debug.Log("Spline has only one curve. Cannot remove last curve.");
                return;
            }

            _loop = false;

            Array.Resize(ref _controlPointsPositions, _controlPointsPositions.Length - 3);
            Array.Resize(ref _controlPointsRotations, _controlPointsRotations.Length - 3);
            Array.Resize(ref _modes, _modes.Length - 1);
            Array.Resize(ref _controlPointsNormals, _controlPointsNormals.Length - 1);
        }

        /// <summary>
        /// Curve spline on the desired direction based on the current "new curve angle" value
        /// </summary>
        /// <param name="direction"></param>
        public void ShapeCurve(Vector3 direction)
        {
            if (_transform == null)
                _transform = GetComponent<Transform>();

            ResetLastCurve(); //Straighten curve to get initial Slerp values

            // Disable automatic aligment to avoid shape deformation
            if (_modes[_modes.Length - 1] == BezierHandlesAlignment.Automatic)
                _modes[_modes.Length - 1] = BezierHandlesAlignment.Aligned;

            int curveStartIndex = _controlPointsPositions.Length - 4;

            // Avoid divided by zero error and keeps the straighted segment wich is expected result for 0º angle input
            if (_newCurveAngle == 0f)
                return;

            float angleRatio = _newCurveAngle / 360f;
            float circleRadius = _newCurveLength / (angleRatio * 2f * Mathf.PI);

            Vector3 startPointDirection = GetCurveStartDirection(curveStartIndex);
            Vector3 startPointNormal = GetControlPointNormal(curveStartIndex);
            Vector3 startPoint_Right = (Quaternion.LookRotation(startPointDirection, startPointNormal) * Vector3.right);
            Vector3 startPoint_Up = (Quaternion.LookRotation(startPointDirection, startPointNormal) * Vector3.up);
            Vector3 centerDirection = Vector3.zero;
            Vector3 circle_Up = Vector3.zero;

            if (direction == _transform.right) // Curve to the right
            {
                centerDirection = startPoint_Right;
                circle_Up = startPoint_Up;
            }
            else if (direction == -_transform.right) // Curve to the left
            {
                centerDirection = -startPoint_Right;
                circle_Up = -startPoint_Up;
            }
            else if (direction == _transform.up) // Curve up
            {
                centerDirection = startPoint_Up;
                circle_Up = -startPoint_Right;
            }
            else if (direction == -_transform.up) // Curve down
            {
                centerDirection = -startPoint_Up;
                circle_Up = startPoint_Right;
            }

            // Imaginary circle
            Vector3 circleStart = _controlPointsPositions[curveStartIndex];
            Vector3 circleCenter = circleStart + (centerDirection * circleRadius);

            float t = _newCurveAngle / 90f;
            Vector3 intersectionDirection = Vector3.Slerp(-centerDirection, startPointDirection, t);
            Vector3 endPoint = circleCenter + (intersectionDirection * circleRadius);

            float handleDistanceRatio = Mathf.Lerp(0.33f, 0.55f, t); // Optimal handles distance for perfect circunference
            float refDistance = Mathf.Abs(Vector3.Distance(circleStart, endPoint) / 1.4142f); //Pitagoras teorem for calculating side distance of imaginary square

            Vector3 startPointHandle = circleStart + (startPointDirection * refDistance * handleDistanceRatio);

            Vector3 handleDirection = Vector3.Cross(intersectionDirection, circle_Up);
            Vector3 endPointHandle = endPoint + (handleDirection * refDistance * handleDistanceRatio);

            _controlPointsPositions[curveStartIndex + 1] = startPointHandle;
            _controlPointsPositions[curveStartIndex + 2] = endPointHandle;
            _controlPointsPositions[curveStartIndex + 3] = endPoint;
        }

        /// <summary>
        /// Reset last curve
        /// </summary>
        public void ResetLastCurve()
        {
            _loop = false;

            int curveStartIndex = _controlPointsPositions.Length - 4;
            Vector3 startPointPosition = _controlPointsPositions[curveStartIndex];
            Vector3 startPointDirection = GetCurveStartDirection(curveStartIndex);
            Quaternion startPointRotation = GetCurveRotation(curveStartIndex);

            float positionOffset = (_newCurveLength / 3);

            //Reset control points positions and rotations
            for (int i = 3; i > 0; i--)
            {
                //Calculate new position based on last point direction
                startPointPosition += (startPointDirection * positionOffset);
                //Position
                _controlPointsPositions[_controlPointsPositions.Length - i] = startPointPosition;
                //Rotation
                _controlPointsRotations[_controlPointsRotations.Length - i] = startPointRotation;
            }
        }

        /// <summary>
        /// Get curve diretion at the start of the curve
        /// </summary>
        /// <param name="curveStartIndex"></param>
        /// <returns></returns>
        private Vector3 GetCurveStartDirection(int curveStartIndex)
        {
            return transform.InverseTransformDirection(GetDirection(_controlPointsPositions[curveStartIndex], _controlPointsPositions[curveStartIndex + 1], _controlPointsPositions[curveStartIndex + 2], _controlPointsPositions[curveStartIndex + 3], 0f));
        }

        /// <summary>
        /// Get curve diretion at the end of the curve
        /// </summary>
        /// <param name="curveStartIndex"></param>
        /// <returns></returns>
        private Vector3 GetCurveEndDirection(int curveStartIndex)
        {
            return transform.InverseTransformDirection(GetDirection(_controlPointsPositions[curveStartIndex], _controlPointsPositions[curveStartIndex + 1], _controlPointsPositions[curveStartIndex + 2], _controlPointsPositions[curveStartIndex + 3], 1f));
        }

        /// <summary>
        /// Get curve rotation at the start of the curve
        /// </summary>
        /// <param name="curveStartIndex"></param>
        /// <returns></returns>
        private Quaternion GetCurveRotation(int curveStartIndex)
        {
            return Bezier.GetPointRotation(_controlPointsRotations[curveStartIndex], _controlPointsRotations[curveStartIndex + 1], _controlPointsRotations[curveStartIndex + 2], _controlPointsRotations[curveStartIndex + 3], 0f);
        }

        /// <summary>
        /// Disable colliders and save their previous state
        /// </summary>
        /// <param name="colliders"></param>
        /// <param name="collidersState"></param>
        private static bool[] DisableColliders(MeshCollider[] colliders)
        {
            bool[] collidersState = new bool[colliders.Length];

            for (int i = 0; i < colliders.Length; i++)
            {
                collidersState[i] = colliders[i].enabled;
                colliders[i].enabled = false;
            }

            return collidersState;
        }

        /// <summary>
        /// Reenable colliders
        /// </summary>
        /// <param name="colliders"></param>
        /// <param name="collidersState"></param>
        private static void RenableColliders(MeshCollider[] colliders, bool[] collidersState)
        {
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = collidersState[i];
        }

        /// <summary>
        /// Disable colliders and save their active previous state
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        private static bool[] DisableIgnoredObjects(SMR_IgnoredObject[] objects)
        {
            bool[] objectsState = new bool[objects.Length];

            for (int i = 0; i < objects.Length; i++)
            {
                objectsState[i] = objects[i].gameObject.activeInHierarchy;
                objects[i].gameObject.SetActive(false);
            }

            return objectsState;
        }

        /// <summary>
        /// Reenable objects that were enabled before
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="objectsState"></param>
        private static void RenableIgnoredObjects(SMR_IgnoredObject[] objects, bool[] objectsState)
        {
            for (int i = 0; i < objects.Length; i++)
                objects[i].gameObject.SetActive(objectsState[i]);
        }

        /// <summary>
        /// Reset control points heights
        /// </summary>
        public void Flatten()
        {
            Vector3 firstPointPosition = GetControlPointPosition(0);

            // Flatten control points
            for (int i = 0; i < _controlPointsPositions.Length; i += 3)
            {
                FlattenPoint(firstPointPosition, i);
            }

            // Flaten handles
            for (int i = 0; i < _controlPointsPositions.Length; i++)
            {
                if (i % 3 == 0)
                    continue;

                FlattenPoint(firstPointPosition, i);
            }

            UpdateMeshRenderer(false);
        }

        /// <summary>
        /// Flatten selected point based on flatten axis property
        /// </summary>
        /// <param name="firstPointPosition"></param>
        /// <param name="pointIndex"></param>
        private void FlattenPoint(Vector3 firstPointPosition, int pointIndex)
        {
            float x = _flattenAxis == Vector3Axis.x ? firstPointPosition.x : _controlPointsPositions[pointIndex].x;
            float y = _flattenAxis == Vector3Axis.y ? firstPointPosition.y : _controlPointsPositions[pointIndex].y;
            float z = _flattenAxis == Vector3Axis.z ? firstPointPosition.z : _controlPointsPositions[pointIndex].z;

            _controlPointsPositions[pointIndex] = new Vector3(x, y, z);
        }

        /// <summary>
        /// Updates Spline Mesh Renderer component (if any)
        /// </summary>
        /// <param name="forceUpdate">Update immediately even when manual mesh generation is selected</param>
        protected void UpdateMeshRenderer(bool forceUpdate)
        {
            SplineMeshRenderer splineMeshRenderer = GetComponent<SplineMeshRenderer>();

            if (splineMeshRenderer != null)
            {
                if (forceUpdate || splineMeshRenderer.MeshGenerationMethod == MeshGenerationMethod.Realtime)
                    splineMeshRenderer.ExtrudeMesh();
            }
        }

        /// <summary>
        /// Return bezier curve aproximated Length in meters
        /// </summary>
        /// <param name="realDistance"></param>
        /// <returns></returns>
        public float GetTotalDistance(bool realDistance = false)
        {
            float length = 0f;

            // Calculate real length based on oriented points
            if (realDistance)
            {
                _orientedPoints = (_orientedPoints == null || _orientedPoints.Length == 0) ? CalculateOrientedPoints(1f) : _orientedPoints;

                int penultimateIndex = (_orientedPoints.Length - 2);
                for (int i = 0; i <= penultimateIndex; i++)
                    length += Vector3.Distance(_orientedPoints[i].Position, _orientedPoints[i + 1].Position);
            }
            else // Calculate aproximated length
            {
                for (float t = 0f; t < 1f; t += 0.1f)
                    length += Vector3.Distance(GetPoint(t), GetPoint(t + 0.1f));
            }

            return length;
        }

        /// <summary>
        /// Split spline in two using the selected control point as reference
        /// </summary>
        /// <param name="selectedIndex"></param>
        public void SplitSpline(int selectedIndex)
        {
            ValidateNormals();
            _loop = false; // Disable loop on Split

            if (selectedIndex == 0)
            {
                SubdivideCurve(selectedIndex);
                selectedIndex = 3;
            }

            _transform = _transform == null ? GetComponent<Transform>() : _transform;

            //Creating new spline control points
            int newSplineLength = _controlPointsPositions.Length - selectedIndex;
            int newModesLength = (newSplineLength / 3) + 1;
            Vector3[] newControlPointsPositions = new Vector3[newSplineLength];
            Quaternion[] newControlPointsRotations = new Quaternion[newSplineLength];
            Vector3[] newControlPointsNormals = new Vector3[newModesLength];
            BezierHandlesAlignment[] newModes = new BezierHandlesAlignment[newModesLength];

            //Populating new spline control points
            int newModesStart = (selectedIndex / 3);
            Array.Copy(_controlPointsPositions, selectedIndex, newControlPointsPositions, 0, newSplineLength);
            Array.Copy(_controlPointsRotations, selectedIndex, newControlPointsRotations, 0, newSplineLength);
            Array.Copy(_modes, newModesStart, newModes, 0, newModesLength);
            Array.Copy(_controlPointsNormals, newModesStart, newControlPointsNormals, 0, newModesLength);

            //Creating new spline game object
            GameObject newSplineGameObject = Instantiate(this.gameObject, this.transform.parent);
            Transform newSplineTransform = newSplineGameObject.GetComponent<Transform>();
            Spline newSpline = newSplineGameObject.GetComponent<Spline>();
            newSpline._controlPointsPositions = newControlPointsPositions;
            newSpline._controlPointsRotations = newControlPointsRotations;
            newSpline._modes = newModes;
            newSpline._controlPointsNormals = newControlPointsNormals;

            //Adjusting new spline world position and pivot
            Vector3 firstControlPoint = newSpline._controlPointsPositions[0]; //Look towards first handle to get spline correct rotation
            newSplineTransform.position = transform.TransformPoint(firstControlPoint);

            Vector3 lookTarget = transform.TransformPoint(newControlPointsPositions[1]);
            Vector3[] worldPos = new Vector3[newControlPointsPositions.Length];

            //Save as world position to preserve relative position after rotation
            for (int i = 0; i < newSpline._controlPointsPositions.Length; i++)
            {
                newSpline._controlPointsPositions[i] -= firstControlPoint;
                worldPos[i] = newSplineTransform.TransformPoint(newSpline._controlPointsPositions[i]);
            }

            //Rotate spline
            newSplineTransform.LookAt(lookTarget);

            //Convert back to local positions
            for (int i = 0; i < newSpline._controlPointsPositions.Length; i++)
            {
                newSpline._controlPointsPositions[i] = newSplineTransform.InverseTransformPoint(worldPos[i]);
            }

            //Removing control points from older spline
            int resizeLength = selectedIndex + 1;
            int modeResizeLength = newModesStart + 1;
            Array.Resize(ref _controlPointsPositions, resizeLength);
            Array.Resize(ref _controlPointsRotations, resizeLength);
            Array.Resize(ref _modes, modeResizeLength);
            Array.Resize(ref _controlPointsNormals, modeResizeLength);

            UpdateMeshRenderer(true);
            newSpline.UpdateMeshRenderer(true);

#if UNITY_EDITOR
            //Select new spline createdon Editor
            UnityEditor.Selection.activeGameObject = newSplineGameObject;
            EnforceUniqueName(newSpline.gameObject);
#endif
            ValidateMeshRendererCaps(this.GetComponent<SplineMeshRenderer>(), newSpline.GetComponent<SplineMeshRenderer>());

            Debug.Log("Spline Splitted Successfully!");
        }

        /// <summary>
        /// Make sure new object has a unique name
        /// </summary>
        /// <param name="targetObject"></param>
        private static void EnforceUniqueName(GameObject targetObject)
        {
#if UNITY_EDITOR
            targetObject.name = targetObject.name.Replace("(Clone)", string.Empty);
            UnityEditor.GameObjectUtility.EnsureUniqueNameForSibling(targetObject);
#endif
        }

        /// <summary>
        /// Merge with target spline
        /// WARNING: target spline will be deleted after merging
        /// </summary>
        /// <param name="target"></param>
        /// <param name="deleteTarget"></param>
        public void Merge(GameObject target, bool deleteTarget = true, bool disableTargetIfNotDeleted = true)
        {
            ValidateNormals();
            Spline targetSpline = target.GetComponent<Spline>();

            if (targetSpline == null)
            {
                Debug.Log("Merging not possible, target GameObject is not a Spline");
                return;
            }

            List<Vector3> mergedPoints = new List<Vector3>();
            List<Quaternion> mergedRotations = new List<Quaternion>();
            List<Vector3> mergedNormals = new List<Vector3>();
            List<BezierHandlesAlignment> mergedModes = new List<BezierHandlesAlignment>();

            int enforceAligmentIndex = _controlPointsPositions.Length - 1;

            mergedPoints.AddRange(_controlPointsPositions);
            mergedRotations.AddRange(_controlPointsRotations);
            mergedModes.AddRange(_modes);
            mergedNormals.AddRange(_controlPointsNormals);

            Vector3 worldPos;
            Transform targetTransform = targetSpline.transform;
            _transform = _transform == null ? GetComponent<Transform>() : _transform;
            for (int pointIndex = 1; pointIndex < targetSpline.ControlPointsPositions.Length; pointIndex++)
            {
                worldPos = targetTransform.TransformPoint(targetSpline.ControlPointsPositions[pointIndex]);
                mergedPoints.Add(_transform.InverseTransformPoint(worldPos));
                mergedRotations.Add(targetSpline.ControlPointsRotations[pointIndex]);
            }

            for (int modeIndex = 1; modeIndex < targetSpline.Modes.Length; modeIndex++)
            {
                mergedModes.Add(targetSpline.Modes[modeIndex]);
                mergedNormals.Add(targetSpline.ControlPointsNormals[modeIndex]);
            }

            _controlPointsPositions = mergedPoints.ToArray();
            _controlPointsRotations = mergedRotations.ToArray();
            _modes = mergedModes.ToArray();
            _controlPointsNormals = mergedNormals.ToArray();

            //Expand vertices limit to avoid autospliting merged spline automatically
            SplineMeshRenderer splineMeshRenderer = GetComponent<SplineMeshRenderer>();
            if (splineMeshRenderer != null)
            {
                SplineMeshRenderer targetMeshRenderer = targetSpline.GetComponent<SplineMeshRenderer>();

                if (targetMeshRenderer != null)
                {
                    splineMeshRenderer.VerticesLimit += targetMeshRenderer.VerticesLimit;
                    splineMeshRenderer.EndCap = targetMeshRenderer.EndCap;
                }
            }

            if (deleteTarget)
            {
                if (Application.IsPlaying(this))
                    Destroy(targetSpline.gameObject); //Play mode or build
                else
                    DestroyImmediate(targetSpline.gameObject); // Unity Editor
            }
            else if (disableTargetIfNotDeleted)
                targetSpline.gameObject.SetActive(false);

            EnforceHandleAlignment(enforceAligmentIndex);
            UpdateMeshRenderer(true);
        }

        /// <summary>
        /// Connect target to the end of this spline
        /// </summary>
        /// <param name="target"></param>
        public void ConnectTarget(GameObject target)
        {
            Vector3 point = GetPoint(1f);
            Vector3 direction = GetDirection(1f);

            target.transform.position = point;
            target.transform.LookAt(point + direction);

            ValidateMeshRendererCaps(this.GetComponent<SplineMeshRenderer>(), target.GetComponent<SplineMeshRenderer>());
        }

        /// <summary>
        /// Creates a new curve to close the gap between this and target object
        /// </summary>
        /// <param name="targetSpline"></param>
        public void BridgeGap(GameObject target)
        {
            _transform = _transform == null ? GetComponent<Transform>() : _transform;

            int lastCurveStartIndex = _controlPointsPositions.Length - 4;
            Vector3 handle1_Direction = GetCurveEndDirection(lastCurveStartIndex);
            Vector3 handle2_Direction = _transform.InverseTransformDirection(-target.transform.forward);//(-targetSpline.GetCurveStartDirection(0));

            AddCurve();
            int endPointIndex = _controlPointsPositions.Length - 1;
            int handle2_Index = _controlPointsPositions.Length - 2;
            int handle1_Index = _controlPointsPositions.Length - 3;
            lastCurveStartIndex = _controlPointsPositions.Length - 4; //Recalculate after adding new curve

            _controlPointsPositions[endPointIndex] = _transform.InverseTransformPoint(target.transform.position);
            float handleDistance = 0.33f * Vector3.Distance(_controlPointsPositions[endPointIndex], _controlPointsPositions[lastCurveStartIndex]);

            _controlPointsPositions[handle1_Index] = _controlPointsPositions[lastCurveStartIndex] + handle1_Direction * handleDistance;
            _controlPointsPositions[handle2_Index] = _controlPointsPositions[endPointIndex] + handle2_Direction * handleDistance;

            ValidateMeshRendererCaps(this.GetComponent<SplineMeshRenderer>(), target.GetComponent<SplineMeshRenderer>());
        }

        /// <summary>
        /// Insert a new curve between the current selected curve and the next one
        /// </summary>
        /// <param name="selectedIndex"></param>
        /// <returns>new selectedIndex</returns>
        public void SubdivideCurve(int selectedIndex)
        {
            //Temp lists for inserting operation
            List<Vector3> newPositions = new List<Vector3>(_controlPointsPositions);
            List<Quaternion> newRotations = new List<Quaternion>(_controlPointsRotations);
            List<BezierHandlesAlignment> newModes = new List<BezierHandlesAlignment>(_modes);
            List<Vector3> newNormals = new List<Vector3>(_controlPointsNormals);

            // Curve segment information
            int insertIndex = selectedIndex + 2;
            int insertModeIndex = (selectedIndex / 3) + 1;
            Vector3 startPosition = _controlPointsPositions[selectedIndex];
            Vector3 handle1Position = _controlPointsPositions[selectedIndex + 1];
            Vector3 handle2Position = _controlPointsPositions[selectedIndex + 2];
            Vector3 endPosition = _controlPointsPositions[selectedIndex + 3];
            Quaternion startRotation = _controlPointsRotations[selectedIndex];
            Quaternion endRotation = _controlPointsRotations[selectedIndex + 3];

            Vector3 newHandlesDirection = (endPosition - startPosition).normalized;
            Vector3 handle1Direction = (handle1Position - startPosition).normalized;
            Vector3 handle2Direction = (handle2Position - endPosition).normalized;
            float handlesDistance = (Vector3.Distance(startPosition, handle1Position) + Vector3.Distance(handle1Position, handle2Position) + Vector3.Distance(handle2Position, endPosition)) / 7f;

            // Creating curve and handles positions
            Vector3 newPosition = GetPoint(startPosition, handle1Position, handle2Position, endPosition, 0.5f);
            Vector3 previousHandlePosition = newPosition + (-newHandlesDirection * handlesDistance);
            Vector3 afterHandlePosition = newPosition + (newHandlesDirection * handlesDistance);

            // Recalculate old handles positions
            handle1Position = startPosition + (handle1Direction * handlesDistance);
            handle2Position = endPosition + (handle2Direction * handlesDistance);

            // Curve and handle rotations
            Quaternion newRotation = Quaternion.Lerp(startRotation, endRotation, 0.5f);
            Quaternion previousHandleRotation = Quaternion.Lerp(startRotation, newRotation, 0.66f);
            Quaternion afterHandleRotation = Quaternion.Lerp(newRotation, endRotation, 0.33f);
            // Curve handle Mode
            BezierHandlesAlignment newMode = _modes[selectedIndex / 3];

            Vector3 newNormal = _controlPointsNormals[selectedIndex / 3];

            // Inserting positions
            newPositions.Insert(insertIndex, afterHandlePosition);
            newPositions.Insert(insertIndex, newPosition);
            newPositions.Insert(insertIndex, previousHandlePosition);
            // Inserting rotations
            newRotations.Insert(insertIndex, afterHandleRotation);
            newRotations.Insert(insertIndex, newRotation);
            newRotations.Insert(insertIndex, previousHandleRotation);
            // Insert mode
            newModes.Insert(insertModeIndex, newMode);
            //Insert normal
            newNormals.Insert(insertModeIndex, newNormal);

            //Applying New Control Point Insertion
            _controlPointsPositions = newPositions.ToArray();
            _controlPointsRotations = newRotations.ToArray();
            _modes = newModes.ToArray();
            _controlPointsNormals = newNormals.ToArray();

            //Updates old handles positions
            _controlPointsPositions[selectedIndex + 1] = handle1Position;
            _controlPointsPositions[selectedIndex + 5] = handle2Position;

            Debug.Log("Curve Subdivide Successfully!");
        }

        /// <summary>
        /// Dissolve selected curve
        /// </summary>
        /// <param name="selectedIndex"></param>
        public void DissolveCurve(int selectedIndex)
        {
            int previousPoint = selectedIndex - 3;
            int previousPointHandle1 = selectedIndex - 2;
            int previousPointHandle2 = selectedIndex - 1;
            int nextPointHandle1 = selectedIndex + 1;
            int nextPointHandle2 = selectedIndex + 2;
            int nextPoint = selectedIndex + 3;

            // Recalculate handles positions
            float handlesDistance = Vector3.Distance(_controlPointsPositions[previousPoint], _controlPointsPositions[previousPointHandle1]);
            handlesDistance += Vector3.Distance(_controlPointsPositions[previousPointHandle1], _controlPointsPositions[previousPointHandle2]);
            handlesDistance += Vector3.Distance(_controlPointsPositions[previousPointHandle2], _controlPointsPositions[selectedIndex]);
            handlesDistance += Vector3.Distance(_controlPointsPositions[selectedIndex], _controlPointsPositions[nextPointHandle1]);
            handlesDistance += Vector3.Distance(_controlPointsPositions[nextPointHandle1], _controlPointsPositions[nextPointHandle2]);
            handlesDistance += Vector3.Distance(_controlPointsPositions[nextPointHandle2], _controlPointsPositions[nextPoint]);
            handlesDistance /= 3f;

            float referenceDistance = Vector3.Distance(_controlPointsPositions[previousPoint], _controlPointsPositions[nextPoint]);
            handlesDistance = handlesDistance >= referenceDistance * 0.5 ? referenceDistance * 0.33f : handlesDistance; // Make sure handles dont cross os overlap

            // Calculate handles directions
            Vector3 handle1Direction = (_controlPointsPositions[previousPointHandle1] - _controlPointsPositions[previousPoint]).normalized;
            Vector3 handle2Direction = (_controlPointsPositions[nextPointHandle2] - _controlPointsPositions[nextPoint]).normalized;

            // Update handles positions
            _controlPointsPositions[previousPointHandle1] = _controlPointsPositions[previousPoint] + (handlesDistance * handle1Direction);
            _controlPointsPositions[nextPointHandle2] = _controlPointsPositions[nextPoint] + (handlesDistance * handle2Direction);

            int removeStartIndex = selectedIndex - 1; //remove previous handle, control point and next handle
            int modeToRemove = (selectedIndex / 3);

            //Temp lists for removal
            List<Vector3> newPositions = new List<Vector3>(_controlPointsPositions);
            List<Quaternion> newRotations = new List<Quaternion>(_controlPointsRotations);
            List<BezierHandlesAlignment> newModes = new List<BezierHandlesAlignment>(_modes);
            List<Vector3> newNormals = new List<Vector3>(_controlPointsNormals);

            //Control Point removal
            newPositions.RemoveRange(removeStartIndex, 3);
            newRotations.RemoveRange(removeStartIndex, 3);
            newModes.RemoveAt(modeToRemove);
            newNormals.RemoveAt(modeToRemove);

            //Applying Control Point removal
            _controlPointsPositions = newPositions.ToArray();
            _controlPointsRotations = newRotations.ToArray();
            _modes = newModes.ToArray();
            _controlPointsNormals = newNormals.ToArray();

            Debug.Log("Curve Dissolved Successfully!");
        }

        /// <summary>
        /// Populates the OrientedPoints array with a list of evenly spaced points along the spline.
        /// </summary>
        /// <param name="spacing"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public OrientedPoint[] CalculateOrientedPoints(float spacing, bool overrideSplineOrientedPoints = true, float resolution = 10)
        {
            Profiler.BeginSample("CalculateOrientedPoints");

            #region CALCULATING SEGMENTS SPACING
            float distanceSinceLastEvenPoint = 0;
            List<OrientedPoint> tempOrientedPoints = new List<OrientedPoint>();

            OrientedPoint start, handle1, handle2, end, interpolationPoint;
            Vector3 curveStartNormal, curveEndNormal;

            OrientedPoint previousPoint = GetOrientedPoint(0f); // Get start of spline
            previousPoint.Position = transform.InverseTransformPoint(previousPoint.Position);

            //Adding spline start point
            tempOrientedPoints.Add(previousPoint);

            int lastCurveIndex = ControlPointCount - 3;
            for (int curveIndex = 0; curveIndex < lastCurveIndex; curveIndex += 3)
            {
                start = new OrientedPoint(_controlPointsPositions[curveIndex], _controlPointsRotations[curveIndex]);
                handle1 = new OrientedPoint(_controlPointsPositions[curveIndex + 1], _controlPointsRotations[curveIndex + 1]);
                handle2 = new OrientedPoint(_controlPointsPositions[curveIndex + 2], _controlPointsRotations[curveIndex + 2]);
                end = new OrientedPoint(_controlPointsPositions[curveIndex + 3], _controlPointsRotations[curveIndex + 3]);
                curveStartNormal = GetControlPointNormal(curveIndex);
                curveEndNormal = GetControlPointNormal(curveIndex + 3);

                float controlNetLength = Vector3.Distance(start.Position, handle1.Position) + Vector3.Distance(handle1.Position, handle2.Position) + Vector3.Distance(handle2.Position, end.Position);
                float estimatedCurveLength = Vector3.Distance(start.Position, end.Position) + controlNetLength / 2f;
                float divisions = estimatedCurveLength * resolution;

                float timeSteps = (1f / divisions);

                float t = 0;
                while (t <= 1)
                {
                    t += timeSteps;

                    interpolationPoint = GetOrientedPoint(start.Position, handle1.Position, handle2.Position, end.Position, start.Rotation, handle1.Rotation, handle2.Rotation, end.Rotation, curveStartNormal, curveEndNormal, t);

                    distanceSinceLastEvenPoint += Vector3.Distance(previousPoint.Position, interpolationPoint.Position);

                    while (distanceSinceLastEvenPoint >= spacing)
                    {
                        float exceededDistance = distanceSinceLastEvenPoint - spacing;
                        OrientedPoint newEvenlySpacedPoint = previousPoint;
                        newEvenlySpacedPoint.Position = interpolationPoint.Position + (previousPoint.Position - interpolationPoint.Position).normalized * exceededDistance;
                        newEvenlySpacedPoint.Rotation = interpolationPoint.Rotation;
                        newEvenlySpacedPoint.Up = interpolationPoint.Up;
                        newEvenlySpacedPoint.Forward = interpolationPoint.Forward;

                        tempOrientedPoints.Add(newEvenlySpacedPoint);
                        distanceSinceLastEvenPoint = exceededDistance;
                        previousPoint = newEvenlySpacedPoint;
                    }

                    previousPoint = interpolationPoint;
                }
            }

            int endPointIndex = _controlPointsPositions.Length - 1;
            int lastEvenlySpacedPointIndex = tempOrientedPoints.Count - 1;
            float lastPointAndSplineEndDistance = Mathf.Abs(Vector3.Distance(tempOrientedPoints[lastEvenlySpacedPointIndex].Position, _controlPointsPositions[endPointIndex]));

            interpolationPoint = GetOrientedPoint(1f); // Get end of spline
            interpolationPoint.Position = transform.InverseTransformPoint(interpolationPoint.Position);

            #endregion

            #region ENSURE 10% ERROR MARGIN ON LAST POINTS DISTANCES
            // Ensure up to 10% error margin on last point distance
            if (lastPointAndSplineEndDistance <= (spacing * 0.1f))
            {
                tempOrientedPoints[lastEvenlySpacedPointIndex] = interpolationPoint;  //Move last point to close gap
            }
            else
            {
                tempOrientedPoints.Add(interpolationPoint); //Add new point to close gap

                //Distribute error margin among last 5 points
                float adjustedpoints = 5f;
                float adjustmentRatio = 1f;
                for (int i = tempOrientedPoints.Count - 6; i <= tempOrientedPoints.Count - 2; i++)
                {
                    if (i <= 0)
                    {
                        adjustedpoints--;
                        continue;
                    }

                    adjustmentRatio = ((adjustedpoints * spacing) + lastPointAndSplineEndDistance) / (adjustedpoints + 1f);

                    OrientedPoint toAdjust = tempOrientedPoints[i];
                    toAdjust.Position = tempOrientedPoints[i - 1].Position + (tempOrientedPoints[i].Position - tempOrientedPoints[i - 1].Position).normalized * adjustmentRatio;
                    tempOrientedPoints[i] = toAdjust;
                }
            }
            #endregion

            #region DISABLING UNWANTED COLLIDERS
            //Disable colliders
            MeshCollider[] colliders = GetComponentsInChildren<MeshCollider>();
            bool[] collidersState = DisableColliders(colliders);

            //Ignored objects
            SMR_IgnoredObject[] ignoredObjects = GameObject.FindObjectsOfType<SMR_IgnoredObject>();
            bool[] ignoredObjectsState = DisableIgnoredObjects(ignoredObjects);
            #endregion

            #region TERRAIN COLLISION DETECTION
            RaycastHit hit = new RaycastHit();

            //Recalculating positions to world space
            Vector3 worldSpacePos, newForward, newNormal;
            Quaternion newRotation;
            Vector3 lastPointPos = (transform.position - transform.forward);
            Vector3 segmentDirection;
            for (int i = 0; i < tempOrientedPoints.Count; i++)
            {
                worldSpacePos = transform.TransformPoint(tempOrientedPoints[i].Position);
                newRotation = tempOrientedPoints[i].Rotation;
                newForward = tempOrientedPoints[i].Forward;
                newNormal = tempOrientedPoints[i].Up;

                if (_followTerrain)
                {
                    if (TerrainCollision(worldSpacePos, out hit))
                    {
                        worldSpacePos = hit.point;

                        if (i == 0) //First point
                            lastPointPos = (worldSpacePos - transform.forward);

                        segmentDirection = (worldSpacePos - lastPointPos).normalized;
                        newRotation.SetLookRotation(segmentDirection, Vector3.up);
                        newNormal = newRotation * Vector3.up;
                        newForward = segmentDirection;
                    }
                }

                tempOrientedPoints[i] = new OrientedPoint(worldSpacePos, newRotation, newForward, newNormal);
                lastPointPos = tempOrientedPoints[i].Position;
            }

            #endregion

            #region RENABLING UNWANTED COLLIDERS
            //Renable colliders
            RenableColliders(colliders, collidersState);
            //Renable ignored objects
            RenableIgnoredObjects(ignoredObjects, ignoredObjectsState);
            #endregion

            Profiler.EndSample();

            if (overrideSplineOrientedPoints)
            {
                _orientedPoints = tempOrientedPoints.ToArray();
                tempOrientedPoints.Clear();
                return _orientedPoints;
            }
            else
                return tempOrientedPoints.ToArray();
        }

        /// <summary>
        /// Reset spline oriented points
        /// </summary>
        public void DeleteOrientedPoints()
        {
            _orientedPoints = null;
        }

        /// <summary>
        /// Follow terrain feature collision detection
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="hit"></param>
        /// <returns>True if raycast hits anything</returns>
        [ExecuteInEditMode]
        private bool TerrainCollision(Vector3 origin, out RaycastHit hit)
        {
            Profiler.BeginSample("Follow Terrain Collision");

            if (Physics.Raycast((origin + (Vector3.up * _terrainCheckDistance)), Vector3.down, out hit, _terrainCheckDistance * 2))
                return true;

            Profiler.EndSample();

            return false;
        }

        /// <summary>
        /// Create new spline at the end of the current one
        /// </summary>
        public GameObject AppendSpline()
        {
            ValidateNormals();

            Vector3 lastPointPosition = this.GetControlPointPosition(this.ControlPointCount - 1);
            Quaternion lastPointRotation = this.GetRotation(1f);
            Vector3 lastPointNormal = this.GetNormal(1f);

            Vector3 position = transform.TransformPoint(lastPointPosition);
            GameObject clone = Instantiate(this.gameObject, position, Quaternion.LookRotation(this.GetDirection(1)));

            Spline newRendererSpline = clone.GetComponent<Spline>();
            newRendererSpline.Reset();
            newRendererSpline.ResetNormals(lastPointNormal);
            newRendererSpline.ResetRotations(lastPointRotation);

            SplineMeshRenderer newRendererSplineMeshRenderer = clone.GetComponent<SplineMeshRenderer>();
            if (newRendererSplineMeshRenderer != null)
            {
                SplineMeshRenderer splineMeshRenderer = this.GetComponent<SplineMeshRenderer>();

                newRendererSplineMeshRenderer.MeshGenerationMethod = splineMeshRenderer.MeshGenerationMethod;
                newRendererSplineMeshRenderer.ExtrudeMesh();
                ValidateMeshRendererCaps(splineMeshRenderer, newRendererSplineMeshRenderer);
            }

            EnforceUniqueName(clone);

            return clone;
        }

        /// <summary>
        /// Create Parallel Spline
        /// </summary>
        /// <returns></returns>
        public GameObject CreateParallelSpline()
        {
            ValidateNormals();

            if (_parallelSplineDirection == Vector2.zero)
            {
                Debug.LogWarning("Parallel Spline Direction cannot be zero");
                return null;
            }

            _transform = _transform == null ? GetComponent<Transform>() : _transform;

            // Create clone
            GameObject clone = Instantiate(this.gameObject, this.transform.parent);
            Transform cloneTransform = clone.GetComponent<Transform>();
            Spline cloneSpline = clone.GetComponent<Spline>();

            // Save points positions
            Vector3[] worldPositions = new Vector3[_controlPointsPositions.Length];
            for (int i = 0; i < worldPositions.Length; i++)
            {
                worldPositions[i] = cloneTransform.TransformPoint(_controlPointsPositions[i]);
            }

            Vector3[] pointForwards = new Vector3[_controlPointsPositions.Length];
            for (int i = 0; i < _controlPointsPositions.Length; i += 3)
            {
                //Control point direction
                if (i < _controlPointsPositions.Length - 1)
                    pointForwards[i] = GetDirection(_controlPointsPositions[i], _controlPointsPositions[i + 1], _controlPointsPositions[i + 2], _controlPointsPositions[i + 3], 0f);
                else //Last control point
                    pointForwards[i] = GetDirection(_controlPointsPositions[i - 3], _controlPointsPositions[i - 2], _controlPointsPositions[i - 1], _controlPointsPositions[i], 1f);

                //Handles direction (in relation to control point)
                if (i < worldPositions.Length - 1)
                    pointForwards[i + 1] = (worldPositions[i + 1] - worldPositions[i]).normalized; //Next handle direction
                if (i > 0)
                    pointForwards[i - 1] = (worldPositions[i] - worldPositions[i - 1]).normalized; //Previous handle direction
            }

            // Update clone object position
            Vector3 cloneLocalPos = cloneTransform.localPosition;
            cloneLocalPos += _parallelSplineDirection.x * _transform.right;
            cloneLocalPos += _parallelSplineDirection.y * _transform.up;
            cloneTransform.localPosition = cloneLocalPos;

            // Update control points positions
            float previousHandleDistance = 0f, nextHandleDistance = 0f;
            Vector3 newPoint, previousHandle, nextHandle, pointNormal, pointRight;

            for (int i = 0; i < worldPositions.Length; i += 3)
            {
                pointNormal = GetControlPointNormal(i);
                pointRight = Vector3.Cross(pointNormal, pointForwards[i]).normalized;

                newPoint = worldPositions[i];
                newPoint += pointRight * _parallelSplineDirection.x;
                newPoint += _transform.up * _parallelSplineDirection.y;

                cloneSpline.ControlPointsPositions[i] = cloneTransform.InverseTransformPoint(newPoint);
            }

            // Update handles positions
            float newCurveRatio = 1f, originalDistance, newDistance, previousCurveRatio = 1f;
            for (int i = 0; i < worldPositions.Length; i += 3)
            {
                if (i < worldPositions.Length - 1)
                {
                    originalDistance = Vector3.Distance(_controlPointsPositions[i], _controlPointsPositions[i + 3]);
                    newDistance = Vector3.Distance(cloneSpline.ControlPointsPositions[i], cloneSpline.ControlPointsPositions[i + 3]);
                    newCurveRatio = newDistance / originalDistance;
                }

                newPoint = cloneTransform.TransformPoint(cloneSpline.ControlPointsPositions[i]);

                if (i > 0)
                    previousHandleDistance = Vector3.Distance(worldPositions[i], worldPositions[i - 1]);
                if (i < worldPositions.Length - 1)
                    nextHandleDistance = Vector3.Distance(worldPositions[i], worldPositions[i + 1]);

                if (i > 0)
                {
                    previousHandle = newPoint - pointForwards[i - 1] * previousHandleDistance * previousCurveRatio;
                    cloneSpline.ControlPointsPositions[i - 1] = cloneTransform.InverseTransformPoint(previousHandle);
                }
                if (i < worldPositions.Length - 1)
                {
                    nextHandle = newPoint + pointForwards[i + 1] * nextHandleDistance * newCurveRatio;
                    cloneSpline.ControlPointsPositions[i + 1] = cloneTransform.InverseTransformPoint(nextHandle);
                }

                previousCurveRatio = newCurveRatio;
            }

            // Update mesh renderer (if any)
            cloneSpline.UpdateMeshRenderer(true);

            EnforceUniqueName(clone);

            return clone;
        }

        /// <summary>
        /// Revert spline direction
        /// </summary>
        public void RevertSpline()
        {
            _transform = _transform == null ? GetComponent<Transform>() : _transform;

            //Instantiate temporary data sets
            List<Vector3> revertedPositions = new List<Vector3>();
            List<Quaternion> revertedRotations = new List<Quaternion>();
            List<BezierHandlesAlignment> revertedModes = new List<BezierHandlesAlignment>();
            List<Vector3> revertedNormals = new List<Vector3>();

            //Get target location and look direction
            Vector3 targetPosition = _transform.TransformPoint(_controlPointsPositions[_controlPointsPositions.Length - 1]);
            Vector3 targetDirection = _transform.TransformPoint(_controlPointsPositions[_controlPointsPositions.Length - 2]);

            //Save reverted positions
            for (int i = _controlPointsPositions.Length - 1; i >= 0; i--)
            {
                revertedPositions.Add(_transform.TransformPoint(_controlPointsPositions[i]));
            }

            //Save reverted rotations
            for (int i = _controlPointsRotations.Length - 1; i >= 0; i--)
            {
                revertedRotations.Add(_controlPointsRotations[i]);
            }

            //Save reverted modes
            for (int i = _modes.Length - 1; i >= 0; i--)
            {
                revertedModes.Add(_modes[i]);
            }

            //Save reverted normals
            for (int i = _controlPointsNormals.Length - 1; i >= 0; i--)
            {
                revertedNormals.Add(_controlPointsNormals[i]);
            }

            //Move spline to new position
            _transform.position = targetPosition;
            _transform.LookAt(targetDirection);

            //Apply reverted positions
            for (int i = 0; i < revertedPositions.Count; i++)
            {
                _controlPointsPositions[i] = _transform.InverseTransformPoint(revertedPositions[i]);
            }

            //Apply reverted rotations
            for (int i = 0; i < revertedRotations.Count; i++)
            {
                _controlPointsRotations[i] = revertedRotations[i];
            }

            //Apply reverted modes
            for (int i = 0; i < revertedModes.Count; i++)
            {
                _modes[i] = revertedModes[i];
            }

            //Apply reverted normals
            for (int i = 0; i < revertedNormals.Count; i++)
            {
                _controlPointsNormals[i] = revertedNormals[i];
            }

            UpdateMeshRenderer(true);
        }

        /// <summary>
        /// Update mesh renderer caps for seamless connection
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="next"></param>
        private static void ValidateMeshRendererCaps(SplineMeshRenderer previous, SplineMeshRenderer next)
        {
            if (previous == null || next == null)
                return;

            next.StartCap = false; // Disable start cap for seamless connection
            previous.EndCap = false; // Disable end cap for seamless connection

            next.SpawnCaps();
            previous.SpawnCaps();
        }

        /// <summary>
        /// Check if spline has changed
        /// </summary>
        /// <returns></returns>
        public bool CheckChanges()
        {
            bool hasChanged = false;

            if (_transform == null)
                _transform = GetComponent<Transform>();

            hasChanged = (_lastTransformPosition != _transform.position) || (_lastTransformRotation != _transform.rotation);

            _lastTransformPosition = _transform.position;
            _lastTransformRotation = _transform.rotation;

            return hasChanged;
        }

        /// <summary>
        /// Adjuste terrains to spline
        /// </summary>
        public void Terraform(bool setHeights = true, bool paintTextures = true)
        {
            try
            {
                OrientedPoint[] orientedPoints = CalculateOrientedPoints(1f, true);

                _transform = _transform == null ? GetComponent<Transform>() : _transform;

                List<Terrain> terrains = new List<Terrain>();
                int[] terrainIndexes = new int[orientedPoints.Length];
                Vector3[] path = new Vector3[orientedPoints.Length];
                Vector3[] directions = new Vector3[orientedPoints.Length];
                RaycastHit hit;

                Vector3 worldPos;
                Vector3 lastPointPos = Vector3.zero;
                Vector3 forward;
                Vector3 right;

                #region DISABLING UNWANTED COLLIDERS
                //Disable colliders
                MeshCollider[] colliders = GetComponentsInChildren<MeshCollider>();
                bool[] collidersState = DisableColliders(colliders);

                //Ignored objects
                SMR_IgnoredObject[] ignoredObjects = GameObject.FindObjectsOfType<SMR_IgnoredObject>();
                bool[] ignoredObjectsState = DisableIgnoredObjects(ignoredObjects);
                #endregion

                _terraformingStepsCount = orientedPoints.Length;

                for (int i = 0; i < orientedPoints.Length; i++)
                {
                    _terraformingCurrentStep = i;
                    DisplayProgressBar("Scanning Terrains...");

                    worldPos = orientedPoints[i].Position; //Oriented points is already in world position

                    if (i == 0)
                        forward = (worldPos - (worldPos - transform.forward)).normalized;
                    else
                        forward = (worldPos - lastPointPos).normalized;

                    right = Vector3.Cross(Vector3.up, forward).normalized;

                    lastPointPos = worldPos;

                    if (TerrainCollision(worldPos, out hit))
                    {
                        Terrain terrain = hit.collider.GetComponent<Terrain>();
                        if (terrain != null)
                        {
                            if (!terrains.Contains(terrain))
                                terrains.Add(terrain);

                            terrainIndexes[i] = terrains.IndexOf(terrain);

                            terrain.allowAutoConnect = true;
                        }
                    }
                    else
                    {
                        terrainIndexes[i] = -1;
                    }

                    path[i] = worldPos;
                    directions[i] = right;
                }

                Terrain.SetConnectivityDirty(); //tries to reconnect terrains

                #region RENABLING UNWANTED COLLIDERS
                //Renable colliders
                RenableColliders(colliders, collidersState);
                //Renable ignored objects
                RenableIgnoredObjects(ignoredObjects, ignoredObjectsState);
                #endregion

                List<Vector3> terrainPath = new List<Vector3>();
                List<Vector3> slopeDirection = new List<Vector3>();

                _terraformingStepsCount = terrains.Count * 3; //3 operations (Set height, smooth height, paint)
                _terraformingCurrentStep = 0f;

                //Identify all terrains and respective points
                for (int i = 0; i < terrains.Count; i++)
                {
                    terrainPath.Clear();

                    for (int j = 0; j < terrainIndexes.Length; j++)
                    {
                        if (terrainIndexes[j] == i)
                        {
                            terrainPath.Add(path[j]);
                            slopeDirection.Add(directions[j]);
                        }
                    }

                    if (setHeights)
                    {
                        List<string> pathCoordinates;
                        DisplayProgressBar("Terraforming...");
                        WSM_TerrainTools.AdjustHeightAlongPath(terrains[i], terrainPath.ToArray(), _terraformingWidth, out pathCoordinates);
                        _terraformingCurrentStep++;
                        DisplayProgressBar("Smoothing slopes...");
                        WSM_TerrainTools.SmoothenSlopeAlongPath(terrains[i], terrainPath.ToArray(), _terraformingWidth, _embankmentWidth, _embankmentSlope, pathCoordinates);
                    }

                    if (paintTextures)
                    {
                        _terraformingCurrentStep++;
                        DisplayProgressBar("Painting terrains...");
                        WSM_TerrainTools.PaintTextureAlongPath(terrains[i], terrainPath.ToArray(), _terraformingTexture, _terraformingWidth, _terraformingTexture, _embankmentWidth, _minTextureBlending, _maxTextureBlending, _minTextureBlending, _maxTextureBlending);
                    }

                    _terraformingCurrentStep++;
                    DisplayProgressBar("Finishing...");

                    WSM_TerrainTools.StitchBorders(terrains[i]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
            finally
            {
                CloseProgressBar();
            }
        }

        private void DisplayProgressBar(string info)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("Terraforming", info, TerraformingProgress);
#endif
        }

        private static void CloseProgressBar()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
        }

        /// <summary>
        /// Create or update terrain backups
        /// </summary>
        public void BackupTerrains()
        {
            Terrain[] terrains = FindObjectsOfType<Terrain>();

            if (terrains == null) return;

            foreach (Terrain terrain in terrains)
            {
                WSM_TerrainTools.Backup(terrain);
            }
        }

        /// <summary>
        /// Restore terrain bakcups (if any)
        /// WARNING: Affects all terrains in the scene
        /// </summary>
        public void RestoreTerrains()
        {
            Terrain[] terrains = FindObjectsOfType<Terrain>();

            if (terrains == null) return;

            foreach (Terrain terrain in terrains)
            {
                WSM_TerrainTools.RestoreBackup(terrain);
            }
        }

        /// <summary>
        /// Close or open spline based on loop property current state
        /// </summary>
        private void EnforceLoop()
        {
            if (_loop == true)
            {
                _modes[_modes.Length - 1] = _modes[0];
                Vector3 handleDirection = transform.InverseTransformDirection(-GetDirection(0));
                float handleDistance = Vector3.Distance(_controlPointsPositions[_controlPointsPositions.Length - 1], _controlPointsPositions[_controlPointsPositions.Length - 2]);

                _controlPointsPositions[_controlPointsPositions.Length - 1] = _controlPointsPositions[0];
                _controlPointsPositions[_controlPointsPositions.Length - 2] = _controlPointsPositions[_controlPointsPositions.Length - 1] + handleDirection * handleDistance;
            }
            else
            {
                ResetLastCurve();
            }
        }

        /// <summary>
        /// Project all nodes (control points) on surface
        /// </summary>
        public void ProjectNodesOnSurface()
        {
            for (int i = 0; i < _controlPointsPositions.Length; i += 3)
            {
                ProjectNodeOnSurface(i);
            }
        }

        /// <summary>
        /// Project target node (control point) on surface
        /// </summary>
        /// <param name="index"></param>
        public void ProjectNodeOnSurface(int index)
        {
            _transform = _transform == null ? GetComponent<Transform>() : _transform;

            #region DISABLING UNWANTED COLLIDERS
            //Disable colliders
            MeshCollider[] colliders = GetComponentsInChildren<MeshCollider>();
            bool[] collidersState = DisableColliders(colliders);

            //Ignored objects
            SMR_IgnoredObject[] ignoredObjects = GameObject.FindObjectsOfType<SMR_IgnoredObject>();
            bool[] ignoredObjectsState = DisableIgnoredObjects(ignoredObjects);
            #endregion

            #region TERRAIN COLLISION DETECTION
            RaycastHit hit = new RaycastHit();

            //Recalculating positions to world space
            Vector3 worldSpacePos;

            worldSpacePos = _transform.TransformPoint(_controlPointsPositions[index]);

            if (TerrainCollision(worldSpacePos, out hit))
            {
                worldSpacePos = hit.point;

                if (index == 0) //First point
                {
                    _transform.position = worldSpacePos;
                    SetControlPointPosition(index, Vector3.zero);
                }
                else
                {
                    SetControlPointPosition(index, _transform.InverseTransformPoint(worldSpacePos));
                }
            }

            #endregion

            #region RENABLING UNWANTED COLLIDERS
            //Renable colliders
            RenableColliders(colliders, collidersState);
            //Renable ignored objects
            RenableIgnoredObjects(ignoredObjects, ignoredObjectsState);
            #endregion

        }
    }
}
