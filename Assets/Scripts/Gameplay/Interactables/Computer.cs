using UnityEngine;

public class Computer : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private string interactionPrompt;

    private SceneBlackboard _sceneBlackboard;
    private bool _initialized = false;

    public bool Interactable { get; set; }

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Computer.Interactable, () =>
        {
            if (Interactable != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Computer.Interactable))
                Interactable = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Computer.Interactable);
        });

        Interactable = false;
        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with dependencies: {sceneBlackboard.GetType().Name}");
    }

    public void Interact()
    {
        if (!_initialized)
            return;

        _sceneBlackboard.Set(SceneBlackboardKeys.Computer.Interacted, true);
    }

    public string GetInteractPrompt() => interactionPrompt;

    public void SetInteractPrompt(string text) => interactionPrompt = text;
}
