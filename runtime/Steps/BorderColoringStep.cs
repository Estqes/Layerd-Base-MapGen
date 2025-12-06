using Estqes.MapGen.RegionGenerator;
using System.Collections.Generic;
using UnityEngine;

namespace Estqes.MapGen
{
    public class BorderColoringStep : IStepGeneration
    {
        private readonly string _borderLayerName;

        public BorderColoringStep(string borderLayerName)
        {
            _borderLayerName = borderLayerName;
        }

        public void Procces(MapData mapData)
        {
            if (!mapData.TryGetLayer(_borderLayerName, out RegionLayer voronoiLayer))
            {
                Debug.LogError("Voronoi layer dont find");
                return;
            }

            if (!mapData.TryGetMapLayer<Color>("Color", out var colorLayer))
            {
                Debug.LogError("Color layer dont wind");
                return;
            }

            var processedEdges = new HashSet<RegionEdge>();

            foreach (var region in voronoiLayer.AllRegions)
            {
                if (region.Borders == null || region.Borders.Count == 0) return;
                foreach (var border in region.Borders.Values)
                {
                    if (processedEdges.Contains(border))
                    {
                        continue;
                    }
                    Color borderColor = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);

                    foreach (var cell in border.Cells)
                    {
                        colorLayer.Layer[cell.Index] = borderColor;
                    }
                    processedEdges.Add(border);
                }
            }
        }
    }
}