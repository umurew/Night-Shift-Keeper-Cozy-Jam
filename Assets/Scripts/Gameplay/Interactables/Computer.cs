using UnityEngine;

public class Computer : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private string interactionPrompt;

    private SceneBlackboard _sceneBlackboard;
    private bool _initialized = false;
    private readonly string _cachedId = "computer";

    public bool Interactable { get; set; }

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;

        _sceneBlackboard.ListenTo($"{_cachedId}_interactable", () =>
        {
            Interactable = _sceneBlackboard.Get<bool>($"{_cachedId}_interactable");
        });

        _sceneBlackboard.ListenTo($"{_cachedId}_interactionprompt", () =>
        {
            SetInteractPrompt(_sceneBlackboard.Get<string>($"{_cachedId}_interactionprompt"));
        });

        Interactable = false;
        _initialized = true;

        Debug.Log($"{GetType().Name} initialized.");
    }

    public void Interact()
    {
        if (!_initialized)
            return;

        _sceneBlackboard.Set($"{_cachedId}_interacted", true);
    }

    public string GetInteractPrompt() => interactionPrompt;

    public void SetInteractPrompt(string text) => interactionPrompt = text;
}
