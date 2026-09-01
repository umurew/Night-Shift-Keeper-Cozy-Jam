using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PauseMenu : MonoBehaviour
{
    private InputService _inputService;

    private VisualElement _pauseContainer;
    private VisualElement _pauseMenuBox;
    private VisualElement _settingsContainer;

    private Button _resumeButton;
    private Button _settingsButton;
    private Button _settingsBackButton;

    private Slider _volumeSlider;
    private Slider _sensitivitySlider;
    private Toggle _fullscreenToggle;
    private DropdownField _resolutionDropdown;

    private Resolution[] _resolutions;
    private bool _isPaused = false;
    private bool _initialized = false;

    public void Initialize(InputService inputService)
    {
        _inputService = inputService;

        var root = GetComponent<UIDocument>().rootVisualElement;
        _pauseContainer = root.Q<VisualElement>("pause-container");
        _pauseMenuBox = root.Q<VisualElement>("pause-menu-box");
        _settingsContainer = root.Q<VisualElement>("settings-container");
        _resumeButton = root.Q<Button>("resume-button");
        _settingsButton = root.Q<Button>("settings-button");
        _settingsBackButton = root.Q<Button>("settings-back-button");
        _volumeSlider = root.Q<Slider>("volume-slider");
        _sensitivitySlider = root.Q<Slider>("sensitivity-slider");
        _fullscreenToggle = root.Q<Toggle>("fullscreen-toggle");
        _resolutionDropdown = root.Q<DropdownField>("resolution-dropdown");

        _pauseContainer.style.display = DisplayStyle.None;
        _isPaused = false;

        _sensitivitySlider.value = 0.2f;
        _sensitivitySlider.RegisterValueChangedCallback(OnSensitivityChanged);

        _volumeSlider.value = AudioListener.volume;
        _volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);

        _fullscreenToggle.value = Screen.fullScreen;
        _fullscreenToggle.RegisterValueChangedCallback(OnFullscreenChanged);

        _resolutions = Screen.resolutions;
        List<string> options = new();
        int currentResIndex = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            options.Add($"{_resolutions[i].width} x {_resolutions[i].height}");
            if (_resolutions[i].width == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
                currentResIndex = i;
        }

        _resolutionDropdown.choices = options;
        _resolutionDropdown.index = currentResIndex;
        _resolutionDropdown.RegisterValueChangedCallback(OnResolutionChanged);

        _resumeButton.clicked += ResumeGame;
        _settingsButton.clicked += ShowSettings;
        _settingsBackButton.clicked += HideSettings;

        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with dependencies: {inputService.GetType().Name}");
    }

    private void Update()
    {
        if (!_initialized)
            return;

        if (_inputService.UIActions.Cancel.WasPressedThisFrame())
        {
            if (_isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    private void PauseGame()
    {
        _isPaused = true;
        Time.timeScale = 0f;

        _pauseContainer.style.display = DisplayStyle.Flex;
        _inputService.DisablePlayerControls();

        HideSettings();
    }

    private void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        _pauseContainer.style.display = DisplayStyle.None;
        _inputService.EnablePlayerControls();
    }

    private void ShowSettings()
    {
        _pauseMenuBox.AddToClassList("hidden");
        _settingsContainer.RemoveFromClassList("hidden");
    }

    private void HideSettings()
    {
        _pauseMenuBox.RemoveFromClassList("hidden");
        _settingsContainer.AddToClassList("hidden");
    }

    private void OnVolumeChanged(ChangeEvent<float> e)
    {
        AudioListener.volume = e.newValue;
        Debug.Log($"{GetType().Name} Volume: {e.newValue}");
    }

    private void OnSensitivityChanged(ChangeEvent<float> e)
    {
        _inputService.SetCameraSensitivity(e.newValue);
        Debug.Log($"{GetType().Name} Sensitivity: {e.newValue}");
    }

    private void OnFullscreenChanged(ChangeEvent<bool> e)
    {
        Screen.fullScreen = e.newValue;
        Debug.Log($"{GetType().Name} Fullscreen: {e.newValue}");
    }

    private void OnResolutionChanged(ChangeEvent<string> e)
    {
        int index = _resolutionDropdown.index;
        if (index >= 0 && index < _resolutions.Length)
        {
            Resolution res = _resolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            Debug.Log($"{GetType().Name} Resolution: {res.width} x {res.height}");
        }
    }

    private void OnDisable()
    {
        if (_resumeButton != null)
            _resumeButton.clicked -= ResumeGame;

        if (_settingsButton != null)
            _settingsButton.clicked -= ShowSettings;

        if (_settingsBackButton != null)
            _settingsBackButton.clicked -= HideSettings;

        _volumeSlider?.UnregisterValueChangedCallback(OnVolumeChanged);
        _fullscreenToggle?.UnregisterValueChangedCallback(OnFullscreenChanged);
        _resolutionDropdown?.UnregisterValueChangedCallback(OnResolutionChanged);
    }
}