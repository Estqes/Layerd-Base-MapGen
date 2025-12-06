using Estqes.MapGen.RegionGenerator;
using System;
using System.Linq;
using UnityEngine;

namespace Estqes.MapGen
{
    public interface IRegionDrawer<T>
    {
        void Init(MapData mapData);
        void StartIteration(int iteration);
        T Draw(Region region);
    }

    public abstract class DrawerRegionToRegion<TOut, TComponent> : BaseRegionBlender<Region, TOut, TComponent>, IRegionDrawer<TOut>
    {
        public DrawerRegionToRegion(RegionLayer region) : base(region) { }
        public DrawerRegionToRegion(string layerName) : base(layerName) { }

        protected override Cell GetCellFromTarget(Region target) => target.Cell;

        protected override Region GetOwnerRegion(Region target)
        {
            return _regionLayer.Layer[target.Cell.Index];
        }

        public TOut Draw(Region region)
        {
            return ExecuteDraw(region);
        }
    }

    public abstract class DrawerByRegions<TOut, TComponent> : BaseRegionBlender<Cell, TOut, TComponent>, IDrawer<TOut>
    {
        public DrawerByRegions(RegionLayer region) : base(region) { }
        public DrawerByRegions(string layerName) : base(layerName) { }

        protected override Cell GetCellFromTarget(Cell target) => target;

        protected override Region GetOwnerRegion(Cell target)
        {
            return _regionLayer.Layer[target.Index];
        }

        public TOut Draw(Cell cell)
        {
            return ExecuteDraw(cell);
        }

        public void PreInit(MapData mapData) => base.Init(mapData);
    }

    public abstract class BaseRegionBlender<TTarget, TOut, TComponent>
    {
        public Func<Cell, Cell, float> Metric { get; set; } = Metrics.EucludMetric;

        protected RegionLayer _regionLayer;
        protected string _regionLayerName;

        public BaseRegionBlender(RegionLayer region)
        {
            _regionLayer = region;
        }

        public BaseRegionBlender(string regionLayerName)
        {
            _regionLayerName = regionLayerName;
        }

        protected abstract Cell GetCellFromTarget(TTarget target);
        protected abstract Region GetOwnerRegion(TTarget target);
        public abstract TOut Medium(WeightValue<TComponent>[] values, float weightSum, TTarget target);

        public virtual void Init(MapData mapData)
        {
            if (_regionLayer == null)
            {
                if (!mapData.TryGetLayer(_regionLayerName, out _regionLayer))
                {
                    throw new Exception($"BaseRegionBlender: layer {_regionLayerName} not found");
                }
            }
        }

        public virtual void StartIteration(int iteration) { }

        protected virtual TComponent GetValue(Region region)
        {
            if (region != null && typeof(IRegionComponent).IsAssignableFrom(typeof(TComponent)))
            {
                return region.Components.OfType<TComponent>().FirstOrDefault();
            }
            return default;
        }

        protected virtual float CalculateWeight(Cell targetCell, Region neighbor, Region owner)
        {
            if (neighbor == null) return 0f;
            if (neighbor == owner) return 1f;

            float distToOwner = Metric(targetCell, owner.Cell);
            float distToNeighbor = Metric(targetCell, neighbor.Cell);

            if (distToNeighbor < 0.0001f) return 1f;

            float ratio = distToOwner / distToNeighbor;
            float falloffSpeed = 6;

            return Mathf.Pow(ratio, falloffSpeed);
        }

        protected TOut ExecuteDraw(TTarget target)
        {
            var owner = GetOwnerRegion(target);
            if (owner == null) return default;

            var targetCell = GetCellFromTarget(target);

            var weightsSum = CalculateWeight(targetCell, owner, owner);
            var startValue = GetValue(owner);

            if (startValue == null) return default;

            var neighbors = owner.Neighbours.Where(x => GetValue(x) != null);

            var values = new WeightValue<TComponent>[neighbors.Count() + 1];

            values[0] = new WeightValue<TComponent>()
            {
                component = startValue,
                weight = weightsSum,
            };

            int i = 1;
            foreach (var n in neighbors)
            {
                var value = GetValue(n);
                var weight = CalculateWeight(targetCell, n, owner);

                values[i] = new WeightValue<TComponent>()
                {
                    component = value,
                    weight = weight,
                };

                weightsSum += weight;
                i++;
            }

            return Medium(values, weightsSum, target);
        }
    }

    public struct WeightValue<T>
    {
        public T component;
        public float weight;
    }
}