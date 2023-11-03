using UnityEngine;

public class BufferManager
{
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer triCountBuffer;

    public ComputeBuffer TriangleBuffer => triangleBuffer;
    public ComputeBuffer PointsBuffer => pointsBuffer;
    public ComputeBuffer TriCountBuffer => triCountBuffer;

    public void CreateBuffers(int pointsPerAxis)
    {
        int numPoints = pointsPerAxis * pointsPerAxis * pointsPerAxis;
        int numVoxelsPerAxis = pointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        if (!Application.isPlaying || (pointsBuffer == null || numPoints != pointsBuffer.count))
        {
            ReleaseBuffers();
            triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
            pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
            triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        }
    }

    public void ReleaseBuffers()
    {
        triangleBuffer?.Release();
        pointsBuffer?.Release();
        triCountBuffer?.Release();
    }
}
