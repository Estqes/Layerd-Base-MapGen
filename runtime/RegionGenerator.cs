using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Estqes.MapGen.RegionGenerator
{
    public class RegionEdge
    {
        public HashSet<Cell> Cells { get; private set; }

        public RegionEdge()
        {
            Cells = new HashSet<Cell>();
        }
    }

    public class Region
    {
        public HashSet<Cell> Cells { get; private set; }
        public Color Color;
        public HashSet<Region> Neighbours { get; private set; }
        public HashSet<Cell> Egde { get; private set; }
        public Dictionary<Region, RegionEdge> Borders { get; private set; }

        public Cell Cell { get; set; }

        public List<IRegionComponent> Components { get; private set; }

        public Region(Cell cell) 
        {
            Color = Random.ColorHSV();
            Cells = new HashSet<Cell>();
            Neighbours = new HashSet<Region>();
            Egde = new HashSet<Cell>();
            Cell = cell;
            Components = new List<IRegionComponent>();
            Borders = new Dictionary<Region, RegionEdge>();

        }

        public void AddComponent(IRegionComponent component)
        {
            if (component == null) return;
            Components.Add(component);
            component.Region = this;
            component.AddedToCell();
        }

        public T GetComponent<T>() where T : class
        {
            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i] is T component)
                {
                    return component;
                }
            }
            return null;
        }

        public bool ContainComponent<T>() where T : class 
        {
            return GetComponent<T>() != null;
        }

        public bool TryGetComponent<T>(out T component) where T : class
        {
            component = GetComponent<T>();
            return component != null;
        }

        public void RecalculateCenter(IGrid grid)
        {
            if (Cells == null || Cells.Count == 0) return;

            Vector3 sumPosition = Vector3.zero;
            foreach (var cell in Cells)
            {
                sumPosition += cell.Center;
            }

            Vector3 averagePosition = sumPosition / Cells.Count;

            int newIndex = grid.CalculateIndex(averagePosition);
            Cell newCenterCell = grid.GetCell(newIndex);

            if (newCenterCell != null && newCenterCell != Cell)
            {
                Cell = newCenterCell;
            }
        }

    }

    public class VoronoiGenerationData
    {
        public int cellSize = 100;
        public int offsetScale = 4;
        public Func<Cell, Cell, float> metric;
        public string layerName = "Voronoi";

        public VoronoiGenerationData()
        {
            metric = Metrics.EucludMetric;
        }

        public static Func<Cell, Cell, float> CreateWarpedMetric(float noiseFrequency, float noiseStrength)
        {
            return (cellA, cellB) =>
            {
                var posA = cellA.Center;

                float noiseAX = Mathf.PerlinNoise(posA.x * noiseFrequency, posA.y * noiseFrequency);
                float noiseAY = Mathf.PerlinNoise((posA.x + 1000f) * noiseFrequency, (posA.y + 1000f) * noiseFrequency);

                noiseAX = (noiseAX - 0.5f) * 2f;
                noiseAY = (noiseAY - 0.5f) * 2f;

                var warpVectorA = new Vector3(noiseAX, noiseAY, 0) * noiseStrength;
                var warpedPosA = posA + warpVectorA;

                var posB = cellB.Center;

                float noiseBX = Mathf.PerlinNoise(posB.x * noiseFrequency, posB.y * noiseFrequency);
                float noiseBY = Mathf.PerlinNoise((posB.x + 1000f) * noiseFrequency, (posB.y + 1000f) * noiseFrequency);

                noiseBX = (noiseBX - 0.5f) * 2f;
                noiseBY = (noiseBY - 0.5f) * 2f;

                var warpVectorB = new Vector3(noiseBX, noiseBY, 0) * noiseStrength;
                var warpedPosB = posB + warpVectorB;

                return Vector3.Distance(warpedPosA, warpedPosB);
            };
        }
        public static float P { get; set; }
    }

    public static class RegionGenerator2D
    {

        public static void Generate(RectangleGrid parentGrid, MapData data, VoronoiGenerationData voronoiGeneration)
        {
            var voronoiGrid = new RectangleGrid(parentGrid.Ratio);
            var a = voronoiGeneration.cellSize;

            var cellSize = parentGrid.Size.x / a;
            var regionWidth = a;
            var regionHeight = a * parentGrid.Ratio;

            var ratioX = data.Size / parentGrid.Size.x;
            var ratioY = data.Size * parentGrid.Ratio / parentGrid.Size.y;

            var centroidMap = new Dictionary<Cell, Region>();
            var voronoiLayer = new VoronoiLayer(voronoiGeneration, centroidMap);

            if (!data.TryAddMapLayer(voronoiLayer, voronoiGeneration.layerName))
            {
                return;
            }
            

            voronoiGrid.SetSize(parentGrid.Size.x / a);
            voronoiGrid.GenerateGrid();

            foreach (var cell in voronoiGrid.GetAllCells())
            {
                var coord = cell.Center;

                var centerX = regionWidth / 2;
                var centerY = regionHeight / 2;

                var offset = (regionWidth / voronoiGeneration.offsetScale);
                
                var xOffset = Random.Range(offset, regionWidth - offset);
                var yOffset = Random.Range(offset, regionWidth - offset);

                var centroidCell = parentGrid.GetCell(parentGrid.CalculateIndex(new Vector3(coord.x * a + xOffset, coord.y * a + yOffset)));
                centroidMap.Add(cell, new Region(centroidCell));
            }
            var i = 0;
            foreach (var item in centroidMap)
            {
                var centroid = item.Value;
                var cell = voronoiGrid.GetCell(i);
                for (int x = 0; x < regionWidth; x++)
                {
                    for (int y = 0; y < regionHeight; y++)
                    {
                        var parentCellIndex = parentGrid.CalculateIndex(new Vector3(x + cell.Center.x * regionWidth, y + cell.Center.y * regionHeight));
                        var parentCell = parentGrid.GetCell(parentCellIndex);

                        var minCentroid = centroid;
                        var minDistance = voronoiGeneration.metric.Invoke(minCentroid.Cell, parentCell);

                        foreach (var n in cell.Neighbours)
                        {
                            var nCentroid = centroidMap[n];
                            var dist = voronoiGeneration.metric.Invoke(nCentroid.Cell, parentCell);
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                minCentroid = nCentroid;
                            }
                        }

                        minCentroid.Cells.Add(parentCell);
                        voronoiLayer.Layer[parentCellIndex] = minCentroid;
                    }
                }
                i++;
            }

            voronoiLayer.AllRegions = centroidMap.Values.ToList();
            CalculateRegionBorders(voronoiLayer);
        }

        public static void CalculateRegionBorders(RegionLayer voronoiLayer)
        {
            foreach (var region in voronoiLayer.AllRegions)
            {
                foreach (var cell in region.Cells)
                {
                    foreach (var neighbourCell in cell.Neighbours)
                    {
                        Region neighbourRegion = voronoiLayer.Layer[neighbourCell.Index];

                        if (neighbourRegion != null && neighbourRegion != region)
                        {
                            region.Neighbours.Add(neighbourRegion);
                            neighbourRegion.Neighbours.Add(region);

                            if (!region.Borders.TryGetValue(neighbourRegion, out RegionEdge edge))
                            {
                                edge = new RegionEdge();

                                region.Borders.Add(neighbourRegion, edge);
                                neighbourRegion.Borders.Add(region, edge);
                            }

                            edge.Cells.Add(cell);
                        }
                    }
                }
            }
        }
        public static void Attach(VoronoiLayer layer, IGrid grid, Vector3 coord)
        {
            int centerIndex = grid.CalculateIndex(coord);
            Cell centerCell = grid.GetCell(centerIndex);

            if (centerCell == null)
            {
                Debug.LogError($"Coord {coord} dont in grid!");
                return;
            }

            var newRegion = new Region(centerCell);

            layer.AllRegions.Add(newRegion);

            var metric = layer.GenerationData.metric;
            var allCells = grid.GetAllCells();

            for (int i = 0; i < allCells.Length; i++)
            {
                Cell currentCell = allCells[i];
                float distToNew = metric.Invoke(newRegion.Cell, currentCell);
                Region currentOwner = layer.Layer[i];

                bool shouldCapture = false;

                if (currentOwner == null)
                {
                    shouldCapture = true;
                }
                else
                {
                    float distToOld = metric.Invoke(currentOwner.Cell, currentCell);

                    if (distToNew < distToOld)
                    {
                        shouldCapture = true;
                        currentOwner.Cells.Remove(currentCell);
                    }
                }

                if (shouldCapture)
                {
                    layer.Layer[i] = newRegion;
                    newRegion.Cells.Add(currentCell);
                }
            }
        }
    }
}


