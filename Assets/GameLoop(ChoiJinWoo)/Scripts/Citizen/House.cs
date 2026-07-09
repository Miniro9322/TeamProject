using UnityEngine;
using VContainer;

public class House : MonoBehaviour
{
    [SerializeField] private int maxCitizenAmount;
    private CitizenManager manager;

    [Inject]
    public void Construct(CitizenManager manager)
    {
        this.manager = manager;
    }

    private void Awake()
    {
        manager.IncreaseMaxCitizen(maxCitizenAmount);
    }
}
