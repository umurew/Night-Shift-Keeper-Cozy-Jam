using UnityEngine;

public class Moppable : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private string id;
    [SerializeField] private string interactionPrompt;

    private SceneBlackboard _sceneBlackboard;
    private PlayerDialog _playerDialog;
    private bool _initialized = false;
    private bool _isMopEquipped = false;

    public void Initialize(SceneBlackboard sceneBlackboard, PlayerDialog playerDialog)
    {
        _sceneBlackboard = sceneBlackboard;
        _playerDialog = playerDialog;
        id = id.ToLower();

        string interactable_key = SceneBlackboardKeys.Scene.Decals.Example.Interactable.Replace("id", id);
        _sceneBlackboard.ListenTo(interactable_key, () =>
        {
            if (Interactable != _sceneBlackboard.Get<bool>(interactable_key))
                Interactable = _sceneBlackboard.Get<bool>(interactable_key);
        });

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Player.Mop.IsEquipped, () =>
        {
            if (_isMopEquipped != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Mop.IsEquipped))
                _isMopEquipped = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Mop.IsEquipped);
        });

        Interactable = false;
        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with dependencies: {sceneBlackboard.GetType().Name}");
    }

    public bool Interactable { get; set; }

    public void Interact()
    {
        if (!_initialized)
            return;

        if (!_isMopEquipped)
        {
            _playerDialog.ExecuteDialog("I need the mop for that.");
            return;
        }

        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Decals.Example.Removed.Replace("id", id), true);
        Destroy(gameObject);
    }

    public string GetInteractPrompt() => interactionPrompt;

    public void SetInteractPrompt(string text) => interactionPrompt = text;
}
