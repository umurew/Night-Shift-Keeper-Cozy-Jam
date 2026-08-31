using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerDayTransition : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument dayTransitionDocument;

    [Header("Configuration")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float overlayDuration = 0.3f;

    private SceneBlackboard _sceneBlackboard;

    private VisualElement _overlayElement;
    private Label _headerLabel;
    private Label _subLabel;

    private bool _initialized = false;

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;

        VisualElement rootVisualElement = dayTransitionDocument.rootVisualElement;
        _overlayElement = rootVisualElement.Q<VisualElement>("overlay");
        _headerLabel = rootVisualElement.Q<Label>("header-label");
        _subLabel = rootVisualElement.Q<Label>("sub-label");

        _initialized = true;
        Debug.Log($"{GetType().Name} initialized.");
    }

    public async UniTask ExecuteAsync(Action action)
    {
        if (!_initialized)
            return;

        await ExecuteDayTransitionAsync(_sceneBlackboard.Get<int>(SceneBlackboardKeys.Scene.Day), _sceneBlackboard.Get<string>(SceneBlackboardKeys.Scene.DayDescription), action);
    }

    private async UniTask ExecuteDayTransitionAsync(int day, string dayDescription, Action action)
    {
        _overlayElement.style.display = DisplayStyle.Flex;

        _headerLabel.text = $"DAY {day}";
        _subLabel.text = dayDescription;

        _overlayElement.AddToClassList("overlay-visible");
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: this.GetCancellationTokenOnDestroy());

        action();

        await UniTask.Delay(TimeSpan.FromSeconds(fadeDuration + overlayDuration), cancellationToken: this.GetCancellationTokenOnDestroy());

        _overlayElement.RemoveFromClassList("overlay-visible");
        await UniTask.Delay(TimeSpan.FromSeconds(fadeDuration), cancellationToken: this.GetCancellationTokenOnDestroy());

        _overlayElement.style.display = DisplayStyle.None;
    }
}