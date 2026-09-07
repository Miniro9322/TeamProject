public class DayNightData
{
    private bool isNight;

    public bool IsNight => isNight;

    public void Keep(bool value)
    {
        isNight = value;
    }
}
