using Estqes.MapGen;
using Estqes.MapGen.RegionGenerator;

public class BiomeComponent : IRegionComponent
{
    public Region Region { get; set; }
    public Biome Biome { get; private set; }

    public BiomeComponent(Biome biome)
    {
        Biome = biome;
    }

    public void AddedToCell()
    {
    }
}