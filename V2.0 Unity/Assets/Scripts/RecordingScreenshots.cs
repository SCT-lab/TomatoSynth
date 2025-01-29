using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.IO;
using UnityEngine;
using TMPro;
using Newtonsoft.Json;

public class RecordingScreenshots : MonoBehaviour
{
    public float captureInterval = 2f; // Time in seconds between screenshots
    public bool isRecording = false;
    private int screenshotCount = 0;

    public Camera mainCamera;
    public Camera mainCamera2;
    public GameObject canvasOld;
    public GameObject canvasNew;
    
    public Camerasystem cameraSystem;
    public Positioning_tomatoes posTomatoes;

    public Menu2 menu2;

    public TMP_Text textScreenshotCount;

    private string screenshotFilename;
    private string screenshotFilenameGT;

    private RenderTexture renderTexture;

    public CocoDataset cocoDataset = new CocoDataset();
    public MeshCollider[] posCollidersTomatoesGT;
    public MeshCollider[] visibleTomatoes;
    public List<TomatoData> bbBox = new List<TomatoData>();

    public int resWidth = 1280;
    public int resHeight = 720;

    public TMP_Text textWayPoints;
    // Start is called before the first frame update
    
    public void StartRecording(bool bool_value)
    {
        if(bool_value)
        {
            if(!isRecording)
            {
                Application.targetFrameRate = 30;
                 
                mainCamera2.gameObject.SetActive(true);
                mainCamera.gameObject.SetActive(true);

                mainCamera.targetDisplay = 1;
                mainCamera2.targetDisplay = 0;
            
                canvasOld.SetActive(false);
                canvasNew.SetActive(true);
            
                menu2.DisableOverviewCamera(false);

                renderTexture = RenderTexture.GetTemporary(resWidth, resHeight, 24);

                StartCoroutine(CaptureScreenshots());
            }
        }
        else
        {
            StopRecording();
        }
        
    }

    public void ChangeInterval(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogError("Input is null or empty.");
            captureInterval = 2f;
            return;
        }

