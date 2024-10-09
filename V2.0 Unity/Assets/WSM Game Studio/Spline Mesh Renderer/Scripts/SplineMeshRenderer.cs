using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;

namespace WSMGameStudio.Splines
{
    [System.Serializable]
    [ExecuteInEditMode]
    [RequireComponent(typeof(LODGroup))]
    public class SplineMeshRenderer : MonoBehaviour
    {
        #region VARIABLES
        //Mesh generation parameters
        [SerializeField] private Spline _spline;
        [SerializeField] private MeshGenerationMethod _meshGenerationMethod;
        [SerializeField] private SMR_MeshGenerationProfile _meshGenerationProfile;
        [SerializeField] private Vector3 _meshOffset;
        [SerializeField] private bool _autoSplit = true;
        [Range(3, 65000)]
        [SerializeField] private int _verticesLimit = 65000;
        [SerializeField] private bool _startCap = true;
        [SerializeField] private bool _endCap = true;
        //Generated Objects
        [SerializeField] private IGeneratedMesh[] _generatedMeshes;
        [SerializeField] private IGeneratedMesh[] _generatedColliders;

        [System.NonSerialized] private float _splineLength;
        [System.NonSerialized] private Transform _auxTransform1;
        [System.NonSerialized] private Transform _auxTransform2;
        [System.NonSerialized] private LODGroup _LODGroup;
        //Base mesh scanned values
        [System.NonSerialized] private float _baseMeshLength;
        [System.NonSerialized] private float _baseMeshMinZ;
        [System.NonSerialized] private float _baseMeshMaxZ;
        [System.NonSerialized] private Vector3 _verticeScanned = new Vector3();
        [System.NonSerialized] private int _baseMesh_VertexCount;
        [System.NonSerialized] private int _baseMesh_SubMeshCount;
        [System.NonSerialized] private Vector3[] _baseMesh_vertices;
        [System.NonSerialized] private Vector3[] _baseMesh_normals;
        [System.NonSerialized] private Vector4[] _baseMesh_tangents;
        [System.NonSerialized] private Vector2[] _baseMesh_uv;
        //Add Mesh Segment
        [System.NonSerialized] private int[] _segmentIndices;
        //Create Triangle
        [System.NonSerialized] private Vector3[] _triangleVertices;
        [System.NonSerialized] private Vector3[] _triangleNormals;
        [System.NonSerialized] private Vector2[] _triangleUvs;
        [System.NonSerialized] private Vector4[] _triangleTangents;
        [System.NonSerialized] private Vector3 _triangleVerticesStart;
        [System.NonSerialized] private Vector3 _triangleVerticesEnd;
        [System.NonSerialized] private Vector3 _triangleNormalsStart;
        [System.NonSerialized] private Vector3 _triangleNormalsEnd;
        [System.NonSerialized] private Vector4 _triangleTangentsStart;
        [System.NonSerialized] private Vector4 _triangleTangentsEnd;
        [System.NonSerialized] private Matrix4x4 _triangleLocalToWorld_Start;
        [System.NonSerialized] private Matrix4x4 _triangleLocalToWorld_End;
        [System.NonSerialized] private Matrix4x4 _triangleWorldToLocal;
        //Mesh generations values
        [System.NonSerialized] private List<Vector3> _vertices = new List<Vector3>();
        [System.NonSerialized] private List<Vector3> _normals = new List<Vector3>();
        [System.NonSerialized] private List<Vector2> _uvs = new List<Vector2>();
        [System.NonSerialized] private List<Vector4> _tangents = new List<Vector4>();
        [System.NonSerialized] private List<List<int>> _subMeshTriangles = new List<List<int>>();
        #endregion

        #region PROPERTIES

        public Spline Spline
        {
            get { return _spline; }
            set
            {
                _spline = value;
                ExtrudeMesh();
            }
        }

        public MeshGenerationMethod MeshGenerationMethod
        {
            get { return _meshGenerationMethod; }
            set { _meshGenerationMethod = value; }
        }

        public SMR_MeshGenerationProfile MeshGenerationProfile
        {
            get { return _meshGenerationProfile; }
            set
            {
                _meshGenerationProfile = value;
                ExtrudeMesh();
            }
        }

        public Vector3 MeshOffset
        {
            get { return _meshOffset; }
            set { _meshOffset = value; }
        }

        public IGeneratedMesh[] GeneratedMeshes
        {
            get { return _generatedMeshes; }
        }

