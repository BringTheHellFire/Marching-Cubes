using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MeshGenerator : MonoBehaviour {

    const int threadGroupSize = 8;

    [Header ("Dependancies")]
    [SerializeField] private DensityGenerator densityGenerator;
    [SerializeField] private ComputeShader marchingCubesComputeShader;

    [Header("Auto Update Settings")]
    [SerializeField] private bool autoUpdateInEditor = true;
    [SerializeField] private bool autoUpdateInGame = true;

    [Header("General Settings")]
    [SerializeField] private Material meshMaterial;
    [SerializeField] private bool generateColliders;

    [Header("Map Size Settings")]
    [SerializeField] private bool fixedMapSize;
    [ConditionalHide (nameof (fixedMapSize), true)]
    public Vector3Int numChunks = Vector3Int.one;
    [ConditionalHide (nameof (fixedMapSize), false)]
    [SerializeField] private Transform viewer;
    [ConditionalHide (nameof (fixedMapSize), false)]
    public float viewDistance = 30;

    [Header ("Voxel Settings")]
    public float isoLevel;
    public float boundsSize = 1;
    public Vector3 noiseOffset = Vector3.zero;

    [Range (2, 100)]
    public int numPointsPerAxis = 30;

    [Header ("Gizmos")]
    public bool showBoundsGizmo = true;
    public Color boundsGizmoCol = Color.white;

    GameObject chunkHolder;
    const string chunkHolderName = "Chunks Holder";
    List<Chunk> chunks = new();
    Dictionary<Vector3Int, Chunk> existingChunks = new();
    Queue<Chunk> recycleableChunks = new();

    bool settingsUpdated;

    private BufferManager bufferManager = new();

    private void Awake () {
        if (Application.isPlaying && !fixedMapSize)
        {
            DestroyOldChunks();
        }
    }

    //TODO: Introduce object-pooling instead of destroying chunks when they aren't needed anymore
    private static void DestroyOldChunks()
    {
        foreach (var chunk in FindObjectsOfType<Chunk>())
        {
            Destroy(chunk.gameObject);
        }
    }

    private void OnValidate()
    {
        settingsUpdated = true;
    }

    //TODO: see if this can be done outside of the update loop to optimize code
    private void Update () {
        // Update endless terrain
        if (Application.isPlaying && !fixedMapSize) {
            Run();
        }

        if (settingsUpdated) {
            RequestMeshUpdate();
            settingsUpdated = false;
        }
    }

    public void Run () {
        CreateBuffers ();
        if (fixedMapSize) {
            InitializeChunks();
            UpdateAllChunks();

        } else if (Application.isPlaying) {
                InitializeVisibleChunks();
        }

        if (!Application.isPlaying) {
            ReleaseBuffers();
        }
    }

    private void CreateBuffers()
    {
        bufferManager.CreateBuffers(numPointsPerAxis);
    }

    //TODO: This whole thing could be optimized, removing as many for loops will help performance
    // Create/get references to all chunks
    private void InitializeChunks()
    {
        CreateChunkHolder();
        chunks = new List<Chunk>();
        List<Chunk> oldChunks = new List<Chunk>(FindObjectsOfType<Chunk>());

        // Go through all coords and create a chunk there if one doesn't already exist
        for (int x = 0; x < numChunks.x; x++)
        {
            for (int y = 0; y < numChunks.y; y++)
            {
                for (int z = 0; z < numChunks.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    bool chunkAlreadyExists = false;

                    // If chunk already exists, add it to the chunks list, and remove from the old list.
                    for (int i = 0; i < oldChunks.Count; i++)
                    {
                        if (oldChunks[i].coord == coord)
                        {
                            chunks.Add(oldChunks[i]);
                            oldChunks.RemoveAt(i);
                            chunkAlreadyExists = true;
                            break;
                        }
                    }

                    // Create new chunk
                    if (!chunkAlreadyExists)
                    {
                        var newChunk = CreateChunk(coord);
                        chunks.Add(newChunk);
                    }

                    chunks[chunks.Count - 1].SetUp(meshMaterial, generateColliders);
                }
            }
        }

        // Delete all unused chunks
        for (int i = 0; i < oldChunks.Count; i++)
        {
            oldChunks[i].DestroyOrDisable();
        }
    }

    private void CreateChunkHolder()
    {
        // Create/find mesh holder object for organizing chunks under in the hierarchy
        if (chunkHolder == null)
        {
            if (GameObject.Find(chunkHolderName))
            {
                chunkHolder = GameObject.Find(chunkHolderName);
            }
            else
            {
                chunkHolder = new GameObject(chunkHolderName);
            }
        }
    }

    private Chunk CreateChunk(Vector3Int coord)
    {
        var chunk = new GameObject($"Chunk {coord}").AddComponent<Chunk>();
        chunk.transform.parent = chunkHolder.transform;
        chunk.coord = coord;
        return chunk;
    }

    public void UpdateAllChunks()
    {
        foreach (Chunk chunk in chunks)
        {
            UpdateChunkMesh(chunk);
        }
    }

    public void UpdateChunkMesh(Chunk chunk)
    {
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);
        float pointSpacing = boundsSize / (numPointsPerAxis - 1);

        Vector3Int coord = chunk.coord;
        Vector3 centre = CentreFromCoord(coord);

        Vector3 worldBounds = new Vector3(numChunks.x, numChunks.y, numChunks.z) * boundsSize;

        densityGenerator.Generate(bufferManager.PointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, noiseOffset, pointSpacing);

        bufferManager.TriangleBuffer.SetCounterValue(0);
        marchingCubesComputeShader.SetBuffer(0, "points", bufferManager.PointsBuffer);
        marchingCubesComputeShader.SetBuffer(0, "triangles", bufferManager.TriangleBuffer);
        marchingCubesComputeShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        marchingCubesComputeShader.SetFloat("isoLevel", isoLevel);

        marchingCubesComputeShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(bufferManager.TriangleBuffer, bufferManager.TriCountBuffer, 0);
        int[] triCountArray = { 0 };
        bufferManager.TriCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        bufferManager.TriangleBuffer.GetData(tris, 0, 0, numTris);

        Mesh mesh = chunk.mesh;
        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();
    }

    private Vector3 CentreFromCoord(Vector3Int coord)
    {
        // Centre entire map at origin
        if (fixedMapSize)
        {
            Vector3 totalBounds = (Vector3)numChunks * boundsSize;
            return -totalBounds / 2 + (Vector3)coord * boundsSize + Vector3.one * boundsSize / 2;
        }

        return new Vector3(coord.x, coord.y, coord.z) * boundsSize;
    }

    private void InitializeVisibleChunks () {
        if (chunks==null) {
            return;
        }
        CreateChunkHolder ();

        Vector3 p = viewer.position;
        Vector3 ps = p / boundsSize;
        Vector3Int viewerCoord = new Vector3Int (Mathf.RoundToInt (ps.x), Mathf.RoundToInt (ps.y), Mathf.RoundToInt (ps.z));

        int maxChunksInView = Mathf.CeilToInt (viewDistance / boundsSize);
        float sqrViewDistance = viewDistance * viewDistance;

        // Go through all existing chunks and flag for recyling if outside of max view dst
        for (int i = chunks.Count - 1; i >= 0; i--) {
            Chunk chunk = chunks[i];
            Vector3 centre = CentreFromCoord (chunk.coord);
            Vector3 viewerOffset = p - centre;
            Vector3 o = new Vector3 (Mathf.Abs (viewerOffset.x), Mathf.Abs (viewerOffset.y), Mathf.Abs (viewerOffset.z)) - Vector3.one * boundsSize / 2;
            float sqrDst = new Vector3 (Mathf.Max (o.x, 0), Mathf.Max (o.y, 0), Mathf.Max (o.z, 0)).sqrMagnitude;
            if (sqrDst > sqrViewDistance) {
                existingChunks.Remove (chunk.coord);
                recycleableChunks.Enqueue (chunk);
                chunks.RemoveAt (i);
            }
        }

        for (int x = -maxChunksInView; x <= maxChunksInView; x++) {
            for (int y = -maxChunksInView; y <= maxChunksInView; y++) {
                for (int z = -maxChunksInView; z <= maxChunksInView; z++) {
                    Vector3Int coord = new Vector3Int (x, y, z) + viewerCoord;

                    if (existingChunks.ContainsKey (coord)) {
                        continue;
                    }

                    Vector3 centre = CentreFromCoord (coord);
                    Vector3 viewerOffset = p - centre;
                    Vector3 o = new Vector3 (Mathf.Abs (viewerOffset.x), Mathf.Abs (viewerOffset.y), Mathf.Abs (viewerOffset.z)) - Vector3.one * boundsSize / 2;
                    float sqrDst = new Vector3 (Mathf.Max (o.x, 0), Mathf.Max (o.y, 0), Mathf.Max (o.z, 0)).sqrMagnitude;

                    // Chunk is within view distance and should be created (if it doesn't already exist)
                    if (sqrDst <= sqrViewDistance) {

                        Bounds bounds = new Bounds (CentreFromCoord (coord), Vector3.one * boundsSize);
                        if (IsVisibleFrom (bounds, Camera.main)) {
                            if (recycleableChunks.Count > 0) {
                                Chunk chunk = recycleableChunks.Dequeue ();
                                chunk.coord = coord;
                                existingChunks.Add (coord, chunk);
                                chunks.Add (chunk);
                                UpdateChunkMesh (chunk);
                            } else {
                                Chunk chunk = CreateChunk (coord);
                                chunk.coord = coord;
                                chunk.SetUp (meshMaterial, generateColliders);
                                existingChunks.Add (coord, chunk);
                                chunks.Add (chunk);
                                UpdateChunkMesh (chunk);
                            }
                        }
                    }

                }
            }
        }
    }

    void ReleaseBuffers()
    {
        bufferManager.ReleaseBuffers();
    }

    //TODO: Does this have to have such a silly check?
    public void RequestMeshUpdate()
    {
        if ((Application.isPlaying && autoUpdateInGame) || (!Application.isPlaying && autoUpdateInEditor))
        {
            Run();
        }
    }

    public bool IsVisibleFrom (Bounds bounds, Camera camera) {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes (camera);
        return GeometryUtility.TestPlanesAABB (planes, bounds);
    }

    void OnDestroy () {
        if (Application.isPlaying) {
            ReleaseBuffers ();
        }
    }

    void OnDrawGizmos () {
        if (showBoundsGizmo) {
            Gizmos.color = boundsGizmoCol;

            List<Chunk> chunks = (this.chunks == null) ? new List<Chunk> (FindObjectsOfType<Chunk> ()) : this.chunks;
            foreach (var chunk in chunks) {
                Gizmos.DrawWireCube (CentreFromCoord (chunk.coord), Vector3.one * boundsSize);
            }
        }
    }

}

public struct Triangle
{
#pragma warning disable 649 // disable unassigned variable warning
    public Vector3 a;
    public Vector3 b;
    public Vector3 c;

    public Vector3 this[int i]
    {
        get
        {
            switch (i)
            {
                case 0:
                    return a;
                case 1:
                    return b;
                default:
                    return c;
            }
        }
    }
}