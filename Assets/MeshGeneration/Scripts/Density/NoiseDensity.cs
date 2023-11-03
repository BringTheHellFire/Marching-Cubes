using System.Collections.Generic;
using UnityEngine;

public class NoiseDensity : DensityGenerator 
{
    [SerializeField] private NoiseSettings noiseSettings;

    private Vector4 shaderParams = new Vector4(1f,0f,0f,0f);

    public NoiseSettings NoiseSettings { get => noiseSettings; set => noiseSettings = value; }

    public override ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 centre, Vector3 offset, float spacing)
    {
        buffersToRelease = new List<ComputeBuffer>();

        Vector3[] offsets = GenerateOffsets(NoiseSettings.seed, NoiseSettings.numOctaves, 1000);

        ComputeBuffer offsetsBuffer = CreateOffsetsBuffer(offsets);
        buffersToRelease.Add(offsetsBuffer);

        SetDensityShaderParameters(centre, NoiseSettings, shaderParams, offsetsBuffer);

        return base.Generate(pointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, offset, spacing);
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

    private void SetDensityShaderParameters(Vector3 centre, NoiseSettings settings, Vector4 shaderParams, ComputeBuffer offsetsBuffer)
    {
        DensityShader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
        DensityShader.SetInt("octaves", Mathf.Max(1, settings.numOctaves));
        DensityShader.SetFloat("lacunarity", settings.lacunarity);
        DensityShader.SetFloat("persistence", settings.persistence);
        DensityShader.SetFloat("noiseScale", settings.noiseScale);
        DensityShader.SetFloat("noiseWeight", settings.noiseWeight);
        DensityShader.SetBool("closeEdges", settings.closeEdges);
        DensityShader.SetBuffer(0, "offsets", offsetsBuffer);
        DensityShader.SetFloat("floorOffset", settings.floorOffset);
        DensityShader.SetFloat("weightMultiplier", settings.weightMultiplier);
        DensityShader.SetFloat("hardFloor", settings.hardFloorHeight);
        DensityShader.SetFloat("hardFloorWeight", settings.hardFloorWeight);
        DensityShader.SetVector("params", shaderParams);
    }
}