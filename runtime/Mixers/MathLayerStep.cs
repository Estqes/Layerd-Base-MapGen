using System.Threading.Tasks;
using UnityEngine;

namespace Estqes.MapGen
{
    public class MathLayerStep<T> : IStepGeneration
    {
        public string AName { get; set; }
        public string BName { get; set; }
        public OperatinsType Operation { get; set; }
        public T Value { get; set; }

        private IMapLayer<T> _layerA;
        private IMapLayer<T> _layerB;
        private IMath<T> _math;

        public MathLayerStep(string v1, string v2, float v3, object multiply)
        {

        }

        public void Procces(MapData mapData)
        {
            if(!mapData.TryGetOrAddSimpleLayer(AName, out _layerA))
            {
                Debug.LogError($"Math layer: dont get layer A {AName}");
                return;
            }

            if(BName != null)
            {
                if(!mapData.TryGetLayer(BName, out _layerB))
                {
                    Debug.LogError($"Math layer: dont get layer B {BName}");
                    return;
                }
            }

            _math = MathProvider.Get<T>();

            if (BName == null)
            {
                Parallel.For(0, mapData.CellsCount, i =>
                {
                    _layerA.Layer[i] = PerformOperation(_layerA.Layer[i], Value);
                });
            }
            else
            {
                Parallel.For(0, mapData.CellsCount, i =>
                {
                    _layerA.Layer[i] = PerformOperation(_layerA.Layer[i], _layerB.Layer[i]);
                });
            }
        }

        private T PerformOperation(T a, T b)
        {
            switch (Operation)
            {
                case OperatinsType.Add:
                    return _math.Add(a, b);
                case OperatinsType.Subtract:
                    return _math.Subtract(a, b);
                case OperatinsType.Multiply:
                    return _math.Multiply(a, b);
                case OperatinsType.Divide:
                    return _math.Divide(a, b);
                case OperatinsType.Max:
                    return _math.Max(a, b);
                case OperatinsType.Min:
                    return _math.Min(a, b);
                default:
                    break;
            }

            return a;
        }

        public enum OperatinsType
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Power,
            Max,
            Min,
        }
    }
}