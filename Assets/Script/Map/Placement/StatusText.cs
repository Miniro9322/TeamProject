// 화면에 보여줄 상태 문자열 하나를 보관한다.
public class StatusText
{
    private string _text = "";

    public string Text
    {
        get { return _text; }
    }

    public void Set(string value)
    {
        _text = value;
    }
}
