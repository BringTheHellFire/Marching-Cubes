using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseSettings
{
    [Header("General Settings")]
    [Tooltip("The seed of the generation.")]
    public int seed;
    [Tooltip("If set to true, it ensures that the edges of the density field are 'closed' or 'sealed'.")]
    public bool closeEdges;
    [Tooltip("Number of layers of the noise. For making finer details.")]
    public int numOctaves = 4;
    [Tooltip("The frequency of each octave. For making the noise more detailed or smoother.")]
    public float lacunarity = 2;
    [Tooltip("Amplitude of each octave. A value less than 1 decreases the contirbution of each subsequen octave.")]
    public float persistence = .5f;

    [Header("Randomized Properties")]
    [Tooltip("This scales the noise value, effectively zooming in or out on the noise pattern.")]
    public float noiseScale = 1;
    [Space(2)]
    [Tooltip("This weights the noise value, allowing for a heavier or lighter influence of the noise on the overall density.")]
    public float noiseWeight = 1;
    [Space(2)]
    [Tooltip("Used to raise or lower the base height of the noise, effectively acting as a vertical offset.")]
    public float floorOffset = 1;
    [Space(2)]
    [Tooltip("Weight multiplier for the floor.")]
    public float weightMultiplier = 1;
    [Space(2)]
    [Tooltip("At what height (or depth) the density is adjusted more sharply, creating a 'hard' floor.")]
    public float hardFloorHeight;
    [Space(2)]
    [Tooltip("Specifies how 'hard' or strong the effect of the hard floor is on the density.")]
    public float hardFloorWeight;

    public static NoiseSettings Lerp(NoiseSettings a, NoiseSettings b, float t)
    {
        NoiseSettings result = new NoiseSettings();
        result.seed = Mathf.RoundToInt(Mathf.Lerp(a.seed, b.seed, t));
        result.numOctaves = Mathf.RoundToInt(Mathf.Lerp(a.numOctaves, b.numOctaves, t));
        result.lacunarity = Mathf.Lerp(a.lacunarity, b.lacunarity, t);
        result.persistence = Mathf.Lerp(a.persistence, b.persistence, t);
        result.noiseScale = Mathf.Lerp(a.noiseScale, b.noiseScale, t);
        result.noiseWeight = Mathf.Lerp(a.noiseWeight, b.noiseWeight, t);
        result.closeEdges = t < 0.5f ? a.closeEdges : b.closeEdges;
        result.floorOffset = Mathf.Lerp(a.floorOffset, b.floorOffset, t);
        result.weightMultiplier = Mathf.Lerp(a.weightMultiplier, b.weightMultiplier, t);
        result.hardFloorHeight = Mathf.Lerp(a.hardFloorHeight, b.hardFloorHeight, t);
        result.hardFloorWeight = Mathf.Lerp(a.hardFloorWeight, b.hardFloorWeight, t);
        return result;
    }

    public static NoiseSettings GenerateRandomSettings(NoiseSettingsRangeData noiseSettingsData)
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


