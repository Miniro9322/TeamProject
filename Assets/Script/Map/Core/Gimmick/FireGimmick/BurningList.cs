using System.Collections.Generic;
using UnityEngine;

// 불타는 유닛을 처리 순서대로 보관한다. 계산과 피해 적용은 하지 않는다.
public static class BurningList
{
    private static readonly LinkedList<BurningUnit> waitingLine = new();
    private static readonly Dictionary<GameObject, LinkedListNode<BurningUnit>> lineSeats = new();

    // 도메인 리로드를 끈 플레이 모드에서 지난 세션의 줄이 남는 것을 막는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        waitingLine.Clear();
        lineSeats.Clear();
    }

    public static bool IsEmpty => waitingLine.Count == 0;
    public static BurningUnit FirstBurning => waitingLine.First.Value;

    public static bool TryGetBurning(
        GameObject unitObject,
        out BurningUnit burningUnit)
    {
        bool hasBurningUnit = lineSeats.TryGetValue(unitObject, out LinkedListNode<BurningUnit> lineSeat);
        burningUnit = hasBurningUnit ? lineSeat.Value : null;
        return hasBurningUnit;
    }

    public static void AddLast(BurningUnit burningUnit)
    {
        LinkedListNode<BurningUnit> lineSeat = waitingLine.AddLast(burningUnit);
        lineSeats.Add(burningUnit.UnitObject, lineSeat);
    }

    public static void RemoveFirst()
    {
        BurningUnit burningUnit = waitingLine.First.Value;
        waitingLine.RemoveFirst();
        lineSeats.Remove(burningUnit.UnitObject);
    }

    public static void SendFirstToBack()
    {
        LinkedListNode<BurningUnit> lineSeat = waitingLine.First;

        waitingLine.RemoveFirst();
        waitingLine.AddLast(lineSeat);
    }
}
