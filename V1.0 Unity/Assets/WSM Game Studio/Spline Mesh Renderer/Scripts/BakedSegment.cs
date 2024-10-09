using UnityEngine;
using UnityEngine.Serialization;

namespace WSMGameStudio.Splines
{
    public class BakedSegment : MonoBehaviour
    {
        [FormerlySerializedAs("endPoint")]
        [SerializeField] private Transform _endPoint;
        [SerializeField] private GameObject _operationTarget;

        public Transform EndPoint
        {
            get { return _endPoint; }
            set { _endPoint = value; }
        }

        public GameObject OperationTarget
        {
            get { return _operationTarget; }
            set { _operationTarget = value; }
        }

        /// <summary>
        /// Connect target at the end of this segment
        /// </summary>
        public void ConnectTarget()
        {
            ConnectTarget(_operationTarget);
        }

        /// <summary>
        /// Connect target at the end of this segment
        /// </summary>
        /// <param name="target"></param>
        public void ConnectTarget(GameObject target)
        {
            if (_endPoint == null)
            {
                Debug.Log(string.Format("{0}: End point not found", name));
                return;
            }

            if (target != null)
            {
                target.transform.position = _endPoint.position;
                target.transform.rotation = _endPoint.rotation;
            }

            ResetOperationTarget();
        }

        /// <summary>
        /// Reset operation target
        /// </summary>
        private void ResetOperationTarget()
        {
            _operationTarget = null;
        }
    } 
}
