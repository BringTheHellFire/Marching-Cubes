using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseDensity : DensityGenerator 
{
    public NoiseSettings noiseSettings;

    public Vector4 shaderParams;

    public override ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 centre, Vector3 offset, float spacing, float isoLevel)
    {
        buffersToRelease = new List<ComputeBuffer>();

        Vector3[] offsets = GenerateOffsets(noiseSettings.seed, noiseSettings.numOctaves, 1000);

        ComputeBuffer offsetsBuffer = CreateOffsetsBuffer(offsets);
        buffersToRelease.Add(offsetsBuffer);

        SetDensityShaderParameters(centre, noiseSettings, shaderParams, offsetsBuffer, isoLevel);

        return base.Generate(pointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, offset, spacing, isoLevel);
    }

    private Vector3[] GenerateOffsets(int seed, int numOctaves, float offsetRange)
    {
        System.Random prng = new System.Random(seed);
        Vector3[] offsets = new Vector3[numOctaves];

        for (int i = 0; i < numOctaves; i++)
        {
            offsets[i] = new Vector3(
                (float)prng.NextDouble() * 2 - 1,
                (float)prng.NextDouble() * 2 - 1,
                (float)prng.NextDouble() * 2 - 1
            ) * offsetRange;
        }

        return offsets;
    }

    private ComputeBuffer CreateOffsetsBuffer(Vector3[] offsets)
    {
        ComputeBuffer offsetsBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 3);
        offsetsBuffer.SetData(offsets);
        return offsetsBuffer;
    }

    private void SetDensityShaderParameters(Vector3 centre, NoiseSettings settings, Vector4 shaderParams, ComputeBuffer offsetsBuffer, float isoLevel)
    {
        densityShader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
        densityShader.SetInt("octaves", Mathf.Max(1, settings.numOctaves));
        densityShader.SetFloat("lacunarity", settings.lacunarity);
        densityShader.SetFloat("persistence", settings.persistence);
        densityShader.SetFloat("noiseScale", settings.noiseScale);
        densityShader.SetFloat("noiseWeight", settings.noiseWeight);
        densityShader.SetBool("closeEdges", settings.closeEdges);
        densityShader.SetBuffer(0, "offsets", offsetsBuffer);
        densityShader.SetFloat("floorOffset", settings.floorOffset);
        densityShader.SetFloat("weightMultiplier", settings.weightMultiplier);
        densityShader.SetFloat("hardFloor", settings.hardFloorHeight);
        densityShader.SetFloat("hardFloorWeight", settings.hardFloorWeight);
        densityShader.SetVector("params", shaderParams);
        densityShader.SetFloat("isoLevel", isoLevel);
    }
}