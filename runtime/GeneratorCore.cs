using Estqes.MapGen.RegionGenerator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Estqes.MapGen
{
    public class GeneratorCore
    {
        private IGrid _grid;
        private List<IStepGeneration> _steps;
        public MapData MapData { get; private set; }
        public GeneratorCore(IGrid grid, int size)
        {
            _grid = grid;
            if (!_grid.IsCreated()) _grid.GenerateGrid();

            MapData = new MapData(grid, size);
            _steps = new List<IStepGeneration>();
        }

        public GeneratorCore AddStepGeneration(IStepGeneration step)
        {
            _steps.Add(step);
            return this;
        }

        public GeneratorCore AddDrawingStep<T>(IDrawer<T> drawer, string outputName)
        {
            _steps.Add(new DrawingStep<T>(outputName, drawer));
            return this;
        }
        public GeneratorCore AddDrawingStep<T>(IDrawer<T> drawer, string outputName, int iterations)
        {
            _steps.Add(new DrawingStep<T>(outputName, drawer)
            {
                Iterations = iterations
            });
            return this;
        }
        public GeneratorCore AddDrawingByEgdeStep<T>(IDrawer<T> drawer, string outputName, string regionLayerName)
        {
            _steps.Add(new DrawByEgdeStep<T>(outputName, regionLayerName, drawer));
            return this;
        }

        public GeneratorCore AddDrawingByEgdeStep<T>(IDrawer<T> drawer, string outputName, string regionLayerName, Func<Region, Region, bool> canDraw)
        {
            _steps.Add(new DrawByEgdeStep<T>(outputName, regionLayerName, drawer)
            {
                CanDraw = canDraw
            });
            return this;
        }

        public void Generate()
        {
            foreach (var item in _steps)
            {
                item.Procces(MapData);
            }
        }
    }

    public interface IStepGeneration
    {
        void Procces(MapData mapData);
    }
    public interface IMapLayer<T>
    {
        public T[] Layer { get; set; }
    }
    public class MapData
    {
        private Dictionary<string, IternalLayer> _layers;
        public readonly IGrid Grid;
        public int CellsCount { get; private set; }
        public int Size { get; }

        public MapData(IGrid grid, int size) 
        {
            if (!grid.IsCreated()) throw new System.Exception("grid not created");
            _layers = new Dictionary<string, IternalLayer>();
            CellsCount = grid.GetAllCells().Length;
            Grid = grid;
            Size = size;
        }

        public bool TryAddMapLayer<T>(IMapLayer<T> layer, string name)
        {
            if (_layers.TryGetValue(name, out var l))
            {
                return false;
            }
            layer.Layer = new T[CellsCount];
            _layers.Add(name, new IternalLayer { layer = layer });
            return true;
        }

        public bool TryGetMapLayer<T>(string name, out IMapLayer<T> layer)
        {
            if (_layers.TryGetValue(name, out var l))
            {
                layer = l.layer as IMapLayer<T>;
                return true;
            }

            layer = null;
            return false;
        }
        public bool TryGetLayer<TLayer>(string name, out TLayer layer) where TLayer : class
        {
            if (_layers.TryGetValue(name, out var l))
            {
                layer = l.layer as TLayer;
                return layer != null; 
            }

            layer = null;
            return false;
        }

        public bool TryGetOrAddSimpleLayer<T>(string layerName, out IMapLayer<T> layer)
        {
            
            if(TryGetMapLayer<T>(layerName, out layer))
            {
                return true;
            }
            layer = new SimpleLayer<T>();
            if(TryAddMapLayer(layer, layerName))
            {
                return true;
            }

            return false;
        }

        private class IternalLayer
        {
            public object layer;
        }
    }
    public class HexagonClusterStep : IStepGeneration
    {
        private readonly float _hexRadius;
        private readonly int _layers;
        private readonly string _layerName;

        private readonly Vector3Int[] _hexDirections = new Vector3Int[]
        {
            new Vector3Int(1, -1, 0),
            new Vector3Int(1, 0, -1),
            new Vector3Int(0, 1, -1),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(-1, 0, 1),
            new Vector3Int(0, -1, 1) 
        };

        public HexagonClusterStep(float hexRadius, int layers, string layerName = "Regions")
        {
            _hexRadius = hexRadius;
            _layers = layers;
            _layerName = layerName;
        }

        public void Procces(MapData mapData)
        {
            var voronoiLayer = new RegionLayer();
            voronoiLayer.Layer = new Region[mapData.CellsCount];
            mapData.TryAddMapLayer(voronoiLayer, _layerName);

            var createdRegions = new Dictionary<Vector3Int, Region>();
            Region zeroRegion = null;

            Vector2 centerOffset = new Vector2(mapData.Size / 2f, (mapData.Size * ((RectangleGrid)mapData.Grid).Ratio) / 2f);

            for (int i = 0; i < mapData.CellsCount; i++)
            {
                Cell cell = mapData.Grid.GetCell(i);

                float x = cell.Center.x - centerOffset.x;
                float y = cell.Center.y - centerOffset.y;

                Vector3Int hexCoord = Utils.PixelToHex(x, y, _hexRadius);

                if (Utils.GetHexDistance(hexCoord) <= _layers)
                {
                    if (!createdRegions.TryGetValue(hexCoord, out Region region))
                    {
                        region = new Region(cell);
                        createdRegions.Add(hexCoord, region);
                    }

                    voronoiLayer.Layer[i] = region;
                    region.Cells.Add(cell);
                }
                else
                {
                    if (zeroRegion == null) zeroRegion = new Region(cell);
                    voronoiLayer.Layer[i] = zeroRegion;
                }
            }

            FillRegionsInSpiralOrder(voronoiLayer, createdRegions);

            RegionGenerator2D.CalculateRegionBorders(voronoiLayer);

            foreach (var region in voronoiLayer.AllRegions)
            {
                region.RecalculateCenter(mapData.Grid);
            }

            if(zeroRegion != null)
            zeroRegion.AddComponent(new EmptyRegion());
        }

        private void FillRegionsInSpiralOrder(RegionLayer layer, Dictionary<Vector3Int, Region> regionDict)
        {
            var centerHex = Vector3Int.zero;
            if (regionDict.TryGetValue(centerHex, out var centerRegion))
            {
                layer.AllRegions.Add(centerRegion);
            }

            for (int radius = 1; radius <= _layers; radius++)
            {
                var currentHex = _hexDirections[4] * radius;

                for (int i = 0; i < 6; i++)
                {
                    for (int j = 0; j < radius; j++)
                    {
                        if (regionDict.TryGetValue(currentHex, out var region))
                        {
                            if (!layer.AllRegions.Contains(region))
                            {
                                layer.AllRegions.Add(region);
                            }
                        }
                        currentHex += _hexDirections[i];
                    }
                }
            }
        }
    }


    public class RegionLayer : IMapLayer<Region>
    {
        public List<Region> AllRegions { get; set; }
        public Region[] Layer { get; set; }

        public RegionLayer()
        {
            AllRegions = new List<Region>();
        }
    }

    public class VoronoiLayer : RegionLayer
    {
        public Dictionary<Cell, Region> CentroidMap { get; }
        public VoronoiGenerationData GenerationData { get; }

        public VoronoiLayer(VoronoiGenerationData voronoiGeneration, Dictionary<Cell, Region> centroidMap)
        {
            AllRegions = new List<Region>();
            CentroidMap = centroidMap;
            GenerationData = voronoiGeneration;
        }
    }
    public class VoronoiStep : IStepGeneration
    {
        private readonly VoronoiGenerationData _settings;
        private readonly RectangleGrid _parentGrid;

        public VoronoiStep(RectangleGrid parentGrid, VoronoiGenerationData settings)
        {
            _parentGrid = parentGrid;
            _settings = settings;
        }

        public void Procces(MapData mapData)
        {
            RegionGenerator2D.Generate(_parentGrid, mapData, _settings);
        }
    }

    public class EveryCellStepAction : IStepGeneration
    {
        public Action<Cell, MapData> Action { get; set; }
        public Action<MapData> StartStep { get; set; }

        public EveryCellStepAction(Action<Cell, MapData> action, Action<MapData> startStep)
        {
            Action = action;
            StartStep = startStep;
        }
        public void Procces(MapData mapData)
        {
            StartStep?.Invoke(mapData);
            for (int i = 0; i < mapData.CellsCount; i++)
            {
                Action.Invoke(mapData.Grid.GetCell(i), mapData);
            }
        }
    }

    public class CarvingLayer : IStepGeneration
    {
        public Func<Cell, bool> Conditon { get; set; }
        public readonly string LayerName;

        public CarvingLayer(Func<Cell, bool> conditon, string layerName = "CarvingLayer")
        {
            Conditon = conditon;
            LayerName = layerName;
        }

        public void Procces(MapData mapData)
        {
            Region a = null;
            Region b = null;

            var carvingLayer = new RegionLayer();
            mapData.TryAddMapLayer(carvingLayer, LayerName);

            for (int i = 0; i < mapData.CellsCount; i++)
            {
                var cell = mapData.Grid.GetCell(i);
                if (Conditon.Invoke(cell))
                {
                    a ??= new Region(cell)
                    {
                        Color = Color.white
                    };
                    carvingLayer.Layer[i] = a;
                }
                else
                {
                    b ??= new Region(cell)
                    {
                        Color = Color.black
                    };
                    carvingLayer.Layer[i] = b;
                }
            }

            if(a != null) carvingLayer.AllRegions.Add(a);
            if(b != null) carvingLayer.AllRegions.Add(b);
        }
    }

    public class Utils
    {
        public static Vector3Int PixelToHex(float x, float y, float hexSize)
        {
            float q = (Mathf.Sqrt(3) / 3 * x - 1.0f / 3 * y) / hexSize;
            float r = (2.0f / 3 * y) / hexSize;
            float s = -q - r;

            return CubeRound(q, r, s);
        }

        private static Vector3Int CubeRound(float x, float y, float z)
        {
            int rx = Mathf.RoundToInt(x);
            int ry = Mathf.RoundToInt(y);
            int rz = Mathf.RoundToInt(z);

            float xDiff = Mathf.Abs(rx - x);
            float yDiff = Mathf.Abs(ry - y);
            float zDiff = Mathf.Abs(rz - z);

            if (xDiff > yDiff && xDiff > zDiff)
                rx = -ry - rz;
            else if (yDiff > zDiff)
                ry = -rx - rz;
            else
                rz = -rx - ry;

            return new Vector3Int(rx, ry, rz);
        }

        public static int GetHexDistance(Vector3Int hex)
        {
            return (Mathf.Abs(hex.x) + Mathf.Abs(hex.y) + Mathf.Abs(hex.z)) / 2;
        }

        public static void CopyDataLayer<T>(IMapLayer<T> layer, ref T[] copy)
        {
            if(copy == null || copy.Length != layer.Layer.Length) copy = new T[layer.Layer.Length];
            Array.Copy(layer.Layer, copy, layer.Layer.Length);
        }
    }
}