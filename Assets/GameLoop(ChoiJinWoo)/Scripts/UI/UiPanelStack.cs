using System.Collections.Generic;

// 거점 UI(지역 오버뷰/디테일/건설 선택/건물 정보/중앙 허브)가 공유하는 열림 순서 스택.
// 패널은 열릴 때(OnEnable) Push, 닫힐 때(OnDisable) Remove만 하면 되고, ESC는 CloseTop()으로
// 제일 나중에 연 패널부터 하나씩 닫는다.
public class UiPanelStack
{
    private readonly List<IClosablePanel> stack = new();

    public void Push(IClosablePanel panel)
    {
        stack.Remove(panel); // 이미 있으면 맨 위로 옮긴다(중복 방지)
        stack.Add(panel);
    }

    public void Remove(IClosablePanel panel)
    {
        stack.Remove(panel);
    }

    // 스택 제일 위 패널을 닫는다. 닫을 게 있었으면 true.
    public bool CloseTop()
    {
        if (stack.Count == 0) return false;

        var top = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        top.Close();
        return true;
    }
}