        public IGeneratedMesh[] GeneratedColliders
        {
            get { return _generatedColliders; }
        }

        public bool AutoSplit
        {
            get { return _autoSplit; }
            set { _autoSplit = value; }
        }

        public int VerticesLimit
        {
            get { return _verticesLimit; }
            set { _verticesLimit = Mathf.Clamp(value, 0, 65000); }
        }

        public bool StartCap
        {
            get { return _startCap; }
            set { _startCap = value; }
        }

        public bool EndCap
        {
            get { return _endCap; }
            set { _endCap = value; }
        }

        #endregion

        /// <summary>
        /// On Enable
        /// </summary>
        void OnEnable()
        {
            if (_spline == null)
                _spline = GetComponent<Spline>();

            _LODGroup = GetComponent<LODGroup>();
            GetAuxTranforms();
        }

        /// <summary>
        /// Once per frame
        /// </summary>
        private void Update()
        {
            //EDITOR ONLY REALTIME MESH GENERATION
            if (!Application.isPlaying)
            {
                if (_meshGenerationMethod == MeshGenerationMethod.Realtime)
                {
                    int activeGameObjectID = 0;
#if UNITY_EDITOR
                    activeGameObjectID = UnityEditor.Selection.activeInstanceID;
#endif
                    if (gameObject.GetInstanceID() == activeGameObjectID)
                        RealtimeMeshGeneration();
                }
            }
        }

        /// <summary>
        /// Get aux tranforms used for mesh generation
        /// </summary>
        private void GetAuxTranforms()
        {
            foreach (Transform child in transform)
            {
                switch (child.name)
                {
                    case "Aux1":
                        _auxTransform1 = child;
                        break;
                    case "Aux2":
                        _auxTransform2 = child;
                        break;
                }
            }

            //If not found, create them
            if (_auxTransform1 == null)
                _auxTransform1 = CreateAuxiliarTransform("Aux1");
            if (_auxTransform2 == null)
                _auxTransform2 = CreateAuxiliarTransform("Aux2");
        }

        /// <summary>
        /// Create auxiliar transform if not found
        /// Usefull for when user add Spline Mesh Renderer component manually
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Transform CreateAuxiliarTransform(string name)
        {
            GameObject aux = new GameObject(name);
            Transform auxTransform = aux.GetComponent<Transform>();
            auxTransform.SetParent(this.transform);
            auxTransform.localPosition = Vector3.zero;
            auxTransform.localRotation = Quaternion.identity;

            return auxTransform;
        }

        /// <summary>
        /// Reset auxiliar tranforms values
        /// </summary>
        private void ResetAuxTranforms()
        {
            Profiler.BeginSample("ResetAuxTranforms");

            if (_auxTransform1 == null)
                GetAuxTranforms();

            if (_auxTransform1 == null) //Avoid error if not found yet
                return;

            _auxTransform1.position = Vector3.zero;
            _auxTransform1.rotation = new Quaternion();
            _auxTransform2.position = Vector3.zero;
            _auxTransform2.rotation = new Quaternion();

            Profiler.EndSample();
        }

        /// <summary>
        /// Realtime Mesh Generation (EDITOR ONLY)
        /// </summary>
        private void RealtimeMeshGeneration()
        {
            ExtrudeMesh();
        }

