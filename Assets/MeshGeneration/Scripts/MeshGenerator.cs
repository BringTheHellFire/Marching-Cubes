using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MeshGenerator : MonoBehaviour {

    private const int threadGroupSize = 8;
    private const string chunkHolderName = "Chunks Holder";

    [Header ("Dependancies")]
    [SerializeField] private DensityGenerator densityGenerator;
    [SerializeField] private ComputeShader marchingCubesComputeShader;

    [Header("Noise Settings")]
    [SerializeField] private NoiseSettingsRangeData noiseSettingsData;

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
    [SerializeField] private int numPointsPerAxis = 30;

    [Header ("Gizmos")]
    [SerializeField] private bool showBoundsGizmo = true;
    [SerializeField] private Color boundsGizmoCol = Color.white;

    private GameObject chunkHolder; 
    private List<Chunk> chunks = new();
    private Dictionary<Vector3Int, Chunk> existingChunks = new();
    private Queue<Chunk> recycleableChunks = new();

    private bool settingsUpdated;

    private BufferManager bufferManager = new();

    private NoiseSettings currentNoiseSettings;
    private NoiseSettings targetNoiseSettings;
    private float transitionDuration = 5f;
    private float transitionTimer = 0.0f;
    private bool isTransitioning = false;
    private float targetIsoLevel;

    

    private void Awake () {
        if (Application.isPlaying && !fixedMapSize)
        {
            DestroyOldChunks();
        }
    }

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

    private void Update () 
    {
        if (Application.isPlaying && !fixedMapSize) {
            Run();
        }
        if (settingsUpdated) {
            RequestMeshUpdate();
            settingsUpdated = false;
        }
        if (Application.isPlaying)
        {
            UpdateNoiseSettings();
        }
    }

    public void RequestMeshUpdate()
    {
        if ((Application.isPlaying && autoUpdateInGame) || (!Application.isPlaying && autoUpdateInEditor))
        {
            Run();
        }
    }

    public void Run () {
        CreateBuffers ();

        if (fixedMapSize) {
            InitializeChunks();
            UpdateAllChunks();
        }else if (Application.isPlaying) {
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

    private void ReleaseBuffers()
    {
        bufferManager.ReleaseBuffers();
    }

    #region Initialize Chunks
    private void InitializeChunks()
    {
        CreateChunkHolder();
        chunks = new List<Chunk>();
        List<Chunk> oldChunks = new List<Chunk>(FindObjectsOfType<Chunk>());

        for (int x = 0; x < numChunks.x; x++)
        {
            for (int y = 0; y < numChunks.y; y++)
            {
                for (int z = 0; z < numChunks.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    AddOrCreateChunk(coord, oldChunks);
                }
            }
        }

        DeleteUnusedChunks(oldChunks);
    }

    private void CreateChunkHolder()
    {
        if (chunkHolder == null)
        {
            chunkHolder = GameObject.Find(chunkHolderName);
            if (chunkHolder == null)
            {
                chunkHolder = new GameObject(chunkHolderName);
            }
        }
    }

    private void AddOrCreateChunk(Vector3Int coord, List<Chunk> oldChunks)
    {
        Chunk existingChunk = FindChunkWithCoordinate(coord, oldChunks);
        if (existingChunk != null)
        {
            chunks.Add(existingChunk);
            oldChunks.Remove(existingChunk);
        }
        else
        {
            Chunk newChunk = CreateChunk(coord);
            chunks.Add(newChunk);
        }

        chunks[chunks.Count - 1].SetUp(meshMaterial, generateColliders, isoLevel);
    }

    private Chunk FindChunkWithCoordinate(Vector3Int coord, List<Chunk> chunks)
    {
        foreach (Chunk chunk in chunks)
        {
            if (chunk.coord == coord)
            {
                return chunk;
            }
        }
        return null;
    }

    private Chunk CreateChunk(Vector3Int coord)
    {
        GameObject chunk = new GameObject($"Chunk ({coord.x}, {coord.y}, {coord.z})");
        chunk.transform.parent = chunkHolder.transform;
        Chunk newChunk = chunk.AddComponent<Chunk>();
        newChunk.coord = coord;
        return newChunk;
    }

    private void DeleteUnusedChunks(List<Chunk> oldChunks)
    {
        foreach (Chunk chunk in oldChunks)
        {
            chunk.DestroyOrDisable();
        }
    }
    #endregion

    #region Update Chunks
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
        float pointSpacing = boundsSize / numVoxelsPerAxis;

        Vector3Int coord = chunk.coord;
        Vector3 centre = CentreFromCoord(coord);

        Vector3 worldBounds = new Vector3(numChunks.x, numChunks.y, numChunks.z) * boundsSize;

        GeneratePoints(worldBounds, centre, pointSpacing);

        int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        chunk.pointArray = new Vector4[numPoints];
        bufferManager.PointsBuffer.GetData(chunk.pointArray);

        ComputeMeshTriangles(numThreadsPerAxis, out Triangle[] triangles, out int numOfTriangles);
        CreateMeshForChunk(chunk, triangles, numOfTriangles);
    }

    private void GeneratePoints(Vector3 worldBounds, Vector3 centre, float pointSpacing)
    {
        densityGenerator.Generate(bufferManager.PointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, noiseOffset, pointSpacing, isoLevel);
    }

    private void ComputeMeshTriangles(int numThreadsPerAxis, out Triangle[] triangles, out int numOfTriangles)
    {
        bufferManager.TriangleBuffer.SetCounterValue(0);
        marchingCubesComputeShader.SetBuffer(0, "points", bufferManager.PointsBuffer);
        marchingCubesComputeShader.SetBuffer(0, "triangles", bufferManager.TriangleBuffer);
        marchingCubesComputeShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        marchingCubesComputeShader.SetFloat("isoLevel", isoLevel);

        marchingCubesComputeShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        numOfTriangles = GetTriangleCount();

        triangles = new Triangle[numOfTriangles];
        bufferManager.TriangleBuffer.GetData(triangles, 0, 0, numOfTriangles);
    }

    private int GetTriangleCount()
    {
        ComputeBuffer.CopyCount(bufferManager.TriangleBuffer, bufferManager.TriCountBuffer, 0);
        int[] triCountArray = { 0 };
        bufferManager.TriCountBuffer.GetData(triCountArray);
        return triCountArray[0];
    }

    private void CreateMeshForChunk(Chunk chunk, Triangle[] triangles, int numOfTriangles)
    {
        Mesh mesh = chunk.mesh;
        mesh.Clear();

        Vector3[] vertices = new Vector3[numOfTriangles * 3];
        int[] meshTriangles = new int[numOfTriangles * 3];

        for (int i = 0; i < numOfTriangles; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = triangles[i][j];
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateNormals();
    }

    private Vector3 CentreFromCoord(Vector3Int coord)
    {
        if (fixedMapSize)
        {
            Vector3 totalBounds = (Vector3)numChunks * boundsSize;
            return -totalBounds / 2 + (Vector3)coord * boundsSize + Vector3.one * boundsSize / 2;
        }

        return new Vector3(coord.x, coord.y, coord.z) * boundsSize;
    }
    #endregion

    #region Visible Chunks
    private void InitializeVisibleChunks()
    {
        if (chunks == null)
        {
            return;
        }
        CreateChunkHolder();

        Vector3 viewerPosition = viewer.position;
        Vector3Int viewerCoord = GetViewerCoord(viewerPosition);

        int maxChunksInView = Mathf.CeilToInt(viewDistance / boundsSize);
        float sqrViewDistance = viewDistance * viewDistance;

        RemoveChunksOutsideView(viewerPosition, sqrViewDistance);

        CreateNewChunksInView(viewerCoord, maxChunksInView, sqrViewDistance);
    }

    private Vector3Int GetViewerCoord(Vector3 viewerPosition)
    {
        Vector3 viewerNormalizedPosition = viewerPosition / boundsSize;
        return new Vector3Int(Mathf.RoundToInt(viewerNormalizedPosition.x), Mathf.RoundToInt(viewerNormalizedPosition.y), Mathf.RoundToInt(viewerNormalizedPosition.z));
    }

    private void RemoveChunksOutsideView(Vector3 viewerPosition, float sqrViewDistance)
    {
        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            Chunk chunk = chunks[i];
            if (IsChunkOutsideView(chunk, viewerPosition, sqrViewDistance))
            {
                RemoveChunk(chunk);
            }
        }
    }

    private bool IsChunkOutsideView(Chunk chunk, Vector3 viewerPosition, float sqrViewDistance)
    {
        Vector3 centre = CentreFromCoord(chunk.coord);
        Vector3 viewerOffset = viewerPosition - centre;
        Vector3 o = new Vector3(Mathf.Abs(viewerOffset.x), Mathf.Abs(viewerOffset.y), Mathf.Abs(viewerOffset.z)) - Vector3.one * boundsSize / 2;
        float sqrDst = new Vector3(Mathf.Max(o.x, 0), Mathf.Max(o.y, 0), Mathf.Max(o.z, 0)).sqrMagnitude;
        return sqrDst > sqrViewDistance;
    }

    private void RemoveChunk(Chunk chunk)
    {
        existingChunks.Remove(chunk.coord);
        recycleableChunks.Enqueue(chunk);
        chunks.Remove(chunk);
    }

    private void CreateNewChunksInView(Vector3Int viewerCoord, int maxChunksInView, float sqrViewDistance)
    {
        for (int x = -maxChunksInView; x <= maxChunksInView; x++)
        {
            for (int y = -maxChunksInView; y <= maxChunksInView; y++)
            {
                for (int z = -maxChunksInView; z <= maxChunksInView; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z) + viewerCoord;

                    if (existingChunks.ContainsKey(coord))
                    {
                        continue;
                    }

                    CreateChunkInView(coord, sqrViewDistance);
                }
            }
        }
    }

    private void CreateChunkInView(Vector3Int coord, float sqrViewDistance)
    {
        Vector3 centre = CentreFromCoord(coord);
        Vector3 viewerOffset = viewer.position - centre;
        Vector3 o = new Vector3(Mathf.Abs(viewerOffset.x), Mathf.Abs(viewerOffset.y), Mathf.Abs(viewerOffset.z)) - Vector3.one * boundsSize / 2;
        float sqrDst = new Vector3(Mathf.Max(o.x, 0), Mathf.Max(o.y, 0), Mathf.Max(o.z, 0)).sqrMagnitude;

        Bounds bounds = new Bounds(CentreFromCoord(coord), Vector3.one * boundsSize);

        if (sqrDst <= sqrViewDistance && IsVisibleFrom(bounds, Camera.main))
        {
            if (recycleableChunks.Count > 0)
            {
                ReuseChunk(coord);
            }
            else
            {
                CreateNewChunk(coord);
            }
        }
    }

    public bool IsVisibleFrom(Bounds bounds, Camera camera)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    private void ReuseChunk(Vector3Int coord)
    {
        Chunk chunk = recycleableChunks.Dequeue();
        chunk.coord = coord;
        existingChunks.Add(coord, chunk);
        chunks.Add(chunk);
        UpdateChunkMesh(chunk);
    }

    private void CreateNewChunk(Vector3Int coord)
    {
        Chunk chunk = CreateChunk(coord);
        chunk.coord = coord;
        chunk.SetUp(meshMaterial, generateColliders, isoLevel);
        existingChunks.Add(coord, chunk);
        chunks.Add(chunk);
        UpdateChunkMesh(chunk);
    }
    #endregion 

    #region Update Noise Settings
    private void UpdateNoiseSettings()
    {
        if (!densityGenerator.TryGetComponent(out NoiseDensity noiseDensityComponent))
        {
            return;
        }

        currentNoiseSettings = noiseDensityComponent.noiseSettings;

        if (isTransitioning)
        {
            UpdateTransition(noiseDensityComponent);
        }
        else
        {
            StartTransition();
        }
    }

    private void StartTransition()
    {
        targetNoiseSettings = NoiseSettings.GenerateRandomSettings(noiseSettingsData);
        targetIsoLevel = Random.Range(0f, 20f);
        transitionTimer = 0.0f;
        isTransitioning = true;
    }

    private void UpdateTransition(NoiseDensity noiseDensityComponent)
    {
        if (transitionTimer < transitionDuration)
        {
            float t = transitionTimer / transitionDuration;
            float lerpedIsoLevel = Mathf.Lerp(isoLevel, targetIsoLevel, t);
            isoLevel = lerpedIsoLevel;
            noiseDensityComponent.noiseSettings = NoiseSettings.Lerp(currentNoiseSettings, targetNoiseSettings, t);
            RequestMeshUpdate();
            transitionTimer += Time.deltaTime;
        }
        else
        {
            currentNoiseSettings = targetNoiseSettings;
            isoLevel = targetIsoLevel;
            isTransitioning = false;
        }
    }
    #endregion

    private void OnDestroy () 
    {
        if (Application.isPlaying) {
            ReleaseBuffers ();
        }
    }

    private void OnDrawGizmos () 
    {
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