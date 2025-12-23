using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using TMPro;

public class Menu : MonoBehaviour
{
    public GameObject canvasMenu;
    public GameObject canvasOptions;
    public GameObject canvasHowToPlay;
    public TMP_Text[] buttonText;
    public TMP_Text[] buttonText1;

    public AudioMixer masterMixer;
    public Slider volumeSlider;
    public Slider sensitivitySlider;
    public Slider gammaSlider;
    public Slider saturationSlider;
    public TMP_Dropdown dropDownUI;
    public Toggle toggleUI;
    public TMP_Text textVolume;
    public TMP_Text textSensitivity;
    public TMP_Text textGamma;
    public TMP_Text textSaturation;

    public AudioSource audioSource;
    private bool firstEntry = true;

    public int widthRes;
    public int heightRes;
    public int refreshRate;
    public bool fullScreenEnabled = true;
    public int numRes = 0;

    public PostProcessVolume PostProcessVolume;
    private ColorGrading colorGrading;
    public PostProcessLayer postProcessLayer;
    public static Menu Instance;

    void Awake()
    {
        Instance = this;
    }
    
    //public ChangeFont changeFontAsset;

    void Start()
    {        
        //Debug.Log("Res:" + numRes);
        //int getFullScreenInt = PlayerPrefs.GetInt("FullscreenEnabled", 1);
        //Debug.Log("Fullscreen INT:" + getFullScreenInt);
        //fullScreenEnabled = (getFullScreenInt == 1);
        //Debug.Log("fullScreenEnabled:" + fullScreenEnabled);
        //Debug.Log("toggleUI" + toggleUI.isOn);

        //toggleUI.isOn = fullScreenEnabled;
        //Screen.fullScreen = fullScreenEnabled;
        LoadSettings();
        ApplySettings();

        int getFullScreenInt = PlayerPrefs.GetInt("FullscreenEnabled", 1);
        fullScreenEnabled = (getFullScreenInt == 1);
        toggleUI.isOn = fullScreenEnabled;
        Screen.fullScreen = toggleUI.isOn;

        float gammaValue = PlayerPrefs.GetFloat("GammaValue", 0.0f);
        gammaSlider.value = gammaValue;
        ChangeContrast(gammaValue);

        toggleUI.onValueChanged.AddListener(FullScreenChange);
        dropDownUI.onValueChanged.AddListener(ResolutionChange);
        volumeSlider.onValueChanged.AddListener(ChangeAudio);
        sensitivitySlider.onValueChanged.AddListener(ChangeMouseSensitivity);
        gammaSlider.onValueChanged.AddListener(ChangeContrast);

        float db = PlayerPrefs.GetFloat("MasterDB", 0f);
        masterMixer.SetFloat("Master", db);
        volumeSlider.value = Mathf.Pow(10, db / 20f);

        UpdateFirstEntry();
        Debug.Log(toggleUI.isOn);
    }

    void LoadSettings()
    {
        numRes = PlayerPrefs.GetInt("ResolutionIndex", 0);
        dropDownUI.value = numRes; 
    }

    void ApplySettings()
    {
        AddHighestResolution();
        PostProcessVolume.profile.TryGetSettings(out colorGrading);
        
        ResolutionChange(numRes);
    }

    public void StartGame()
    {
        audioSource.Play();
        SceneManager.LoadScene(1);
    }

    public void Options()
    {
        canvasMenu.SetActive(false);
        canvasOptions.SetActive(true);
        audioSource.Play();
    }

    public void UltraQuality()
    {
        QualitySettings.SetQualityLevel(6);
        for(int i = 0; i < buttonText.Length; i++)
        {
            buttonText[i].color = Color.red;
        }
        buttonText[0].color = Color.green;
        if(!firstEntry) {audioSource.Play();}
    }

    public void VeryHighQuality()
    {
        QualitySettings.SetQualityLevel(5);
        for(int i = 0; i < buttonText.Length; i++)
        {
            buttonText[i].color = Color.red;
        }
        buttonText[1].color = Color.green;
        if(!firstEntry) {audioSource.Play();}
    }

    public void HighQuality()
    {
        QualitySettings.SetQualityLevel(4);
        for(int i = 0; i < buttonText.Length; i++)
        {
            buttonText[i].color = Color.red;
        }
        buttonText[2].color = Color.green;
        if(!firstEntry) {audioSource.Play();}
    }

    public void MediumQuality()
    {
        QualitySettings.SetQualityLevel(3);
        for(int i = 0; i < buttonText.Length; i++)
        {
            buttonText[i].color = Color.red;
        }
        buttonText[3].color = Color.green;
        if(!firstEntry) {audioSource.Play();}
    }

