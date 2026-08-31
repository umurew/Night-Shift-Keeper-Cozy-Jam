using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Mop : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private string interactionPrompt;

    private SceneBlackboard _sceneBlackboard;
    private PlayerDialog _playerDialog;
    private bool _initialized = false;
    private bool _isEquipped = false;

    public void Initialize(SceneBlackboard sceneBlackboard, PlayerDialog playerDialog)
    {
        _sceneBlackboard = sceneBlackboard;
        _playerDialog = playerDialog;

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Player.Mop.Interactable, () =>
        {
            if (Interactable != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Mop.Interactable))
                Interactable = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Mop.Interactable);
        });

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Player.Mop.IsEquipped, () =>
        {
            if (_isEquipped != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Mop.IsEquipped))
                _isEquipped = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Mop.IsEquipped);
        });

        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with dependencies: {sceneBlackboard.GetType().Name} | {playerDialog.GetType().Name}");
    }

    public bool Interactable { get; set; }

    public void Interact()
    {
        if (!_initialized)
            return;

        if (_sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Flashlight.IsEquipped))
        {
            _playerDialog.ExecuteDialog("I should put my flashlight away first.");
            return;
        }

        if (!_isEquipped)
            SetInteractPrompt("Put Back");
        else
            SetInteractPrompt("Take");

        _sceneBlackboard.Set(SceneBlackboardKeys.Player.Mop.IsEquipped, !_isEquipped);
    }

    public string GetInteractPrompt() => interactionPrompt;

    public void SetInteractPrompt(string text) => interactionPrompt = text;
}
