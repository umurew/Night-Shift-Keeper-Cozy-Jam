using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class Door : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private string id;
    [SerializeField] private string interactionPrompt;

    [Space(10)]
    [SerializeField] private bool locked = false;
    [SerializeField] private bool inwards = true;
    [SerializeField] private float angle = 105f;
    [SerializeField] private float speed = 10f;

    [Space(10)]
    [SerializeField] private AudioClip doorUnlockClip;
    [SerializeField] private AudioClip doorOpenClip;
    [SerializeField] private AudioClip DoorCloseClip;

    private Collider _collider;
    private SceneBlackboard _sceneBlackboard;
    private AudioSource _audioSource;
    private readonly static WaitForSeconds _debounceDuration = new(0.3f);
    private Quaternion _closedRotation;
    private Quaternion _targetRotation;
    private string _cachedId;
    private bool _initialized = false;
    private bool _hasKey = false;
    private bool _isDoorOpened = false;
    private bool _debounce = false;

    public bool Interactable { get; set; }

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;
        _audioSource = GetComponent<AudioSource>();

        _closedRotation = transform.localRotation;
        _targetRotation = transform.localRotation;
        _cachedId = id.ToLower();

        _sceneBlackboard.ListenTo($"{_cachedId}_interactable", () =>
        {
            Interactable = _sceneBlackboard.Get<bool>($"{_cachedId}_interactable");
        });

        _sceneBlackboard.ListenTo($"{_cachedId}_locked", () =>
        {
            locked = _sceneBlackboard.Get<bool>($"{_cachedId}_locked");
        });

        _sceneBlackboard.ListenTo($"{_cachedId}_hasKey", () =>
        {
            _hasKey = _sceneBlackboard.Get<bool>($"{_cachedId}_hasKey");
        });

        _initialized = true;
        Debug.Log($"{GetType().Name} ({_cachedId}) initialized with the following dependencies: Scene Blackboard");
    }

    public bool IsLocked() => locked;

    public void Lock() => locked = true;

    public void Unlock() => locked = false;

    public string GetInteractPrompt()
    {
        if (!_initialized || _debounce)
            return string.Empty;

        if (locked)
            return _hasKey ? "Unlock" : "Locked";

        return _isDoorOpened ? "Close" : "Open";
    }

    public void Interact()
    {
        if (!_initialized || _debounce)
            return;

        StartCoroutine(DebounceRoutine());

        if (locked && _hasKey)
        {
            locked = false;
            _sceneBlackboard.Set($"{_cachedId}_locked", false);

            _audioSource.PlayOneShot(doorUnlockClip);

            return;
        }
        else if (locked && !_hasKey)
            return;

        _isDoorOpened = !_isDoorOpened;

        if (_isDoorOpened)
            _audioSource.PlayOneShot(doorOpenClip);
        else
            _audioSource.PlayOneShot(DoorCloseClip);

        float targetAngle = _isDoorOpened ? angle : 0f;

        if (inwards)
            targetAngle = -targetAngle;

        _targetRotation = _closedRotation * Quaternion.Euler(0f, targetAngle, 0f);
    }

    public void SetInteractPrompt(string text) => interactionPrompt = text;

    private IEnumerator DebounceRoutine()
    {
        _debounce = true;
        yield return _debounceDuration;
        _debounce = false;
    }

    private void Update()
    {
        if (!_initialized)
            return;

        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, _targetRotation, speed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (_collider == null)
            _collider = GetComponent<Collider>();

        Gizmos.color = ColorProvider.GizmoColors.InteractableCollider;
        Gizmos.DrawWireCube(_collider.bounds.center, _collider.bounds.size);

        Vector3 position = transform.position;

        Gizmos.color = ColorProvider.GizmoColors.DoorAxis;
        Vector3 axisStart = position - transform.up * 0.25f;
        Vector3 axisEnd = position + transform.up * 2.5f;
        Gizmos.DrawLine(axisStart, axisEnd);

        Gizmos.color = ColorProvider.GizmoColors.DoorArrow;
        Vector3 arrowBase = position + transform.right * 1.125f + transform.up * 1.125f;
        Vector3 arrowTip = arrowBase + transform.forward * 0.2f;

        Gizmos.DrawLine(arrowBase - transform.forward * 0.2f, arrowBase + transform.forward * 0.2f);

        Vector3 leftFlank = arrowTip - transform.right * 0.1f - transform.forward * 0.1f;
        Gizmos.DrawLine(leftFlank, arrowTip);

        Vector3 rightFlank = arrowTip + transform.right * 0.1f - transform.forward * 0.1f;
        Gizmos.DrawLine(rightFlank, arrowTip);

#if UNITY_EDITOR
        if (UnityEditor.SceneView.currentDrawingSceneView == null || UnityEditor.SceneView.currentDrawingSceneView.camera == null)
            return;

        Transform sceneCamera = UnityEditor.SceneView.currentDrawingSceneView.camera.transform;
        float distance = Vector3.Distance(sceneCamera.position, position);

        if (distance <= 10f)
        {
            Vector3 labelPosition = position + transform.right * 0.625f + transform.up * 1.125f;

            GUIStyle style = new()
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = ColorProvider.GizmoColors.HandleLabel;

            UnityEditor.Handles.Label(labelPosition, locked ? "LOCKED" : "UNLOCKED", style);
        }
#endif
    }
}
