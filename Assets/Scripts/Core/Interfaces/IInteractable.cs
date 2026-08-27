public interface IInteractable
{
    bool Interactable { get; set; }
    void Interact();
    void SetInteractPrompt(string text);
    string GetInteractPrompt();
}
