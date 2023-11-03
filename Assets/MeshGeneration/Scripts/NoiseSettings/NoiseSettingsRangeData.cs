using UnityEngine;

[System.Serializable]
public struct PropertyRange
{
    public float minValue;
    public float maxValue;
}

[CreateAssetMenu(fileName = "NoiseSettings", menuName = "Custom/Noise Settings")]
public class NoiseSettingsRangeData : ScriptableObject
{
    [Header("Static Settings")]
    [SerializeField] private int seed;
    [SerializeField] private bool closeEdges;
    [SerializeField] private int numOctaves = 4;
    [SerializeField] private float lacunarity = 2;
    [SerializeField] private float persistence = .5f;

    [Header("Randomized Settings")]
    [SerializeField] private PropertyRange noiseScaleRandomizationRange;
    [SerializeField] private PropertyRange noiseWeightRandomizationRange;
    [SerializeField] private PropertyRange floorOffsetRandomizationRange;
    [SerializeField] private PropertyRange weightMultiplierRandomizationRange;
    [SerializeField] private PropertyRange hardFloorHeightRandomizationRange;
    [SerializeField] private PropertyRange hardFloorWeightRandomizationRange;

    public int Seed { get => seed; set => seed = value; }
    public bool CloseEdges { get => closeEdges; set => closeEdges = value; }
    public int NumOctaves { get => numOctaves; set => numOctaves = value; }
    public float Lacunarity { get => lacunarity; set => lacunarity = value; }
    public float Persistence { get => persistence; set => persistence = value; }

    public PropertyRange NoiseScaleRandomizationRange { get => noiseScaleRandomizationRange; set => noiseScaleRandomizationRange = value; }
    public PropertyRange NoiseWeightRandomizationRange { get => noiseWeightRandomizationRange; set => noiseWeightRandomizationRange = value; }
    public PropertyRange FloorOffsetRandomizationRange { get => floorOffsetRandomizationRange; set => floorOffsetRandomizationRange = value; }
    public PropertyRange WeightMultiplierRandomizationRange { get => weightMultiplierRandomizationRange; set => weightMultiplierRandomizationRange = value; }
    public PropertyRange HardFloorHeightRandomizationRange { get => hardFloorHeightRandomizationRange; set => hardFloorHeightRandomizationRange = value; }
    public PropertyRange HardFloorWeightRandomizationRange { get => hardFloorWeightRandomizationRange; set => hardFloorWeightRandomizationRange = value; }
    
}
