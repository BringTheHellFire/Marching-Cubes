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
    [SerializeField] private Vector3Int numChunks = Vector3Int.one;
    [SerializeField] private float boundsSize = 1;

    [Header("Voxel Settings")]
    [SerializeField] private float isoLevel;
    [SerializeField] private Vector3 noiseOffset = Vector3.zero;

    [Range (2, 100)]
    [SerializeField] private int numPointsPerAxis = 30;

    [Header ("Gizmos")]
    [SerializeField] private bool showBoundsGizmo = true;
    [SerializeField] private Color boundsGizmoCol = Color.white;

    private GameObject chunkHolder; 
    private List<Chunk> chunks = new();

    private bool settingsUpdated;

    private BufferManager bufferManager = new();

    private NoiseSettings currentNoiseSettings;
    private NoiseSettings targetNoiseSettings;
    private float transitionDuration = 5f;
    private float transitionTimer = 0.0f;
    private bool isTransitioning = false;
    private float targetIsoLevel;

    public Vector3Int NumChunks { get => numChunks; set => numChunks = value; }
    public float BoundsSize { get => boundsSize; set => boundsSize = value; }
    public float IsoLevel { get => isoLevel; set => isoLevel = value; }
    public Vector3 NoiseOffset { get => noiseOffset; set => noiseOffset = value; }

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

        for (int x = 0; x < NumChunks.x; x++)
        {
            for (int y = 0; y < NumChunks.y; y++)
            {
                for (int z = 0; z < NumChunks.z; z++)
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

        chunks[chunks.Count - 1].SetUp(meshMaterial, generateColliders);
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
        float pointSpacing = BoundsSize / numVoxelsPerAxis;

        Vector3Int coord = chunk.coord;
        Vector3 centre = CentreFromCoord(coord);

        Vector3 worldBounds = new Vector3(NumChunks.x, NumChunks.y, NumChunks.z) * BoundsSize;

        GeneratePoints(worldBounds, centre, pointSpacing);
        ComputeMeshTriangles(numThreadsPerAxis, out Triangle[] triangles, out int numOfTriangles);
        CreateMeshForChunk(chunk, triangles, numOfTriangles);
    }

    private void GeneratePoints(Vector3 worldBounds, Vector3 centre, float pointSpacing)
    {
        densityGenerator.Generate(bufferManager.PointsBuffer, numPointsPerAxis, BoundsSize, worldBounds, centre, NoiseOffset, pointSpacing);
    }

    private void ComputeMeshTriangles(int numThreadsPerAxis, out Triangle[] triangles, out int numOfTriangles)
    {
        bufferManager.TriangleBuffer.SetCounterValue(0);
        marchingCubesComputeShader.SetBuffer(0, "points", bufferManager.PointsBuffer);
        marchingCubesComputeShader.SetBuffer(0, "triangles", bufferManager.TriangleBuffer);
        marchingCubesComputeShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        marchingCubesComputeShader.SetFloat("isoLevel", IsoLevel);

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
            Vector3 totalBounds = (Vector3)NumChunks * BoundsSize;
            return -totalBounds / 2 + (Vector3)coord * BoundsSize + Vector3.one * BoundsSize / 2;
        }

        return new Vector3(coord.x, coord.y, coord.z) * BoundsSize;
    }
    #endregion

    #region Update Noise Settings
    private void UpdateNoiseSettings()
    {
        if (!densityGenerator.TryGetComponent(out NoiseDensity noiseDensityComponent))
        {
            return;
        }

        currentNoiseSettings = noiseDensityComponent.NoiseSettings;

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
            float lerpedIsoLevel = Mathf.Lerp(IsoLevel, targetIsoLevel, t);
            IsoLevel = lerpedIsoLevel;
            noiseDensityComponent.NoiseSettings = NoiseSettings.Lerp(currentNoiseSettings, targetNoiseSettings, t);
            RequestMeshUpdate();
            transitionTimer += Time.deltaTime;
        }
        else
        {
            currentNoiseSettings = targetNoiseSettings;
            IsoLevel = targetIsoLevel;
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
                Gizmos.DrawWireCube (CentreFromCoord (chunk.coord), Vector3.one * BoundsSize);
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