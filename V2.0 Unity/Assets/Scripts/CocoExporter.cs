using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public static class CocoExporter
{
    // Adds the category if missing (call once at Start)
    public static void EnsureCategory(CocoDataset dataset, int id, string name)
    {
        if (dataset.categories == null) dataset.categories = new List<CocoCategory>();
        if (!dataset.categories.Any(c => c.id == id))
        {
            dataset.categories.Add(new CocoCategory { id = id, name = name });
        }
    }

    // Adds annotations for visible colliders projected onto the camera image
    public static void AddAnnotationsFromColliders(
        CocoDataset dataset,
        Camera camera,
        MeshCollider[] colliders,
        int imageId,
        string fileName,
        int imageWidth,
        int imageHeight,
        int categoryId = 1)
    {
        if (dataset.annotations == null) dataset.annotations = new List<CocoAnnotation>();
        if (dataset.images == null) dataset.images = new List<CocoImage>();

        // Add image entry
        var img = new CocoImage { id = imageId, file_name = Path.GetFileName(fileName), width = imageWidth, height = imageHeight };
        dataset.images.Add(img);

        // Frustum planes for visibility check
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);

        // Sequential annotation IDs
        int nextAnnotationId = dataset.annotations.Count > 0 ? dataset.annotations.Max(a => a.id) + 1 : 1;

        foreach (var col in colliders)
        {
            if (!GeometryUtility.TestPlanesAABB(planes, col.bounds)) continue;

            // Project all 8 corners
            var corners = GetBoundingBoxCorners(col.bounds);
            Vector3[] projected = new Vector3[8];
            bool anyInFront = false;
            for (int i = 0; i < 8; i++)
            {
                projected[i] = camera.WorldToScreenPoint(corners[i]);
                if (projected[i].z > 0f) anyInFront = true;
            }
            if (!anyInFront) continue;

            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                float px = projected[i].x;
                float py = projected[i].y;
                minX = Mathf.Min(minX, px);
                minY = Mathf.Min(minY, py);
                maxX = Mathf.Max(maxX, px);
                maxY = Mathf.Max(maxY, py);
            }

            // Clamp to image size
            minX = Mathf.Clamp(minX, 0f, imageWidth);
            maxX = Mathf.Clamp(maxX, 0f, imageWidth);
            minY = Mathf.Clamp(minY, 0f, imageHeight);
            maxY = Mathf.Clamp(maxY, 0f, imageHeight);
            if (minX >= maxX || minY >= maxY) continue;

            float bboxX = minX;
            float bboxWidth = maxX - minX;
            float bboxHeight = maxY - minY;
            float bboxY = imageHeight - (minY + bboxHeight); // Convert to COCO top-left

            CocoAnnotation ann = new CocoAnnotation
            {
                id = nextAnnotationId++,
                image_id = imageId,
                category_id = categoryId,
                bbox = new List<float> { bboxX, bboxY, bboxWidth, bboxHeight },
                area = bboxWidth * bboxHeight
            };
            dataset.annotations.Add(ann);
        }
    }

    // Save JSON to file
    public static void SaveCocoJson(CocoDataset dataset, string jsonPath)
    {
        string dir = Path.GetDirectoryName(jsonPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string jsonOutput = JsonConvert.SerializeObject(dataset, Formatting.Indented);
        File.WriteAllText(jsonPath, jsonOutput);
        Debug.Log($"COCO JSON saved: {jsonPath}");
    }

    private static Vector3[] GetBoundingBoxCorners(Bounds b)
    {
        Vector3 c = b.center;
        Vector3 e = b.extents;
        return new Vector3[8]
        {
            new Vector3(c.x - e.x, c.y - e.y, c.z - e.z),
            new Vector3(c.x + e.x, c.y - e.y, c.z - e.z),
            new Vector3(c.x - e.x, c.y + e.y, c.z - e.z),
            new Vector3(c.x + e.x, c.y + e.y, c.z - e.z),
            new Vector3(c.x - e.x, c.y - e.y, c.z + e.z),
            new Vector3(c.x + e.x, c.y - e.y, c.z + e.z),
            new Vector3(c.x - e.x, c.y + e.y, c.z + e.z),
            new Vector3(c.x + e.x, c.y + e.y, c.z + e.z)
        };
    }
}

// Reuse your existing serializable classes:
[Serializable]
public class CocoDataset
{
    public List<CocoImage> images = new List<CocoImage>();
    public List<CocoAnnotation> annotations = new List<CocoAnnotation>();
    public List<CocoCategory> categories = new List<CocoCategory>();
}

[Serializable]
public class CocoImage
{
    public int id;
    public string file_name;
    public int width;
    public int height;
}

[Serializable]
public class CocoAnnotation
{
    public int id;
    public int image_id;
    public int category_id;
    public List<float> bbox;
    public float area;
}

[Serializable]
public class CocoCategory
{
    public int id;
    public string name;
}
