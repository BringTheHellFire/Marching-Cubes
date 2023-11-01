using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseDensity : DensityGenerator 
{
    public NoiseSettings noiseSettings;

    [Tooltip("")]
    public Vector4 shaderParams;

    public override ComputeBuffer Generate (ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 centre, Vector3 offset, float spacing) {
        buffersToRelease = new List<ComputeBuffer> ();

        // Noise parameters
        var prng = new System.Random (noiseSettings.seed);
        var offsets = new Vector3[noiseSettings.numOctaves];
        float offsetRange = 1000;
        for (int i = 0; i < noiseSettings.numOctaves; i++) {
            offsets[i] = new Vector3 ((float) prng.NextDouble () * 2 - 1, (float) prng.NextDouble () * 2 - 1, (float) prng.NextDouble () * 2 - 1) * offsetRange;
        }

        var offsetsBuffer = new ComputeBuffer (offsets.Length, sizeof (float) * 3);
        offsetsBuffer.SetData (offsets);
        buffersToRelease.Add (offsetsBuffer);

        densityShader.SetVector ("centre", new Vector4 (centre.x, centre.y, centre.z));
        densityShader.SetInt ("octaves", Mathf.Max (1, noiseSettings.numOctaves));
        densityShader.SetFloat ("lacunarity", noiseSettings.lacunarity);
        densityShader.SetFloat ("persistence", noiseSettings.persistence);
        densityShader.SetFloat ("noiseScale", noiseSettings.noiseScale);
        densityShader.SetFloat ("noiseWeight", noiseSettings.noiseWeight);
        densityShader.SetBool ("closeEdges", noiseSettings.closeEdges);
        densityShader.SetBuffer (0, "offsets", offsetsBuffer);
        densityShader.SetFloat ("floorOffset", noiseSettings.floorOffset);
        densityShader.SetFloat ("weightMultiplier", noiseSettings.weightMultiplier);
        densityShader.SetFloat ("hardFloor", noiseSettings.hardFloorHeight);
        densityShader.SetFloat ("hardFloorWeight", noiseSettings.hardFloorWeight);

        densityShader.SetVector ("params", shaderParams);

        return base.Generate (pointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, offset, spacing);
    }
}