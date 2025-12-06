using Estqes.MapGen.RegionGenerator;
using System;
using UnityEngine;

namespace Estqes.MapGen 
{
    public class EmptyRegion : IRegionComponent
    {
        public Region Region { get; set; }

        public void AddedToCell() { }
    }
    public class DrawByEgdeStep<T> : DrawingStep<T>
    {
        private readonly string _regionLayerName;
        public Func<Region, Region, bool> CanDraw;
        public DrawByEgdeStep(string outputLayerName, string regionLayerName, IDrawer<T> drawer) : base(outputLayerName, drawer)
        {
            _regionLayerName = regionLayerName;
        }

        public override void Procces(MapData mapData)
        {
            GetDrawLayer(mapData);
            _drawer.Init(mapData);

            if(!mapData.TryGetLayer<RegionLayer>(_regionLayerName, out var regionLayer))
            {
                Debug.LogError($"dont get region layer {_regionLayerName}");
                return;
            }

            foreach (var region in regionLayer.AllRegions)
            {
                foreach (var pair in region.Borders)
                {
                    var egde = pair.Value;
                    var b = pair.Key;
                    foreach (var cell in egde.Cells)
                    {
                        if (CanDraw == null || CanDraw(region, b))
                        _layer.Layer[cell.Index] = _drawer.Draw(cell);
                    }
                }
            }
        }
    }
}
