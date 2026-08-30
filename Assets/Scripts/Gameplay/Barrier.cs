using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Barrier : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string id;

    private SceneBlackboard _sceneBlackboard;
    private Collider _collider;

    private bool _isActive = false;

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;
        _collider = GetComponent<Collider>();
        id = id.ToLower();

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Scene.Barriers.Example.IsActive.Replace("id", id), () =>
        {
            if (_isActive != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Scene.Barriers.Example.IsActive.Replace("id", id)))
                _isActive = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Scene.Barriers.Example.IsActive.Replace("id", id));

            _collider.enabled = _isActive;
        });
    }
}
