using System.Collections.Generic;

public class WindShelterData
{
    private readonly Dictionary<Tile, WindShelter> shelterByTile = new();

    public void KeepShelter(Tile tile, WindShelter shelter)
    {
        shelterByTile[tile] = shelter;
    }

    public WindShelter ReadShelter(Tile tile)
    {
        return shelterByTile[tile];
    }
}
