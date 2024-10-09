#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif
using UnityEngine;

namespace WSMGameStudio.TerrainTools
{
    public class WSM_TerrainBackup : MonoBehaviour
    {
        /// <summary>
        /// Save terrain backup to disk
        /// </summary>
        public void SaveBackup()
        {
#if UNITY_EDITOR
            Terrain terrain = GetComponent<Terrain>();

            if (terrain == null) return;

            TerrainData originalData = terrain.terrainData;

            string originalDataFileName = AssetDatabase.GetAssetPath(originalData);
            string backupDataFileName = originalDataFileName.Replace(".asset", "_backup.asset");

            FileInfo fileinfo = new FileInfo(backupDataFileName);

            if (fileinfo != null)
            {
                AssetDatabase.CopyAsset(originalDataFileName, backupDataFileName);
            }
#endif
        }

        /// <summary>
        /// Load terrain backup from disk
        /// </summary>
        public void LoadBackup()
        {
#if UNITY_EDITOR
            Terrain terrain = GetComponent<Terrain>();

            if (terrain == null) return;

            TerrainData originalData = terrain.terrainData;

            string originalDataFileName = AssetDatabase.GetAssetPath(originalData);
            string backupDataFileName = originalDataFileName.Replace(".asset", "_backup.asset");

            FileInfo fileinfo = new FileInfo(backupDataFileName);

            if (fileinfo != null && fileinfo.Exists)
            {
                TerrainData backupData = AssetDatabase.LoadAssetAtPath<TerrainData>(backupDataFileName);

                if (backupData != null)
                {
                    // Heightmap
                    if (originalData.heightmapResolution == backupData.heightmapResolution)
                        originalData.SetHeights(0, 0, backupData.GetHeights(0, 0, backupData.heightmapResolution, backupData.heightmapResolution));
                    else
                        Debug.LogWarning(string.Format("{0} - Not possible to restore HEIGHTMAP due to resolution disparity. Current: {1}x{2} Backup: {3}x{4}", terrain.gameObject.name, originalData.heightmapResolution, originalData.heightmapResolution, backupData.heightmapResolution, backupData.heightmapResolution));

                    // Textures
                    if (originalData.alphamapWidth == backupData.alphamapWidth && originalData.alphamapHeight == backupData.alphamapHeight && originalData.alphamapLayers == backupData.alphamapLayers)
                        originalData.SetAlphamaps(0, 0, backupData.GetAlphamaps(0, 0, backupData.alphamapWidth, backupData.alphamapHeight));
                    else
                        Debug.LogWarning(string.Format("{0} - Not possible to restore TEXTURES due to resolution disparity. Current: {1}x{2} Backup: {3}x{4}. Original Layers: {5} Backup Layers: {6}", terrain.gameObject.name, originalData.alphamapWidth, originalData.alphamapHeight, backupData.alphamapWidth, backupData.alphamapHeight, originalData.alphamapLayers, backupData.alphamapLayers));
                    
                    //Trees and details are not supported yet
                    // Trees
                    //originalData.treeInstances = backupData.treeInstances;
                    // Grasses
                    //originalData.SetDetailLayer(0, 0, 0, backupData.GetDetailLayer(0, 0, backupData.detailWidth, backupData.detailHeight, 0));

                    terrain.terrainData.SyncHeightmap();
                }
            }
#endif
        }
    }
}
