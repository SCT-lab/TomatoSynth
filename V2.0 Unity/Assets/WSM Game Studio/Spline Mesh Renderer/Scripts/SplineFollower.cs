using System;
using System.Collections.Generic;
using UnityEngine;

namespace WSMGameStudio.Splines
{
    public class SplineFollower : MonoBehaviour
    {
        [Tooltip("One or more splines to be followed")]
        public List<Spline> splines;
        [Tooltip("Following speed")]
        public float speed;
        [Tooltip("Speed Unit")]
        public SplineFollowerSpeedUnit speedUnit;
        [Tooltip("Following behaviour")]
        public SplineFollowerBehaviour followerBehaviour;
        [Tooltip("Customize start position along the spline (From 0% to 100%)")]
        [Range(0, 100)]
        public float customStartPosition = 0f;
        [Tooltip("Apply spline rotation on object")]
        public bool applySplineRotation = true;
        [Tooltip("Configure stops")]
        public SplineFollowerStops cycleEndStops;
        [Tooltip("Configure stop time")]
        public float cycleStopTime = 0f;
        [Tooltip("Offset between master and linked followers")]
        public float followersOffset = 0;
        [Tooltip("WRAP: Loop around spline list | OVERFLOW: Position can overflow spline")]
        public LinkedFollowerBehaviour linkedFollowersBehaviour;
        [Tooltip("Linked followers connected to this follower")]
        public List<LinkedSplineFollower> linkedFollowers;
        [Tooltip("Visualize follower path for debugging purposes")]
        public bool visualizePathOnEditor = false;

        [System.NonSerialized] private float _currentStopTime = 0f;
        [System.NonSerialized] private float _cicleDuration;
        [System.NonSerialized] private float _splineProgress;
        [System.NonSerialized] private bool _goingForward = true;
        [System.NonSerialized] private float _currentSplineLength;
        [System.NonSerialized] protected int _currentSplineIndex = 0;
        [System.NonSerialized] protected int _lastSplineIndex = 0;
        [System.NonSerialized] private float _oldSpeedValue;
        [System.NonSerialized] private bool _oldGoingForward;

        [System.NonSerialized] private Transform _transform;
        [System.NonSerialized] private Vector3 _currentPosition = Vector3.zero;
        [System.NonSerialized] private Quaternion _currentRotation = Quaternion.identity;
        [System.NonSerialized] protected OrientedPoint[][] _normalizedOrientedPoints;

        [System.NonSerialized] private Vector3 _startPosition = Vector3.zero;
        [System.NonSerialized] private Vector3 _endPosition = Vector3.zero;
        [System.NonSerialized] private Quaternion _startRotation = Quaternion.identity;
        [System.NonSerialized] private Quaternion _endRotation = Quaternion.identity;
        [System.NonSerialized] private int _lastFramePointIndex = -1;
        [System.NonSerialized] private int _currentPointIndex = -1;
        [System.NonSerialized] private int _nextPointIndex = 0;
        [System.NonSerialized] private float _segmentdistance;
        [System.NonSerialized] private float _segmentTime;
        [System.NonSerialized] private float _segmentStep;
        [System.NonSerialized] private float _segmentProgress;
        [System.NonSerialized] private float _segmentPercent;

        public bool SpeedChanged { get { return _oldSpeedValue != speed; } }
        public bool DirectionChanged { get { return _oldGoingForward != _goingForward; } }
        public bool CurrentPointChanged { get { return _lastFramePointIndex != _currentPointIndex; } }
        public bool FirstFrame { get { return _currentPointIndex == -1; } }

        //Properties used by linked followers
        public int CurrentSplineIndex { get { return _currentSplineIndex; } }
        public int CurrentPointIndex { get { return _currentPointIndex; } }
        public float SegmentProgress { get { return _segmentProgress; } }
        public OrientedPoint[][] NormalizedOrientedPoints
        {
            get
            {
                if (_normalizedOrientedPoints == null) NormalizeOrientedPoints();
                return _normalizedOrientedPoints;
            }
        }

