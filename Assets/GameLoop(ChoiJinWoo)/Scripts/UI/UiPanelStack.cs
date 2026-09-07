using System.Collections.Generic;

public class UiPanelStack
{
    private readonly List<IClosablePanel> stack = new();

    public bool HasAny => stack.Count > 0;

    public void Push(IClosablePanel panel)
    {
        stack.Remove(panel);
        stack.Add(panel);
    }

    public void Remove(IClosablePanel panel)
    {
        stack.Remove(panel);
    }

    public bool IsTop(IClosablePanel panel)
    {
        return stack.Count > 0 && ReferenceEquals(stack[^1], panel);
    }
}
