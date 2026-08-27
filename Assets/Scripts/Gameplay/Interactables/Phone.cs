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
    private readonly string _cachedId = "phone";
    private bool _incoming = false;

    public bool Interactable { get; set; }

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;
        _audioSource = GetComponent<AudioSource>();

        _sceneBlackboard.ListenTo($"{_cachedId}_interactable", () =>
        {
            Interactable = _sceneBlackboard.Get<bool>($"{_cachedId}_interactable");
        });

        _sceneBlackboard.ListenTo($"{_cachedId}_interactionprompt", () =>
        {
            SetInteractPrompt(_sceneBlackboard.Get<string>($"{_cachedId}_interactionprompt"));
        });

        _sceneBlackboard.ListenTo($"{_cachedId}_incoming", () =>
        {
            _incoming = _sceneBlackboard.Get<bool>($"{_cachedId}_incoming");

            if (_incoming)
            {
                _audioSource.clip = incomingClip;
                _audioSource.loop = true;
                _audioSource.Play();
            }
            else
                _audioSource.Stop();
        });

        _sceneBlackboard.ListenTo($"{_cachedId}_speak", () =>
        {
            _audioSource.PlayOneShot(speakClip);
        });

        Interactable = false;
        _initialized = true;

        Debug.Log($"{GetType().Name} initialized.");
    }

    public void Interact()
    {
        if (!_initialized && !_incoming)
            return;

        _sceneBlackboard.Set($"{_cachedId}_incoming", false);
        _sceneBlackboard.Set($"{_cachedId}_interacted", true);
    }

    public string GetInteractPrompt() => interactionPrompt;

    public void SetInteractPrompt(string text) => interactionPrompt = text;
}