        public bool GoingForward
        {
            get
            {
                _goingForward = (speed >= 0);
                return _goingForward;
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        protected void Start()
        {
            _transform = GetComponent<Transform>();
            InitializeFollower();
        }

        /// <summary>
        /// Update each frame
        /// </summary>
        protected void Update()
        {
            if (cycleEndStops != SplineFollowerStops.Disabled && _currentStopTime > 0f)
            {
                _currentStopTime -= Time.deltaTime;
                return;
            }

            FollowSpline();

            _oldSpeedValue = speed;
            _oldGoingForward = _goingForward;
        }

        /// <summary>
        /// Initialize follower and set it starting position
        /// </summary>
        private void InitializeFollower()
        {
            _oldSpeedValue = speed;
            _oldGoingForward = _goingForward;

            InitializeLinkedFollowers();//initialize even if splines not set

            if (splines == null || splines.Count == 0)
                return;

            _lastFramePointIndex = -1;
            _currentPointIndex = -1;
            _currentSplineIndex = 0;
            _currentSplineLength = splines[_currentSplineIndex].Length;
            _splineProgress = (customStartPosition * 0.01f);

            cycleStopTime = Mathf.Abs(cycleStopTime);

            NormalizeOrientedPoints();
            CalculateCicleDuration();
        }

        /// <summary>
        /// Initialize linked followers if any
        /// </summary>
        private void InitializeLinkedFollowers()
        {
            if (linkedFollowers != null)
            {
                foreach (LinkedSplineFollower link in linkedFollowers)
                {
                    link.Master = this;
                    link.FollowerBehaviour = linkedFollowersBehaviour;
                }
            }
        }

        /// <summary>
        /// Populate the normalized orientes points collection to use as follower reference
        /// </summary>
        protected void NormalizeOrientedPoints()
        {
            if (splines == null || splines.Count == 0)
                return;

            _normalizedOrientedPoints = new OrientedPoint[splines.Count][];

            float optimalSpacing = 2f, splineLength;
            for (int i = 0; i < splines.Count; i++)
            {
                splineLength = splines[i].GetTotalDistance(true);
                optimalSpacing = splineLength / Mathf.Floor(splineLength);
                _normalizedOrientedPoints[i] = splines[i].CalculateOrientedPoints(optimalSpacing, false);
            }
        }

        /// <summary>
        /// Follow spline path
        /// </summary>
        private void FollowSpline()
        {
            if (splines == null || splines.Count == 0)
                return;

            _goingForward = (speed >= 0);

            if (SpeedChanged)
            {
                CalculateCicleDuration();

                if (DirectionChanged)
                {
                    Vector3 tempPos = _startPosition;
                    _startPosition = _endPosition;
                    _endPosition = tempPos;

                    Quaternion tempRot = _startRotation;
                    _startRotation = _endRotation;
                    _endRotation = tempRot;
                }
            }

            if (_cicleDuration > 0f)
                _splineProgress = _goingForward ? (_splineProgress + (Time.deltaTime / _cicleDuration)) : (_splineProgress - (Time.deltaTime / _cicleDuration));

            //Avoid getting stuck on high speeds
            if (followerBehaviour == SplineFollowerBehaviour.Loop)
            {
                int splinesCount = _normalizedOrientedPoints.Length;
                _splineProgress = (_goingForward && _splineProgress > splinesCount) ? (_splineProgress % splinesCount) : ((!_goingForward && _splineProgress < -splinesCount) ? (_splineProgress % -splinesCount) : _splineProgress);
            }

            // Reached end of spline
            if ((_goingForward && _splineProgress > 1f) || (!_goingForward && _splineProgress < 0f))
            {
                _lastSplineIndex = _currentSplineIndex;

                // Moving to next spline
                if ((_goingForward && _currentSplineIndex < splines.Count - 1) || (!_goingForward && _currentSplineIndex > 0))
                {
                    _currentSplineIndex = _goingForward ? (_currentSplineIndex + 1) : (_currentSplineIndex - 1);
                    _splineProgress = CalculateProgressOnSplineTransition(_lastSplineIndex, _currentSplineIndex);
                    CalculateCicleDuration();

                    if (cycleEndStops == SplineFollowerStops.EachSpline)
                        _currentStopTime = cycleStopTime;
                }
                else //Reached end of spline list
                {
                    switch (followerBehaviour)
                    {
                        case SplineFollowerBehaviour.StopAtTheEnd:
                            _splineProgress = _goingForward ? 1f : 0f;
                            OnStopAtTheEnd();
                            break;
                        case SplineFollowerBehaviour.Loop:
                            _currentSplineIndex = _goingForward ? 0 : splines.Count - 1;
                            _splineProgress = CalculateProgressOnSplineTransition(_lastSplineIndex, _currentSplineIndex);
                            CalculateCicleDuration();
                            OnLoop();
                            break;
                        case SplineFollowerBehaviour.BackAndForward:
                            _splineProgress = _goingForward ? 1f : 0f;
                            speed *= -1;
                            _goingForward = !_goingForward;
                            OnBackAndForward();
                            break;
                    }

                    if (cycleEndStops == SplineFollowerStops.LastSpline || cycleEndStops == SplineFollowerStops.EachSpline)
                        _currentStopTime = cycleStopTime;
                }
            }

            _lastFramePointIndex = _currentPointIndex;

            _currentPointIndex = CalculateStartPointIndex();
            CalculateSegmentPositionsAndDirection(!_goingForward);

            _segmentPercent = (1f / (_normalizedOrientedPoints[_currentSplineIndex].Length - 1));

            if (_goingForward)
                _segmentProgress = _splineProgress - (_currentPointIndex * _segmentPercent);
            else
                _segmentProgress = _splineProgress - (_nextPointIndex * _segmentPercent);

            _segmentProgress = _segmentProgress / _segmentPercent;
            _segmentProgress = _goingForward ? _segmentProgress : (1f - _segmentProgress);

            _currentPosition = Vector3.Lerp(_startPosition, _endPosition, _segmentProgress);
            _currentRotation = Quaternion.Slerp(_startRotation, _endRotation, _segmentProgress);

            _transform = _transform == null ? GetComponent<Transform>() : _transform;

            _transform.position = _currentPosition;
            if (applySplineRotation) _transform.rotation = _currentRotation;

            UpdateLinkedFollowers();
        }

        /// <summary>
        /// Update linked followers positions along the splines
        /// </summary>
        private void UpdateLinkedFollowers()
        {
            if (linkedFollowers != null)
            {
                float acumulatedOffset = 0;
                for (int i = 0; i < linkedFollowers.Count; i++)
                {
                    acumulatedOffset = CalculateLinkedFollowersAcumulatedOffset(i, acumulatedOffset);
                    linkedFollowers[i].FollowMaster(acumulatedOffset);
                }
            }
        }

        /// <summary>
        /// Since splines can have different lengths, the spline progress needs to be proportionally adjusted when transitioning between splines
        /// </summary>
        /// <param name="lastSplineIndex"></param>
        /// <param name="currentSplineIndex"></param>
        /// <returns></returns>
        private float CalculateProgressOnSplineTransition(int lastSplineIndex, int currentSplineIndex)
        {
            float newSplineProgress = _splineProgress;

            if (ValidateOrientedPoints(lastSplineIndex) && ValidateOrientedPoints(currentSplineIndex))
            {
                float newSplineRatio = CalculateSplineTransitionRatio(lastSplineIndex, currentSplineIndex);
                newSplineProgress = _goingForward ? ((_splineProgress - 1f) * newSplineRatio) : (1f - (Mathf.Abs(_splineProgress) * newSplineRatio));
            }

            return newSplineProgress;
        }

        private float CalculateSplineTransitionRatio(int lastSplineIndex, int currentSplineIndex)
        {
            return splines[lastSplineIndex].GetTotalDistance(true) / splines[currentSplineIndex].GetTotalDistance(true);
        }

        /// <summary>
        /// Calculates segment start point index based on follower direction
        /// </summary>
        /// <returns></returns>
        private int CalculateStartPointIndex()
        {
            int ret = 0;

            if (ValidateOrientedPoints(_currentSplineIndex))
            {
                ret = (int)((_normalizedOrientedPoints[_currentSplineIndex].Length - 1) * _splineProgress);

                if (_goingForward)
                {
                    ret = (ret >= _normalizedOrientedPoints[_currentSplineIndex].Length - 1) ? (_normalizedOrientedPoints[_currentSplineIndex].Length - 2) : ret;
                    ret = ret < 0 ? 0 : ret;
                }
                else
                {
                    ret = (ret >= _normalizedOrientedPoints[_currentSplineIndex].Length - 1) ? (_normalizedOrientedPoints[_currentSplineIndex].Length - 1) : ret;
                    ret = ret < _normalizedOrientedPoints[_currentSplineIndex].Length - 1 ? (ret + 1) : ret;
                    ret = ret <= 1 ? 1 : ret;
                }
            }

            return ret;
        }

        /// <summary>
        /// Calculate current seggment position and direction for the follow terrain method
        /// </summary>
        /// <param name="reversed"></param>
        private void CalculateSegmentPositionsAndDirection(bool reversed = false)
        {
            if (CurrentPointChanged || splines[_currentSplineIndex].StaticSpline == false)
            {
                _nextPointIndex = reversed ? (_currentPointIndex - 1) : (_currentPointIndex + 1);
                //Out of bounds safety check
                _nextPointIndex = _nextPointIndex < 0 ? _currentPointIndex : (_nextPointIndex >= _normalizedOrientedPoints[_currentSplineIndex].Length ? _currentPointIndex : _nextPointIndex);

                if (ValidateOrientedPoints(_currentSplineIndex))
                {
                    _startPosition = _normalizedOrientedPoints[_currentSplineIndex][_currentPointIndex].Position;
                    _startRotation = _normalizedOrientedPoints[_currentSplineIndex][_currentPointIndex].Rotation;

                    _endPosition = _normalizedOrientedPoints[_currentSplineIndex][_nextPointIndex].Position;
                    _endRotation = _normalizedOrientedPoints[_currentSplineIndex][_nextPointIndex].Rotation;
                }
            }
        }

        /// <summary>
        /// Calculate cicle duration to keep consistent speed
        /// </summary>
        private void CalculateCicleDuration()
        {
            if (ValidateOrientedPoints(_currentSplineIndex))
            {
                _currentSplineLength = splines[_currentSplineIndex].GetTotalDistance(true);
                _cicleDuration = speed == 0f ? 0f : _currentSplineLength / Mathf.Abs(GetConvertedSpeedUnit());
            }
        }

        /// <summary>
        /// Convert speed to selected Speed Unit
        /// </summary>
        /// <returns></returns>
        public float GetConvertedSpeedUnit()
        {
            float convertedSpeed = 0f;

            switch (speedUnit)
            {
                case SplineFollowerSpeedUnit.ms:
                    convertedSpeed = speed;
                    break;
                case SplineFollowerSpeedUnit.kph:
                    convertedSpeed = (speed / 3.6f);
                    break;
                case SplineFollowerSpeedUnit.mph:
                    convertedSpeed = (speed / 2.237f);
                    break;
                case SplineFollowerSpeedUnit.kn:
                    convertedSpeed = (speed / 1.944f);
                    break;
            }

            return convertedSpeed;
        }

        /// <summary>
        /// Validates if oriented points were generated
        /// </summary>
        /// <param name="splineIndex"></param>
        /// <returns></returns>
        private bool ValidateOrientedPoints(int splineIndex)
        {
            // Out of bounds and null validation
            if (splines == null || splineIndex < 0 || splineIndex > splines.Count - 1)
                return false;

            // Control points validation
            if (splines[splineIndex].OrientedPoints == null
                || splines[splineIndex].OrientedPoints.Length == 0
                || (splines[splineIndex].StaticSpline == false && splines[splineIndex].CheckChanges()))
            {
                float splineLength = splines[splineIndex].GetTotalDistance();
                float optimalSpacing = splineLength / (int)splineLength;
                _normalizedOrientedPoints[splineIndex] = splines[splineIndex].CalculateOrientedPoints(optimalSpacing, true);
            }

            return true;
        }

        /// <summary>
        /// Move object to start position
        /// </summary>
        public void MoveToStartPosition()
        {
            if (splines == null || splines.Count == 0)
                return;

            NormalizeOrientedPoints();

            float t = customStartPosition * 0.01f;
            OrientedPoint[] points = _normalizedOrientedPoints[0];

            _currentPointIndex = (int)(points.Length * t);
            OrientedPoint start = points[_currentPointIndex];

            _transform = _transform == null ? GetComponent<Transform>() : _transform;

            _transform.position = start.Position;
            if (applySplineRotation)
                _transform.rotation = start.Rotation;

            MoveLinkedFollowersToStartPosition();
        }

        /// <summary>
        /// Set linked followers to their initial positions along the splines
        /// </summary>
        private void MoveLinkedFollowersToStartPosition()
        {
            if (linkedFollowers != null)
            {
                float acumulatedOffset = 0;
                for (int i = 0; i < linkedFollowers.Count; i++)
                {
                    linkedFollowers[i].Master = this;
                    linkedFollowers[i].FollowerBehaviour = linkedFollowersBehaviour;
                    acumulatedOffset = CalculateLinkedFollowersAcumulatedOffset(i, acumulatedOffset);
                    linkedFollowers[i].MoveToStartPosition(acumulatedOffset);
                }
            }
        }

        private float CalculateLinkedFollowersAcumulatedOffset(int index, float acumulatedOffset)
        {
            acumulatedOffset = index == 0 ? followersOffset + (linkedFollowers[index].followingOffset / 2f) : (acumulatedOffset + (linkedFollowers[index].followingOffset / 2f) + (linkedFollowers[index - 1].followingOffset / 2f));
            return acumulatedOffset;
        }

        /// <summary>
        /// Restarts follower to its initial position
        /// </summary>
        public void RestartFollower()
        {
            InitializeFollower();
        }

        /// <summary>
        /// Executes when follower reaches the end of the spline list and follow behaviour is set to BACK AND FORWARD
        /// </summary>
        protected virtual void OnBackAndForward()
        {
        }

        /// <summary>
        /// Executes when follower reaches the end of the spline list and follow behaviour is set to LOOP
        /// </summary>
        protected virtual void OnLoop()
        {
        }

        /// <summary>
        /// Executes when follower reaches the end of the spline list and follow behaviour is set to STOP AT THE END
        /// </summary>
        protected virtual void OnStopAtTheEnd()
        {
        }
    }
}
