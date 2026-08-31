using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private string id;
    [SerializeField] private string interactionPrompt;

    [Space(10)]
    [SerializeField] private Light connectedLight;

    [Space(10)]
    [SerializeField] private AudioClip lightSwitchToggleAudioClip;

    private SceneBlackboard _sceneBlackboard;
    private AudioSource _audioSource;
    private bool _initialized = false;
    private bool _debounce = false;
    private bool _switchEnabled = false;
    private readonly WaitForSeconds _debounceDuration = new(0.125f);

    public bool Interactable { get; set; }

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;
        _audioSource = GetComponent<AudioSource>();
        id = id.ToLower();

        _sceneBlackboard.ListenTo($"{id}_{SceneBlackboardKeys.LightSwitch.Interactable}", () =>
        {
            if (Interactable != _sceneBlackboard.Get<bool>($"{id}_{SceneBlackboardKeys.LightSwitch.Interactable}"))
                Interactable = _sceneBlackboard.Get<bool>($"{id}_{SceneBlackboardKeys.LightSwitch.Interactable}");
        });

        _sceneBlackboard.ListenTo($"{id}_{SceneBlackboardKeys.LightSwitch.Enabled}", () =>
        {
            if (connectedLight.enabled != _sceneBlackboard.Get<bool>($"{id}_{SceneBlackboardKeys.LightSwitch.Enabled}"))
                connectedLight.enabled = _sceneBlackboard.Get<bool>($"{id}_{SceneBlackboardKeys.LightSwitch.Enabled}");
        });

        Interactable = false;
        _initialized = true;
        Debug.Log($"{GetType().Name} ({id}) initialized with dependencies: {sceneBlackboard.GetType().Name}");
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

        _sceneBlackboard.Set($"{id}_{SceneBlackboardKeys.LightSwitch.Enabled}", _switchEnabled);
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
