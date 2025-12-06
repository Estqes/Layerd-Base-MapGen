using Estqes.MapGen;
using Estqes.MapGen.RegionGenerator;
using UnityEngine;

public enum HexTerrainType
{
    Plain, Hill
}
public class HexCellData : IRegionComponent
{
    public Region Region { get; set; }
    public HexTerrainType TerrainType { get; set; }
    public float height;
    private FastNoiseLite _noise;
    private float _heightAmplitude;
    private float _heightOffset;

    public HexCellData(HexTerrainType terrainType)
    {
        TerrainType = terrainType;
        _noise = new FastNoiseLite();

        switch (terrainType)
        {
            case HexTerrainType.Plain:
                _noise.SetFractalType(FastNoiseLite.FractalType.FBm);
                _noise.SetFrequency(0.01f);
                _noise.SetFractalOctaves(3);

                _heightAmplitude = 5f;
                _heightOffset = 2f;
                break;

            case HexTerrainType.Hill:

                _noise.SetFractalType(FastNoiseLite.FractalType.FBm);
                _noise.SetFrequency(0.005f);
                _noise.SetFractalOctaves(5);
                _noise.SetFractalGain(0.5f);

                _heightAmplitude = 100f;
                _heightOffset = 5f;
                break;
            default:
                break;
        }
    }

    public void AddedToCell()
    {
        
    }

    public float GetHeight(Cell cell)
    {
        float rawNoise = _noise.GetPositiveNoise(cell.Center.x, cell.Center.y, cell.Center.z);

        return (rawNoise * _heightAmplitude) + _heightOffset;
    }

}
