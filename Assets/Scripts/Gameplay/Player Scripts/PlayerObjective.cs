using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerObjective : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument objectiveDocument;

    private SceneBlackboard _sceneBlackboard;
    private bool _initialized = false;

    private VisualElement _objectiveContainer;

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;

        VisualElement rootVisualElement = objectiveDocument.rootVisualElement;
        _objectiveContainer = rootVisualElement.Q<VisualElement>("objective-container");

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Scene.Objectives.MainObjective, async () => await AddMainObjective(_sceneBlackboard.Get<string>(SceneBlackboardKeys.Scene.Objectives.MainObjective)));

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Scene.Objectives.SubObjective, async () => await AddSubObjective(_sceneBlackboard.Get<string>(SceneBlackboardKeys.Scene.Objectives.SubObjective)));

        _sceneBlackboard.ListenTo($"{SceneBlackboardKeys.Scene.Objectives.MainObjective}{SceneBlackboardKeys.Suffix.Completed}", async () =>
        {
            VisualElement objectiveHeader = _objectiveContainer.Q<VisualElement>(null, "objective-header") ?? throw new NullReferenceException($"Objective header was null!");
            Label headerText = objectiveHeader.Q<Label>(null, "header-text") ?? throw new NullReferenceException($"Objective header's Label element was null!");

            headerText.text = $"<s>{headerText.text}</s>";

            await Task.Delay(2500);
            RemoveObjectives();
        });

        _sceneBlackboard.ListenTo($"{SceneBlackboardKeys.Scene.Objectives.SubObjective}{SceneBlackboardKeys.Suffix.Completed}", () =>
        {
            string rawObjectiveQuery = _sceneBlackboard.Get<string>($"{SceneBlackboardKeys.Scene.Objectives.SubObjective}{SceneBlackboardKeys.Suffix.Completed}");
            string objectiveText = rawObjectiveQuery[1..];

            VisualElement subObjectiveList = _objectiveContainer.Q<VisualElement>("sub-objective-list") ?? throw new NullReferenceException($"Sub-objective list was null!");

            foreach (VisualElement subObjectiveItem in subObjectiveList.Children())
            {
                Label subText = subObjectiveItem.Q<Label>(null, "sub-text");
                if (subText == null)
                {
                    Debug.LogWarning($"Sub-objective's Label element was null!");
                    continue;
                }

                if (subText.text != objectiveText)
                    continue;

                VisualElement squareIcon = subObjectiveItem.Q<VisualElement>(null, "square-icon");
                squareIcon.RemoveFromClassList("icon-outline");
                squareIcon.AddToClassList("icon-filled-sub");

                subText.text = $"<s>{subText.text}</s>";
                break;
            }
        });

        RemoveObjectives(true);

        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with the following dependencies: {sceneBlackboard.GetType().Name}");
    }

    private async void RemoveObjectives(bool self = false)
    {
        if (!self && !_initialized)
            return;

        VisualElement objectiveHeader = _objectiveContainer.Q<VisualElement>(null, "objective-header");
        if (objectiveHeader != null)
        {
            VisualElement squareIconFilled = objectiveHeader.Q<VisualElement>(null, "square-icon");
            Label headerText = objectiveHeader.Q<Label>(null, "header-text");

            squareIconFilled?.RemoveFromClassList("fader-visible");
            headerText?.RemoveFromClassList("fader-visible");
        }

        VisualElement subObjectiveList = _objectiveContainer.Q<VisualElement>(null, "sub-objective-list");
        if (subObjectiveList != null)
        {
            foreach (VisualElement subObjectiveItem in subObjectiveList.Children())
            {
                VisualElement squareIconFilled = subObjectiveItem.Q<VisualElement>(null, "square-icon");
                Label subText = subObjectiveItem.Q<Label>(null, "sub-text");

                squareIconFilled?.RemoveFromClassList("fader-visible");
                subText?.RemoveFromClassList("fader-visible");
            }
                
        }

        if (!self)
            await Task.Delay(400);

        _objectiveContainer.Clear();
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Objectives.ObjectiveCount, 0);
    }

    private async Task AddMainObjective(string text)
    {
        if (!_initialized)
            return;

        VisualElement objectiveHeader = new();
        objectiveHeader.AddToClassList("objective-header");

        VisualElement squareIconFilled = new();
        squareIconFilled.AddToClassList("square-icon");
        squareIconFilled.AddToClassList("icon-filled-header");
        squareIconFilled.AddToClassList("fader-hidden");

        Label headerText = new() { text = text };
        headerText.AddToClassList("header-text");
        headerText.AddToClassList("fader-hidden");

        objectiveHeader.Add(squareIconFilled);
        objectiveHeader.Add(headerText);
        _objectiveContainer.Add(objectiveHeader);

        await Task.Delay(10);
        squareIconFilled.AddToClassList("fader-visible");
        headerText.AddToClassList("fader-visible");

        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Objectives.ObjectiveCount, _sceneBlackboard.Get<int>(SceneBlackboardKeys.Scene.Objectives.ObjectiveCount) + 1);
    }

    private async Task AddSubObjective(string text)
    {
        if (!_initialized)
            return;

        VisualElement subObjectiveList = _objectiveContainer.Q<VisualElement>("sub-objective-list");

        if (subObjectiveList == null)
        {
            subObjectiveList = new() { name = "sub-objective-list" };
            subObjectiveList.AddToClassList("sub-objective-list");
            _objectiveContainer.Add(subObjectiveList);
        }

        VisualElement subObjectiveItem = new();
        subObjectiveItem.AddToClassList("sub-objective-item");

        VisualElement squareIconOutline = new();
        squareIconOutline.AddToClassList("square-icon");
        squareIconOutline.AddToClassList("icon-outline");
        squareIconOutline.AddToClassList("fader-hidden");

        Label subText = new() { text = text[1..] };
        subText.AddToClassList("sub-text");
        subText.AddToClassList("fader-hidden");

        subObjectiveItem.Add(squareIconOutline);
        subObjectiveItem.Add(subText);
        subObjectiveList.Add(subObjectiveItem);

        await Task.Delay(10);
        squareIconOutline.AddToClassList("fader-visible");
        subText.AddToClassList("fader-visible");

        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Objectives.ObjectiveCount, _sceneBlackboard.Get<int>(SceneBlackboardKeys.Scene.Objectives.ObjectiveCount) + 1);
    }
}
