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
        int numThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / (float)threadGroupSize);

        densityShader.SetBuffer(0, "points", pointsBuffer);
        densityShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        densityShader.SetFloat("boundsSize", boundsSize);
        densityShader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
        densityShader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z));
        densityShader.SetFloat("spacing", spacing);
        densityShader.SetVector("worldSize", worldBounds);
        densityShader.SetFloat("isoLevel", isoLevel);

        densityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        ReleaseBuffers();

        return pointsBuffer;
    }

    public virtual ComputeBuffer GenerateCaves(ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 centre, Vector3 offset, float spacing, float isoLevel)
    {
        int numThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / (float)threadGroupSize);

        caveDensityShader.SetBuffer(0, "points", pointsBuffer);
        caveDensityShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        caveDensityShader.SetFloat("boundsSize", boundsSize);
        caveDensityShader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
        caveDensityShader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z));
        caveDensityShader.SetFloat("spacing", spacing);
        caveDensityShader.SetVector("worldSize", worldBounds);
        caveDensityShader.SetFloat("isoLevel", isoLevel);

        caveDensityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        ReleaseBuffers();

        return pointsBuffer;
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