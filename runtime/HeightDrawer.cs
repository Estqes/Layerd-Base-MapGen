using System.Linq;
using UnityEngine;

namespace Estqes.MapGen
{
    public class HeightDrawer : IDrawer<Color>
    {
        private string _heightLayerName;
        private IMapLayer<float> _heightLayer;

        public float max;
        public float min;

        public HeightDrawer(string heightLayerName)
        {
            _heightLayerName = heightLayerName;
        }

        public Color Draw(Cell cell)
        {
            return Color.Lerp(Color.black, Color.white, (_heightLayer.Layer[cell.Index] - min) / (max - min));
        }

        public void Init(MapData mapData)
        {
            if (!mapData.TryGetLayer(_heightLayerName, out _heightLayer))
            {
                throw new System.Exception($"Height layer {_heightLayer} not find");
            }

            max = _heightLayer.Layer.Max();
            min = _heightLayer.Layer.Min();
        }

        public void StartIteration(int iteration) { }
    }
}