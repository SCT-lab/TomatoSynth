using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace WSMGameStudio.TerrainTools
{
    public static class WSM_TerrainTools
    {
        /// <summary>
        /// Backup terrain data that are affected by the terrain tools methods
        /// </summary>
        /// <param name="terrain"></param>
        public static void Backup(Terrain terrain)
        {
            WSM_TerrainBackup backup = terrain.GetComponent<WSM_TerrainBackup>();
            backup = backup == null ? terrain.gameObject.AddComponent<WSM_TerrainBackup>() : backup;

            backup.SaveBackup();
        }

        /// <summary>
        /// Restore last backup (if any)
        /// Only data that is affected by the terrain tools methods are restored
        /// </summary>
        /// <param name="terrain"></param>
        public static void RestoreBackup(Terrain terrain)
        {
            WSM_TerrainBackup backup = terrain.GetComponent<WSM_TerrainBackup>();

            if (backup == null) return;

            backup.LoadBackup();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="path"></param>
        /// <param name="size"></param>
        /// <param name="affectedCoordinates"></param>
        public static void AdjustHeightAlongPath(Terrain terrain, Vector3[] path, int size, out List<string> affectedCoordinates)
        {
            Profiler.BeginSample("AdjustHeightAlongPath");

            //Normalize size to avoid zero width flat area
            size = size + 1;

            affectedCoordinates = new List<string>();
            string tempCoord = string.Empty;

            Transform terrainTransform = terrain.GetComponent<Transform>();
            TerrainData terrainData = terrain.terrainData;
            Vector3 localPathPoint;
            Vector2 coordinates = Vector2.zero;
            float[,] terrainHeights;
            int xBase, yBase, width, height;

            for (int pointIndex = 0; pointIndex < path.Length; pointIndex++)
            {
                // Convert spline points into terrain coordinates
                localPathPoint = terrainTransform.InverseTransformPoint(path[pointIndex]);
                ConvertPointToCoordinates(terrainData, localPathPoint, (terrainData.heightmapResolution - 1), (terrainData.heightmapResolution - 1), out coordinates);

                CalculateAffectedAreaBoundaries(coordinates, size, terrainData.heightmapResolution, terrainData.heightmapResolution, out xBase, out yBase, out width, out height);

                // Use terrainData.GetHeights() to retrieve current terrain heights
                //The array has the dimensions[height, width] and is indexed as [y, x].
                terrainHeights = terrainData.GetHeights(xBase, yBase, width, height);

                // Calculate new height
                int heightArrayLength = terrainHeights.GetLength(0);
                int widthArrayLength = terrainHeights.GetLength(1);
                for (int y = 0; y < heightArrayLength; y++)
                {
                    for (int x = 0; x < widthArrayLength; x++)
                    {
                        terrainHeights[y, x] = CalculatePointHeight(terrainData, localPathPoint);

                        tempCoord = string.Format("{0}x{1}", xBase + x, yBase + y);

                        if (!affectedCoordinates.Contains(tempCoord))
                            affectedCoordinates.Add(tempCoord);
                    }
                }

                // Use terrainData.SetHeights/SetHeightsDelayLOD to apply the new heights
                terrainData.SetHeightsDelayLOD(xBase, yBase, terrainHeights);
            }

            // Apply modification Use Terrain.ApplyDelayedHeightmapModification (Unity 2018) / TerrainData.SyncHeightmap (Unity 2019)
            terrain.terrainData.SyncHeightmap();

            Profiler.EndSample();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="path"></param>
        /// <param name="size"></param>
        /// <param name="embankment"></param>
        /// <param name="embankmentSlope"></param>
        /// <param name="pathCoordinates"></param>
        public static void SmoothenSlopeAlongPath(Terrain terrain, Vector3[] path, int size, float embankment, AnimationCurve embankmentSlope, List<string> pathCoordinates)
        {
            if (embankment == 0)
                return;

            Profiler.BeginSample("SmoothenSlopeAlongPath");

            //Normalize size to avoid zero width flat area
            size += ((int)embankment * 2) + 1;

            string tempCoord = string.Empty;

            Transform terrainTransform = terrain.GetComponent<Transform>();
            TerrainData terrainData = terrain.terrainData;
            Vector3 localPathPoint;
            Vector2 coordinates = Vector2.zero;
            float[,] terrainOriginalHeights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            float[,] terrainHeights;
            int xBase, yBase, width, height;

            for (int pointIndex = 0; pointIndex < path.Length; pointIndex++)
            {
                // Convert spline points into terrain coordinates
                localPathPoint = terrainTransform.InverseTransformPoint(path[pointIndex]);
                ConvertPointToCoordinates(terrainData, localPathPoint, (terrainData.heightmapResolution - 1), (terrainData.heightmapResolution - 1), out coordinates);

                CalculateAffectedAreaBoundaries(coordinates, size, terrainData.heightmapResolution, terrainData.heightmapResolution, out xBase, out yBase, out width, out height);

                // Use terrainData.GetHeights() to retrieve current terrain heights
                //The array has the dimensions[height, width] and is indexed as [y, x].
                terrainHeights = terrainData.GetHeights(xBase, yBase, width, height);

                // Calculate new height
                int heightArrayLength = terrainHeights.GetLength(0);
                int widthArrayLength = terrainHeights.GetLength(1);
                for (int y = 0; y < heightArrayLength; y++)
                {
                    for (int x = 0; x < widthArrayLength; x++)
                    {
                        tempCoord = string.Format("{0}x{1}", xBase + x, yBase + y);

                        if (!pathCoordinates.Contains(tempCoord))
                        {
                            terrainHeights[y, x] = CalculateSlopeHeight(terrainData, terrainOriginalHeights, localPathPoint, embankmentSlope, x, y, widthArrayLength - 1, heightArrayLength - 1, embankment, xBase, yBase);
                        }
                    }
                }

                // Use terrainData.SetHeights/SetHeightsDelayLOD to apply the new heights
                terrainData.SetHeightsDelayLOD(xBase, yBase, terrainHeights);
            }

            // Apply modification Use Terrain.ApplyDelayedHeightmapModification (Unity 2018) / TerrainData.SyncHeightmap (Unity 2019)
            terrain.terrainData.SyncHeightmap();

            Profiler.EndSample();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="path"></param>
        /// <param name="texture"></param>
        /// <param name="size"></param>
        /// <param name="embankment"></param>
        public static void PaintTextureAlongPath(Terrain terrain, Vector3[] path, Texture2D texture, int size, Texture2D embankmentTexture, float embankment, float minTextureBlending, float maxTextureBlending, float minEmbankmentTextureBlending, float maxEmbankmentTextureBlending)
        {
            if (texture == null)
                return;

            Profiler.BeginSample("PaintTextureAlongPath");

            int paintedArea;

            int strokeWidth, strokeHeight, texturesCount;

            Transform terrainTransform = terrain.GetComponent<Transform>();
            TerrainData terrainData = terrain.terrainData;

            Vector3 localPathPoint;
            Vector2 coordinates = Vector2.zero;
            float[,,] terrainAlphamap;
            int xBase, yBase, width, height;

            // Check if terrain does have corresponding texture
            int textureIndex = FindTextureIndex(texture, terrainData);
            if (textureIndex == -1)
            {
                Debug.LogWarning(string.Format("{0} - Corresponding terrain texture not found. Please make sure your terrain has a layer with the same diffuse map texture selected.", terrain.name));
                Profiler.EndSample();
                return;
            }

            /*
             * Paint embankment texture
             */
            int embankmentTextureIndex = FindTextureIndex(embankmentTexture, terrainData);
            if (embankmentTextureIndex == -1)
            {
                Debug.LogWarning(string.Format("{0} - Corresponding Embankment terrain texture not found. Please make sure your terrain has a layer with the same diffuse map texture selected.", terrain.name));
            }
            else // Embankment Texture found
            {
                paintedArea = size + ((int)embankment * 2) + 1;
                for (int pointIndex = 0; pointIndex < path.Length; pointIndex++)
                {
                    // Convert spline points into terrain coordinates
                    localPathPoint = terrainTransform.InverseTransformPoint(path[pointIndex]);
                    ConvertPointToCoordinates(terrainData, localPathPoint, (terrainData.alphamapWidth - 1), (terrainData.alphamapHeight - 1), out coordinates);

                    //CalculateAffectedAreaBoundaries(coordinates, paintedArea, terrainData.alphamapWidth, terrainData.alphamapHeight, out xBase, out yBase, out width, out height);
                    CalculateAffectedAreaBoundaries(coordinates, paintedArea, terrainData.alphamapWidth, terrainData.alphamapHeight, out xBase, out yBase, out width, out height, true);
                    // Retrieve current textures "blending"
                    terrainAlphamap = terrainData.GetAlphamaps(xBase, yBase, width, height);

                    strokeWidth = terrainAlphamap.GetLength(0);
                    strokeHeight = terrainAlphamap.GetLength(1);
                    texturesCount = terrainAlphamap.GetLength(2);

                    // Calculate new textures alpha weights
                    for (int x = 0; x < strokeWidth; x++)
                    {
                        for (int y = 0; y < strokeHeight; y++)
                        {
                            SmoothTexturing(ref terrainAlphamap, embankmentTextureIndex, x, y, strokeWidth, strokeHeight, texturesCount, minEmbankmentTextureBlending, maxEmbankmentTextureBlending);
                        }
                    }

                    // Use terrainData.SetAlphamaps to apply the new texturing
                    terrainData.SetAlphamaps(xBase, yBase, terrainAlphamap);
                }
            }

            /*
             * Paint main texture
             */
            paintedArea = size + 1;
            for (int pointIndex = 0; pointIndex < path.Length; pointIndex++)
            {
                // Convert spline points into terrain coordinates
                localPathPoint = terrainTransform.InverseTransformPoint(path[pointIndex]);
                ConvertPointToCoordinates(terrainData, localPathPoint, (terrainData.alphamapWidth - 1), (terrainData.alphamapHeight - 1), out coordinates);

                CalculateAffectedAreaBoundaries(coordinates, paintedArea, terrainData.alphamapWidth, terrainData.alphamapHeight, out xBase, out yBase, out width, out height);
                // Retrieve current textures "blending"
                terrainAlphamap = terrainData.GetAlphamaps(xBase, yBase, width, height);

                strokeWidth = terrainAlphamap.GetLength(0);
                strokeHeight = terrainAlphamap.GetLength(1);
                texturesCount = terrainAlphamap.GetLength(2);

                // Calculate new textures alpha weights
                for (int x = 0; x < strokeWidth; x++)
                {
                    for (int y = 0; y < strokeHeight; y++)
                    {
                        SmoothTexturing(ref terrainAlphamap, textureIndex, x, y, strokeWidth, strokeHeight, texturesCount, minTextureBlending, maxTextureBlending);
                    }
                }

                // Use terrainData.SetAlphamaps to apply the new texturing
                terrainData.SetAlphamaps(xBase, yBase, terrainAlphamap);
            }

            Profiler.EndSample();
        }

        private static void SmoothTexturing(ref float[,,] terrainAlphamap, int targetTextureIndex, int x, int y, int strokeWidth, int strokeHeight, int texturesCount, float minTextureBlending, float maxTextureBlending)
        {
            float strongestTextureWeight = 0f;
            int strongestTextureIndex = 0;

            // Read texture weights, identify strongest texture
            for (int t = 0; t < texturesCount; t++)
            {
                if (terrainAlphamap[x, y, t] > strongestTextureWeight)
                {
                    strongestTextureWeight = terrainAlphamap[x, y, t];
                    strongestTextureIndex = t;
                }
            }

            // Apply new texture weights
            if (strongestTextureIndex == targetTextureIndex)
            {
                for (int t = 0; t < texturesCount; t++)
                {
                    terrainAlphamap[x, y, t] = t == targetTextureIndex ? 1f : 0f;
                }
            }
            else
            {
                float xWeight = x <= (strokeWidth * minTextureBlending) ? (x + 1) / (strokeWidth / 2f)
                    : (x >= strokeWidth * maxTextureBlending ? (strokeWidth - x + 1) / (strokeWidth / 2f) : 1f);

                float yWeight = y <= (strokeHeight * minTextureBlending) ? (y + 1) / (strokeHeight / 2f)
                    : (y >= strokeHeight * maxTextureBlending ? (strokeHeight - y + 1) / (strokeHeight / 2f) : 1f);

                float targetWeight = Mathf.Clamp01(xWeight * yWeight);

                for (int t = 0; t < texturesCount; t++)
                {
                    if (t == targetTextureIndex)
                        terrainAlphamap[x, y, t] = targetWeight;
                    else if (t == strongestTextureIndex)
                        terrainAlphamap[x, y, t] = 1f - targetWeight;
                    else
                        terrainAlphamap[x, y, t] = 0f;
                }
            }
        }

        /// <summary>
        /// Convert point to terrain coordinates
        /// </summary>
        /// <param name="terrainData"></param>
        /// <param name="localPathPoint"></param>
        /// <param name="mapWidth"></param>
        /// <param name="mapHeight"></param>
        /// <param name="coordinates"></param>
        private static void ConvertPointToCoordinates(TerrainData terrainData, Vector3 localPathPoint, int mapWidth, int mapHeight, out Vector2 coordinates)
        {
            coordinates.x = (localPathPoint.x / terrainData.size.x) * mapWidth;
            coordinates.y = (localPathPoint.z / terrainData.size.z) * mapHeight;
        }

        /// <summary>
        /// Calculate and validates affected area boundaries
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="size"></param>
        /// <param name="mapWidth"></param>
        /// <param name="mapHeight"></param>
        /// <param name="xBase"></param>
        /// <param name="yBase"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private static void CalculateAffectedAreaBoundaries(Vector2 coordinates, int size, int mapWidth, int mapHeight, out int xBase, out int yBase, out int width, out int height, bool paintingBorders = false)
        {
            //Convert coordinates into array indexes
            float offset = size / 2f;
            xBase = paintingBorders ? Mathf.FloorToInt(coordinates.x - offset) : Mathf.CeilToInt(coordinates.x - offset);
            yBase = paintingBorders ? Mathf.FloorToInt(coordinates.y - offset) : Mathf.CeilToInt(coordinates.y - offset);
            width = (xBase < 0) ? (size + xBase) : size;
            height = (yBase < 0) ? (size + yBase) : size;

            //Expanding selection if smallest value (useful for borders)
            width = paintingBorders ? width + 1 : width;
            height = paintingBorders ? height + 1 : height;

            // Validating map boundaries to prevent stack overflow
            xBase = (xBase < 0) ? 0 : xBase;
            yBase = (yBase < 0) ? 0 : yBase;
            width = ((xBase + width) > mapWidth) ? (mapWidth - xBase) : width;
            height = ((yBase + height) > mapHeight) ? (mapHeight - yBase) : height;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="terrainData"></param>
        /// <param name="terrainOriginalHeights"></param>
        /// <param name="localPathPoint"></param>
        /// <param name="embankmentSlope"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="embankment"></param>
        /// <param name="xBase"></param>
        /// <param name="yBase"></param>
        /// <returns></returns>
        private static float CalculateSlopeHeight(TerrainData terrainData, float[,] terrainOriginalHeights, Vector3 localPathPoint, AnimationCurve embankmentSlope, int x, int y, int maxX, int maxY, float embankment, int xBase, int yBase)
        {
            float targetHeight = CalculatePointHeight(terrainData, localPathPoint);
            float originalHeight = terrainOriginalHeights[yBase + y, xBase + x];
            float currentHeight = terrainData.GetHeight(xBase + x, yBase + y);
            currentHeight = currentHeight / terrainData.size.y;

            bool originalHeightIsHeighter = originalHeight > targetHeight;

            if (embankment <= 0)
                return currentHeight;

            float t = 1;
            bool left = x < embankment;
            bool right = x > (maxX - embankment);
            bool top = y > (maxY - embankment);
            bool down = y < embankment;

            t = (left || down) ? (x < y ? (x / embankment) : (y / embankment)) : t;
            t = (right || top) ? (x > y ? ((maxX - x) / embankment) : ((maxY - y) / embankment)) : t;
            t = (left && top) ? (x <= (maxY - y) ? (x / embankment) : ((maxY - y) / embankment)) : t;
            t = (right && down) ? ((maxX - x) <= y ? ((maxX - x) / embankment) : (y / embankment)) : t;

            float slop = Mathf.Clamp01(embankmentSlope.Evaluate(t));

            targetHeight = Mathf.Clamp01(Mathf.Lerp(originalHeight, targetHeight, slop));

            if (originalHeightIsHeighter)
                targetHeight = targetHeight > currentHeight ? currentHeight : targetHeight;
            else
                targetHeight = targetHeight > currentHeight ? targetHeight : currentHeight;

            return targetHeight;
        }

        /// <summary>
        /// Converts point to terrain height value
        /// </summary>
        /// <param name="terrainData"></param>
        /// <param name="localPathPoint"></param>
        /// <returns></returns>
        private static float CalculatePointHeight(TerrainData terrainData, Vector3 localPathPoint)
        {
            float targetHeight = (localPathPoint.y / terrainData.size.y);
            targetHeight = Mathf.Clamp01(targetHeight);
            return targetHeight;
        }

        /// <summary>
        /// Returns corresponding terrain layer index. If not found, returns -1
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="terrainData"></param>
        /// <returns></returns>
        private static int FindTextureIndex(Texture2D texture, TerrainData terrainData)
        {
            int textureIndex = -1;

            if (texture == null)
                return textureIndex;

            for (int i = 0; i < terrainData.alphamapLayers; i++)
            {
                if (terrainData.terrainLayers[i].diffuseTexture == null)
                    continue;

                if (texture.name == terrainData.terrainLayers[i].diffuseTexture.name)
                {
                    textureIndex = i;
                    break;
                }
            }

            return textureIndex;
        }

        /// <summary>
        /// Make sure terrain heightmap borders are "stitched" to neighbor terrains
        /// </summary>
        /// <param name="terrain"></param>
        public static void StitchBorders(Terrain terrain)
        {
            TerrainData terrainData = terrain.terrainData;
            Terrain leftNeighbor = terrain.leftNeighbor;
            Terrain bottomNeighbor = terrain.bottomNeighbor;
            Terrain rightNeighbor = terrain.rightNeighbor;
            Terrain topNeighbor = terrain.topNeighbor;
            int resolution = terrainData.heightmapResolution;
            float[,] edgeValues;

            // Stitch this to leftNeighbor
            if (leftNeighbor != null && terrainData.heightmapResolution == leftNeighbor.terrainData.heightmapResolution)
            {
                edgeValues = leftNeighbor.terrainData.GetHeights(resolution - 1, 0, 1, resolution); // Get left neighbor RIGHT border
                terrainData.SetHeights(0, 0, edgeValues);
            }

            // Stitch rightNeighbor to this
            if (rightNeighbor != null && terrainData.heightmapResolution == rightNeighbor.terrainData.heightmapResolution)
            {
                edgeValues = terrainData.GetHeights(resolution - 1, 0, 1, resolution); // Get this terrain RIGHT border
                rightNeighbor.terrainData.SetHeights(0, 0, edgeValues);
            }

            // Stitch this to bottomNeighbor
            if (bottomNeighbor != null && terrainData.heightmapResolution == bottomNeighbor.terrainData.heightmapResolution)
            {
                edgeValues = bottomNeighbor.terrainData.GetHeights(0, resolution - 1, resolution, 1); // Get bottom neighbor TOP border
                terrainData.SetHeights(0, 0, edgeValues);
            }

            // Stitch topNeighbor to this
            if (topNeighbor != null && terrainData.heightmapResolution == topNeighbor.terrainData.heightmapResolution)
            {
                edgeValues = terrainData.GetHeights(0, resolution - 1, resolution, 1); // Get this terrain TOP border
                topNeighbor.terrainData.SetHeights(0, 0, edgeValues);
            }
        }
    }
}
