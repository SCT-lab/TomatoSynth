using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
//using UnityEditor.Recorder;
//using UnityEditor.Recorder.Input;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using TMPro;

public class Menu2 : MonoBehaviour
{
    public GameObject[] objectsUI;
    public bool paused = false;
    public GameObject camera2;

    public Camerasystem cameraSystem;
    public Slider speedSlider;
    public TMP_Text textSpeedSlider;
    
    public GameObject[] canvasUI;

    public GameObject canvasUIRecording;

    public bool cameraEnabled = true;

    public bool speedUp = false;
    private int numSpeedUp = 0;

    public RecordingScreenshots recordingScreenshots;
    //UnityEditor.Recorder.ImageRecorderSettings
    // Recorder settings
    // public ImageRecorderSettings recorderControllerSettings;
    // private RecorderController recorderController;

    // Start is called before the first frame update
    void Start()
    {
/*         recorderControllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        recorderController = new RecorderController(recorderControllerSettings);
        speedSlider.onValueChanged.AddListener(ChangeSpeed);

         ImageRecorderSettings imageRecorder = ScriptableObject.CreateInstance<ImageRecorderSettings>();
         imageRecorder.name = "Image Sequence Recorder";
         imageRecorder.Enabled = true;

        // // Set the output directory to the executable path or any desired path
         string outputPath = Application.isEditor ? Application.dataPath : System.IO.Path.GetDirectoryName(Application.dataPath);
         imageRecorder.OutputFile = outputPath + "/ImageSequence/frame_{frame}.jpg";

        // // Set the image format to JPEG and frame rate to 2 FPS
         imageRecorder.imageInputSettings = new GameViewInputSettings();
         imageRecorder.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.JPEG;
         imageRecorder.FrameRatePlayback = FrameRatePlayback.Constant;
         imageRecorder.RecordMode = RecordMode.Manual;
         imageRecorder.FrameRate = 2;

        recorderControllerSettings.AddRecorderSettings(imageRecorder);
        recorderControllerSettings.SetRecordModeToManual();     */    
    }

    // Update is called once per frame
    private void Update()
    {
        if(!paused)
        {

        
        if (Input.GetKeyDown(KeyCode.Escape))
            {

                Time.timeScale = 0f;
                paused = true;

                foreach(var i in objectsUI)
                {
                    i.SetActive(true);
                }
                foreach(GameObject i in canvasUI)
                {
                    i.SetActive(false);
                }
                
                if(recordingScreenshots.isRecording)
                {
                    canvasUIRecording.SetActive(false);
                }
                
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Time.timeScale = 1f;
                paused = false;
                
                foreach(var i in objectsUI)
                {
                    i.SetActive(false);
                }
                foreach(GameObject i in canvasUI)
                {
                    i.SetActive(true);
                }

                if(recordingScreenshots.isRecording)
                {
                    canvasUIRecording.SetActive(true);
                    canvasUI[0].SetActive(false);
                }
        }
        
    }
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        paused = false;
                
        foreach(var i in objectsUI)
        {
            i.SetActive(false);
        }
        foreach(GameObject i in canvasUI)
        {
            i.SetActive(true);
        }

        Debug.Log("Clicked");
    }

    public void ResumeGame2()
    {
        Time.timeScale = 1f;
        paused = false;

        Debug.Log("Clicked");
    }

    public void PauseGame()
    {
        if(!paused)
        {
            Time.timeScale = 0f;
            paused = true;

            Debug.Log("Clicked");
        }
        else
        {
            ResumeGame2();
        }
        
    }

    public void DisableOverviewCamera(bool camera)
    {
        if(camera && cameraEnabled)
        {
            camera2.SetActive(false);
            cameraEnabled = false;
        }
        else
        {
            camera2.SetActive(true);
            cameraEnabled = true;
        }
    }

    public void ChangeSpeed(float speedValue)
    {
        if(speedValue == 60f)
        {
            speedUp = true;
            if(speedUp)
            {
                numSpeedUp += 1;
                cameraSystem.moveSpeed = speedValue * numSpeedUp;
                PlayerPrefs.SetFloat("CameraSpeed", speedValue);
                textSpeedSlider.text = cameraSystem.moveSpeed.ToString("F0");
            }  
        }
        else
        {
            speedUp = false;
            numSpeedUp = 0;
            cameraSystem.moveSpeed = speedValue;
            PlayerPrefs.SetFloat("CameraSpeed", speedValue);
            textSpeedSlider.text = speedValue.ToString("F0");
        }
        
    }

/*      public void StartRecording()
    {
        if (!recorderController.IsRecording())
        {
            recorderController.PrepareRecording();
            recorderController.StartRecording();
            Debug.Log("Recording started...");
        }
    }

    public void StopRecording()
    {
        if (recorderController.IsRecording())
        {
            recorderController.StopRecording();
            Debug.Log("Recording stopped...");
        }
    } */
}
