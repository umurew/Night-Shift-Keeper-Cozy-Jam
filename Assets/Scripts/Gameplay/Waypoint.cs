using DG.Tweening;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string id;
    [SerializeField] private GameObject canvas;

    private SceneBlackboard _sceneBlackboard;
    private CanvasGroup _canvasGroup;

    private bool _isActive;

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;
        id = id.ToLower();

        _canvasGroup = canvas.GetComponent<CanvasGroup>();

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Scene.Waypoints.Example.IsActive.Replace("id", id), () =>
        {
            if (_isActive != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Scene.Waypoints.Example.IsActive.Replace("id", id)))
                _isActive = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Scene.Waypoints.Example.IsActive.Replace("id", id));

            float targetAlpha = _isActive ? 1f : 0f;
            DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, targetAlpha, 0.3f);
        });

        Debug.Log($"{GetType().Name} initialized with dependencies: {sceneBlackboard.GetType().Name}");
    }
}
