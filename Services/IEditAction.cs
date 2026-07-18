namespace WartalesEditor.Services;

public interface IEditAction
{
    string Description { get; }

    void Undo();

    void Redo();
}
