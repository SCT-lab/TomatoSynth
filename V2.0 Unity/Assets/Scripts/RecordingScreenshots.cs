using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.IO;
using UnityEngine;
using TMPro;

public class RecordingScreenshots : MonoBehaviour
{
    [Header("Capture Settings")]
    public float captureInterval = 2f;
    public int resWidth = 1280;
    public int resHeight = 720;

    [Header("Cameras & Canvas")]
    public Camera mainCamera;
    public Camera mainCamera2;
    public GameObject canvasOld;
    public GameObject canvasNew;

    [Header("References")]
    public Camerasystem cameraSystem;
    public Positioning_tomatoes posTomatoes;
    public Menu2 menu2;
    public TMP_Text textScreenshotCount;
    public TMP_Text textWayPoints;

    public bool isRecording = false;
    private int screenshotCount = 0;
    private RenderTexture renderTexture;
    private string screenshotsFolder;
    private string screenshotsFolderGT;

    public CocoDataset cocoDataset = new CocoDataset();

    // -----------------------------
    // Public Methods
    // -----------------------------
    public void StartRecording(bool enable)
    {
        if (enable && !isRecording)
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

            screenshotsFolder = Path.Combine(Application.dataPath, "Screenshots");
            screenshotsFolderGT = Path.Combine(Application.dataPath, "ScreenshotsGT");

            if (!Directory.Exists(screenshotsFolder)) Directory.CreateDirectory(screenshotsFolder);
            if (!Directory.Exists(screenshotsFolderGT)) Directory.CreateDirectory(screenshotsFolderGT);

            isRecording = true;
            screenshotCount = 0;
            StartCoroutine(CaptureScreenshots());
        }
        else
        {
            StopRecording();
        }
    }

    public void ChangeInterval(string value)
    {
        if (float.TryParse(value, out float temp)) captureInterval = temp;
        else captureInterval = 2f;
        Debug.Log($"Capture interval set to: {captureInterval}");
    }

    public void ChangeWaypoints(string value)
    {
        if (int.TryParse(value, out int var))
        {
            PlayerPrefs.SetInt("Waypoints", var);
            textWayPoints.text = var.ToString("F0");
        }
        else
        {
            PlayerPrefs.SetInt("Waypoints", 129);
        }
    }

    public void StopRecording()
    {
        if (!isRecording) return;

        isRecording = false;
        RenderTexture.ReleaseTemporary(renderTexture);
        renderTexture = null;

        mainCamera.targetDisplay = 0;
        mainCamera2.targetDisplay = 0;

        // Save final COCO JSON
        string jsonPath = Path.Combine(Application.dataPath, "Screenshots", "annotations.json");
        CocoExporter.SaveCocoJson(cocoDataset, jsonPath);

        Debug.Log("Recording stopped and COCO JSON saved.");
    }

    // -----------------------------
    // Capture Coroutine
    // -----------------------------
    private IEnumerator CaptureScreenshots()
    {
        float lastCaptureTime = Time.time - captureInterval;

        while (isRecording && cameraSystem.currentWaypointIndex != PlayerPrefs.GetInt("Waypoints", 129))
        {
            yield return new WaitForEndOfFrame();

            if (Time.time - lastCaptureTime >= captureInterval)
            {
                lastCaptureTime = Time.time;

                // ---------------------
                // Regular Screenshot
                // ---------------------
                string screenshotFile = Path.Combine(screenshotsFolder, $"screenshot_{screenshotCount:D4}.png");
                mainCamera.targetTexture = renderTexture;
                mainCamera.Render();
                AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, req =>
                {
                    SaveScreenshot(req, screenshotFile);
                });

                // ---------------------
                // Ground Truth Screenshot
                // ---------------------
                // 1. Activate GT objects
                posTomatoes.ActivateGT(true);
                cameraSystem.ManageRowVisibility();
                yield return null; // Wait a frame to ensure objects are active

                // 2. Image entry for COCO
                int imageId = screenshotCount;
                string screenshotGTFile = Path.Combine(screenshotsFolderGT, $"screenshotGT_{imageId:D4}.png");

                // 3. Generate annotations
                MeshCollider[] colliders = FindObjectsOfType<MeshCollider>();
                CocoExporter.EnsureCategory(cocoDataset, 1, "tomato");
                CocoExporter.AddAnnotationsFromColliders(cocoDataset, mainCamera, colliders, imageId, screenshotGTFile, resWidth, resHeight);

                // 4. Render GT screenshot and save image asynchronously
                mainCamera.targetTexture = renderTexture;
                mainCamera.Render();
                AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, req =>
                {
                    Texture2D screenshotGT = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                    screenshotGT.LoadRawTextureData(req.GetData<byte>());
                    screenshotGT.Apply();
                    File.WriteAllBytes(screenshotGTFile, screenshotGT.EncodeToJPG(100 - PlayerPrefs.GetInt("CompressionValue", 0)));
                    Destroy(screenshotGT);

                    // Deactivate GT objects
                    posTomatoes.ActivateGT(false);
                    cameraSystem.ManageRowVisibility();
                });

                mainCamera.targetTexture = null;
                screenshotCount++;
                textScreenshotCount.text = screenshotCount.ToString("F0");
            }
        }

        // Stop recording safely
        StopRecording();

        // Optional: Load next scene
        SceneManager.LoadScene(1);
    }

    // -----------------------------
    // Screenshot Save Handlers
    // -----------------------------
    private void SaveScreenshot(AsyncGPUReadbackRequest request, string filePath)
    {
        Texture2D screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        screenshot.LoadRawTextureData(request.GetData<byte>());
        screenshot.Apply();

        byte[] bytes = screenshot.EncodeToJPG(100 - PlayerPrefs.GetInt("CompressionValue", 0));
        File.WriteAllBytes(filePath, bytes);

        Destroy(screenshot);
        Debug.Log($"Screenshot saved: {filePath}");
    }
}
