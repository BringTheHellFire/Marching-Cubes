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
    [SerializeField] private PropertyRange noiseScaleRandomizationRange;
    [SerializeField] private PropertyRange noiseWeightRandomizationRange;
    [SerializeField] private PropertyRange floorOffsetRandomizationRange;
    [SerializeField] private PropertyRange weightMultiplierRandomizationRange;
    [SerializeField] private PropertyRange hardFloorHeightRandomizationRange;
    [SerializeField] private PropertyRange hardFloorWeightRandomizationRange;

    public PropertyRange NoiseScaleRandomizationRange { get => noiseScaleRandomizationRange; set => noiseScaleRandomizationRange = value; }
    public PropertyRange NoiseWeightRandomizationRange { get => noiseWeightRandomizationRange; set => noiseWeightRandomizationRange = value; }
    public PropertyRange FloorOffsetRandomizationRange { get => floorOffsetRandomizationRange; set => floorOffsetRandomizationRange = value; }
    public PropertyRange WeightMultiplierRandomizationRange { get => weightMultiplierRandomizationRange; set => weightMultiplierRandomizationRange = value; }
    public PropertyRange HardFloorHeightRandomizationRange { get => hardFloorHeightRandomizationRange; set => hardFloorHeightRandomizationRange = value; }
    public PropertyRange HardFloorWeightRandomizationRange { get => hardFloorWeightRandomizationRange; set => hardFloorWeightRandomizationRange = value; }
}
