using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class Elk : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform escapeDestination;
    [SerializeField] private SkinnedMeshRenderer meshRenderer;

    [Space(10)]
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip screamClip;
    [SerializeField] private AudioClip chaseClip;

    private SceneBlackboard _sceneBlackboard;
    private Transform _playerTransform;
    private NavMeshAgent _navMeshAgent;
    private AudioSource _audioSource;
    private GameObject _player;

    private readonly int _animatorMovingHash = Animator.StringToHash("Moving");
    private readonly int _animatorScreamingHash = Animator.StringToHash("Screaming");

    private bool _initialized = false;
    private bool _isVisible = false;
    private bool _chasing = false;
    private int _damageDealt = 0;

    public void Initialize(SceneBlackboard sceneBlackboard, Transform playerTransform, Transform _escapeDestinoation, GameObject player)
    {
        _sceneBlackboard = sceneBlackboard;
        _playerTransform = playerTransform;
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _audioSource = GetComponent<AudioSource>();
        _player = player;

        escapeDestination = _escapeDestinoation;

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Elk.IsVisible, () =>
        {
            if (_isVisible != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Elk.IsVisible))
            {
                _isVisible = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Elk.IsVisible);

                if (!_isVisible)
                {
                    _audioSource.Stop();
                }
                   
            }
        });

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Elk.Screaming, () =>
        {
            if (_sceneBlackboard.Get<bool>(SceneBlackboardKeys.Elk.Screaming))
            {
                animator.SetTrigger(_animatorScreamingHash);
                _audioSource.PlayOneShot(screamClip);
            }
            else
                _audioSource.Stop();

        });

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Elk.Chasing, () =>
        {
            if (_chasing != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Elk.Chasing))
            {
                _chasing = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Elk.Chasing);
                animator.SetBool(_animatorScreamingHash, false);
                animator.SetBool(_animatorMovingHash, _chasing);

                if (!_chasing)
                    _navMeshAgent.ResetPath();
                else
                {
                    _audioSource.clip = chaseClip;
                    _audioSource.loop = true;
                    _audioSource.Play();
                }
                    
            }
        });

        _navMeshAgent.updatePosition = true;
        _navMeshAgent.updateRotation = true;

        _damageDealt = 0;

        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with dependencies: {sceneBlackboard.GetType().Name}");
    }

    private void Update()
    {
        if (!_initialized)
            return;

        if (_isVisible)
            meshRenderer.enabled = true;
        else
            meshRenderer.enabled = false;

        if (_chasing)
            _navMeshAgent.SetDestination(_playerTransform.position);
    }

    public async void DealDamage()
    {
        _damageDealt += 1;
        _audioSource.PlayOneShot(hurtClip, 1);
        _chasing = false;

        if (_damageDealt >= 2)
        {
            _navMeshAgent.ResetPath();
            _navMeshAgent.SetDestination(escapeDestination.position);
            _navMeshAgent.speed = 20;

            _sceneBlackboard.Set(SceneBlackboardKeys.Elk.Chasing, false);
            _sceneBlackboard.Set(SceneBlackboardKeys.Elk.Screaming, false);

            await UniTask.Delay(5000);
            _sceneBlackboard.Set(SceneBlackboardKeys.Elk.IsVisible, false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == _player.GetComponent<Collider>())
        {
            _audioSource.loop = false;
            _audioSource.clip = screamClip;
            _audioSource.Play();

            _sceneBlackboard.Set("player_caught", true);
            _sceneBlackboard.Set(SceneBlackboardKeys.Elk.Chasing, false);
        }
    }
}
