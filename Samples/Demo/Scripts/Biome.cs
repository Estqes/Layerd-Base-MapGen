using UnityEngine;
public enum BiomeType
{
    Land,
    Water
}

[CreateAssetMenu(fileName = "New Biome", menuName = "Map Generation/Biome")]
public class Biome : ScriptableObject
{
    public string BiomeName;
    public Color BiomeColor;

    public BiomeType Type;
}