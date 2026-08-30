using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerRigidbodyCollider : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float pushMultiplier = 8f;
    [SerializeField] private float playerMass = 60f;

    private CharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rigidbody = hit.collider.attachedRigidbody;

        if (rigidbody == null || rigidbody.isKinematic || hit.moveDirection.y < -0.3f)
            return;

        Vector3 playerVelocity = _characterController.velocity;
        playerVelocity.y = 0f;

        Vector3 momentum = playerVelocity * playerMass;
        rigidbody.AddForceAtPosition(momentum / pushMultiplier, hit.point, ForceMode.Impulse);
    }
}
