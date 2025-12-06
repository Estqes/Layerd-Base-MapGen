using Estqes.MapGen;
using Estqes.MapGen.RegionGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GeneratorExample : MonoBehaviour
{

    [SerializeField] private Vector2Int size;
    [SerializeField] private List<Biome> availableBiomes;
    [SerializeField] private int regionCount = 32;
    [SerializeField] private int regionOffset = 4;
    [SerializeField] private int continentCount = 4;
    private GeneratorCore core;
    private Texture2D texture;
    [SerializeField] private float landRatio;

    [SerializeField] private float continentNoiseFrequency = 0.05f;
    [SerializeField] private float continentNoiseStrength = 15f;
    [SerializeField] private Vector2[] playerStartPositions;
    [SerializeField] private Terrain terrain;
    private RectangleGrid grid;
    private void Start()
    {
        grid = new RectangleGrid((float)size.y / size.x);
        grid.SetSize(size.x);
        grid.GenerateGrid();

        core = new GeneratorCore(grid, size.x);

        var playerWorldPositions = new List<Vector3>();
        foreach (var normPos in playerStartPositions)
        {
            playerWorldPositions.Add(new Vector3(normPos.x, normPos.y, 0));
        }

        var regionSettings = new VoronoiGenerationData()
        {
            metric = Metrics.MinkowskiMetric,
            
            cellSize = regionCount,
            offsetScale = regionOffset,
            layerName = "Regions",

        };
        VoronoiGenerationData.P = 1.5f;

        var radius = size.x/2;
        var widthCoff = Mathf.Sqrt(3) / 2;
        var dioganalCoff = 1/Mathf.Sqrt(3);

        //core.AddStepGeneration(new VoronoiStep(grid, regionSettings));

        core.AddStepGeneration(new CarvingLayer((cell) =>
        {
            var x = (cell.Center.x - size.x/2);
            var y = (cell.Center.y - size.y/2);

            return Utils.GetHexDistance(Utils.PixelToHex(x, y, radius / 3)) <= 1; 

        }, "Carving"));

        core.AddStepGeneration(new VoronoiStep(grid, regionSettings));
        core.AddStepGeneration(new HexagonClusterStep(radius / 2.8f, 3, "Hex"));
        core.AddStepGeneration(new SetHexDataStep("Hex"));

        core.AddDrawingStep(new HexTerrainDrawer("Hex"), "Heights1");

        core.AddStepGeneration(new TerrainHeightOutputStep(terrain, "Heights1"));
        //core.AddStepGeneration(new NormalizeStep<float>("Heights1", "Dirt"));
        core.AddStepGeneration(new SlopeCalculationStep("Heights1", "Grass", 0.01f));
        core.AddStepGeneration(new NoiseStep("Heights2", new NoiseSettings()));

        core.AddStepGeneration(new TerrainTextureManagerStep(terrain, new List<TextureLayerSettings>()
        {
            new TextureLayerSettings() {InputLayerName = "Grass", TextureIndex = 1},
            new TextureLayerSettings() {InputLayerName = "Heights2", TextureIndex = 0}
        }));
        core.AddDrawingStep(new HeightDrawer("Grass"), "Color");

        core.Generate();
        DrawTexture();
    }

    public void DrawTexture()
    {
        texture = new Texture2D(size.x, size.y);
        texture.filterMode = FilterMode.Point;
        core.MapData.TryGetMapLayer<Color>("Color", out var colorLayer);

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                texture.SetPixel(x, y, colorLayer.Layer[grid.CalculateIndex(new Vector3(x, y))]);
            }
        }

        texture.Apply();
        var mr = GetComponent<MeshRenderer>();
        if (mr.material == null)
        {
            mr.material = new Material(Shader.Find("Unlit/Texture"));
        }
        mr.material.mainTexture = texture;
    }
}

public class BlendHexes : DrawerRegionToRegion<HexTerrainType, HexCellData>
{
    private System.Random random;
    public BlendHexes(RegionLayer region) : base(region) 
    {
        random = new System.Random();
    }
    public BlendHexes(string region) : base(region) 
    {
        random = new System.Random();
    }

    protected override float CalculateWeight(Cell targetCell, Region neighbor, Region owner)
    {
        return 1/Metric(targetCell, neighbor.Cell);
    }
    public override HexTerrainType Medium(WeightValue<HexCellData>[] values, float weightSum, Region target)
    {
        var votes = new Dictionary<HexTerrainType, float>();

        for (int i = 0; i < values.Length; i++)
        {
            var item = values[i];

            if (item.component == null) continue;

            var type = item.component.TerrainType;

            if (votes.ContainsKey(type))
            {
                votes[type] += item.weight;
            }

            else
            {
                votes.Add(type, item.weight);
            }
        }

        HexTerrainType bestType = default;
        float maxWeight = -1f;

        foreach (var pair in votes)
        {
            if (pair.Value > maxWeight)
            {
                maxWeight = pair.Value * random.Next(0, 100)/100f;
                bestType = pair.Key;
            }
        }

        return bestType;
    }
}

public class SetHexDataStep : IStepGeneration
{
    public readonly string LayerName;
    public SetHexDataStep(string hexLayerName)
    {
        LayerName = hexLayerName;
    }
    public void Procces(MapData mapData)
    {
        if(!mapData.TryGetMapLayer<Region>(LayerName, out var hexLayer))
        {
            Debug.LogError("Dont find regions layer");
            return;
        }

        var hexs = hexLayer as RegionLayer;
        var i = 0;

        foreach (var item in hexs.AllRegions)
        {
            if (Random.value < 0.5) item.AddComponent(new HexCellData(HexTerrainType.Hill));
            else item.AddComponent(new HexCellData(HexTerrainType.Plain));
            i++;
        }
    }
}

public enum ContinentType
{
    Land,
    Sea
}

public class ContinentComponent : IRegionComponent
{
    public Region Region { get; set; }
    public ContinentType Type { get; private set; }

    public ContinentComponent(ContinentType type)
    {
        Type = type;
    }

    public void AddedToCell() { }
}

