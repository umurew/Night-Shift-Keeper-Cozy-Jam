using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Phone : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private AudioClip incomingClip;
    [SerializeField] private AudioClip speakClip;

    [Header("Configuration")]
    [SerializeField] private string interactionPrompt;

    private SceneBlackboard _sceneBlackboard;
    private AudioSource _audioSource;
    private bool _initialized = false;
    private bool _ringing = false;

    public bool Interactable { get; set; }

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;
        _audioSource = GetComponent<AudioSource>();

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Phone.Interactable, () =>
        {
            if (Interactable != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Phone.Interactable))
                Interactable = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Phone.Interactable);
        });

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Phone.Ringing, () =>
        {
            if (_ringing != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Phone.Ringing))
                _ringing = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Phone.Ringing);

            if (_ringing)
            {
                _audioSource.clip = incomingClip;
                _audioSource.loop = true;
                _audioSource.Play();
            }
            else
                _audioSource.Stop();
        });

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Phone.Speaking, () =>
        {
            _audioSource.PlayOneShot(speakClip);
        });

        Interactable = false;
        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with the following dependencies: {sceneBlackboard.GetType().Name}");
    }

    public void Interact()
    {
        if (!_initialized && !_ringing)
            return;

        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Ringing, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interacted, true);
    }

    public string GetInteractPrompt() => interactionPrompt;

    public void SetInteractPrompt(string text) => interactionPrompt = text;
}