    public void LowQuality()
    {
        QualitySettings.SetQualityLevel(2);
        for(int i = 0; i < buttonText.Length; i++)
        {
            buttonText[i].color = Color.red;
        }
        buttonText[4].color = Color.green;
        if(!firstEntry) {audioSource.Play();}
    }

    public void VeryLowQuality()
    {
        QualitySettings.SetQualityLevel(1);
        for(int i = 0; i < buttonText.Length; i++)
        {
            buttonText[i].color = Color.red;
        }
        buttonText[5].color = Color.green;
        if(!firstEntry) {audioSource.Play();}
    }

    public void ChangeAudio(float volume)
    {
        float decibels = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
        masterMixer.SetFloat("Master", decibels);
        PlayerPrefs.SetFloat("MasterDB", decibels);

        float temp = decibels;
        if (temp > 0)
        {
            textVolume.text = "+" + temp.ToString("F2");
            textVolume.color = Color.green;
        }
        else if(temp < 0)
        {
            textVolume.text = "" + temp.ToString("F2");
            textVolume.color = Color.red;
        }
        else
        {
            textVolume.text = "" + temp.ToString("F2");
            textVolume.color = Color.white;
        }
    }

    public void ChangeMouseSensitivity(float factor)
    {
        float temp1 = factor * 400;
        textSensitivity.text = factor.ToString("F3");

        if(factor != 1.000)
        {
            textSensitivity.color = Color.green;
        }
        PlayerPrefs.SetFloat("MouseSensitivity", temp1);
    }

    public void Default()
    {
        ChangeContrast(0);
        gammaSlider.value = 0f;
        saturationSlider.value = 0f;
        SetFXAA();
        ChangeSaturation(0f);
        dropDownUI.value = 0;
        ResolutionChange(0);  
        QualitySettings.SetQualityLevel(6);
        for(int i = 0; i < buttonText.Length; i++)
        {
            buttonText[i].color = Color.red;
        }
        buttonText[0].color = Color.green;

        masterMixer.SetFloat("Master", 1.0f);
        float temp2 = 0.0f;
        textVolume.text = "" + temp2.ToString("F2");
        volumeSlider.value = 1.0f;
        textVolume.color = Color.white;

        PlayerPrefs.SetFloat("MouseSensitivity", 400);
        textSensitivity.text = "1.000";
        sensitivitySlider.value = 1.0f;
        textSensitivity.color = Color.white;
        audioSource.Play();

        toggleUI.isOn = fullScreenEnabled;
        Screen.fullScreen = toggleUI.isOn;
    }

    public void UpdateFirstEntry()
    {
        bool previousFirstEntry = firstEntry;
        firstEntry = true;

        ResolutionChange(numRes);

        int currentQualityLevel = QualitySettings.GetQualityLevel();
        if(currentQualityLevel == 6)
        {
            UltraQuality();
        }
        else if(currentQualityLevel == 5)
        {
            VeryHighQuality();
        }
        else if(currentQualityLevel == 4)
        {
            HighQuality();
        }
        else if(currentQualityLevel == 3)
        {
            MediumQuality();
        }
        else if(currentQualityLevel == 2)
        {
            LowQuality();
        }
        else if(currentQualityLevel == 1)
        {
            VeryLowQuality();
        }

        float volume;
        masterMixer.GetFloat("Master", out volume);
        volumeSlider.value = Mathf.Pow(10, volume / 20);
        ChangeAudio(volumeSlider.value);

        // Update mouse sensitivity UI
        float temp5 = PlayerPrefs.GetFloat("MouseSensitivity") / 400;
        sensitivitySlider.value = temp5;
        ChangeMouseSensitivity(temp5); // Update UI text and color
        firstEntry = false;
    }

    public void AddHighestResolution()
    {
        Resolution[] resolution = Screen.resolutions;
        Resolution highestResolution = resolution[resolution.Length - 1];
        widthRes = highestResolution.width;
        heightRes = highestResolution.height;
        if (firstEntry)
        {
            TMP_Dropdown.OptionData updateOption = new TMP_Dropdown.OptionData
            {
                text = $"Highest available ({widthRes} x {heightRes})"
            };
            dropDownUI.options.Insert(0, updateOption);
        }
    }

