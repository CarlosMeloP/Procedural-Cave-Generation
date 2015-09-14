using UnityEngine;
using System.Collections;
using System;

public class MapGenerator : MonoBehaviour 
{
    public int width = 128;
    public int height = 72;

    public string seed;
    public bool useRandomSeed;

    [Range(0, 100)]
    public int randomFillPercent = 50;

    [Range(0, 10)]
    public int smoothIterations = 5;

    /// <summary>
    /// How far from the border the map will be created.
    /// </summary>
    public int BorderSize = 5;

    int[,] map;

    void Start()
    {
        generateMap();
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0))
            generateMap();
    }

    private void generateMap()
    {
        map = new int[width, height];
        randomFillMap();

        for (int i = 0; i < smoothIterations; i++)
            smoothMap();

        int[,] borderedMap = new int[width + BorderSize * 2, height + BorderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if (x >= BorderSize && x < width + BorderSize && y >= BorderSize && y < height + BorderSize)
                    borderedMap[x, y] = map[x - BorderSize, y - BorderSize];
                else
                    borderedMap[x, y] = 1;
            }
        }

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(borderedMap, 1);
    }

    void randomFillMap()
    {
        if(useRandomSeed)
            seed = DateTime.Now.Ticks.ToString();

        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for(int x = 0;x<width;x++)
        {
            for(int y = 0;y<height;y++)
            {
                if(x == 0 || x == width - 1|| y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1:0;
            }
        }
    }

    void smoothMap()
    {
        for(int x = 0;x<width ;x++)
        {
            for(int y = 0;y<height;y++)
            {
                int neighbourWallTiles = getSurroundingWallCount(x, y);

                if(neighbourWallTiles > 4)
                    map[x, y] = 1;
                else if(neighbourWallTiles < 4)
                    map[x, y] = 0;
            }
        }
    }

    int getSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for(int neighbourX = gridX - 1;neighbourX <= gridX + 1;neighbourX++)
        {
            for(int neighbourY = gridY - 1;neighbourY <= gridY + 1;neighbourY++)
            {
                if (neighbourX >= 0 && neighbourX < width && neighbourY >= 0 && neighbourY < height)
                {
                    if(neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    //void OnDrawGizmos()
    //{
    //    if(map != null)
    //    {
    //        for(int x = 0;x<width;x++)
    //        {
    //            for(int y = 0;y<height;y++)
    //            {
    //                Gizmos.color = (map[x, y] == 1) ? Color.black : Color.white;
    //                Vector3 pos = new Vector3(-width / 2 + x + .5f, 0, -height / 2 + y + .5f);
    //                Gizmos.DrawCube(pos, Vector3.one);
    //            }
    //        }
    //    }
    //}
}