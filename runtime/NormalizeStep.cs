using System;
using System.Threading.Tasks;

namespace Estqes.MapGen
{
    public class NormalizeStep<T> : IStepGeneration
    {
        public string LayerName { get; set; }
        public string OutputLayerName { get; set; }

        private IMath<T> _math;
        private IMapLayer<T> _layer;
        private IMapLayer<T> _outputLayer;

        public NormalizeStep(string layerName)
        {
            LayerName = layerName;
        }

        public NormalizeStep(string layerName, string outputLayerName)
        {
            LayerName = layerName;
            OutputLayerName = outputLayerName;
        }
        
        protected void Init(MapData mapData)
        {
            _math = MathProvider.Get<T>();

            if (!mapData.TryGetMapLayer(LayerName, out _layer))
            {
                throw new Exception($"NormalizeStep: Dont get layer {LayerName}");
            }

            if(OutputLayerName != null)
            {
                if (!mapData.TryGetOrAddSimpleLayer(OutputLayerName, out _outputLayer))
                {
                    throw new Exception($"NormalizeStep: Dont get or create layer {OutputLayerName}");
                }
            }    
        }

        public void Procces(MapData mapData)
        {
            Init(mapData);

            T min = _math.MaxValue;
            T max = _math.MinValue;

            foreach (var item in mapData.Grid.GetAllCells())
            {
                var index = item.Index;
                min = _math.Min(_layer.Layer[index], min);
                max = _math.Max(_layer.Layer[index], max);
            }

            if(_outputLayer == null)
            {
                Parallel.For(0, mapData.CellsCount, (i) =>
                {
                    var value = _layer.Layer[i];
                    _layer.Layer[i] = _math.Divide(_math.Add(value, min), _math.Add(min, max));
                });
            }
            else
            {
                Parallel.For(0, mapData.CellsCount, (i) =>
                {
                    var value = _layer.Layer[i];
                    _outputLayer.Layer[i] = _math.Divide(_math.Add(value, min), _math.Add(min, max));
                });
            }
        }
    }
}