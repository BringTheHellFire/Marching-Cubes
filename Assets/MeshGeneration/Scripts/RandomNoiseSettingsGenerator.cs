using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomNoiseSettingsGenerator : MonoBehaviour
{
    [SerializeField] private NoiseSettingsRangeData rangeData;

    public NoiseSettings GenerateRandomSettings(NoiseSettingsRangeData noiseSettingsData)
    {
        NoiseSettings randomSettings = new NoiseSettings();

        randomSettings.seed = 1;
        randomSettings.closeEdges = true;
        randomSettings.numOctaves = 8;
        randomSettings.lacunarity = 2f;
        randomSettings.persistence = 0.54f;
        randomSettings.noiseScale = Random.Range(noiseSettingsData.NoiseScaleRandomizationRange.minValue, noiseSettingsData.NoiseScaleRandomizationRange.maxValue);
        randomSettings.noiseWeight = Random.Range(noiseSettingsData.NoiseWeightRandomizationRange.minValue, noiseSettingsData.NoiseWeightRandomizationRange.maxValue);
        randomSettings.floorOffset = Random.Range(noiseSettingsData.FloorOffsetRandomizationRange.minValue, noiseSettingsData.FloorOffsetRandomizationRange.maxValue);
        randomSettings.weightMultiplier = Random.Range(noiseSettingsData.WeightMultiplierRandomizationRange.minValue, noiseSettingsData.WeightMultiplierRandomizationRange.maxValue);
        randomSettings.hardFloorHeight = Random.Range(noiseSettingsData.HardFloorHeightRandomizationRange.minValue, noiseSettingsData.HardFloorHeightRandomizationRange.maxValue);
        randomSettings.hardFloorWeight = Random.Range(noiseSettingsData.HardFloorWeightRandomizationRange.minValue, noiseSettingsData.HardFloorWeightRandomizationRange.maxValue);

        return randomSettings;
    }
}