        if (float.TryParse(value, out float temp))
        {
            //float temp = float.Parse(value);
            captureInterval = temp;
            Debug.Log($"Capture interval set to: {captureInterval}");
        }
        else
        {
            Debug.LogError($"Failed to parse '{value}' to a float.");
            captureInterval = 2f;
        }
    }

    public void ChangeWaypoints(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogError("Input is null or empty.");
            PlayerPrefs.SetInt("Waypoints", 129);
            return;
        }
        else
        {
            int var = System.Convert.ToInt32(value);
            PlayerPrefs.SetInt("Waypoints", var);
            textWayPoints.text = var.ToString("F0");
            Debug.Log($"Capture interval set to: {value}");
        }
    }

        // CHATGPT GENERATED PARTIALLY
    public IEnumerator CaptureScreenshots()
    {
        isRecording = true;
        screenshotCount = 0;

        mainCamera2.gameObject.SetActive(true);          // Enable the blank screen camera

        // Create a directory to save screenshots if it doesn't exist
        string outputPath = Application.isEditor ? Application.dataPath : System.IO.Path.GetDirectoryName(Application.dataPath);
        string screenshotsFolder = Path.Combine(Application.dataPath, "Screenshots");
        string screenshotsFolder1 = Path.Combine(Application.dataPath, "ScreenshotsGT");

        if (!Directory.Exists(screenshotsFolder))
        {
            Directory.CreateDirectory(screenshotsFolder);
        }
        if (!Directory.Exists(screenshotsFolder1))
        {
            Directory.CreateDirectory(screenshotsFolder1);
        }

        while (isRecording && cameraSystem.currentWaypointIndex != PlayerPrefs.GetInt("Waypoints", 129))
        {
            yield return new WaitForFixedUpdate();
            // Define the filename with an incrementing number

            float timeCapture = Time.time - (screenshotCount * captureInterval);
            if(timeCapture >= captureInterval)
            {
                posTomatoes.ActivateGT(false);
                cameraSystem.ManageRowVisibility();

                //yield return new WaitForEndOfFrame();
                yield return null;

                screenshotFilename = Path.Combine(screenshotsFolder, $"screenshot_{screenshotCount:D4}.png");
                screenshotFilenameGT = Path.Combine(screenshotsFolder1, $"screenshotGT_{screenshotCount:D4}.png");

                mainCamera.targetTexture = renderTexture;
                mainCamera.Render();

                AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, request => 
                {
                    OnCompleteReadback(request, screenshotFilename);
                } );

                posTomatoes.ActivateGT(true);
                
                cameraSystem.ManageRowVisibility();

                // GT
                mainCamera.targetTexture = renderTexture;
                mainCamera.Render();

                AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, request => 
                {
                    OnCompleteReadbackGT(request, screenshotFilenameGT);
                } );

                mainCamera.targetTexture = null;  // Unbind the render texture from the camera

                // Increment the screenshot count for unique filenames
                screenshotCount++;
                textScreenshotCount.text = screenshotCount.ToString("F0");
            }
        }
            SceneManager.LoadScene(1);
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request, string filename)
    {
        Texture2D screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        screenshot.LoadRawTextureData(request.GetData<byte>());
        screenshot.Apply();
        
        int tempInt = 100 - PlayerPrefs.GetInt("CompressionValue", 0);
        byte[] bytes = screenshot.EncodeToJPG(tempInt);

        System.Threading.Tasks.Task.Run(() => File.WriteAllBytes(filename, bytes));
        //File.WriteAllBytes(filename, bytes);
                
        Debug.Log($"Screenshot saved to: {screenshotFilename}");

        Destroy(screenshot);
    }

    void OnCompleteReadbackGT(AsyncGPUReadbackRequest request, string filenameGT)
    {
        Texture2D screenshotGT = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        screenshotGT.LoadRawTextureData(request.GetData<byte>());
        screenshotGT.Apply();
                
        int tempInt1 = 100 - PlayerPrefs.GetInt("CompressionValue", 0);
        byte[] bytesGT = screenshotGT.EncodeToJPG(tempInt1);

        System.Threading.Tasks.Task.Run(() => File.WriteAllBytes(filenameGT, bytesGT));
        //File.WriteAllBytes(filenameGT, bytesGT);

        CocoImage cocoImage = new CocoImage
        {
            id = screenshotCount,
            file_name = Path.GetFileName(filenameGT),
            width = screenshotGT.width,
            height = screenshotGT.height
        };
    
        cocoDataset.images.Add(cocoImage);
        PreCalculateBoundingBoxes();

        AddBB(cocoImage.id);
        
        SaveCocoJson();
                
        Debug.Log($"Screenshot saved to: {screenshotFilenameGT}");

         // Clean up the screenshot texture
        Destroy(screenshotGT);

        void SaveCocoJson()
        {
            string jsonOutput = JsonConvert.SerializeObject(cocoDataset, Formatting.Indented);
            string jsonPath = Path.Combine(Application.dataPath, "Screenshots", "annotations.json");
 
            File.WriteAllText(jsonPath, jsonOutput);
            Debug.Log($"COCO JSON file saved to: {jsonPath}");
        }

        void AddBB(int cocoID)
        {
            // Add bounding boxes for each tomato to the annotations list
            foreach (var tomatoData in bbBox)
            {
                CocoAnnotation cocoAnnotation = new CocoAnnotation
                {
                    id = tomatoData.id,
                    image_id = cocoID,  // The ID of the image this annotation is related to
                    category_id = 1,    // Assuming category_id is 1 for tomatoes; you can change it accordingly
                    bbox = tomatoData.bbox, // The bounding box [x, y, width, height]
                    area = tomatoData.bbox[2] * tomatoData.bbox[3] // Calculate the area (width * height)
                };

                cocoDataset.annotations.Add(cocoAnnotation);
            }
        }
    }

    public void StopRecording()
    {
        if (isRecording)
        {
            isRecording = false;
            Debug.Log("Recording stopped.");

            RenderTexture.ReleaseTemporary(renderTexture);
            renderTexture = null;

            mainCamera.targetDisplay = 0;
            mainCamera2.targetDisplay = 0;
        }    
    }

     bool IsVisible(Vector3 pos, Vector3 boundSize, Camera mainCamera)
    {
        var bounds = new Bounds(pos, boundSize);
        var planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    public void PreCalculateBoundingBoxes()
    {
        // Get the frustum planes from the camera
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);        
        posCollidersTomatoesGT = FindObjectsOfType<MeshCollider>();
        List<MeshCollider> visibleTomatoes = new List<MeshCollider>();
        // Loop through each collider to check visibility
        foreach (MeshCollider collider in posCollidersTomatoesGT)
        {
            if (IsVisible(collider.transform.position, collider.bounds.size, mainCamera))
            {
                // If visible, add to the list of visible tomatoes
                visibleTomatoes.Add(collider);
            }
        }

        bbBox.Clear();
        // Store the bounding boxes and other necessary data
        foreach (MeshCollider collider in visibleTomatoes)
        {
            // Get the bounds of the collider
            Bounds colliderBounds = collider.bounds;

            // Convert the bounds to a bounding box in COCO format: [x, y, width, height]
            float xMin = colliderBounds.min.x;
            float yMin = colliderBounds.min.y;
            float width = colliderBounds.max.x - xMin;
            float height = colliderBounds.max.y - yMin;

            // Store the bounding box information in a list or directly in your data structure
            TomatoData tomatoData = new TomatoData
            {
                id = collider.gameObject.GetInstanceID(), // Use the instance ID or another unique identifier
                bbox = new List<float> { xMin, yMin, width, height }
            };

            // Add the pre-calculated data to the list for later use
            bbBox.Add(tomatoData);
        }
    }

}

public class TomatoData
{
    public int id;
    public List<float> bbox;  // [x, y, width, height]
}


[System.Serializable]
public class CocoDataset
{
    public List<CocoImage> images = new List<CocoImage>();
    public List<CocoAnnotation> annotations = new List<CocoAnnotation>();
    public List<CocoCategory> categories = new List<CocoCategory>();
}

[System.Serializable]
public class CocoImage
{
    public int id;
    public string file_name;
    public int width;
    public int height;
}

[System.Serializable]
public class CocoAnnotation
{
    public int id;
    public int image_id;
    public int category_id;
    public List<float> bbox;  // [x, y, width, height]
    public float area;
}

[System.Serializable]
public class CocoCategory
{
    public int id;
    public string name;
}