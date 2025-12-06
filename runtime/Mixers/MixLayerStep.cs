using System;
using System.Threading.Tasks;

namespace Estqes.MapGen
{
    public class BlenderMixer<T> : Mixer<T>
    {
        public BlendMode Mode { get; set; }
        public bool Clamp { get; set; }

        public BlenderMixer(BlendMode mode, bool clamp = false)
        {
            SetNewData(mode, clamp);
        }

        public void SetNewData(BlendMode mode, bool clamp = false)
        {
            Mode = mode;
            Clamp = clamp;
        }

        public override T Lerp(T a, T b, float t)
        {
            T blendResult = default;
            switch (Mode)
            {
                case BlendMode.Mix:
                    blendResult = b;
                    break;
                case BlendMode.Add:
                    blendResult = Math.Add(a, b);
                    break;
                case BlendMode.Subtract:
                    blendResult = Math.Subtract(a, b);
                    break;
                case BlendMode.Multiply:
                    blendResult = Math.Multiply(a, b);
                    break;
                case BlendMode.Divide:
                    blendResult = Math.Divide(a, b);
                    break;
                case BlendMode.Darken:
                    blendResult = Math.Min(a, b);
                    break;
                case BlendMode.Lighten:
                    blendResult = Math.Max(a, b);
                    break;
                case BlendMode.Difference:
                    T max = Math.Max(a, b);
                    T min = Math.Min(a, b);
                    blendResult = Math.Subtract(max, min);
                    break;
            }

            return base.Lerp(a, blendResult, t);
        }
    }
    public enum BlendMode
    {
        Mix,
        Add,
        Subtract,
        Multiply,
        Divide,
        Darken,
        Lighten,
        Difference
    }
    public class ThresholdMixer<T> : Mixer<T>
    {
        public override T Lerp(T a, T b, float t)
        {
            return t > 0.5f ? b : a;
        }
    }

    public abstract class Mixer<T>
    {
        public readonly IMath<T> Math;
        public Mixer()
        {
            Math = MathProvider.Get<T>();
        }

        public virtual T Lerp(T a, T b, float t)
        {
            return Math.Add(a, Math.MultiplyScalar(Math.Subtract(b, a), t));
        }
    }
    public class MixLayerStep<T> : IStepGeneration
    {
        public float Alpha { get; private set; }
        public Mixer<T> Mixer { get; private set; }
        
        private readonly string _layerAName;
        private readonly string _layerBName;
        private readonly string _maskLayerName;
        private readonly string _outputLayerName;

        private IMapLayer<T> _layerA;
        private IMapLayer<T> _layerB;
        private IMapLayer<T> _outputLayer;
        private IMapLayer<float> _maskLayer;

        public MixLayerStep(string layerA, string layerB, string outputLayer, float t, Mixer<T> mix)
        {
            _layerAName = layerA;
            _layerBName = layerB;
            _outputLayerName = outputLayer;
            _maskLayerName = null;

            Mixer = mix;
            Alpha = t;
        }
        public MixLayerStep(string layerA, string layerB, string maskLayer, string outputLayer, Mixer<T> mix)
        {
            _layerAName = layerA;
            _layerBName = layerB;
            _maskLayerName = maskLayer;
            _outputLayerName = outputLayer;
            Mixer = mix;
        }


        public virtual void Procces(MapData mapData)
        {
            Init(mapData);
            Parallel.For(0, mapData.CellsCount, (i) =>
            {
                var a = _layerA.Layer[i];
                var b = _layerB.Layer[i];

                var t = Alpha;

                if(_maskLayer != null) t = _maskLayer.Layer[i];
                _outputLayer.Layer[i] = Mixer.Lerp(a, b, t);
            });
        }

        public virtual void Init(MapData mapData)
        {
            if(!mapData.TryGetMapLayer(_layerAName, out _layerA) || !mapData.TryGetMapLayer(_layerBName, out _layerB)) 
            {
                throw new Exception($"MixLayerStep: Layer A or B '{_layerAName}' not found");
            }
            if(_maskLayerName != null)
            {
                if(!mapData.TryGetMapLayer(_maskLayerName, out _outputLayer))
                {
                    throw new Exception($"MixLayerStep: Mask Layer '{_maskLayerName}' not found");
                }
            }

            if (!mapData.TryGetLayer(_outputLayerName, out _outputLayer))
            {
                _outputLayer = new SimpleLayer<T>();
                if(!mapData.TryAddMapLayer(_outputLayer, _outputLayerName))
                {
                    throw new Exception($"MixLayerStep: Failed to create output layer '{_outputLayerName}'");
                }
            }

        }
    }
}