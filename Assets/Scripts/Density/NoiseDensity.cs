using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseDensity : DensityGenerator 
{
    [Header ("Noise Settings")]
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

    [Tooltip("")]
    public Vector4 shaderParams;

    public override ComputeBuffer Generate (ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 centre, Vector3 offset, float spacing) {
        buffersToRelease = new List<ComputeBuffer> ();

        // Noise parameters
        var prng = new System.Random (seed);
        var offsets = new Vector3[numOctaves];
        float offsetRange = 1000;
        for (int i = 0; i < numOctaves; i++) {
            offsets[i] = new Vector3 ((float) prng.NextDouble () * 2 - 1, (float) prng.NextDouble () * 2 - 1, (float) prng.NextDouble () * 2 - 1) * offsetRange;
        }

        var offsetsBuffer = new ComputeBuffer (offsets.Length, sizeof (float) * 3);
        offsetsBuffer.SetData (offsets);
        buffersToRelease.Add (offsetsBuffer);

        densityShader.SetVector ("centre", new Vector4 (centre.x, centre.y, centre.z));
        densityShader.SetInt ("octaves", Mathf.Max (1, numOctaves));
        densityShader.SetFloat ("lacunarity", lacunarity);
        densityShader.SetFloat ("persistence", persistence);
        densityShader.SetFloat ("noiseScale", noiseScale);
        densityShader.SetFloat ("noiseWeight", noiseWeight);
        densityShader.SetBool ("closeEdges", closeEdges);
        densityShader.SetBuffer (0, "offsets", offsetsBuffer);
        densityShader.SetFloat ("floorOffset", floorOffset);
        densityShader.SetFloat ("weightMultiplier", weightMultiplier);
        densityShader.SetFloat ("hardFloor", hardFloorHeight);
        densityShader.SetFloat ("hardFloorWeight", hardFloorWeight);

        densityShader.SetVector ("params", shaderParams);

        return base.Generate (pointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, offset, spacing);
    }
}