using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerShotgun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private ParentConstraint parentConstraint;
    [SerializeField] private GameObject shotgun;

    [Space(10)]
    [SerializeField] private GameObject barrelLight;
    [SerializeField] private AudioClip shotgunShootClip;
    [SerializeField] private AudioClip shotgunEquipClip;

    [Header("Configuration")]
    [SerializeField] private LayerMask shootableLayer;
    [SerializeField] private float weaponRange;

    private IInputService _inputService;
    private SceneBlackboard _sceneBlackboard;
    private PlayerDialog _playerDialog;
    private Camera _mainCamera;

    private bool _initialized = false;
    private bool _shotgunEquipped = false;
    private bool _canEquipShotgun = false;
    private bool _debounce = false;

    private Dictionary<string, int> _animationHashes;
    private static readonly WaitForSeconds cooldown = new(0.25f);
    private static readonly WaitForSeconds actionDelay = new(0.3f);
    private static readonly WaitForSeconds shootDelay = new(2f);

    public void Initialize(IInputService inputService, SceneBlackboard sceneBlackboard, PlayerDialog playerDialog, Camera mainCamera)
    {
        _inputService = inputService;
        _sceneBlackboard = sceneBlackboard;
        _playerDialog = playerDialog;
        _mainCamera = mainCamera;

        _animationHashes = new()
        {
            { "ShotgunEquipped", Animator.StringToHash("ShotgunEquipped") },
            { "ShotgunShoot", Animator.StringToHash("ShotgunShoot") },
        };

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Player.Shotgun.CanEquip, () =>
        {
            if (_canEquipShotgun != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Shotgun.CanEquip))
                _canEquipShotgun = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Shotgun.CanEquip);
        });

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Player.Shotgun.IsEquipped, () =>
        {
            if (_shotgunEquipped != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Shotgun.IsEquipped))
                _shotgunEquipped = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Shotgun.IsEquipped);
        });

        shotgun.SetActive(false);

        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with dependencies: {inputService.GetType().Name} | {sceneBlackboard.GetType().Name} | {playerDialog.GetType().Name} | {mainCamera.GetType().Name}");
    }

    private void Update()
    {
        if (!_initialized)
            return;

        if (_canEquipShotgun && !_debounce && _inputService.PlayerActions.ToggleShotgun.WasPressedThisFrame())
        {
            if (_sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Mop.IsEquipped))
            {
                _playerDialog.ExecuteDialog("I should put the mop away first.");
                return;
            }

            if (_sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Flashlight.IsEquipped))
            {
                _playerDialog.ExecuteDialog("I should put the flashlight away first.");
                return;
            }

            _debounce = true;

            _shotgunEquipped = !_shotgunEquipped;
            if (_shotgunEquipped)
                StartCoroutine(EquipShotgun());
            else
                StartCoroutine(UnequipShotgun());

            animator.SetBool(_animationHashes["ShotgunEquipped"], _shotgunEquipped);
            _sceneBlackboard.Set(SceneBlackboardKeys.Player.Shotgun.IsEquipped, _shotgunEquipped);
        }

        if (_shotgunEquipped && !_debounce && _inputService.PlayerActions.Attack.WasPressedThisFrame())
        {
            _debounce = true;
            StartCoroutine(Shoot());
        }
        
        UpdateLayerWeight();
    }

    private void UpdateLayerWeight()
    {
        int viewmodelLayerIndex = animator.GetLayerIndex("Shotgun Layer");

        if (viewmodelLayerIndex == -1)
        {
            Debug.LogWarning($"Shotgun layer was not found.");
            return;
        }

        float targetWeight = _shotgunEquipped ? 1f : 0f;
        float currentWeight = animator.GetLayerWeight(viewmodelLayerIndex);

        float newWeight = Mathf.MoveTowards(currentWeight, targetWeight, Time.deltaTime * 5f);
        animator.SetLayerWeight(viewmodelLayerIndex, newWeight);
    }

    private void SetSourceWeight(int sourceIndex, float weight)
    {
        if (parentConstraint == null || sourceIndex < 0 || sourceIndex >= parentConstraint.sourceCount)
        {
            Debug.LogWarning($"Invalid constraint or source index.");
            return;
        }

        ConstraintSource source = parentConstraint.GetSource(sourceIndex);
        source.weight = Mathf.Clamp01(weight);

        parentConstraint.SetSource(sourceIndex, source);
    }

    private IEnumerator Shoot()
    {
        barrelLight.SetActive(true);
        yield return new WaitForSeconds(Time.deltaTime);
        barrelLight.SetActive(false);

        audioSource.PlayOneShot(shotgunShootClip, 1);
        animator.SetTrigger(_animationHashes["ShotgunShoot"]);

        Raycast();
        yield return shootDelay;

        _debounce = false;
    }

    private IEnumerator UnequipShotgun()
    {
        audioSource.PlayOneShot(shotgunEquipClip);

        SetSourceWeight(0, 0);
        SetSourceWeight(1, 1);

        yield return actionDelay;

        shotgun.SetActive(false);

        yield return cooldown;
        _debounce = false;
    }

    private IEnumerator EquipShotgun()
    {
        shotgun.SetActive(true);

        SetSourceWeight(0, 1);
        SetSourceWeight(1, 0);

        audioSource.PlayOneShot(shotgunEquipClip);

        yield return cooldown;
        _debounce = false;
    }

    private void Raycast()
    {
        Vector3 screenCenter = new(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray ray = _mainCamera.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, weaponRange, shootableLayer))
        {
            Debug.Log("Direct hit on: " + hit.collider.gameObject.name);

            // You can add your damage logic here. For example:
            // Enemy enemyScript = hit.collider.GetComponent<Enemy>();
            // if (enemyScript != null) { enemyScript.TakeDamage(10); }

            Debug.DrawLine(ray.origin, hit.point, Color.red, 2f);
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * weaponRange, Color.green, 2f);
        }
    }
}
