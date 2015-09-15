using System;
using UnityEngine;
using System.Collections.Generic;

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

    public int WallThresholdSize = 50;
    public int RoomThresholdSize = 50;

    struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int x, int y)
        {
            tileX = x;
            tileY = y;
        }
    }

    class Room : IComparable<Room>
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;
        public int RoomSize;
        public bool IsAccessibleFromMainRoom;
        public bool IsMainRoom;

        public Room()
        {

        }

        public Room(List<Coord> roomTiles, int[,] map)
        {
            tiles = roomTiles;
            RoomSize = tiles.Count;
            connectedRooms = new List<Room>();

            edgeTiles = new List<Coord>();
            foreach(Coord tile in tiles)
            {
                for(int x = tile.tileX-1;x<=tile.tileX+1;x++)
                {
                    for(int y = tile.tileY - 1;y<=tile.tileY;y++)
                    {
                        if(x==tile.tileX || y == tile.tileY)    /// Excluding diagonal
                        {
                            if(map[x,y] == 1)
                                edgeTiles.Add(tile);
                        }
                    }
                }
            }
        }

        public void SetAccessibleFromMainRoom()
        {
            if(!IsAccessibleFromMainRoom)
            {
                IsAccessibleFromMainRoom = true;
                foreach (Room connectedRoom in connectedRooms)
                    connectedRoom.SetAccessibleFromMainRoom();
            }
        }

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.IsAccessibleFromMainRoom)
                roomB.SetAccessibleFromMainRoom();
            else if (roomB.IsAccessibleFromMainRoom)
                roomA.SetAccessibleFromMainRoom();

            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }

        public int CompareTo(Room otherRoom)
        {
            return otherRoom.RoomSize.CompareTo(RoomSize);
        }
    }

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

        processMap();

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

    /// <summary>
    /// Find small chunks of regions and remove them.
    /// Find remaining rooms.
    /// </summary>
    void processMap()
    {
        // Process walls
        List<List<Coord>> wallRegions = getRegions(1);

        foreach (List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < WallThresholdSize)
            {
                foreach(Coord tile in wallRegion)
                    map[tile.tileX, tile.tileY] = 0;
            }
            
        }

        // Process walls
        List<List<Coord>> roomRegions = getRegions(0);
        List<Room> survivingRooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < RoomThresholdSize)
            {
                foreach (Coord tile in roomRegion)
                    map[tile.tileX, tile.tileY] = 1;
            }
            else
                survivingRooms.Add(new Room(roomRegion, map));
        }

        survivingRooms.Sort();
        survivingRooms[0].IsMainRoom = true;
        survivingRooms[0].IsAccessibleFromMainRoom = true;
        connectClosestRooms(survivingRooms);
    }

    void connectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if(forceAccessibilityFromMainRoom)
        {
            foreach(Room room in allRooms)
            {
                if (room.IsAccessibleFromMainRoom)
                    roomListB.Add(room);
                else
                    roomListA.Add(room);
            }
        }
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach(Room roomA in roomListA)
        {
            if(!forceAccessibilityFromMainRoom)
            {
                possibleConnectionFound = false;
                if(roomA.connectedRooms.Count > 0)
                    continue;
            }
            
            foreach(Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB))
                    continue;

                for(int tileIndexA = 0;tileIndexA< roomA.edgeTiles.Count;tileIndexA++)
                {
                    for(int tileIndexB = 0;tileIndexB<roomB.edgeTiles.Count;tileIndexB++)
                    {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        if(distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }

            if(possibleConnectionFound && !forceAccessibilityFromMainRoom)
                createPassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
        }

        if (possibleConnectionFound && forceAccessibilityFromMainRoom)
        {
            createPassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            connectClosestRooms(allRooms, true);
        }
        
        if(!forceAccessibilityFromMainRoom)
            connectClosestRooms(allRooms, true);
    }

    void createPassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);
        Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.green, 10f);
    }

    /// <summary>
    /// Returns the 3d position of a tile.
    /// 
    /// Assumes map is drawn centered.
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3(-width/2+.5f+tile.tileX, 2, -height/2+.5f+tile.tileY);
    }

    /// <summary>
    /// Get a chunk of tileType.
    /// </summary>
    /// <param name="tileType"></param>
    /// <returns></returns>
    List<List<Coord>> getRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for(int x = 0;x<width ;x++)
        {
            for(int y = 0;y<height;y++)
            {
                if(mapFlags[x,y] == 0 && map[x,y] == tileType)
                {
                    List<Coord> newRegion = getRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach(Coord tile in newRegion)
                        mapFlags[tile.tileX, tile.tileY] = 1;
                }
            }
        }

        return regions;
    }

    /// <summary>
    /// Finds all tiles inside a region and returns it.
    /// </summary>
    /// <param name="startX"></param>
    /// <param name="startY"></param>
    /// <returns></returns>
    List<Coord> getRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height]; // If 1 then the tile has been checked.

        int tileType = map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while(queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for(int x = tile.tileX - 1;x<=tile.tileX + 1;x++)
            {
                for(int y = tile.tileY-1;y<=tile.tileY + 1;y++)
                {
                    if(isInMapRange(x,y) && (y == tile.tileY || x == tile.tileX))   // In map and not diagonal.
                    {
                        if(mapFlags[x,y] == 0 && map[x,y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    /// <summary>
    /// Checks if a tile exists in map.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    bool isInMapRange(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
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
                    map[x, y] = 1;
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
                if (isInMapRange(neighbourX, neighbourY))
                {
                    if(neighbourX != gridX || neighbourY != gridY)
                        wallCount += map[neighbourX, neighbourY];
                }
                else
                    wallCount++;
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