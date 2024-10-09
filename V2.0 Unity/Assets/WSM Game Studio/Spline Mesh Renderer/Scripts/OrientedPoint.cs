using UnityEngine;

namespace WSMGameStudio.Splines
{
    [System.Serializable]
    public struct OrientedPoint
    {
        [SerializeField]
        private Vector3 _position;
        [SerializeField]
        private Quaternion _rotation;
        [SerializeField]
        private Vector3 _up;
        [SerializeField]
        private Vector3 _forward;

        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }
        
        public Quaternion Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        public Vector3 Up
        {
            get { return _up; }
            set { _up = value; }
        }

        public Vector3 Forward
        {
            get { return _forward; }
            set { _forward = value; }
        }

        public Vector3 Right
        {
            get { return Vector3.Cross(_up, _forward).normalized; }
        }

        public OrientedPoint(Vector3 position, Quaternion rotation)
        {
            _position = position;
            _rotation = rotation;
            _up = Vector3.up;
            _forward = Vector3.forward;
        }

        public OrientedPoint(Vector3 position, Quaternion rotation, Vector3 forward, Vector3 up)
        {
            _position = position;
            _rotation = rotation;
            _up = up;
            _forward = forward;
        }

        public Vector3 LocalToWorld(Vector3 point)
        {
            return _position + _rotation * point;
        }

        public Vector3 WorldToLocal(Vector3 point)
        {
            return Quaternion.Inverse(_rotation) * (point - _position);
        }

        public Vector3 LocalToWorldDirection(Vector3 direction)
        {
            return _rotation * direction;
        }
    } 
}
