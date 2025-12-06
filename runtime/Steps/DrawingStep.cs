using Estqes.MapGen.RegionGenerator;
using System;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace Estqes.MapGen
{
    public class DrawingRegions<T> : IStepGeneration
    {
        public string InputLayer { get; set; }
        public IRegionDrawer<T> RegionDrawer { get; set; }
        public int Iteration { get; set; } = 1;
        public Action<T, Region> AssginAction { get; set; }

        protected RegionLayer _layerA;

        public DrawingRegions(string fromLayerName, IRegionDrawer<T> regionDrawer, Action<T, Region> assginAction)
        {
            InputLayer = fromLayerName;
            RegionDrawer = regionDrawer;
            AssginAction = assginAction;
        }

        public void Procces(MapData mapData)
        {
            if(!mapData.TryGetLayer(InputLayer, out _layerA))
            {
                throw new System.Exception("DrawingRegions: InputLayer dont find");
            }

            RegionDrawer.Init(mapData);

            for (int i = 0; i < Iteration; i++)
            {
                RegionDrawer.StartIteration(i);
                Parallel.ForEach(_layerA.AllRegions, (region) =>
                {
                    AssginAction.Invoke(RegionDrawer.Draw(region), region);
                });
            }
        }
    }
    public class DrawingStep<T> : IStepGeneration
    {
        public readonly string OutputLayerName;
        protected IDrawer<T> _drawer;
        protected SimpleLayer<T> _layer;
        public int Iterations { get; set; } = 1;

        public DrawingStep(string outputLayerName, IDrawer<T> drawer)
        {
            OutputLayerName = outputLayerName;
            _drawer = drawer;
        }

        protected virtual void GetDrawLayer(MapData mapData)
        {
            if (!mapData.TryGetLayer(OutputLayerName, out _layer))
            {
                _layer = new SimpleLayer<T>();
                if (!mapData.TryAddMapLayer(_layer, OutputLayerName))
                {
                    Debug.LogError($"The drawing layer {OutputLayerName} was not added ");
                    return;
                }
            }

        }

        public virtual void Procces(MapData mapData)
        {
            GetDrawLayer(mapData);
            _drawer.Init(mapData);

            for (int j = 0; j < Iterations; j++)
            {
                _drawer.StartIteration(j);

                Parallel.For(0, mapData.CellsCount, (i) =>
                {
                    var cell = mapData.Grid.GetCell(i);
                    _layer.Layer[i] = _drawer.Draw(cell);
                });
            }
        }
    }
}