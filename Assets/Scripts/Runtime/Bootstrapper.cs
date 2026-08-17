using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private ObjectDataRegistry objectDataRegistry;
    [SerializeField] private SceneBlackboard sceneBlackboard;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject staticGeometry;
    [SerializeField] private GameObject dynamicGeometry;

    [Header("Prefab References")]
    [SerializeField] private InputService inputServicePrefab;

    private InputService _inputService;

    private void Awake()
    {
        _inputService = Instantiate(inputServicePrefab, transform);
        _inputService.Initialize();

        objectDataRegistry = Instantiate(objectDataRegistry);
        objectDataRegistry.Initialize();

        sceneBlackboard = Instantiate(sceneBlackboard);
        sceneBlackboard.ResetStates();
    }
}
