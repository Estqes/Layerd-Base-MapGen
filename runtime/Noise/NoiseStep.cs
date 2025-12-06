using UnityEngine;

namespace Estqes.MapGen
{
    [System.Serializable]
    public class NoiseSettings
    {
        [Header("General Settings")]
        public int seed = 1337;
        public float frequency = 0.01f;
        public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
        public FastNoiseLite.RotationType3D rotationType3D = FastNoiseLite.RotationType3D.None;

        [Header("Fractal Settings")]
        public FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.None;
        [Min(1)] public int octaves = 3;
        public float lacunarity = 2.0f;
        public float gain = 0.5f;
        public float weightedStrength = 0.0f;
        public float pingPongStrength = 2.0f;

        [Header("Cellular Settings")]
        public FastNoiseLite.CellularDistanceFunction cellularDistanceFunction = FastNoiseLite.CellularDistanceFunction.EuclideanSq;
        public FastNoiseLite.CellularReturnType cellularReturnType = FastNoiseLite.CellularReturnType.Distance;
        public float cellularJitter = 1.0f;

        [Header("Domain Warp Settings")]
        public FastNoiseLite.DomainWarpType domainWarpType = FastNoiseLite.DomainWarpType.OpenSimplex2;
        public float domainWarpAmp = 1.0f;
    }


    public class NoiseStep : IStepGeneration
    {
        private readonly string _outputLayerName;
        private FastNoiseLite _noise;
        public float NoiseAmplitude { get; set; } = 1;
        public NoiseStep(string outputLayerName, NoiseSettings settings)
        {
            _outputLayerName = outputLayerName;
            _noise = new FastNoiseLite();

            ApplySettings(settings);
        }
        private void ApplySettings(NoiseSettings s)
        {
            // General
            _noise.SetSeed(s.seed);
            _noise.SetFrequency(s.frequency);
            _noise.SetNoiseType(s.noiseType);
            _noise.SetRotationType3D(s.rotationType3D);

            // Fractal
            _noise.SetFractalType(s.fractalType);
            _noise.SetFractalOctaves(s.octaves);
            _noise.SetFractalLacunarity(s.lacunarity);
            _noise.SetFractalGain(s.gain);
            _noise.SetFractalWeightedStrength(s.weightedStrength);
            _noise.SetFractalPingPongStrength(s.pingPongStrength);

            // Cellular
            _noise.SetCellularDistanceFunction(s.cellularDistanceFunction);
            _noise.SetCellularReturnType(s.cellularReturnType);
            _noise.SetCellularJitter(s.cellularJitter);

            // Domain Warp
            _noise.SetDomainWarpType(s.domainWarpType);
            _noise.SetDomainWarpAmp(s.domainWarpAmp);
        }

        public void Procces(MapData mapData)
        {
            if (!mapData.TryGetOrAddSimpleLayer<float>(_outputLayerName, out var layer))
            {
                Debug.LogError($"NoiseStep: Failed to add layer or get {_outputLayerName}");
                return;
            }

            for (int i = 0; i < mapData.CellsCount; i++)
            {
                var cell = mapData.Grid.GetCell(i);
                layer.Layer[i] = _noise.GetNoise(cell.Center.x, cell.Center.y, cell.Center.z) * NoiseAmplitude;
            }
        }
    }
}
