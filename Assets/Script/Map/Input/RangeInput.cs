using UnityEngine;

// 마우스를 누르고 있는 동안에만 사거리 보관소가 차 있게 한다.
// 사거리를 직접 구하거나 그리지는 않고 RangeCalc·RangeStore에 시킨다.
public class RangeInput : MonoBehaviour
{
    [SerializeField] private MapInput input;

    // MapAssemble이 조립할 때 넣어준다
    public PointerPick pointerPick;
    public RangeCalc rangeCalc;
    public RangeTileData rangeStore;

    private void OnEnable()
    {
        input.Pressed += KeepRange;
    }

    private void OnDisable()
    {
        input.Pressed -= KeepRange;
    }

    private void Update()
    {
        DropRange();
    }

    // 누른 자리의 유닛이 닿는 칸을 보관소에 채운다.
    private void KeepRange()
    {
        rangeStore.KeepRange(rangeCalc.GetRange(pointerPick.UnderPointer()));
    }

    // 손을 떼면 보관소를 비운다.
    // UI 위에서 떼면 뗌 신호가 오지 않으므로 눌림 상태를 직접 본다.
    private void DropRange()
    {
        if (input.LeftHolding)
        {
            return;
        }

        rangeStore.ClearRange();
    }
}
