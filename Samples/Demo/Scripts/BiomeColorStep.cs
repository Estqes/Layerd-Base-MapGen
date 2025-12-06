using Estqes.MapGen;
using Estqes.MapGen.RegionGenerator;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BiomeColorStep : IStepGeneration
{
    private readonly string _regionLayerName;

    public BiomeColorStep(string regionLayerName)
    {
        _regionLayerName = regionLayerName;
    }
    public void Procces(MapData mapData)
    {
        var colorLayer = new SimpleLayer<Color>();

        if (!mapData.TryAddMapLayer(colorLayer, "Color")) return;

        mapData.TryGetMapLayer<Region>(_regionLayerName, out var voronoiLayer);

        for (int i = 0; i < mapData.CellsCount; i++)
        {
            Region currentRegion = voronoiLayer.Layer[i];
            if (currentRegion == null) continue;

            var biomeComp = currentRegion.Components
                .OfType<BiomeComponent>()
                .FirstOrDefault();

            colorLayer.Layer[i] = currentRegion.Color;
        }
    }
}

