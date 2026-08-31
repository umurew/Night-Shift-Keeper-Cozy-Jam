using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenu : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private int sceneLoadDelayTime = 500;

    private Label _newBadge;
    private VisualElement _fadeOverlay;
    private VisualElement _menuContainer;
    private VisualElement _settingsContainer;

    private Button _playButton;
    private Button _settingsButton;
    private Button _quitButton;
    private Button _settingsBackButton;

    // Setting Controls
    private Slider _volumeSlider;
    private Toggle _fullscreenToggle;
    private DropdownField _resolutionDropdown;

    // Cache resolutions
    private Resolution[] _resolutions;

    private void Start()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Visual Elements
        _newBadge = root.Q<Label>("new-badge");
        _fadeOverlay = root.Q<VisualElement>("fade-overlay");
        _menuContainer = root.Q<VisualElement>("menu-container");
        _settingsContainer = root.Q<VisualElement>("settings-container");

        // Buttons
        _playButton = root.Q<Button>("play-button");
        _settingsButton = root.Q<Button>("settings-button");
        _quitButton = root.Q<Button>("quit-button"); // Assuming you added this to the UXML!
        _settingsBackButton = root.Q<Button>("settings-back-button");

        // Settings Controls
        _volumeSlider = root.Q<Slider>("volume-slider");
        _fullscreenToggle = root.Q<Toggle>("fullscreen-toggle");
        _resolutionDropdown = root.Q<DropdownField>("resolution-dropdown");

        InitializeSettings();

        // Register Callbacks
        _newBadge.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
        _playButton.clicked += PlayButtonClicked;
        _settingsButton.clicked += ShowSettings;
        _settingsBackButton.clicked += HideSettings;

        if (_quitButton != null)
            _quitButton.clicked += Quit;

        // Initial setup
        root.schedule.Execute(() =>
        {
            _newBadge.AddToClassList("new-badge-enlarged");
            _fadeOverlay.RemoveFromClassList("fade-overlay-visible");
        }).StartingIn(50);
    }

    private void InitializeSettings()
    {
        // 1. Volume
        if (_volumeSlider != null)
        {
            _volumeSlider.value = AudioListener.volume;
            _volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
        }

        // 2. Fullscreen
        if (_fullscreenToggle != null)
        {
            _fullscreenToggle.value = Screen.fullScreen;
            _fullscreenToggle.RegisterValueChangedCallback(OnFullscreenChanged);
        }

        // 3. Resolution
        if (_resolutionDropdown != null)
        {
            _resolutions = Screen.resolutions;
            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                // Format the resolution text (e.g., "1920 x 1080")
                string option = $"{_resolutions[i].width} x {_resolutions[i].height}";
                options.Add(option);

                // Find the index that matches our current screen resolution
                if (_resolutions[i].width == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            _resolutionDropdown.choices = options;
            _resolutionDropdown.index = currentResolutionIndex;
            _resolutionDropdown.RegisterValueChangedCallback(OnResolutionChanged);
        }
    }

    private void OnDisable()
    {
        // Unregister visual events
        if (_newBadge != null)
            _newBadge.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);

        // Unregister button clicks
        if (_playButton != null)
            _playButton.clicked -= PlayButtonClicked;
        if (_settingsButton != null)
            _settingsButton.clicked -= ShowSettings;
        if (_quitButton != null)
            _quitButton.clicked -= Quit;
        if (_settingsBackButton != null)
            _settingsBackButton.clicked -= HideSettings;

        // Unregister setting callbacks
        if (_volumeSlider != null)
            _volumeSlider.UnregisterValueChangedCallback(OnVolumeChanged);
        if (_fullscreenToggle != null)
            _fullscreenToggle.UnregisterValueChangedCallback(OnFullscreenChanged);
        if (_resolutionDropdown != null)
            _resolutionDropdown.UnregisterValueChangedCallback(OnResolutionChanged);
    }

    private async void PlayButtonClicked()
    {
        _fadeOverlay.AddToClassList("fade-overlay-visible");

        await Task.Delay(sceneLoadDelayTime);

        if (this == null)
            return;

        SceneManager.LoadScene(gameplaySceneName);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowSettings()
    {
        _menuContainer.AddToClassList("hidden");
        _settingsContainer.RemoveFromClassList("hidden");
    }

    private void HideSettings()
    {
        _menuContainer.RemoveFromClassList("hidden");
        _settingsContainer.AddToClassList("hidden");
    }

    private void OnTransitionEnd(TransitionEndEvent e)
    {
        if (!e.stylePropertyNames.Contains("scale"))
            return;

        _newBadge.ToggleInClassList("new-badge-enlarged");
    }

    // --- Settings Logic ---

    private void OnVolumeChanged(ChangeEvent<float> e)
    {
        AudioListener.volume = e.newValue;
    }

    private void OnFullscreenChanged(ChangeEvent<bool> e)
    {
        Screen.fullScreen = e.newValue;
    }

    private void OnResolutionChanged(ChangeEvent<string> e)
    {
        // Get the index of the newly selected option
        int index = _resolutionDropdown.index;

        // Apply the resolution corresponding to that index
        if (index >= 0 && index < _resolutions.Length)
        {
            Resolution res = _resolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        }
    }
}