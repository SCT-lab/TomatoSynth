using UnityEngine;

namespace WSMGameStudio.Splines
{
    [System.Serializable]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class SMR_GeneratedMesh : MonoBehaviour, IGeneratedMesh
    {
        [HideInInspector] int ownerID;

        [SerializeField]
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        public Mesh Mesh
        {
            get
            {
                bool isOwner = (ownerID == gameObject.GetInstanceID());

                ownerID = gameObject.GetInstanceID();
                string meshName = string.Format("Mesh [{0}]", ownerID);

                _meshFilter = _meshFilter == null ? GetComponent<MeshFilter>() : _meshFilter;
                if (_meshFilter.sharedMesh == null || !isOwner || (_meshFilter.sharedMesh != null && _meshFilter.sharedMesh.name != meshName))
                {
                    _mesh = new Mesh();
                    _meshFilter.sharedMesh = _mesh;
                    _mesh.name = meshName;
                }
                return _mesh;
            }
        }

        public GameObject GetGameObject
        {
            get { return gameObject; }
        }

        public void SetMaterials(Material[] materials)
        {
            _meshRenderer = _meshRenderer == null ? GetComponent<MeshRenderer>() : _meshRenderer;
            _meshRenderer.sharedMaterials = materials;
        }
    }
}
