using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerMop : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ParentConstraint parentConstraint;
    [SerializeField] private GameObject mop;

    private SceneBlackboard _sceneBlackboard;

    private bool _initialized = false;
    private bool _mopEquipped = false;

    private Dictionary<string, int> _animationHashes;
    private static readonly WaitForSeconds cooldown = new(0.25f);
    private static readonly WaitForSeconds actionDelay = new(0.5f);

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;

        _animationHashes = new()
        {
            { "MopEquipped", Animator.StringToHash("MopEquipped") }
        };

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Player.Mop.IsEquipped, () =>
        {
            if (_mopEquipped != _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Mop.IsEquipped))
                _mopEquipped = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Mop.IsEquipped);

            if (_mopEquipped)
                StartCoroutine(EquipMop());
            else
                StartCoroutine(UnequipMop());
        });

        mop.SetActive(false);
        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with dependencies: {sceneBlackboard.GetType().Name}");
    }

    private void Update()
    {
        if (!_initialized)
            return;

        UpdateLayerWeight();
    }

    private void UpdateLayerWeight()
    {
        int viewmodelLayerIndex = animator.GetLayerIndex("Mop Layer");

        if (viewmodelLayerIndex == -1)
        {
            Debug.LogWarning($"Mop layer was not found.");
            return;
        }

        float targetWeight = _mopEquipped ? 1f : 0f;
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

    private IEnumerator UnequipMop()
    {
        yield return actionDelay;

        animator.SetBool(_animationHashes["MopEquipped"], false);

        SetSourceWeight(0, 0);
        SetSourceWeight(1, 1);

        mop.SetActive(false);

        yield return cooldown;
    }

    private IEnumerator EquipMop()
    {
        mop.SetActive(true);
        animator.SetBool(_animationHashes["MopEquipped"], true);

        SetSourceWeight(0, 1);
        SetSourceWeight(1, 0);

        yield return cooldown;
    }
}
