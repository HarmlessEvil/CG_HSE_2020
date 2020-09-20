using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    public MetaBallField Field = new MetaBallField();

    private MeshFilter _filter;
    private Mesh _mesh;

    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<int> indices = new List<int>();

    /// <summary>
    /// Executed by Unity upon object initialization. <see cref="https://docs.unity3d.com/Manual/ExecutionOrder.html"/>
    /// </summary>
    private void Awake()
    {
        // Getting a component, responsible for storing the mesh
        _filter = GetComponent<MeshFilter>();

        // instantiating the mesh
        _mesh = _filter.mesh = new Mesh();

        // Just a little optimization, telling unity that the mesh is going to be updated frequently
        _mesh.MarkDynamic();
    }

    /// <summary>
    /// Executed by Unity on every frame <see cref="https://docs.unity3d.com/Manual/ExecutionOrder.html"/>
    /// You can use it to animate something in runtime.
    /// </summary>
    private void Update()
    {
        vertices.Clear();
        indices.Clear();
        normals.Clear();

        Field.Update();
        // ----------------------------------------------------------------
        // Generate mesh here. Below is a sample code of a cube generation.
        // ----------------------------------------------------------------

        const float step = 0.25f;
        for (float x = -4; x <= 4; x += step)
        {
            for (float y = -4; y <= 4; y += step)
            {
                for (float z = -4; z <= 4; z += step)
                {
                    var currentPivotVertex = new Vector3(x, y, z);

                    var mask = 0;
                    const float surfaceLevel = 0;
                    
                    for (var i = 0; i < MarchingCubes.Tables._cubeVertices.Length; ++i)
                    {
                        var vertexPos = currentPivotVertex + MarchingCubes.Tables._cubeVertices[i] * step;

                        if (Field.F(vertexPos) >= surfaceLevel)
                        {
                            mask |= 1 << i;
                        }
                    }

                    byte trianglesCount = MarchingCubes.Tables.CaseToTrianglesCount[mask];
                    for (var i = 0; i < trianglesCount; ++i)
                    {
                        var edges = MarchingCubes.Tables.CaseToVertices[mask][i];
                        for (var j = 0; j < 3; ++j)
                        {
                            var A = currentPivotVertex +
                                    MarchingCubes.Tables._cubeVertices[MarchingCubes.Tables._cubeEdges[edges[j]][0]] *
                                    step;
                            var B = currentPivotVertex +
                                    MarchingCubes.Tables._cubeVertices[MarchingCubes.Tables._cubeEdges[edges[j]][1]] *
                                    step;

                            var t = -Field.F(A) / (Field.F(B) - Field.F(A));
                            var p = Vector3.Lerp(A, B, t);

                            // Uncomment for some animation:
                            // p += new Vector3
                            // (
                            //     Mathf.Sin(Time.time + p.z),
                            //     Mathf.Sin(Time.time + p.y),
                            //     Mathf.Sin(Time.time + p.x)
                            // );

                            indices.Add(vertices.Count);
                            vertices.Add(p);

                            const float eps = 0.01f;
                            normals.Add(new Vector3(
                                Field.F(p + Vector3.right * eps) - Field.F(p + Vector3.left * eps),
                                Field.F(p + Vector3.up * eps) - Field.F(p + Vector3.down * eps),
                                Field.F(p + Vector3.forward * eps) - Field.F(p + Vector3.back * eps)
                            ).normalized);
                        }
                    }
                }
            }
        }

        // Here unity automatically assumes that vertices are points and hence (x, y, z) will be represented as (x, y, z, 1) in homogenous coordinates
        _mesh.Clear();
        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(indices, 0);
        _mesh.SetNormals(normals);

        // Upload mesh data to the GPU
        _mesh.UploadMeshData(false);
    }
}