        /// <summary>
        /// Extrude base mesh along spline
        /// </summary>
        public void ExtrudeMesh()
        {
            Profiler.BeginSample("ExtrudeMesh");

            if (_meshGenerationProfile == null)
            {
                Debug.LogWarning(string.Format("{0}: Mesh Generation Profile Cannot be null. Please Select a profile and try again.", gameObject.name));
                return;
            }

            bool exceededVertsLimit = false;
            int autoSplitIndex = 0;

            ScanGenerationProfile();
            ConfigureLODGroup();
            //Generate mesh LODs
            ProceduralMeshGeneration(_meshGenerationProfile.baseMeshLODS, _generatedMeshes, out exceededVertsLimit, out autoSplitIndex);

            if (exceededVertsLimit && _autoSplit)
            {
                _spline.SplitSpline(autoSplitIndex);
            }
            else
            {
                //Generate colliders
                ProceduralMeshGeneration(_meshGenerationProfile.meshColliders, _generatedColliders, out exceededVertsLimit, out autoSplitIndex);
                //Force Physics Engine to update mesh colliders (Temporary Fix)
                ProceduralMeshGeneration(_meshGenerationProfile.meshColliders, _generatedColliders, out exceededVertsLimit, out autoSplitIndex);

                SpawnCaps(); // Update caps for consistency (if any)
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// Scan generation profile settings
        /// </summary>
        private void ScanGenerationProfile()
        {
            //Renderers
            _generatedMeshes = gameObject.GetComponentsInChildren<SMR_GeneratedMesh>();
            _generatedColliders = gameObject.GetComponentsInChildren<SMR_GeneratedCollider>();

            _generatedMeshes = EqualizeBaseAndTargetArrays(_meshGenerationProfile.baseMeshLODS, _generatedMeshes, "Mesh_LOD");
            _generatedColliders = EqualizeBaseAndTargetArrays(_meshGenerationProfile.meshColliders, _generatedColliders, "Collider_", true);

            //Materials
            foreach (SMR_GeneratedMesh item in _generatedMeshes)
            {
                item.SetMaterials(_meshGenerationProfile.materials);
            }

            //Physics Materials
            int index = 0;
            foreach (SMR_GeneratedCollider item in _generatedColliders)
            {
                if (_meshGenerationProfile.physicMaterials != null && _meshGenerationProfile.physicMaterials.Length > index)
                    item.SetPhysicMaterial(_meshGenerationProfile.physicMaterials[index]);
                else
                    item.SetPhysicMaterial(null);

                index++;
            }
        }

        /// <summary>
        /// Configure LOD Group based generated meshes
        /// </summary>
        private void ConfigureLODGroup()
        {
            _LODGroup = _LODGroup == null ? GetComponent<LODGroup>() : _LODGroup;
            _LODGroup = _LODGroup == null ? gameObject.AddComponent<LODGroup>() : _LODGroup;

            LOD[] lods = new LOD[_generatedMeshes.Length];

            for (int i = 0; i < _generatedMeshes.Length; i++)
            {
                Renderer[] renderers = new Renderer[1];
                renderers[0] = _generatedMeshes[i].GetGameObject.GetComponent<Renderer>();
                float transitionHeight = (i < _generatedMeshes.Length - 1) ? (1.0f / (i + 1.5f)) : _meshGenerationProfile.cullingRatio;
                lods[i] = new LOD(transitionHeight, renderers);
            }

            _LODGroup.SetLODs(lods);
            _LODGroup.RecalculateBounds();
        }

        /// <summary>
        /// Generates and applies meshes to target child objects
        /// </summary>
        /// <param name="baseMeshes">Generation profile meshes</param>
        /// <param name="targetMeshes">Target objects on the scene</param>
        private void ProceduralMeshGeneration(Mesh[] baseMeshes, IGeneratedMesh[] targetMeshes, out bool exceededVertsLimit, out int autoSplitIndex)
        {
            bool notUVMapped = false;
            exceededVertsLimit = false;
            autoSplitIndex = 0;

            for (int i = 0; i < targetMeshes.Length; i++)
            {
                ResetAuxTranforms();

                if (_auxTransform1 != null)
                {
                    //Reset mesh for new mesh creation
                    ResetMeshValues();
                    //Scan mesh
                    BaseMeshScan(baseMeshes[i], out notUVMapped);

                    if (notUVMapped)
                    {
                        Debug.LogWarning(string.Format("{0} is not UV mapped. Mesh segment must be UV mapped for mesh generation to work.", baseMeshes[i].name));
                        continue;
                    }

                    //Create extruded mesh
                    CreateMesh(baseMeshes[i], out exceededVertsLimit, out autoSplitIndex);

                    if (exceededVertsLimit)
                    {
                        UpdateMeshRenderer(targetMeshes[i].Mesh); //Update mesh to identify where generation stopped
                        break;
                    }

                    //Update mesh renderer
                    UpdateMeshRenderer(targetMeshes[i].Mesh);
                }
            }
        }

        /// <summary>
        /// MAkes sure there is a one-to-one relationship between the profile base meshes and the target child objects
        /// </summary>
        /// <param name="baseMeshes"></param>
        /// <param name="targetMeshes"></param>
        /// <returns></returns>
        private IGeneratedMesh[] EqualizeBaseAndTargetArrays(Mesh[] baseMeshes, IGeneratedMesh[] targetMeshes, string namePrefix, bool isMeshCollider = false)
        {
            int baseMeshCount = baseMeshes == null ? 0 : baseMeshes.Length;
            if (baseMeshCount != targetMeshes.Length)
            {
                List<IGeneratedMesh> meshList = targetMeshes.ToList();

                if (baseMeshCount > targetMeshes.Length)
                {
                    for (int i = targetMeshes.Length; i < baseMeshes.Length; i++)
                    {
                        GameObject newGenMesh = new GameObject();
                        newGenMesh.transform.SetParent(transform);
                        newGenMesh.transform.localPosition = Vector3.zero;
                        newGenMesh.transform.localRotation = Quaternion.identity;
                        newGenMesh.name = isMeshCollider ? string.Format("{0}{1}", namePrefix, i + 1) : string.Format("{0}{1}", namePrefix, i);
                        if (isMeshCollider)
                        {
                            SMR_GeneratedCollider genMeshScript = newGenMesh.AddComponent<SMR_GeneratedCollider>();
                            meshList.Add(genMeshScript);
                            newGenMesh.AddComponent<SMR_IgnoredObject>();
                        }
                        else
                        {
                            SMR_GeneratedMesh genMeshScript = newGenMesh.AddComponent<SMR_GeneratedMesh>();
                            meshList.Add(genMeshScript);
                        }
                    }

                }
                else if (baseMeshCount < targetMeshes.Length)
                {
                    int diff = targetMeshes.Length - baseMeshCount;
                    int lastIndex = targetMeshes.Length - 1;
                    for (int i = lastIndex; i > (lastIndex - diff); i--)
                    {
                        meshList.RemoveAt(i);
                        DestroyImmediate(targetMeshes[i].GetGameObject);
                    }
                }

                targetMeshes = meshList.ToArray();
            }

            return targetMeshes;
        }

        /// <summary>
        /// Remove old mesh values
        /// </summary>
        private void ResetMeshValues()
        {
            Profiler.BeginSample("ResetMeshValues");

            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _tangents.Clear();
            _subMeshTriangles.Clear();

            Profiler.EndSample();
        }

        /// <summary>
        /// Scan base mesh for lenght
        /// </summary>
        private void BaseMeshScan(Mesh baseMesh, out bool notUVMapped)
        {
            Profiler.BeginSample("BaseMeshScan");

            notUVMapped = false;
            if (baseMesh.uv == null || baseMesh.uv.Length == 0)
            {
                notUVMapped = true;
                return;
            }

            float min_z = 0.0f;
            float max_z = 0.0f;
            float min_x = 0.0f;
            float max_x = 0.0f;
            _baseMesh_VertexCount = baseMesh.vertexCount;
            _baseMesh_SubMeshCount = baseMesh.subMeshCount;
            _baseMesh_vertices = baseMesh.vertices;
            _baseMesh_normals = baseMesh.normals;
            _baseMesh_tangents = baseMesh.tangents;
            _baseMesh_uv = baseMesh.uv;

            // find length
            for (int i = 0; i < _baseMesh_VertexCount; i++)
            {
                _verticeScanned = _baseMesh_vertices[i];
                min_z = (_verticeScanned.z < min_z) ? _verticeScanned.z : min_z;
                max_z = (_verticeScanned.z > max_z) ? _verticeScanned.z : max_z;
                min_x = (_verticeScanned.x < min_x) ? _verticeScanned.x : min_x;
                max_x = (_verticeScanned.x > max_x) ? _verticeScanned.x : max_x;
            }

            _baseMeshMinZ = min_z;
            _baseMeshMaxZ = max_z;
            _baseMeshLength = max_z - min_z;

            Profiler.EndSample();
        }

        /// <summary>
        /// Calculate mesh generation parameters
        /// </summary>
        private void CreateMesh(Mesh baseMesh, out bool exceededVertsLimit, out int autoSplitIndex)
        {
            Profiler.BeginSample("CreateMesh");

            exceededVertsLimit = false;
            autoSplitIndex = 0;

            if (_spline == null)
                return;

            if (_auxTransform1 != null)
                GetAuxTranforms();

            if (_auxTransform1 == null)
                return;

            _splineLength = _spline.Length;

            // 2.0 Mesh generation
            _spline.CalculateOrientedPoints(_baseMeshLength);

            int penultimateIndex = _spline.OrientedPoints.Length - 2;

            for (int pointIndex = 90; pointIndex <= penultimateIndex; pointIndex++)
            {
                // Set aux transform 1
                _auxTransform1.rotation = Quaternion.LookRotation(_spline.OrientedPoints[pointIndex].Forward, _spline.OrientedPoints[pointIndex].Up);
                _auxTransform1.position = _spline.OrientedPoints[pointIndex].Position + _meshOffset;

                // Set aux transform 2
                _auxTransform2.rotation = Quaternion.LookRotation(_spline.OrientedPoints[pointIndex + 1].Forward, _spline.OrientedPoints[pointIndex + 1].Up);
                _auxTransform2.position = _spline.OrientedPoints[pointIndex + 1].Position + _meshOffset;

                //exceededVertsLimit = false;
                AddMeshSegment(baseMesh, _auxTransform1, _auxTransform2, out exceededVertsLimit);
                if (exceededVertsLimit)
                {
                    autoSplitIndex = _spline.GetCurveStartingPointIndex(pointIndex);
                    _meshGenerationMethod = MeshGenerationMethod.Manual;
                    break;
                }
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// Add new mesh segment along the spline. Each segment corresponds to a copy of the base mesh
        /// </summary>
        private void AddMeshSegment(Mesh baseMesh, Transform start, Transform end, out bool exceededVertsLimit)
        {
            Profiler.BeginSample("AddMeshSegment");

            exceededVertsLimit = false;

            int indicesLength;

            //Each sub-mesh corresponds to a Material
            for (int subMeshIndex = 0; subMeshIndex < _baseMesh_SubMeshCount; subMeshIndex++)
            {
                _segmentIndices = baseMesh.GetIndices(subMeshIndex);
                indicesLength = _segmentIndices.Length;

                //Add new submesh (if needed)
                if (_subMeshTriangles.Count < subMeshIndex + 1)
                {
                    for (int i = _subMeshTriangles.Count; i < subMeshIndex + 1; i++)
                    {
                        _subMeshTriangles.Add(new List<int>());
                    }
                }

                //Triangle vertex indices
                for (int i = 0; i < indicesLength; i += 3)
                {
                    CreateTriangle(start, end, new int[] { _segmentIndices[i], _segmentIndices[i + 1], _segmentIndices[i + 2] }, subMeshIndex, out exceededVertsLimit);

                    if (exceededVertsLimit)
                        return;
                }
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// Recover vetorial values (Vector2, Vector3, Vector4)  
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="vectorArray"></param>
        /// <param name="indices"></param>
        /// <returns></returns>
        private T[] GetVector<T>(T[] vectorArray, int[] indices)
        {
            Profiler.BeginSample("GetVector");

            T[] ret = new T[3];

            for (int i = 0; i < 3; i++)
                ret[i] = vectorArray[indices[i]];

            Profiler.EndSample();
            return ret;
        }

        /// <summary>
        /// Create new triangle
        /// </summary>
        /// <param name="indices"></param>
        /// <param name="subMeshIndex"></param>
        private void CreateTriangle(Transform start, Transform end, int[] indices, int subMeshIndex, out bool exceededVertsLimit)
        {
            Profiler.BeginSample("CreateTriangle");

            exceededVertsLimit = false;

            Profiler.BeginSample("Create Triangle Variables");
            _triangleVertices = GetVector<Vector3>(_baseMesh_vertices, indices);
            _triangleNormals = GetVector<Vector3>(_baseMesh_normals, indices);
            _triangleUvs = GetVector<Vector2>(_baseMesh_uv, indices);
            _triangleTangents = GetVector<Vector4>(_baseMesh_tangents, indices);
            Profiler.EndSample();

            Profiler.BeginSample("Convert Local to World Space");
            _triangleLocalToWorld_Start = start.localToWorldMatrix;
            _triangleLocalToWorld_End = end.localToWorldMatrix;
            _triangleWorldToLocal = transform.worldToLocalMatrix;
            Profiler.EndSample();

            // apply offset
            float lerpValue = 0.0f;

            for (int i = 0; i < 3; i++)
            {
                lerpValue = GetLerpValue(_triangleVertices[i].z, _baseMeshMinZ, _baseMeshMaxZ, 0.0f, 1.0f);
                _triangleVertices[i].z = 0.0f;

                //Calculate vertices worlds positions and length
                _triangleVerticesStart = _triangleLocalToWorld_Start.MultiplyPoint(_triangleVertices[i]);
                _triangleVerticesEnd = _triangleLocalToWorld_End.MultiplyPoint(_triangleVertices[i]);
                _triangleVertices[i] = _triangleWorldToLocal.MultiplyPoint(Vector3.Lerp(_triangleVerticesStart, _triangleVerticesEnd, lerpValue));

                //Calculate normals worlds positions and length
                _triangleNormalsStart = _triangleLocalToWorld_Start.MultiplyVector(_triangleNormals[i]);
                _triangleNormalsEnd = _triangleLocalToWorld_End.MultiplyVector(_triangleNormals[i]);
                _triangleNormals[i] = _triangleWorldToLocal.MultiplyVector(Vector3.Lerp(_triangleNormalsStart, _triangleNormalsEnd, lerpValue));

                //Calculate tangents worlds positions and length
                _triangleTangentsStart = _triangleLocalToWorld_Start.MultiplyVector(_triangleTangents[i]);
                _triangleTangentsEnd = _triangleLocalToWorld_End.MultiplyVector(_triangleTangents[i]);
                _triangleTangents[i] = _triangleWorldToLocal.MultiplyVector(Vector3.Lerp(_triangleTangentsStart, _triangleTangentsEnd, lerpValue));
            }

            exceededVertsLimit = _autoSplit ? (_vertices.Count + _triangleVertices.Length > _verticesLimit) : (_vertices.Count + _triangleVertices.Length > 65000);

            if (exceededVertsLimit)
            {
                string warning = _autoSplit ? string.Format("Vertices limit reached. Auto split triggered.") : string.Format("Meshes cannot have more than 65000 vertices. {0}If you need to go even further, please use the Split Spline operation to reduce the size of your spline and keep building using the newly created spline OR enable the Auto Split Property", System.Environment.NewLine);

                warning += (_meshGenerationMethod == MeshGenerationMethod.Realtime) ? string.Format("{0}Realtime mesh generation disabled.", System.Environment.NewLine) : string.Empty;
                Debug.LogWarning(warning);
                return;
            }
            else
                AddTriangle(_triangleVertices, _triangleNormals, _triangleUvs, _triangleTangents, subMeshIndex);

            Profiler.EndSample();
        }

        /// <summary>
        /// Add created triangle
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="normals"></param>
        /// <param name="uvs"></param>
        /// <param name="tangents"></param>
        /// <param name="subMeshIndex"></param>
        public void AddTriangle(Vector3[] vertices, Vector3[] normals, Vector2[] uvs, Vector4[] tangents, int subMeshIndex)
        {
            Profiler.BeginSample("AddTriangle");

            int initialVertCount = _vertices.Count;
            int currentVertCount = initialVertCount;
            int lastSegmentStartIndex = (initialVertCount - _baseMesh_VertexCount);

            int newVertexIndex = 0;
            int duplicateIndex = 0;
            int duplicatesFound = 0;

            for (int i = 0; i < 3; i++)
            {
                if (!IsDuplicate(vertices[i], normals[i], uvs[i], lastSegmentStartIndex, initialVertCount, out duplicateIndex))
                {
                    _vertices.Add(vertices[i]);
                    _normals.Add(normals[i]);
                    _uvs.Add(uvs[i]);
                    _tangents.Add(tangents[i]);

                    currentVertCount++;

                    newVertexIndex = (initialVertCount + i - duplicatesFound);
                    _subMeshTriangles[subMeshIndex].Add(newVertexIndex); //New vertex added index
                }
                else
                {
                    duplicatesFound++;
                    _subMeshTriangles[subMeshIndex].Add(duplicateIndex); //Duplicated vertex, use original poligon index for the triangle
                }
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// Check for duplicated vertices
        /// </summary>
        /// <param name="vertice"></param>
        /// <param name="normal"></param>
        /// <param name="uv"></param>
        /// <param name="lastSegmentStartIndex"></param>
        /// <param name="initialVertCount"></param>
        /// <param name="originalIndex"></param>
        /// <returns></returns>
        private bool IsDuplicate(Vector3 vertice, Vector3 normal, Vector2 uv, int lastSegmentStartIndex, int initialVertCount, out int originalIndex)
        {
            Profiler.BeginSample("IsDuplicate");

            bool duplicated = false;
            originalIndex = 0;

            //First segment validation
            if (lastSegmentStartIndex < 0)
                lastSegmentStartIndex = 0;

            for (int i = lastSegmentStartIndex; i < initialVertCount; i++)
            {
                duplicated = (_vertices[i] == vertice && _normals[i] == normal && _uvs[i] == uv);
                if (duplicated)
                {
                    originalIndex = i;
                    break;
                }
            }

            Profiler.EndSample();
            return duplicated;
        }

        /// <summary>
        /// Update Mesh values
        /// </summary>
        public void UpdateMeshRenderer(Mesh targetMesh)
        {
            Profiler.BeginSample("UpdateMeshRenderer");

            targetMesh.Clear();
            targetMesh.SetVertices(_vertices);

            targetMesh.SetNormals(_normals);
            targetMesh.SetUVs(0, _uvs);
            targetMesh.SetUVs(1, _uvs);
            if (_tangents.Count > 1) targetMesh.SetTangents(_tangents);
            targetMesh.subMeshCount = _subMeshTriangles.Count;

            for (int i = 0; i < _subMeshTriangles.Count; i++)
                targetMesh.SetTriangles(_subMeshTriangles[i], i);

            //If not editing realtime, show mesh generation log
            if (_meshGenerationMethod == MeshGenerationMethod.Manual)
                PrintMeshDetails();

            Profiler.EndSample();
        }

        /// <summary>
        /// Print mesh details on console
        /// </summary>
        public void PrintMeshDetails()
        {
            if (_vertices.Count > 0)

                Debug.Log(string.Format("{9} Mesh Generated{0}Vertices: {1} Normals: {2} Uvs: {3} Tangents: {4} subMeshCount: {5} (Base Mesh Vertices: {6} Segments Count: {7} Length: {8}m)"
                                        , System.Environment.NewLine, _vertices.Count, _normals.Count, _uvs.Count, _tangents.Count, _baseMesh_SubMeshCount, _baseMesh_VertexCount, (_vertices.Count / _baseMesh_VertexCount), _splineLength.ToString("n0"), gameObject.name));
            else
                Debug.Log(string.Format("Could not generated mesh. Check warning messages for more details."));
        }

        /// <summary>
        /// Create new renderer at the end of the current one
        /// </summary>
        public GameObject ConnectNewRenderer()
        {
            if (_spline != null)
                return _spline.AppendSpline();
            else
                return null;
        }

        /// <summary>
        /// Spawn or update caps (if any)
        /// </summary>
        public void SpawnCaps()
        {
            if (_meshGenerationProfile == null)
                return;

            if (_spline == null)
                return;

            //Locate existing caps and update/delete based on generation profile
            SMR_MeshCapTag[] caps = GetComponentsInChildren<SMR_MeshCapTag>();

            if (caps != null)
            {
                for (int i = caps.Length - 1; i >= 0; i--)
                {
#if UNITY_EDITOR
                    DestroyImmediate(caps[i].gameObject);
#else
                    Destroy(caps[i].gameObject);
#endif
                }
            }

            // Instantiate caps
            if (!_spline.Loop)
            {
                if (_startCap) InstantiateCap(_meshGenerationProfile.startCap, "StartingCap", 0f);
                if (_endCap) InstantiateCap(_meshGenerationProfile.endCap, "EndingCap", 1f);
            }
        }

        /// <summary>
        /// Instantiate cap along spline
        /// </summary>
        /// <param name="prefab">Base prefab</param>
        /// <param name="name">Instance name</param>
        /// <param name="t">0f - spline start, 1f - spline end</param>
        private void InstantiateCap(GameObject prefab, string name, float t)
        {
            if (prefab != null)
            {
                Vector3 position = transform.InverseTransformPoint(_spline.GetPoint(t));
                Vector3 lookTarget = _spline.GetPoint(t) + _spline.GetDirection(t);

                GameObject cap = Instantiate(prefab, this.transform);
                cap.name = name;
                cap.transform.localPosition = position;
                cap.transform.LookAt(lookTarget);
                SMR_MeshCapTag capTag = cap.GetComponent<SMR_MeshCapTag>();
                if (capTag == null)
                    cap.AddComponent<SMR_MeshCapTag>();
            }
        }

        /// <summary>
        /// Custom Lerp
        /// </summary>
        /// <param name="value"></param>
        /// <param name="oldMin"></param>
        /// <param name="oldMax"></param>
        /// <param name="newMin"></param>
        /// <param name="newMax"></param>
        /// <returns></returns>
        private float GetLerpValue(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            return ((value - oldMin) / (oldMax - oldMin)) * (newMax - newMin) + newMin;
        }
    }
}
