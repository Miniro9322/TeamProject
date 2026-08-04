using System.Collections.Generic;
using UnityEngine;

// 불타는 유닛을 맞을 순서대로 줄 세워 들고 있는 곳. 담고 빼기만 하고 계산도 때리기도 하지 않는다.
public static class BurningList
{
    // 줄. 맨 앞이 가장 먼저 맞을 유닛이다.
    private static readonly LinkedList<BurningUnit> waitingLine = new();

    // 유닛으로 줄의 자리를 바로 찾는 표. 줄을 훑지 않아도 된다.
    private static readonly Dictionary<GameObject, LinkedListNode<BurningUnit>> lineSeats = new();

    // 도메인 리로드를 끈 플레이 모드에서 지난 세션의 줄이 남는 것을 막는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        waitingLine.Clear();
        lineSeats.Clear();
    }

    // 줄에 아무도 없는가.
    public static bool IsEmpty => waitingLine.Count == 0;

    // 맨 앞에 선 유닛.
    public static BurningUnit FirstBurning => waitingLine.First.Value;

    // 이 유닛을 줄 맨 뒤에 세운다.
    public static void AddLast(BurningUnit burning)
    {
        lineSeats[burning.UnitObject] = waitingLine.AddLast(burning);
    }

    // 이 유닛을 줄에서 뺀다.
    public static void RemoveUnit(GameObject unitObject)
    {
        waitingLine.Remove(lineSeats[unitObject]);
        lineSeats.Remove(unitObject);
    }

    // 맨 앞 유닛을 줄 맨 뒤로 보낸다. 자리표는 같은 자리를 그대로 가리킨다.
    public static void SendFirstToBack()
    {
        LinkedListNode<BurningUnit> seat = waitingLine.First;

        waitingLine.RemoveFirst();
        waitingLine.AddLast(seat);
    }
}
