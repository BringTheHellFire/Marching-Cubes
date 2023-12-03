using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {
    public Vector3Int coord;

    [HideInInspector]
    public Mesh mesh;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private bool generateCollider = false;

    public Vector4[] pointArray;
    private float isoLevel;

    public void DestroyOrDisable () {
        if (Application.isPlaying) {
            mesh.Clear ();
            gameObject.SetActive (false);
        } else {
            DestroyImmediate (gameObject, false);
        }
    }

    public void SetUp (Material mat, bool generateCollider, float isoLevel) {
        this.isoLevel = isoLevel;

        this.generateCollider = generateCollider;

        meshFilter = GetComponent<MeshFilter> ();
        meshRenderer = GetComponent<MeshRenderer> ();
        meshCollider = GetComponent<MeshCollider> ();

        if (meshFilter == null) {
            meshFilter = gameObject.AddComponent<MeshFilter> ();
        }

        if (meshRenderer == null) {
            meshRenderer = gameObject.AddComponent<MeshRenderer> ();
        }

        if (meshCollider == null && generateCollider) {
            meshCollider = gameObject.AddComponent<MeshCollider> ();
        }
        if (meshCollider != null && !generateCollider) {
            DestroyImmediate (meshCollider);
        }

        mesh = meshFilter.sharedMesh;
        if (mesh == null) {
            mesh = new Mesh ();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.sharedMesh = mesh;
        }

        if (generateCollider) {
            if (meshCollider.sharedMesh == null) {
                meshCollider.sharedMesh = mesh;
            }

            meshCollider.enabled = false;
            meshCollider.enabled = true;
        }

        meshRenderer.material = mat;
    }

    private void OnDrawGizmosSelected()
    {
        if (pointArray == null) return;

        for (int i = 0; i < pointArray.Length; i+=27)
        {
            Vector3 voxelPosition = new Vector3(pointArray[i].x, pointArray[i].y, pointArray[i].z);
            if(pointArray[i].w > isoLevel)
            {
                Gizmos.DrawSphere(transform.position + voxelPosition, 0.25f*(pointArray[i].w/10f));
            }
        }
    }
}