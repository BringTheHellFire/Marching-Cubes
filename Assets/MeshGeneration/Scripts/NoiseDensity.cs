﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseDensity : DensityGenerator 
{
    public Vector4 shaderParams;

    public override ComputeBuffer Generate(NoiseSettings noiseSettings, ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 centre, Vector3 offset, float spacing, float isoLevel)
    {
        buffersToRelease = new List<ComputeBuffer>();

        Vector3[] offsets = GenerateOffsets(noiseSettings.seed, noiseSettings.numOctaves, 1000);

        ComputeBuffer offsetsBuffer = CreateOffsetsBuffer(offsets);
        buffersToRelease.Add(offsetsBuffer);

        SetShaderParameters(noiseSettings.densityShader, centre, noiseSettings, shaderParams, offsetsBuffer, isoLevel);

        return base.Generate(noiseSettings, pointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, offset, spacing, isoLevel);
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

    private void SetShaderParameters(ComputeShader shader, Vector3 centre, NoiseSettings settings, Vector4 shaderParams, ComputeBuffer offsetsBuffer, float isoLevel)
    {
        shader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
        shader.SetInt("octaves", Mathf.Max(1, settings.numOctaves));
        shader.SetFloat("lacunarity", settings.lacunarity);
        shader.SetFloat("persistence", settings.persistence);
        shader.SetFloat("noiseScale", settings.noiseScale);
        shader.SetFloat("noiseWeight", settings.noiseWeight);
        shader.SetBool("closeEdges", settings.closeEdges);
        shader.SetBuffer(0, "offsets", offsetsBuffer);
        shader.SetFloat("floorOffset", settings.floorOffset);
        shader.SetFloat("weightMultiplier", settings.weightMultiplier);
        shader.SetFloat("hardFloor", settings.hardFloorHeight);
        shader.SetFloat("hardFloorWeight", settings.hardFloorWeight);
        shader.SetVector("params", shaderParams);
        shader.SetFloat("isoLevel", isoLevel);
    }
}