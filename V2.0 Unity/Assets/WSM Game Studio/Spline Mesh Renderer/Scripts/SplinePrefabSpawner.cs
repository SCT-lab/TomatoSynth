using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WSMGameStudio.Splines
{
    [ExecuteInEditMode]
    public class SplinePrefabSpawner : MonoBehaviour
    {
        [Tooltip("Target splines")]
        public List<Spline> splines;
        [Tooltip("Prefab spawning method")]
        public SpawningMethod spawningMethod;
        [Tooltip("Number of instances to be spawned")]
        public int instances = 1;
        [Tooltip("Prefabs to be spawned along spline")]
        public GameObject[] prefabs;
        [Tooltip("(Optional) Spawn position offset relative to spline")]
        public Vector3 spawnOffset = Vector3.zero;

        private int _instanceID;
        private List<GameObject> _toDestroy;

        void OnEnable()
        {
            _instanceID = GetInstanceID();
        }

        /// <summary>
        /// Spawn prefabs along spline
        /// </summary>
        public void SpawnPrefabs()
        {
            if (splines == null || splines.Count == 0)
            {
                Debug.LogWarning(string.Format("{0}: Spline list cannot be empty", gameObject.name));
                return;
            }

            ResetObjects();

            if (spawningMethod == SpawningMethod.DisconnectedSplines)
            {
                foreach (Spline spline in splines)
                    Spawn(spline);
            }
            else if (spawningMethod == SpawningMethod.ConnectedSplines)
            {
                GameObject tempSpline = new GameObject();
                Spline mergedSpline = tempSpline.AddComponent<Spline>();
                mergedSpline.Theme = splines[0].Theme;
                mergedSpline.HandlesVisibility = HandlesVisibility.ShowAllHandles;

                tempSpline.transform.position = splines[0].transform.position;
                tempSpline.transform.rotation = splines[0].transform.rotation;

                mergedSpline.ControlPointsPositions = splines[0].ControlPointsPositions;
                mergedSpline.ControlPointsRotations = splines[0].ControlPointsRotations;
                mergedSpline.ControlPointsNormals = splines[0].ControlPointsNormals;
                mergedSpline.Modes = splines[0].Modes;

                for (int splineIndex = 1; splineIndex < splines.Count; splineIndex++)
                {
                    mergedSpline.Merge(splines[splineIndex].gameObject, false, false);
                }

                Spawn(mergedSpline);

                if (Application.IsPlaying(this))
                    Destroy(tempSpline); //Play mode or build
                else
                    DestroyImmediate(tempSpline); // Unity Editor
            }
        }

        private void Spawn(Spline targetSpline)
        {
            if (targetSpline == null)
            {
                Debug.Log("Please select a reference spline to spawn prefabs.");
                return;
            }

            instances = Mathf.Abs(instances);

            if (instances <= 0 || prefabs == null || prefabs.Length == 0)
                return;

            float stepSize = instances * prefabs.Length;
            float t;
            // if loop does not spawn a double at the end
            stepSize = (targetSpline.Loop || stepSize == 1) ? (1f / stepSize) : (1f / (stepSize - 1));

            GameObject newClone;
            Vector3 clonePosition;
            Quaternion cloneRotation;
            Vector3 cloneDirection;

            for (int positionIndex = 0, instanceIndex = 0; instanceIndex < instances; instanceIndex++)
            {
                for (int prefabIndex = 0; prefabIndex < prefabs.Length; prefabIndex++, positionIndex++)
                {
                    if (prefabs[prefabIndex] == null)
                    {
                        Debug.LogWarning(string.Format("{0}: Prefab entrance cannot be null. Please verify the prefabs references in the Inspector", gameObject.name));
                        continue;
                    }

                    newClone = Instantiate(prefabs[prefabIndex]);
                    t = positionIndex * stepSize;

                    ValidateOrientedPoints(targetSpline);

                    int index = targetSpline.GetClosestOrientedPointIndex(t);
                    clonePosition = targetSpline.OrientedPoints[index].Position;
                    cloneRotation = targetSpline.OrientedPoints[index].Rotation;

                    int nextIndex = index + 1;

                    if (nextIndex > targetSpline.OrientedPoints.Length - 1)
                    {
                        if (targetSpline.Loop)
                            nextIndex = 0;
                        else
                        {
                            nextIndex = index;
                            index--;
                        }
                    }

                    cloneDirection = (targetSpline.OrientedPoints[nextIndex].Position - targetSpline.OrientedPoints[index].Position).normalized;

                    newClone.transform.localPosition = clonePosition;
                    newClone.transform.rotation = cloneRotation;
                    newClone.transform.LookAt(clonePosition + cloneDirection, newClone.transform.up);
                    newClone.transform.parent = transform;
                    //Apply local offset. Axis are applied one at a time to move the position along the spline correctly
                    newClone.transform.localPosition += (newClone.transform.right * spawnOffset.x); //Apply X offset
                    newClone.transform.localPosition += (newClone.transform.up * spawnOffset.y); //Apply Y offset
                    newClone.transform.localPosition += (newClone.transform.forward * spawnOffset.z); //Apply Z offset

                    SplineFollower follower = newClone.GetComponent<SplineFollower>();
                    if (follower != null)
                        follower.customStartPosition = t * 100f;
                }
            }
        }

        /// <summary>
        /// Reset all objects
        /// </summary>
        public void ResetObjects()
        {
            _toDestroy = new List<GameObject>();

            //Get children to delete
            foreach (Transform child in transform)
            {
                if (child.gameObject.GetInstanceID() == _instanceID)
                    continue;

                _toDestroy.Add(child.gameObject);
            }

            //Delete objects
            for (int i = (_toDestroy.Count - 1); i >= 0; i--)
            {
                _toDestroy[i].SetActive(false);
                DestroyImmediate(_toDestroy[i].gameObject);
            }

            _toDestroy.Clear();
        }

        /// <summary>
        /// Make sure Oriented Points were calculated
        /// </summary>
        private void ValidateOrientedPoints(Spline targetSpline)
        {
            if (targetSpline.OrientedPoints == null || (targetSpline.OrientedPoints.Length == 0 || targetSpline.GetComponent<SplineMeshRenderer>() == null))
                targetSpline.CalculateOrientedPoints(1f);
        }
    }
}
