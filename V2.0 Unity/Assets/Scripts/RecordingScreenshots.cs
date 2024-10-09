using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.IO;
using UnityEngine;
using TMPro;

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

                renderTexture = RenderTexture.GetTemporary(1280, 720, 24);

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

        while (isRecording && cameraSystem.currentWaypointIndex != cameraSystem.waypoints.Count)
        {
            yield return new WaitForFixedUpdate();
            // Define the filename with an incrementing number

            float timeCapture = Time.time - (screenshotCount * captureInterval);
            if(timeCapture >= captureInterval)
            {
                posTomatoes.ActivateGT(false);
                cameraSystem.ManageRowVisibility();
                yield return new WaitForEndOfFrame();

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
            SceneManager.LoadScene(0);
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request, string filename)
    {
        Texture2D screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        screenshot.LoadRawTextureData(request.GetData<byte>());
        screenshot.Apply();
            

        byte[] bytes = screenshot.EncodeToJPG(75);
        File.WriteAllBytes(filename, bytes);
                
        Debug.Log($"Screenshot saved to: {screenshotFilename}");

        Destroy(screenshot);
    }

    void OnCompleteReadbackGT(AsyncGPUReadbackRequest request, string filenameGT)
    {
        Texture2D screenshotGT = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        screenshotGT.LoadRawTextureData(request.GetData<byte>());
        screenshotGT.Apply();
                
        byte[] bytesGT = screenshotGT.EncodeToJPG(75);

        File.WriteAllBytes(filenameGT, bytesGT);

                
        Debug.Log($"Screenshot saved to: {screenshotFilenameGT}");

         // Clean up the screenshot texture
        Destroy(screenshotGT);
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
}
