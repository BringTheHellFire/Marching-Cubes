using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseSettings
{
    [Header("Noise Settings")]
    public int seed;
    [Tooltip("Number of layers of the noise. For making finer details.")]
    public int numOctaves = 4;
    [Tooltip("The frequency of each octave. For making the noise more detailed or smoother.")]
    public float lacunarity = 2;
    [Tooltip("Amplitude of each octave. A value less than 1 decreases the contirbution of each subsequen octave.")]
    public float persistence = .5f;
    [Tooltip("This scales the noise value, effectively zooming in or out on the noise pattern.")]
    public float noiseScale = 1;
    [Tooltip("This weights the noise value, allowing for a heavier or lighter influence of the noise on the overall density.")]
    public float noiseWeight = 1;
    [Tooltip("If set to true, it ensures that the edges of the density field are 'closed' or 'sealed'.")]
    public bool closeEdges;
    [Tooltip("Used to raise or lower the base height of the noise, effectively acting as a vertical offset.")]
    public float floorOffset = 1;
    [Tooltip("Weight multiplier for the floor.")]
    public float weightMultiplier = 1;
    [Tooltip("At what height (or depth) the density is adjusted more sharply, creating a 'hard' floor.")]
    public float hardFloorHeight;
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
        result.closeEdges = t < 0.5f ? a.closeEdges : b.closeEdges; // Use the closeEdges value from one of the settings.
        result.floorOffset = Mathf.Lerp(a.floorOffset, b.floorOffset, t);
        result.weightMultiplier = Mathf.Lerp(a.weightMultiplier, b.weightMultiplier, t);
        result.hardFloorHeight = Mathf.Lerp(a.hardFloorHeight, b.hardFloorHeight, t);
        result.hardFloorWeight = Mathf.Lerp(a.hardFloorWeight, b.hardFloorWeight, t);
        return result;
    }

    public static NoiseSettings GenerateRandomSettings()
    {
        NoiseSettings randomSettings = new NoiseSettings();

        // Set the range for each property based on your requirements.
        randomSettings.seed = 1;
        randomSettings.numOctaves = 2;
        randomSettings.lacunarity = Random.Range(1.5f, 3.0f); // Adjust the range as needed.
        randomSettings.persistence = Random.Range(0.3f, 0.5f); // Adjust the range as needed.
        randomSettings.noiseScale = Random.Range(0.5f, 10f); // Adjust the range as needed.
        randomSettings.noiseWeight = Random.Range(0.8f, 20f); // Adjust the range as needed.
        randomSettings.closeEdges = true;
        randomSettings.floorOffset = Random.Range(0.5f, 1.5f); // Adjust the range as needed.
        randomSettings.weightMultiplier = Random.Range(0.8f, 1.2f); // Adjust the range as needed.
        randomSettings.hardFloorHeight = Random.Range(0.0f, 10f); // Adjust the range as needed.
        randomSettings.hardFloorWeight = Random.Range(0.5f, 10f); // Adjust the range as needed.

        return randomSettings;
    }
}
