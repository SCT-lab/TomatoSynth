using UnityEngine;

namespace WSMGameStudio.Splines
{
    /// <summary>
    /// Bezier curve used in spline
    /// </summary>
    public static class Bezier
    {
        /// <summary>
        /// Get bezier point between three control points (QUADRATIC BEZIER)
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * p0 +
                2f * oneMinusT * t * p1 +
                t * t * p2;
        }

        /// <summary>
        /// Gets beziers first derivative (QUADRATIC BEZIER)
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return
                2f * (1f - t) * (p1 - p0) +
                2f * t * (p2 - p1);
        }

        /// <summary>
        /// Get bezier point between four control points (CUBIC BEZIER)
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * oneMinusT * p0 +
                3f * oneMinusT * oneMinusT * t * p1 +
                3f * oneMinusT * t * t * p2 +
                t * t * t * p3;
        }

        /// <summary>
        /// Gets beziers first derivative (CUBIC BEZIER)
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                3f * oneMinusT * oneMinusT * (p1 - p0) +
                6f * oneMinusT * t * (p2 - p1) +
                3f * t * t * (p3 - p2);
        }

        /// <summary>
        /// Get bezier point rotation (CUBIC BEZIER)
        /// </summary>
        /// <param name="r0"></param>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <param name="r3"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Quaternion GetPointRotation(Quaternion r0, Quaternion r1, Quaternion r2, Quaternion r3, float t)
        {
            Vector4 p0 = Convert.QuaternionToVector4(r0);
            Vector4 p1 = Convert.QuaternionToVector4(r1);
            Vector4 p2 = Convert.QuaternionToVector4(r2);
            Vector4 p3 = Convert.QuaternionToVector4(r3);
            Vector4 rotation;

            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            rotation = oneMinusT * oneMinusT * oneMinusT * p0 +
            3f * oneMinusT * oneMinusT * t * p1 +
            3f * oneMinusT * t * t * p2 +
            t * t * t * p3;

            return Convert.Vector4ToQuaternion(rotation);
        }
    }
}
