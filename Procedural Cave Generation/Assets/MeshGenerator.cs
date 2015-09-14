using UnityEngine;
using System.Collections.Generic;

public class MeshGenerator : MonoBehaviour 
{
    public SquareGrid squareGrid;
    public MeshFilter walls;

    List<Vector3> vertices;
    List<int> triangles;
    Dictionary<int, List<Triangle>> triangleDic = new Dictionary<int, List<Triangle>>();
    /// <summary>
    /// List of lists of vertices that'll make up a wall.
    /// </summary>
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();
    public float WallHeight = 5;

    struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;

        int[] vertices;

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3] {a, b, c};
        }

        public int this[int i]
        {
            get
            {
                return vertices[i];
            }
        }

        public bool Contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }

    public void GenerateMesh(int[,] map, float squareSize)
    {
        triangleDic.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        squareGrid = new SquareGrid(map, squareSize);
        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
                triangulateSquare(squareGrid.squares[x, y]);
        }

        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        createWallMesh();
    }

    void createWallMesh()
    {
        calculateMeshOutlines();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();

        foreach(List<int> outline in outlines)
        {
            for(int i = 0;i<outline.Count - 1;i++)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]); // left
                wallVertices.Add(vertices[outline[i+1]]); // right
                wallVertices.Add(vertices[outline[i]] - Vector3.up * WallHeight); // bottom left
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * WallHeight); // bottom right

                /// CCW
                wallTriangles.Add(startIndex + 0); // top       left
                wallTriangles.Add(startIndex + 2); // bottom    left
                wallTriangles.Add(startIndex + 3); // bottom    right

                wallTriangles.Add(startIndex + 3); // bottom    right
                wallTriangles.Add(startIndex + 1); // top       right
                wallTriangles.Add(startIndex + 0); // top       left
            }
        }

        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        walls.mesh = wallMesh;
    }

    void triangulateSquare(Square square)
    {
        switch(square.Configuration)
        {
            case 0:
            break;

            // 1 points:
            case 1:
            MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
                break;

            // 2 points:
            case 3:
                MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 6:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
                break;
            case 5:
                MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            // 3 point:
            case 7:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            // 4 point:
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                // not outline
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
        }
    }

    void MeshFromPoints(params Node[] points)
    {
        assignVertices(points);

        if(points.Length >= 3)
            createTriangle(points[0], points[1], points[2]);
        if(points.Length >= 4)
            createTriangle(points[0], points[2], points[3]);
        if(points.Length >= 5)
            createTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6)
            createTriangle(points[0], points[4], points[5]);
    }

    void assignVertices(Node[] points)
    {
        for(int i = 0;i<points.Length;i++)
        {
            if(points[i].vertexIndex == -1)
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }

    void createTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        addTriangleToDictionary(triangle.vertexIndexA, triangle);
        addTriangleToDictionary(triangle.vertexIndexB, triangle);
        addTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    void addTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if(triangleDic.ContainsKey(vertexIndexKey))
            triangleDic[vertexIndexKey].Add(triangle);
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDic.Add(vertexIndexKey, triangleList);
        }
    }

    void calculateMeshOutlines()
    {
        for(int vertexIndex = 0;vertexIndex < vertices.Count;vertexIndex++)
        {
            if(!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = getConnectedOutlineVertex(vertexIndex);

                if(newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);

                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);
                    followOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    void followOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);

        int nextVertexIndex = getConnectedOutlineVertex(vertexIndex);

        if(nextVertexIndex != -1)
            followOutline(nextVertexIndex, outlineIndex);
    }

    /// <summary>
    /// Searches for connected vertex to the vertexIndex.
    /// If none found returns -1. Else returns the vertex index of the connected vertex.
    /// 
    /// Gets the triangles of the vertex index from the dictionary.
    /// For each triangle gets each vertex. If the vertex is not the same or not checked, then checks if it's an outline edge. If yes returns it.
    /// </summary>
    /// <param name="vertexIndex"></param>
    /// <returns></returns>
    int getConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDic[vertexIndex];

        for (int i = 0; i < trianglesContainingVertex.Count;i++ )
        {
            Triangle tri = trianglesContainingVertex[i];
            for (int j = 0; j < 3; j++)
            {
                int vertexB = tri[j];

                if(vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if (isOutlineEdge(vertexIndex, vertexB))
                        return vertexB;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Checks if the edge between vertexB and vertexA is an outline.
    /// If vertexB is connected once to vertexA, then it's an outline.
    /// </summary>
    /// <param name="vertexA"></param>
    /// <param name="vertexB"></param>
    /// <returns>True if it's an outline edge. False if not.</returns>
    bool isOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDic[vertexA];

        int sharedTriangleCount = 0;

        for(int i = 0;i<trianglesContainingVertexA.Count;i++)
        {
            if(trianglesContainingVertexA[i].Contains(vertexB))
            {
                sharedTriangleCount++;
                if (sharedTriangleCount > 1)
                    break;
            }
        }

        return sharedTriangleCount == 1;
    }

    //void OnDrawGizmos()
    //{
    //    if(squareGrid != null)
    //    {
    //        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
    //        {
    //            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
    //            {
    //                Gizmos.color = (squareGrid.squares[x, y].topLeft.active) ? Color.black : Color.white;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].topLeft.position, Vector3.one * .4f);

    //                Gizmos.color = (squareGrid.squares[x, y].topRight.active) ? Color.black : Color.white;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].topRight.position, Vector3.one * .4f);

    //                Gizmos.color = (squareGrid.squares[x, y].bottomRight.active) ? Color.black : Color.white;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * .4f);

    //                Gizmos.color = (squareGrid.squares[x, y].bottomLeft.active) ? Color.black : Color.white;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].bottomLeft.position, Vector3.one * .4f);

    //                Gizmos.color = Color.gray;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].centerTop.position, Vector3.one * .15f);
    //                Gizmos.DrawCube(squareGrid.squares[x, y].centerRight.position, Vector3.one * .15f);
    //                Gizmos.DrawCube(squareGrid.squares[x, y].centerBottom.position, Vector3.one * .15f);
    //                Gizmos.DrawCube(squareGrid.squares[x, y].centerLeft.position, Vector3.one * .15f);
    //            }
    //        }
    //    }
    //}

    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for(int x = 0;x < nodeCountX;x++)
            {
                for (int y = 0;y<nodeCountY;y++)
                {
                    Vector3 pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, 0, -mapHeight / 2 + y * squareSize + squareSize / 2);
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX-1; x++)
            {
                for (int y = 0; y < nodeCountY-1; y++)
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
            }
        }
    }

    public class Square
    {
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centerTop, centerRight, centerBottom, centerLeft;
        public int Configuration;

        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft)
        {
            topLeft = _topLeft;
            topRight = _topRight;
            bottomRight = _bottomRight;
            bottomLeft = _bottomLeft;

            centerTop = topLeft.right;
            centerRight = bottomRight.above;
            centerBottom = bottomLeft.right;
            centerLeft = bottomLeft.above;

            if (topLeft.active)
                Configuration += 8;
            if (topRight.active)
                Configuration += 4;
            if (bottomRight.active)
                Configuration += 2;
            if (bottomLeft.active)
                Configuration += 1;
        }
    }

    public class Node
    {
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 _pos)
        {
            position = _pos;
        }
    }

    public class ControlNode : Node
    {
        public bool active;
        public Node above, right;

        public ControlNode(Vector3 _pos, bool _active, float squareSize) :base(_pos)
        {
            active = _active;
            above = new Node(position + Vector3.forward * squareSize / 2f);
            right = new Node(position + Vector3.right * squareSize / 2f);
        }
    }
}
