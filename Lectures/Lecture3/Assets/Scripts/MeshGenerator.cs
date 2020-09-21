using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    public MetaBallField Field = new MetaBallField();

    public Vector3 viewportLeftUpperBound = new Vector3(-4, -4, -4);
    public Vector3 viewportRightLowerBound = new Vector3(4, 4, 4);
    public float step = 0.25f;

    private int XBlocksCount => (int) Math.Ceiling((viewportRightLowerBound.x - viewportLeftUpperBound.x) / step);
    private int YBlocksCount => (int) Math.Ceiling((viewportRightLowerBound.y - viewportLeftUpperBound.y) / step);
    private int ZBlocksCount => (int) Math.Ceiling((viewportRightLowerBound.z - viewportLeftUpperBound.z) / step);

    public float surfaceLevel;

    public ComputeShader shader;
    private int _kernelIndex;

    private struct Vertex
    {
        internal Vector3 Position;
        internal Vector3 Normal;
    }
    
    private ComputeBuffer _meshBuffer;
    private Vertex[] _meshCPUBuffer;
    
    private ComputeBuffer _countBuffer;
    private readonly int[] _countCPUBuffer = new int[1];

    private MeshFilter _filter;
    private Mesh _mesh;

    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<Vector3> _normals = new List<Vector3>();
    private readonly List<int> _indices = new List<int>();

    private void InitBuffers()
    {
        // count: maximum 5 triangles
        var count = 5 * XBlocksCount * YBlocksCount * ZBlocksCount;
        // stride: struct vertex { float3 position; float3 normal } x 3 -- triangle consists of 3 vertices
        const int stride = 3 * 2 * 3 * sizeof(float);

        _meshBuffer = new ComputeBuffer(count, stride, ComputeBufferType.Append);
        _meshCPUBuffer = new Vertex[count * 3];
        
        _countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
    }

    private void InitShader()
    {
        _kernelIndex = shader.FindKernel("generate_mesh");
        shader.SetBuffer(_kernelIndex, "mesh_buffer", _meshBuffer);
    }

    private void Start()
    {
        InitBuffers();
        InitShader();

        Field.Init(shader, _kernelIndex);
    }

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
        _meshBuffer.SetCounterValue(0);
        
        _vertices.Clear();
        _indices.Clear();
        _normals.Clear();

        Field.Update();
        // ----------------------------------------------------------------
        // Generate mesh here.
        // ----------------------------------------------------------------

        shader.SetFloats(
            "viewport_left_upper_bound",
            viewportLeftUpperBound.x,
            viewportLeftUpperBound.y,
            viewportLeftUpperBound.z
        );
        shader.SetFloats(
            "viewport_right_lower_bound",
            viewportRightLowerBound.x,
            viewportRightLowerBound.y,
            viewportRightLowerBound.z
        );
        shader.SetFloat("field_step", step);
        shader.SetFloat("surface_level", surfaceLevel);

        shader.Dispatch(_kernelIndex, XBlocksCount, YBlocksCount, ZBlocksCount);
        _meshBuffer.GetData(_meshCPUBuffer);

        ComputeBuffer.CopyCount(_meshBuffer, _countBuffer, 0);
        _countBuffer.GetData(_countCPUBuffer);
        
        // count * 3, because buffer counts triangles, and we're getting vertices one by one
        for (var i = 0; i < _countCPUBuffer[0] * 3; ++i)
        {
            _vertices.Add(_meshCPUBuffer[i].Position);
            _indices.Add(i);
            _normals.Add(_meshCPUBuffer[i].Normal);
        }
        
        // Here unity automatically assumes that vertices are points and hence (x, y, z) will be represented as (x, y, z, 1) in homogenous coordinates
        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetTriangles(_indices, 0);
        _mesh.SetNormals(_normals);

        // Upload mesh data to the GPU
        _mesh.UploadMeshData(false);
    }

    private void OnDestroy()
    {
        _meshBuffer.Dispose();
        _countBuffer.Dispose();

        Field.OnDestroy();
    }
}