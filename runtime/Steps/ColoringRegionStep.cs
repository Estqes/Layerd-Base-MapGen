using Estqes.MapGen.RegionGenerator;
using UnityEngine;

namespace Estqes.MapGen
{
    public interface IDrawer<T>
    {
        void Init(MapData mapData);
        void StartIteration(int iteration);
        T Draw(Cell cell);
        
    }

    public class ColoringRegionStep : IStepGeneration
    {
        private readonly string _regionLayerName;
        public ColoringRegionStep(string borderLayerName)
        {
            _regionLayerName = borderLayerName;
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

                colorLayer.Layer[i] = currentRegion.Color;
            }
        }
    }
}