using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class MetaBallField
{
    public Transform[] Balls = new Transform[0];
    public float BallRadius = 1;

    private ComputeShader _shader;
    private int _kernelIndex;

    private ComputeBuffer _balls;
    private ComputeBuffer _cubeVertices;
    private ComputeBuffer _cubeEdges;
    private ComputeBuffer _caseToTrianglesCount;
    private ComputeBuffer _caseToVertices;
    
    private void InitBuffers()
    {
        _balls = new ComputeBuffer(Balls.Length, 3 * sizeof(float));
        _cubeVertices = new ComputeBuffer(
            MarchingCubes.Tables.CubeVertices.Length,
            3 * sizeof(float),
            ComputeBufferType.Constant
        );
        _cubeEdges = new ComputeBuffer(
            MarchingCubes.Tables.CubeEdges.Length,
            2 * sizeof(int),
            ComputeBufferType.Constant
        );
        _caseToTrianglesCount = new ComputeBuffer(
            MarchingCubes.Tables.CaseToTrianglesCount.Length,
            sizeof(int),
            ComputeBufferType.Constant
        );

        // stride: one cube coloring may produce at most 5 triangles. Each triangle crosses 3 edges of cube
        _caseToVertices = new ComputeBuffer(
            MarchingCubes.Tables.CaseToVertices.Length,
            5 * 3 * sizeof(int),
            ComputeBufferType.Constant
        );
    }

    private void InitShader()
    {
        _shader.SetBuffer(_kernelIndex, "ball_positions", _balls);
        _shader.SetBuffer(_kernelIndex, "cube_vertices", _cubeVertices);
        _shader.SetBuffer(_kernelIndex, "cube_edges", _cubeEdges);
        _shader.SetBuffer(_kernelIndex, "case_to_triangles_count", _caseToTrianglesCount);
        _shader.SetBuffer(_kernelIndex, "case_to_vertices", _caseToVertices);

        _cubeVertices.SetData(MarchingCubes.Tables.CubeVertices);
        _cubeEdges.SetData(MarchingCubes.Tables.CubeEdges.SelectMany(x => x).ToArray());
        _caseToTrianglesCount.SetData(MarchingCubes.Tables.CaseToTrianglesCount);
        _caseToVertices.SetData(
            MarchingCubes.Tables.CaseToVertices
                .SelectMany(x => x.SelectMany(item => new[] {item.x, item.y, item.z})).ToArray()
        );
    }

    public void Init(ComputeShader shader, int kernelIndex)
    {
        InitBuffers();

        _shader = shader;
        _kernelIndex = kernelIndex;

        InitShader();
    }

    /// <summary>
    /// Call Field.Update to react to ball position and parameters in run-time.
    /// </summary>
    public void Update()
    {
        _shader.SetFloat("ball_radius", BallRadius);
        _shader.SetInt("ball_count", Balls.Length);

        _balls.SetData(Balls.Select(x => x.position).ToArray());
    }
    
    public void OnDestroy()
    {
        _balls.Dispose();
        _cubeVertices.Dispose();
        _cubeEdges.Dispose();
        _caseToTrianglesCount.Dispose();
        _caseToVertices.Dispose();
    }
}