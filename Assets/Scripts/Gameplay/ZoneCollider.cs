using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerZone : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string zoneId;

    private SceneBlackboard _sceneBlackboard;
    private bool _initialized = false;

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;
        zoneId = zoneId.ToLower();

        _initialized = true;
        Debug.Log($"{GetType().Name} ({zoneId}) initialized with dependencies: {sceneBlackboard.GetType().Name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_initialized)
            return;

        if (!other.gameObject.CompareTag("Player"))
            return;

        _sceneBlackboard.Set(SceneBlackboardKeys.Player.LastKnownPosition, zoneId);
        _sceneBlackboard.Set($"player_in{zoneId[0].ToString().ToUpper()}{zoneId[1..]}", true); // For example sets "player_inOffice"
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_initialized)
            return;

        if (!other.gameObject.CompareTag("Player"))
            return;

        _sceneBlackboard.Set($"player_in{zoneId[0].ToString().ToUpper()}{zoneId[1..]}", false);
    }
}
