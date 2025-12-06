using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Estqes.MapGen
{
    public class TerrainHeightOutputStep : IStepGeneration
    {
        private readonly Terrain _terrain;
        private readonly string _heightLayerName;
        private readonly float _worldHeight;

        public TerrainHeightOutputStep(Terrain terrain, string heightLayerName, float worldHeight = 100f)
        {
            _terrain = terrain;
            _heightLayerName = heightLayerName;
            _worldHeight = worldHeight;
        }

        public void Procces(MapData mapData)
        {
            if (!mapData.TryGetMapLayer<float>(_heightLayerName, out var heightLayer))
            {
                Debug.LogError($"TerrainOutput: height layer '{_heightLayerName}' dont found");
                return;
            }

            if (_terrain == null)
            {
                Debug.LogError("TerrainOutput: link to Terrain not assgin!");
                return;
            }

            TerrainData tData = _terrain.terrainData;

            int res = mapData.Size;
            if (tData.heightmapResolution != res)
            {
                tData.heightmapResolution = res;
            }

            tData.size = new Vector3(tData.size.x, _worldHeight, tData.size.z);

            float[,] heights = new float[res, res];

            float min = heightLayer.Layer.Min();
            float max = heightLayer.Layer.Max();

            for (int x = 0; x < res; x++)
            {
                for (int y = 0; y < res; y++)
                {
                    int index = x + y * res;

                    if (index < heightLayer.Layer.Length)
                    {
                        float h = heightLayer.Layer[index];
                        heights[y, x] = (h + min)/tData.size.y;
                    }
                }
            }

            tData.SetHeights(0, 0, heights);
            _terrain.GetComponent<TerrainCollider>().terrainData = tData;
        }
    }

    public class TerrainTextureStep : IStepGeneration
    {
        public List<TerrainSplatmapOutputStep> Steps;
        public TerrainTextureStep(GeneratorCore generator)
        {
            foreach (var item in Steps)
            {
                generator.AddStepGeneration(item);
            }
            // тут нормолизация
        }
        public void Procces(MapData mapData)
        {
            throw new System.NotImplementedException();
        }
    }

    public class TerrainSplatmapOutputStep : IStepGeneration
    {
        private readonly Terrain _terrain;
        public string InputLayerName { get; set; }
        public int TextureLayerIndex { get; set; }

        public TerrainSplatmapOutputStep(Terrain terrain, string inputLayerName, int textureLayerIndex)
        {
            _terrain = terrain;
            InputLayerName = inputLayerName;
            TextureLayerIndex = textureLayerIndex;
        }

        public void Procces(MapData mapData)
        {
            if (!mapData.TryGetMapLayer<float>(InputLayerName, out var inputLayer))
            {
                Debug.LogError($"SplatmapOutput: Layer '{InputLayerName}' dont find!");
                return;
            }

            if (_terrain == null)
            {
                Debug.LogError("SplatmapOutput: Terrain not set!");
                return;
            }

            TerrainData tData = _terrain.terrainData;
            int alphaLayers = tData.alphamapLayers;

            if (TextureLayerIndex >= alphaLayers)
            {
                Debug.LogError($"SplatmapOutput: Index texture {TextureLayerIndex} max. Terrain have {alphaLayers} layer.");
                return;
            }

            int alphaRes = tData.alphamapResolution;

            float[,,] splatmaps = tData.GetAlphamaps(0, 0, alphaRes, alphaRes);

            int genSize = mapData.Size;

            var size = alphaRes;

            System.Threading.Tasks.Parallel.For(0, alphaRes, y =>
            {
                for (int x = 0; x < alphaRes; x++)
                {
                    int gx = Mathf.FloorToInt((x / (float)alphaRes) * genSize);
                    int gy = Mathf.FloorToInt((y / (float)alphaRes) * genSize);

                    gx = Mathf.Clamp(gx, 0, genSize - 1);
                    gy = Mathf.Clamp(gy, 0, genSize - 1);

                    int genIndex = gx + gy * genSize;

                    float value = inputLayer.Layer[genIndex];

                    splatmaps[y, x, TextureLayerIndex] = Mathf.Clamp01(value);
                }
            });

            tData.SetAlphamaps(0, 0, splatmaps);
        }
    }
}