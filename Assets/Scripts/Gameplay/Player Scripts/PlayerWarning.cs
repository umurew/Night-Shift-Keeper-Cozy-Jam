using UnityEngine;
using UnityEngine.UIElements;

public class PlayerWarning : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;

    private SceneBlackboard _sceneBlackboard;

    private Label _warningLabel;

    private bool _isVisible = false;

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;

        VisualElement rootVisualElement = uiDocument.rootVisualElement;
        _warningLabel = rootVisualElement.Q<Label>("warning-label");

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Scene.Warning.IsVisible, () =>
        {
            if (_isVisible != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Scene.Warning.IsVisible))
                _isVisible = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Scene.Warning.IsVisible);

            if (_isVisible)
                _warningLabel.RemoveFromClassList("hidden");
            else
                _warningLabel.AddToClassList("hidden");
        });

        if (!_warningLabel.ClassListContains("hidden"))
            _warningLabel.AddToClassList("hidden");

        _warningLabel.schedule.Execute(() =>
        {
            _warningLabel.ToggleInClassList("warning-label-pulsing");
        }).Every(100);

        Debug.Log($"{GetType().Name} initialized with dependencies: {sceneBlackboard.GetType().Name}");
    }
}
