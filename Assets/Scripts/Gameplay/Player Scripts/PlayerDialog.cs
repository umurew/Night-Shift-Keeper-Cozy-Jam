using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerDialog : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument dialogDocument;

    private VisualElement _dialogContainer;

    private bool _initialized = false;

    public void Initialize()
    {
        VisualElement rootVisualElement = dialogDocument.rootVisualElement;
        _dialogContainer = rootVisualElement.Q<VisualElement>("dialog-container");

        _dialogContainer.Clear();

        _initialized = true;
        Debug.Log($"{GetType().Name} initialized.");
    }

    public async void ExecuteDialog(string text, float duration = 1f, bool self = true)
    {
        if (!_initialized)
            return;

        Label dialogLabel = new() { text = text };
        dialogLabel.AddToClassList("dialog-label");

        if (self)
            dialogLabel.AddToClassList("self");
        else
            dialogLabel.AddToClassList("non-self");

        _dialogContainer.Add(dialogLabel);

        await UniTask.Delay(Mathf.FloorToInt(duration * 1000));
        dialogLabel.RemoveFromHierarchy();
    }

    public async UniTask ExecuteDialogAsync(string text, float duration = 2f, bool self = true)
    {
        if (!_initialized)
            return;

        Label dialogLabel = new() { text = text };
        dialogLabel.AddToClassList("dialog-label");

        if (self)
            dialogLabel.AddToClassList("self");
        else
            dialogLabel.AddToClassList("non-self");

        _dialogContainer.Add(dialogLabel);

        await UniTask.Delay(Mathf.FloorToInt(duration * 1000));
        dialogLabel.RemoveFromHierarchy();
    }
}
