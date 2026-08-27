using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerDialog : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string containerName = "dialog-container";
    [SerializeField] private string labelName = "dialog-label";

    private bool _initialized = false;
    private VisualElement _dialogContainer;
    private Label _dialogLabel;
    private SceneBlackboard _sceneBlackboard;

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;

        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            Debug.LogWarning($"UIDocument is missing or not initialized on {GetType().Name}.");
            return;
        }

        var root = uiDocument.rootVisualElement;

        _dialogContainer = root.Q<VisualElement>(containerName);
        _dialogLabel = root.Q<Label>(labelName);

        if (_dialogContainer == null || _dialogLabel == null)
        {
            Debug.LogWarning($"{GetType().Name} failed to initialize: UI elements were missing.");
            return;
        }

        _dialogLabel.text = string.Empty;

        _initialized = true;
        Debug.Log($"{GetType().Name} initialized.");
    }

    public async void SetDialog(string text)
    {
        if (!_initialized)
            return;

        _dialogLabel.text = text;
        await Task.Delay(1000);

        _dialogLabel.text = string.Empty;
    }
        
    public async Task SetDialogAsync(string text, int delaySeconds = 2)
    {
        if (!_initialized)
            return;

        _dialogLabel.text = text;
        await Task.Delay(delaySeconds * 1000);

        _dialogLabel.text = string.Empty;
    }
}
