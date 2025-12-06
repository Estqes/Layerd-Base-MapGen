using System.Collections.Generic;
using System.Linq;

namespace Estqes.MapGen
{
    public class FloatBlur : Blur<float>
    {
        private readonly string _layerName;
        private IMapLayer<float> _layer;
        private float[] _copy;
        public FloatBlur(string layerName) 
        {
            _layerName = layerName;
        }

        public override float GetValue(Cell cell)
        {
            return _copy[cell.Index];
        }

        public override float Medium(float[] values, IEnumerable<Cell> cells)
        {
            return values.Sum(x => x)/values.Length;
        }

        public override void Init(MapData mapData)
        {
            if(!mapData.TryGetMapLayer<float>(_layerName, out _layer))
            {
                throw new System.Exception($"Dont get layer for blur {_layerName}");
            }          
        }

        public override void StartIteration(int iteration)
        {
            Utils.CopyDataLayer(_layer, ref _copy);
        }
    }
    public abstract class Blur<T> : IDrawer<T>
    {
        public int BlurDepth { get; set; } = 1;
        public virtual T Draw(Cell cell)
        {
            var cells = cell.Traverse(BlurDepth);
            var result = new T[cells.Count];

            var i = 0;
            foreach (var item in cells)
            {
                result[i] = GetValue(item);
                i++;
            }

            return Medium(result, cells);
        }

        public abstract T GetValue(Cell cell);

        public virtual void Init(MapData mapData) { }

        public abstract T Medium(T[] values, IEnumerable<Cell> cells);

        public virtual void StartIteration(int iteration) { }
        
        public struct WeightValue
        {
            public T component;
            public float weight;
        }
    }
}