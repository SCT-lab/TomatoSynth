using UnityEngine;

namespace WSMGameStudio.Splines
{
    public interface IGeneratedMesh
    {
        Mesh Mesh { get; }
        GameObject GetGameObject { get; }
    } 
}
