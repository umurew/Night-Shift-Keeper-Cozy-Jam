using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerDayTransition : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument dayTransitionDocument;

    [Header("Configuration")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float darkHoldDuration = 0.3f;

    private SceneBlackboard _sceneBlackboard;
    private VisualElement _fadeOverlay;
    private Label _dayLabel;
    private Label _objectiveLabel;
    private bool _initialized = false;

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;

        var root = dayTransitionDocument.rootVisualElement;
        _fadeOverlay = root.Q<VisualElement>("fade-overlay");
        _dayLabel = root.Q<Label>("day-label");
        _objectiveLabel = root.Q<Label>("objective-label");

        _initialized = true;

        Debug.Log($"{GetType().Name} initialized.");
    }

    public async Awaitable ExecuteAsync(Action action)
    {
        if (!_initialized)
            return;

        await ExecuteDayTransitionAsync(_sceneBlackboard.Get<int>("day"), _sceneBlackboard.Get<string>("objective"), action);
    }

    private async Awaitable ExecuteDayTransitionAsync(int day, string objective, Action action)
    {
        _fadeOverlay.style.display = DisplayStyle.Flex;
        await Awaitable.NextFrameAsync();

        _dayLabel.text = $"DAY {day}";
        _objectiveLabel.text = objective;

        _fadeOverlay.AddToClassList("fade-overlay-visible");
        await Awaitable.WaitForSecondsAsync(0.5f);

        action();

        await Awaitable.WaitForSecondsAsync(fadeDuration + darkHoldDuration);

        _fadeOverlay.RemoveFromClassList("fade-overlay-visible");
        await Awaitable.WaitForSecondsAsync(fadeDuration);

        _fadeOverlay.style.display = DisplayStyle.None;
    }
}
