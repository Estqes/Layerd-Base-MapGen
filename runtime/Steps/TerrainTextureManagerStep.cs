using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Estqes.MapGen
{
        public struct TextureLayerSettings
    {
        public string InputLayerName;
        public int TextureIndex;
    }
    public class TerrainTextureManagerStep : IStepGeneration
    {
        private readonly Terrain _terrain;
        private readonly List<TextureLayerSettings> _layers;

        public TerrainTextureManagerStep(Terrain terrain, List<TextureLayerSettings> layers)
        {
            _terrain = terrain;
            _layers = layers;
        }

        public void Procces(MapData mapData)
        {
            if (_terrain == null) return;

            var inputLayers = new IMapLayer<float>[_layers.Count];
            var targetIndices = new int[_layers.Count];

            for (int i = 0; i < _layers.Count; i++)
            {
                mapData.TryGetMapLayer<float>(_layers[i].InputLayerName, out inputLayers[i]);
                targetIndices[i] = _layers[i].TextureIndex;
            }

            TerrainData tData = _terrain.terrainData;
            int alphaRes = tData.alphamapResolution;
            int totalTerrainLayers = tData.alphamapLayers;

            float[,,] splatmaps = new float[alphaRes, alphaRes, totalTerrainLayers];
            int genSize = mapData.Size;

            Parallel.For(0, alphaRes, y =>
            {
                for (int x = 0; x < alphaRes; x++)
                {
                    // А. Координаты
                    int gx = Mathf.Clamp(Mathf.FloorToInt((x / (float)alphaRes) * genSize), 0, genSize - 1);
                    int gy = Mathf.Clamp(Mathf.FloorToInt((y / (float)alphaRes) * genSize), 0, genSize - 1);
                    int genIndex = gx + gy * genSize;

                    float[] activeWeights = new float[totalTerrainLayers];
                    float sum = 0f;

                    for (int i = 0; i < _layers.Count; i++)
                    {
                        if (inputLayers[i] != null)
                        {
                            float val = inputLayers[i].Layer[genIndex];
                            if (val < 0.001f) val = 0f;

                            int texIndex = targetIndices[i];

                            if (texIndex < totalTerrainLayers)
                            {
                                activeWeights[texIndex] = val;
                                sum += val;
                            }
                        }
                    }

                    if (sum < 0.01f) activeWeights[0] = 1f;
                    else
                    {
                        for (int k = 0; k < totalTerrainLayers; k++)
                        {
                            activeWeights[k] /= sum;
                        }
                    }

                    // Д. Запись в итоговый массив
                    for (int k = 0; k < totalTerrainLayers; k++)
                    {
                        splatmaps[y, x, k] = activeWeights[k];
                    }
                }
            });

            tData.SetAlphamaps(0, 0, splatmaps);
        }

    }
}