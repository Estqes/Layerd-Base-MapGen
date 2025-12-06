using Estqes.MapGen;
using UnityEngine;

public class HexTerrainDrawer : DrawerByRegions<float, HexCellData>
{
    public HexTerrainDrawer(RegionLayer region) : base(region)
    {
    }
    public HexTerrainDrawer(string region) : base(region)
    {
    }
    public override float Medium(WeightValue<HexCellData>[] values, float weightSum, Cell cell)
    {
        var result = 0f;
        foreach (var item in values)
        {
            result += (item.component.GetHeight(cell)) * (item.weight);
        }
        return result/weightSum;
    }

}