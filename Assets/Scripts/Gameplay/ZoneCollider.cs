using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerZone : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string zoneId;

    private SceneBlackboard _sceneBlackboard;
    private bool _initialized = false;
    private string cachedId;

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;
        cachedId = zoneId.ToLower();

        _initialized = true;

        Debug.Log($"{GetType().Name} ({cachedId}) initialized with the following dependencies: Scene Blackboard");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_initialized)
            return;

        if (!other.gameObject.CompareTag("Player"))
            return;

        _sceneBlackboard.Set($"player_in_{cachedId}", true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_initialized)
            return;

        if (!other.gameObject.CompareTag("Player"))
            return;

        _sceneBlackboard.Set($"player_in_{cachedId}", false);
    }
}
