using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private string id;
    [SerializeField] private string interactionPrompt;
    [SerializeField] private Light connectedLight;
    [SerializeField] private AudioClip lightSwitchToggleAudioClip;

    private SceneBlackboard _sceneBlackboard;
    private AudioSource _audioSource;
    private WaitForSeconds _debounceDuration = new(0.125f);
    private string _cachedId;
    private bool _debounce = false;
    private bool _switchEnabled = false;
    private bool _initialized = false;

    public bool Interactable { get; set; }

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;
        _audioSource = GetComponent<AudioSource>();
        _cachedId = id.ToLower();

        _sceneBlackboard.ListenTo($"{_cachedId}_interactable", () =>
        {
            Interactable = _sceneBlackboard.Get<bool>($"{_cachedId}_interactable");
        });

        _sceneBlackboard.ListenTo($"{_cachedId}_enabled", () =>
        {
            connectedLight.enabled = _sceneBlackboard.Get<bool>($"{_cachedId}_enabled");
        });

        Interactable = false;
        _initialized = true;

        Debug.Log($"{GetType().Name} ({_cachedId}) initialized with the following dependencies: Scene Blackboard");
    }

    public void Interact()
    {
        if (!_initialized && _debounce)
            return;

        StartCoroutine(DebounceRoutine());

        transform.Rotate(0, 180f, 0f, Space.Self);
        _audioSource.PlayOneShot(lightSwitchToggleAudioClip);

        _switchEnabled = !_switchEnabled;
        if (_switchEnabled && connectedLight != null)
            connectedLight.enabled = true;
        else
            connectedLight.enabled = false;

        _sceneBlackboard.Set($"{_cachedId}_enabled", _switchEnabled);
    }

    public string GetInteractPrompt() => interactionPrompt;

    public void SetInteractPrompt(string text) => interactionPrompt = text;

    private IEnumerator DebounceRoutine()
    {
        _debounce = true;
        yield return _debounceDuration;
        _debounce = false;
    }
}
