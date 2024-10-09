using UnityEngine;

namespace WSMGameStudio.Splines
{
    public class LinkedSplineFollower : MonoBehaviour
    {
        public float followingOffset;

        private SplineFollower _master;
        private LinkedFollowerBehaviour _followerBehaviour;
        private Transform _transform;
        private Vector3 _startPosition;
        private Vector3 _endPosition;
        private Quaternion _startRotation;
        private Quaternion _endRotation;

        private int _currentSplineIndex;
        private int _currentPointIndex;
        private int _nextPointIndex;
        private float _segmentProgress = 0;

        public OrientedPoint[][] NormalizedOrientedPoints { get { return _master != null ? _master.NormalizedOrientedPoints : null; } }

        public SplineFollower Master
        {
            get { return _master; }
            set { _master = value; }
        }

        public LinkedFollowerBehaviour FollowerBehaviour
        {
            get { return _followerBehaviour; }
            set { _followerBehaviour = value; }
        }

        private bool HasOverflown
        {
            get { return _currentSplineIndex <= -1; }
        }

        protected void Start()
        {
            _transform = GetComponent<Transform>();
        }

        /// <summary>
        /// Follow master position. Invoked from master update method, to ensure synchronized movement
        /// </summary>
        /// <param name="followOffset"></param>
        public void FollowMaster(float followOffset)
        {
            if (NormalizedOrientedPoints == null) return; //Master is null or oriented points not calculated

            int nextIndexDirection = _master.GoingForward ? 1 : -1; //1 = forwards | -1 = backwards

            float masterProgress = _master.GoingForward ? Mathf.Clamp01(Master.SegmentProgress) : 1f - Mathf.Clamp01(Master.SegmentProgress);
            float targetPosition = _master.CurrentPointIndex + masterProgress - followOffset;

            _currentSplineIndex = _master.CurrentSplineIndex;
            _currentPointIndex = _master.GoingForward || _currentPointIndex > 0? Mathf.FloorToInt(targetPosition) : Mathf.CeilToInt(targetPosition);
            _nextPointIndex = _currentPointIndex + nextIndexDirection;

            _segmentProgress = Mathf.Abs(targetPosition - _currentPointIndex);
            _segmentProgress = _master.GoingForward ? _segmentProgress : 1f - _segmentProgress;

            // Calculate current spline and point index based on the follow offset
            while (_currentPointIndex < 0)
            {
                ChangeSpline(-1);

                if (_followerBehaviour == LinkedFollowerBehaviour.Overflow && HasOverflown)
                    break;

                _currentPointIndex = (NormalizedOrientedPoints[_currentSplineIndex].Length - 1) + _currentPointIndex;
                _nextPointIndex = _currentPointIndex + nextIndexDirection;
            }

            _transform = _transform == null ? GetComponent<Transform>() : _transform;

            if (_followerBehaviour == LinkedFollowerBehaviour.Overflow && HasOverflown)
            {
                CalculateOverflownPosition(followOffset);
            }
            else
            {
                //Validate next point on next spline
                if (_nextPointIndex > NormalizedOrientedPoints[_currentSplineIndex].Length - 1)
                {
                    ChangeSpline(1);
                    _currentPointIndex = 0;
                    _nextPointIndex = 1;
                }
                else if (_nextPointIndex < 0) //Validate next point on previous spline
                {
                    ChangeSpline(-1);

                    if (HasOverflown) //Next point has overflown
                    {
                        CalculateOverflownPosition(followOffset);
                        return;
                    }

                    _currentPointIndex = (NormalizedOrientedPoints[_currentSplineIndex].Length - 1);
                    _nextPointIndex = _currentPointIndex - 1;
                }

                _startPosition = NormalizedOrientedPoints[_currentSplineIndex][_currentPointIndex].Position;
                _endPosition = NormalizedOrientedPoints[_currentSplineIndex][_nextPointIndex].Position;

                _startRotation = NormalizedOrientedPoints[_currentSplineIndex][_currentPointIndex].Rotation;
                _endRotation = NormalizedOrientedPoints[_currentSplineIndex][_nextPointIndex].Rotation;

                _transform.position = Vector3.Lerp(_startPosition, _endPosition, _segmentProgress);
                _transform.rotation = Quaternion.Slerp(_startRotation, _endRotation, _segmentProgress);
            }
        }

        /// <summary>
        /// Calculate Overflown Position
        /// </summary>
        /// <param name="followOffset"></param>
        private void CalculateOverflownPosition(float followOffset)
        {
            _transform.position = _master.transform.position - (_master.transform.forward * followOffset);
            _transform.rotation = _master.transform.rotation;
        }

        /// <summary>
        /// Cicly through splines
        /// </summary>
        /// <param name="splineDirection"></param>
        private void ChangeSpline(int splineDirection)
        {
            _currentSplineIndex = _currentSplineIndex + splineDirection;

            if (_followerBehaviour == LinkedFollowerBehaviour.Wrap)
            {
                _currentSplineIndex = _currentSplineIndex < 0 ? (NormalizedOrientedPoints.Length - 1) : _currentSplineIndex; // Return to top
                _currentSplineIndex = _currentSplineIndex > (NormalizedOrientedPoints.Length - 1) ? 0 : _currentSplineIndex; // Return to bottom 
            }
            else if (_followerBehaviour == LinkedFollowerBehaviour.Overflow)
            {
                _currentSplineIndex = _currentSplineIndex < 0 ? -1 : _currentSplineIndex; // Overflow first spline. -1 used as reference value
                _currentSplineIndex = _currentSplineIndex > (NormalizedOrientedPoints.Length - 1) ? (NormalizedOrientedPoints.Length - 1) : _currentSplineIndex;
            }
        }

        /// <summary>
        /// Move linked follower to its initial position on the editor
        /// </summary>
        public void MoveToStartPosition(float followOffset)
        {
            FollowMaster(followOffset);
        }
    }
}
