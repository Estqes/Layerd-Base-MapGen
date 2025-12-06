using System.Threading.Tasks;
using UnityEngine;

namespace Estqes.MapGen
{
    public class SlopeCalculationStep : IStepGeneration
    {
        private readonly string _heightLayerName;
        private readonly string _outputLayerName;
        private readonly float _strength;

        public SlopeCalculationStep(string heightLayerName, string outputLayerName, float strength = 100f)
        {
            _heightLayerName = heightLayerName;
            _outputLayerName = outputLayerName;
            _strength = strength;
        }

        public void Procces(MapData mapData)
        {
            if (!mapData.TryGetMapLayer<float>(_heightLayerName, out var heightLayer))
            {
                Debug.LogError($"SlopeStep: Height layer '{_heightLayerName}' not found.");
                return;
            }

            mapData.TryGetOrAddSimpleLayer<float>(_outputLayerName, out var slopeLayer);

            int width = mapData.Size;
            int height = mapData.CellsCount / width;

            Parallel.For(0, mapData.CellsCount, i =>
            {
                int x = i % width;
                int y = i / width;

                int xLeft = (x > 0) ? x - 1 : x;
                int xRight = (x < width - 1) ? x + 1 : x;
                int yDown = (y > 0) ? y - 1 : y;
                int yUp = (y < height - 1) ? y + 1 : y;

                int idxLeft = xLeft + y * width;
                int idxRight = xRight + y * width;
                int idxDown = x + yDown * width;
                int idxUp = x + yUp * width;

                float hL = heightLayer.Layer[idxLeft];
                float hR = heightLayer.Layer[idxRight];
                float hD = heightLayer.Layer[idxDown];
                float hU = heightLayer.Layer[idxUp];

                float dX = hR - hL;
                float dY = hU - hD;

                float gradient = Mathf.Sqrt(dX * dX + dY * dY);

                slopeLayer.Layer[i] = Mathf.Clamp01(gradient * _strength);
            });
        }
    }
}