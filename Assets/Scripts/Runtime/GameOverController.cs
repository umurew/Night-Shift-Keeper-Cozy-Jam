using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GameOverController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _rootElement;

    private void Awake()
    {
        // Get the UIDocument component attached to this GameObject
        _uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        // Get the root visual element containing your UI
        _rootElement = _uiDocument.rootVisualElement;

        // Hide the Game Over screen by default when the scene starts
        HideGameOver();
    }

    /// <summary>
    /// Call this method from your Game Manager or Player Health script when the player dies.
    /// </summary>
    public void ShowGameOver()
    {
        if (_rootElement != null)
        {
            // Makes the UI visible
            _rootElement.style.display = DisplayStyle.Flex;
        }
    }

    /// <summary>
    /// Hides the Game Over screen.
    /// </summary>
    public void HideGameOver()
    {
        if (_rootElement != null)
        {
            // Hides the UI completely
            _rootElement.style.display = DisplayStyle.None;
        }
    }
}