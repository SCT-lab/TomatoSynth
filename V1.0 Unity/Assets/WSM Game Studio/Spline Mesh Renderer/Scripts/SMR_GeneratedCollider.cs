using UnityEngine;

namespace WSMGameStudio.Splines
{
    [System.Serializable]
    [RequireComponent(typeof(MeshCollider))]
    public class SMR_GeneratedCollider : MonoBehaviour, IGeneratedMesh
    {
        [HideInInspector] int ownerID;

        [SerializeField]
        private Mesh _mesh;
        private MeshCollider _meshCollider;

        public Mesh Mesh
        {
            get
            {
                bool isOwner = (ownerID == gameObject.GetInstanceID());

                ownerID = gameObject.GetInstanceID();
                string meshName = string.Format("Mesh [{0}]", ownerID);

                _meshCollider = _meshCollider == null ? GetComponent<MeshCollider>() : _meshCollider;
                if (_meshCollider.sharedMesh == null || !isOwner || (_meshCollider.sharedMesh != null && _meshCollider.sharedMesh.name != meshName))
                {
                    _mesh = new Mesh();
                    _mesh.name = meshName;
                    _meshCollider.sharedMesh = _mesh;
                    _meshCollider.convex = false;
                }
                return _mesh;
            }
        }

        public GameObject GetGameObject
        {
            get { return gameObject; }
        }

        public void SetPhysicMaterial(PhysicMaterial physicMaterial)
        {
            _meshCollider = _meshCollider == null ? GetComponent<MeshCollider>() : _meshCollider;
            _meshCollider.sharedMaterial = physicMaterial;
        }
    } 
}
