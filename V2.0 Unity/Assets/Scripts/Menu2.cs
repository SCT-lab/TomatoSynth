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
    public TMP_Text textCompressionSlider;
    
    public GameObject[] canvasUI;

    public GameObject canvasUIRecording;

    public bool cameraEnabled = true;

    public bool speedUp = false;
    private int numSpeedUp = 0;

    public RecordingScreenshots recordingScreenshots;

    void Start()
    {
        if(Menu.Instance != null)
        {
            Menu mainMenu = FindObjectOfType<Menu>();
            Menu.Instance.UpdateFirstEntry();
            TMP_Dropdown dropdown = objectsUI[1].GetComponentInChildren<TMP_Dropdown>();
            Resolution[] resolution = Screen.resolutions;
            Resolution highestResolution = resolution[resolution.Length - 1];
            string text = $"Highest available ({highestResolution.width} x {highestResolution.height})";
            
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData { text = text };
            if (dropdown.options.Count == 0 || dropdown.options[0].text != text)
                dropdown.options.Insert(0, option);
        }
        if (!PlayerPrefs.HasKey("CompressionValue"))
            PlayerPrefs.SetInt("CompressionValue", 25);

        if (!PlayerPrefs.HasKey("Waypoints"))
            PlayerPrefs.SetFloat("Waypoints", 129);


        ApplySettings();
        bool fullscreen = PlayerPrefs.GetInt("FullscreenEnabled", 1) == 1;
    }

    void ApplySettings()
    {
        int compression = PlayerPrefs.GetInt("CompressionValue", 25);
        textCompressionSlider.text = compression.ToString("F0");

        int res = PlayerPrefs.GetInt("ResolutionImage", 0);
        ChangeResolution(res);
    }

    void RefreshQualityUI()
    {
        Menu menu = FindObjectOfType<Menu>();
        if(menu != null)
            menu.UpdateFirstEntry(); // Updates quality buttons + colors
    }

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

    public void ChangeCompression(float value)
    {
        int var = System.Convert.ToInt32(value);
        PlayerPrefs.SetInt("CompressionValue", var);
        textCompressionSlider.text = value.ToString("F0");
    }

    public void ChangeResolution(int value)
    {
        PlayerPrefs.SetInt("ResolutionImage", value);

        if(value == 0) {recordingScreenshots.resWidth = 1920; recordingScreenshots.resHeight = 1080;}
        if(value == 1) {recordingScreenshots.resWidth = 1280; recordingScreenshots.resHeight = 720;}
        if(value == 2) {recordingScreenshots.resWidth = 640; recordingScreenshots.resHeight = 480;}
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
