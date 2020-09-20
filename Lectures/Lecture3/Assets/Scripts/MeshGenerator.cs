using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    public MetaBallField Field = new MetaBallField();

    public Vector3 viewportLeftUpperBound = new Vector3(-4, -4, -4);
    public Vector3 viewportRightLowerBound = new Vector3(4, 4, 4);
    public float step = 0.25f;

    public float surfaceLevel;
    
    private MeshFilter _filter;
    private Mesh _mesh;

    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<Vector3> _normals = new List<Vector3>();
    private readonly List<int> _indices = new List<int>();

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
        _vertices.Clear();
        _indices.Clear();
        _normals.Clear();

        Field.Update();
        // ----------------------------------------------------------------
        // Generate mesh here. Below is a sample code of a cube generation.
        // ----------------------------------------------------------------

        for (var x = viewportLeftUpperBound.x; x <= viewportRightLowerBound.x; x += step)
        {
            for (var y = viewportLeftUpperBound.y; y <= viewportRightLowerBound.y; y += step)
            {
                for (var z = viewportLeftUpperBound.z; z <= viewportRightLowerBound.z; z += step)
                {
                    var currentPivotVertex = new Vector3(x, y, z);

                    var mask = 0;
                    
                    for (var i = 0; i < MarchingCubes.Tables.CubeVertices.Length; ++i)
                    {
                        var vertexPos = currentPivotVertex + MarchingCubes.Tables.CubeVertices[i] * step;

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
                            var a = currentPivotVertex +
                                    MarchingCubes.Tables.CubeVertices[MarchingCubes.Tables.CubeEdges[edges[j]][0]] *
                                    step;
                            var b = currentPivotVertex +
                                    MarchingCubes.Tables.CubeVertices[MarchingCubes.Tables.CubeEdges[edges[j]][1]] *
                                    step;

                            var t = -Field.F(a) / (Field.F(b) - Field.F(a));
                            var p = Vector3.Lerp(a, b, t);

                            // Uncomment for some animation:
                            // p += new Vector3
                            // (
                            //     Mathf.Sin(Time.time + p.z),
                            //     Mathf.Sin(Time.time + p.y),
                            //     Mathf.Sin(Time.time + p.x)
                            // );

                            _indices.Add(_vertices.Count);
                            _vertices.Add(p);

                            const float eps = 0.01f;
                            _normals.Add(new Vector3(
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
        _mesh.SetVertices(_vertices);
        _mesh.SetTriangles(_indices, 0);
        _mesh.SetNormals(_normals);

        // Upload mesh data to the GPU
        _mesh.UploadMeshData(false);
    }
}