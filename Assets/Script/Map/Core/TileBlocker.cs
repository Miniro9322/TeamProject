using UnityEngine;

 
//Tile 위에 배치된 오브젝트가 적을 몇 마리까지 저지할 수 있는지 나타낸다.
//근접 유닛 프리팹에 붙여 개별 저지 수를 조정할 수 있고, 없으면 MapGame이 기본값으로 붙인다.
[DisallowMultipleComponent]
public class TileBlocker : MonoBehaviour
{
    [Min(0)]
    public int blockCapacity = 1;

    public int Capacity => Mathf.Max(0, blockCapacity);
}
