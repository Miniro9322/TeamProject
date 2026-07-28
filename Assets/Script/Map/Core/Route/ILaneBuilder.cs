using System.Collections.Generic;

public interface ILaneBuilder
{
    // 맵 입력을 기준으로 각 적 스폰 지점의 이동 레인을 계산합니다.
    IReadOnlyList<LaneData> BuildLanes(LaneInput input);
}
