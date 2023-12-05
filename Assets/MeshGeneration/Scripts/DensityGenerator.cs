﻿using System.Collections.Generic;
using UnityEngine;

public abstract class DensityGenerator : MonoBehaviour {

    const int threadGroupSize = 8;

    [Header("Density Calculator")]
    public ComputeShader densityShader;
    public ComputeShader caveDensityShader;

    protected List<ComputeBuffer> buffersToRelease;

    void OnValidate() {
        if (FindObjectOfType<MeshGenerator>()) {
            FindObjectOfType<MeshGenerator>().RequestMeshUpdate();
        }
    }

    public virtual ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 centre, Vector3 offset, float spacing, float isoLevel)
    {
        int numThreadsPerAxis = GetNumberOfThreadsPerAxis(numPointsPerAxis);

        SetShaderParameters(densityShader, pointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, offset, spacing, isoLevel);

        densityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        ReleaseBuffers();

        return pointsBuffer;
    }

    public virtual ComputeBuffer GenerateCaves(ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 centre, Vector3 offset, float spacing, float isoLevel)
    {
        int numThreadsPerAxis = GetNumberOfThreadsPerAxis(numPointsPerAxis);

        SetShaderParameters(caveDensityShader, pointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, offset, spacing, isoLevel);

        caveDensityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        ReleaseBuffers();

        return pointsBuffer;
    }

    private void SetShaderParameters(ComputeShader shader, ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 centre, Vector3 offset, float spacing, float isoLevel)
    {
        shader.SetBuffer(0, "points", pointsBuffer);
        shader.SetInt("numPointsPerAxis", numPointsPerAxis);
        shader.SetFloat("boundsSize", boundsSize);
        shader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
        shader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z));
        shader.SetFloat("spacing", spacing);
        shader.SetVector("worldSize", worldBounds);
        shader.SetFloat("isoLevel", isoLevel);
    }

    private int GetNumberOfThreadsPerAxis(int numPointsPerAxis)
    {
        return Mathf.CeilToInt(numPointsPerAxis / (float)threadGroupSize);
    }

    private void ReleaseBuffers()
    {
        if (buffersToRelease != null)
        {
            foreach (var b in buffersToRelease)
            {
                b.Release();
            }
        }
    }
}