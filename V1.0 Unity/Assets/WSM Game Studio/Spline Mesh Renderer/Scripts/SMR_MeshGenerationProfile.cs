using UnityEngine;

namespace WSMGameStudio.Splines
{
    [CreateAssetMenu(fileName = "New Mesh Generation Profile", menuName = "WSM Game Studio/Spline Mesh Renderer/Mesh Generation Profile", order = 1)]
    public class SMR_MeshGenerationProfile : ScriptableObject
    {
        [Range(0f, 1f)]
        public float cullingRatio = 0.05f;
        public Mesh[] baseMeshLODS;
        public Material[] materials;
        public Mesh[] meshColliders;
        public PhysicMaterial[] physicMaterials;
        public GameObject startCap;
        public GameObject endCap;
    } 
}
