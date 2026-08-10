using System;
using UnityEngine;

public static class WindShelterQuery
{
    public static bool IsSheltered(WindShelter shelter, Vector2Int windDirection)
    {
        WindShelter currentShelter = ResolveDirection(windDirection);
        return (shelter & currentShelter) == currentShelter;
    }

    private static WindShelter ResolveDirection(Vector2Int windDirection)
    {
        if (windDirection == GridCalculator.Down) return WindShelter.FromNorth;
        if (windDirection == GridCalculator.Up) return WindShelter.FromSouth;
        if (windDirection == GridCalculator.Right) return WindShelter.FromWest;
        if (windDirection == GridCalculator.Left) return WindShelter.FromEast;

        throw new ArgumentOutOfRangeException(nameof(windDirection));
    }
}