    public void ResolutionChange(int numResolution)
    {

        if (numResolution == 0)
        {
            Resolution[] resolution = Screen.resolutions;
            Resolution highestResolution = resolution[resolution.Length - 1];
            widthRes = highestResolution.width;
            heightRes = highestResolution.height;
            numRes = 0;
        }
        else if(numResolution == 1)
        {
            Screen.SetResolution(3840, 2160, fullScreenEnabled);
            widthRes = 3840;
            heightRes = 2160;
            numRes = 1;
        }
        else if(numResolution == 2)
        {
            Screen.SetResolution(2560, 1440, fullScreenEnabled);
            widthRes = 2560;
            heightRes = 1440;
            numRes = 2;
        }
        else if(numResolution == 3)
        {
            Screen.SetResolution(1920, 1080, fullScreenEnabled);
            widthRes = 1920;
            heightRes = 1080;
            numRes = 3;
        }
        else if(numResolution == 4)
        {
            Screen.SetResolution(1280, 720, fullScreenEnabled);
            widthRes = 1280;
            heightRes = 720;
            numRes = 4;
        }
        else if(numResolution == 5)
        {
            Screen.SetResolution(800, 600, fullScreenEnabled);
            widthRes = 800;
            heightRes = 600;
            numRes = 5;
        }

        bool fullscreen = PlayerPrefs.GetInt("FullscreenEnabled", 1) == 1;
        Screen.SetResolution(widthRes, heightRes, fullscreen);

        PlayerPrefs.SetInt("ResolutionIndex", numRes);
        PlayerPrefs.Save();
    }

    public void FullScreenChange(bool isFullscreen)
    {
        fullScreenEnabled = isFullscreen;
        Screen.fullScreen = fullScreenEnabled;
        PlayerPrefs.SetInt("FullscreenEnabled", fullScreenEnabled ? 1 : 0);
        PlayerPrefs.Save();       
    }

    public void ChangeContrast(float gammaValueChange)
    {
        colorGrading.gamma.value = new Vector4(0f,0f,0f,gammaValueChange);

        
        if(gammaValueChange > 0)
        {
            textGamma.color = Color.green;
            textGamma.text = "+" + gammaValueChange.ToString("F3");
        }
        else if(gammaValueChange == 0)
        {
            textGamma.color = Color.white;
            textGamma.text = gammaValueChange.ToString("F3");
        }
        else if(gammaValueChange < 0)
        {
            textGamma.color = Color.red;
            textGamma.text = "" + gammaValueChange.ToString("F3");
        }
        PlayerPrefs.SetFloat("GammaValue", gammaValueChange);
        PlayerPrefs.Save();
    }

    public void ChangeSaturation(float saturationValueChange)
    {
        colorGrading.saturation.value = saturationValueChange;
        if(saturationValueChange > 0)
        {
            textSaturation.color = Color.green;
            textSaturation.text = "+" + saturationValueChange.ToString("F1");
        }
        else if(saturationValueChange == 0)
        {
            textSaturation.color = Color.white;
            textSaturation.text = saturationValueChange.ToString("F1");
        }
        else if(saturationValueChange < 0)
        {
            textSaturation.color = Color.red;
            textSaturation.text = "" + saturationValueChange.ToString("F1");
        }
        
        PlayerPrefs.SetFloat("SaturationValue", saturationValueChange);
        PlayerPrefs.Save();
    }

    public void SetFXAA()
    {
        postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
        for(int i = 0; i < buttonText1.Length; i++)
        {
            buttonText1[i].color = Color.red;
        }
        buttonText1[0].color = Color.green;
        if(!firstEntry) {audioSource.Play();}
    }

    public void SetTAA()
    {
        postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;
        for(int i = 0; i < buttonText1.Length; i++)
        {
            buttonText1[i].color = Color.red;
        }
        buttonText1[1].color = Color.green;
        if(!firstEntry) {audioSource.Play();}
    }

    public void SetSMAA()
    {
        postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        for(int i = 0; i < buttonText1.Length; i++)
        {
            buttonText1[i].color = Color.red;
        }
        buttonText1[2].color = Color.green;
        if(!firstEntry) {audioSource.Play();}
    }

    public void SetNone()
    {
        postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.None;
        for(int i = 0; i < buttonText1.Length; i++)
        {
            buttonText1[i].color = Color.red;
        }
        buttonText1[3].color = Color.green;
        if(!firstEntry) {audioSource.Play();}
    }
    

    public void HowToPlay()
    {
        canvasHowToPlay.SetActive(true);
        canvasMenu.SetActive(false);
    }

    public void BackMidGame()
    {
        Time.timeScale = 1f; // Pause the game time
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AudioListener.pause = false;
        SceneManager.LoadScene(0);
        audioSource.Play();
    }

    public void Back()
    {
        canvasMenu.SetActive(true);
        canvasOptions.SetActive(false);
        canvasHowToPlay.SetActive(false);
        audioSource.Play();
    }

    public void ExitGame()
    {
        audioSource.Play();
        Application.Quit();
    }
}
