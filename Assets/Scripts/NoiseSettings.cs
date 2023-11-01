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
}
