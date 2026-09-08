public class TutorialState
{
    public bool Seen { get; private set; }

    public TutorialState(SaveSlot saveSlot)
    {
        if (TutorialEntryChoice.SkipTutorial.HasValue)
        {
            Seen = TutorialEntryChoice.SkipTutorial.Value;
            TutorialEntryChoice.Clear();
            return;
        }

        Seen = !SelectedSaveSlot.IsNewGame
            && saveSlot.TryReadLatest(SelectedSaveSlot.SlotId, out SaveFile file)
            && file.saveData.tutorialSeen;
    }

    public void MarkSeen()
    {
        Seen = true;
    }

    public void RestoreSeen(bool seen)
    {
        Seen = seen;
    }

    public void Reset()
    {
        Seen = false;
    }
}
