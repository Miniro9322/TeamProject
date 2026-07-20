using TMPro;
using UnityEngine;
using VContainer;
public class SpawnerTest : MonoBehaviour
{
    public TMP_Dropdown _dropdown;
    public TMP_Text text;
    private WaveSpawner waveSpawner;
    private int Region = 1;

    [Inject]
    private void Construct(WaveSpawner waveSpawner)
    {
        this.waveSpawner = waveSpawner;
    }
    public void Inputregion(int region)
    {
        Region = region+1;
        text.text = $"{Region}지역";
    }
    public void SpawnWave(int currentStage)
    {
        waveSpawner.SpawnWave(Region,currentStage);
    }
}
