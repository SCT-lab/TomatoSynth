using UnityEngine;

namespace WSMGameStudio.Splines
{
    public static class Convert
    {
        public static Quaternion Vector3ToQuaternion(Vector3 v3)
        {
            return Quaternion.Euler(v3);
        }

        public static Vector3 QuaternionToVector3(Quaternion q)
        {
            return q.eulerAngles;
        }

        public static Quaternion Vector4ToQuaternion(Vector4 v4)
        {
            return new Quaternion(v4.x, v4.y, v4.z, v4.w);
        }

        public static Vector4 QuaternionToVector4(Quaternion q)
        {
            return new Vector4(q.x, q.y, q.z, q.w);
        }
    }
}
