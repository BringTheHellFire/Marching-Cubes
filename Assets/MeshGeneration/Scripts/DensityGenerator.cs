using System.Collections.Generic;
using UnityEngine;

public abstract class DensityGenerator : MonoBehaviour 
{
    private const int threadGroupSize = 8;

    [Header("Density Calculator")]
    [SerializeField] private ComputeShader densityShader;

    protected List<ComputeBuffer> buffersToRelease;

    public ComputeShader DensityShader { get => densityShader; set => densityShader = value; }

    void OnValidate() {
        if (FindObjectOfType<MeshGenerator>()) {
            FindObjectOfType<MeshGenerator>().RequestMeshUpdate();
        }
    }

    public virtual ComputeBuffer Generate (ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 centre, Vector3 offset, float spacing)
    {
        int numThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / (float)threadGroupSize);

        DensityShader.SetBuffer(0, "points", pointsBuffer);
        DensityShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        DensityShader.SetFloat("boundsSize", boundsSize);
        DensityShader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
        DensityShader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z));
        DensityShader.SetFloat("spacing", spacing);
        DensityShader.SetVector("worldSize", worldBounds);

        DensityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

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