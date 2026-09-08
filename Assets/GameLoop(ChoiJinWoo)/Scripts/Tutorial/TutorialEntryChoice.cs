public static class TutorialEntryChoice
{
    public static bool? SkipTutorial { get; private set; }

    public static void Set(bool skip) => SkipTutorial = skip;

    public static void Clear() => SkipTutorial = null;
}
