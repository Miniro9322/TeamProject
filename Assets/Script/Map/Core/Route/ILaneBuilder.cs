using System.Collections.Generic;

public interface ILaneBuilder
{
    IReadOnlyList<LaneData> BuildLanes(LaneInput input);
}
