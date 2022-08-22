using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static AVL;

public class PathFinding : MonoBehaviour
{
    [SerializeField]
    private bool display = false;
    [SerializeField]
    private float displayActiveTime = 1;
    private enum DebugState {Offline, SelectingStart, SelectingFinish, Searching, Done}
    [SerializeField]
    private DebugState debugState = DebugState.Offline;
    private bool debugFinished;

    [SerializeField]
    private Vector3Int debugStart = new Vector3Int(0, 0, 0);
    [SerializeField]
    private Vector3Int debugFinish = new Vector3Int(0, 0, 0);

    [SerializeField]
    private Tilemap obstacleTilemap;
    [SerializeField]
    private Tilemap displayTilemap;
    [SerializeField]
    private Tile displayPathTile;
    [SerializeField]
    private Tile displaySearchedTile;
    [SerializeField]
    private Tile displayErrorTile;
    [SerializeField]
    private float debugUpdateSpeed = 1;
    private float lastUpdate = 0;

    private AVL avlTree = new AVL();
    private Dictionary<int, PathFindingNode> storedNodes = new Dictionary<int, PathFindingNode>();
    private Dictionary<int,int> expandedNodes = new Dictionary<int,int>();
    private PathFindingNode currentNode;
    private int counter = 0;

    private class PathFindingNode : Node
    {
        public PathFindingNode previousNode;
        public float distanceToFinish;
        public float distancePath;
        public PathFindingNode(float distancePath, float distanceToFinish, Vector3Int tilePosition, PathFindingNode previousNode) : base(tilePosition)
        {
            this.previousNode = previousNode;

            this.distancePath = distancePath;
            this.distanceToFinish = distanceToFinish;
        }

        public override object Clone()
        {
            return new PathFindingNode(this.distancePath, this.distanceToFinish, this.tilePosition, this.previousNode);
        }

        public override float getCost()
        {
            return this.distancePath + this.distanceToFinish;
        }

        public override int GetHashCode()
        {
            return this.tilePosition.x * 1000000 + tilePosition.y;
        }
    }

    private void Start()
    {
        Invoke("test", 1);
    }

    private void test()
    {
        //generateVirtualObstaclesMiddle(1, 0, 60, 0, 60, true);
    }

    private void Update()
    {
        if (debugState == DebugState.SelectingStart)
        {
            if (debugStart != new Vector3Int(0, 0, 0))
            {
                this.debugState = DebugState.SelectingFinish;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                this.debugStart = Vector3Int.FloorToInt(getMousePosition());
                this.debugState = DebugState.SelectingFinish;
            }
        }
        else if (debugState == DebugState.SelectingFinish)
        {
            if (debugFinish != new Vector3Int(0, 0, 0))
            {
                findPathDebug();
                this.debugState = DebugState.Searching;
                this.displayTilemap.ClearAllTiles();
            }
            if (Input.GetMouseButtonDown(0))
            {
                this.debugFinish = Vector3Int.FloorToInt(getMousePosition());

                findPathDebug();
                this.debugState = DebugState.Searching;
                this.displayTilemap.ClearAllTiles();
            }
        }
        else if (debugState == DebugState.Searching)
        {
            float startTime = Time.time;
            debugUpdate();
            float endTime = Time.time;
            //Debug.Log(endTime - startTime);

            if (debugFinished)
            {
                resetDebug();
            }
        }
    }

    private Vector3 getMousePosition()
    {
        Vector3 result = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        result.z = 0;
        return result;
    }

    /// <summary>
    /// Will update the visual debug if debug is enabled
    /// </summary>
    private void debugUpdate()
    {
        if (Time.time - lastUpdate > debugUpdateSpeed)
        {
            findPathStep();
            lastUpdate = Time.time;
        }
    }

    /// <summary>
    /// This method will find a path from start to finish using A* algorithm
    /// </summary>
    /// <param name="start"></param>
    /// <param name="finish"></param>
    /// <returns></returns>
    public List<Vector2> findShortestPath(Vector3Int start, Vector3Int finish, Dictionary<Vector3Int, string> virtualObstacles = null, int maxCounter = 100000)
    {
        // Reset previous display
        if (display) displayTilemap.ClearAllTiles();

        // Counter against infinite loop
        int counter = 0;

        // Storing all expandable nodes with hashcode
        Dictionary<int, PathFindingNode> storedNodes = new Dictionary<int, PathFindingNode>();
        // Storing all expanded nodes so they don't get added again
        Dictionary<int, int> expandedNodes = new Dictionary<int, int>();
        // Balanced tree for finding best node to expand
        AVL avlTree = new AVL();

        // Start expanding with start node
        PathFindingNode firstNode = new PathFindingNode(0, 0, start, null);
        avlTree.Add(firstNode);
        storedNodes.Add(firstNode.GetHashCode(), firstNode);

        PathFindingNode currentNode = (PathFindingNode)avlTree.PopMinValue();
        while (currentNode.tilePosition != finish && counter < maxCounter)
        {
            // Display current node
            if (display) displayTilemap.SetTile(currentNode.tilePosition, displaySearchedTile);

            // Calculate neighbors
            List<Vector3Int> neighbors = getNeighbors(currentNode.tilePosition, virtualObstacles);
            foreach (Vector3Int neighborPosition in neighbors)
            {
                float distanceToFinish = Vector3Int.Distance(neighborPosition, finish);
                float distancePath = currentNode.distancePath + Vector3Int.Distance(currentNode.tilePosition, neighborPosition);
                PathFindingNode neighborNode = new PathFindingNode(distancePath, distanceToFinish, neighborPosition, currentNode);
                int hashCode = neighborNode.GetHashCode();

                // Check if neighbor has already been expanded
                if (!expandedNodes.ContainsKey(hashCode))
                {
                    // Check if neighbor already has been discovered
                    if (storedNodes.ContainsKey(hashCode))
                    {
                        PathFindingNode node = storedNodes[hashCode];
                        // if this node has a shorter path than previous discovery => replace it
                        if (distancePath < node.distancePath)
                        {
                            avlTree.Delete(node);
                            storedNodes.Remove(hashCode);

                            avlTree.Add(neighborNode);
                            storedNodes.Add(hashCode, neighborNode);
                        }
                    }
                    else
                    {
                        avlTree.Add(neighborNode);
                        storedNodes.Add(neighborNode.GetHashCode(), neighborNode);
                    }
                }
            }
            // Get next node depending on: distance from start + distance to finish
            currentNode = (PathFindingNode)avlTree.PopMinValue();
            if(currentNode == null) return null;
            expandedNodes.Add(currentNode.GetHashCode(), 0);
            storedNodes.Remove(currentNode.GetHashCode());

            counter++;
        }
        if (counter < maxCounter)
        {
            // Path was found
            List<Vector2> path = new List<Vector2>();
            while (currentNode.previousNode != null)
            {
                Vector2 currentPosition = (Vector3)currentNode.tilePosition;
                path.Add(currentPosition + new Vector2(0.5f, 0.5f));
                if (display)
                {
                    displayTilemap.SetTile(currentNode.tilePosition, displayPathTile);
                }
                currentNode = currentNode.previousNode;
            }
            Vector2 lastPosition = (Vector3)currentNode.tilePosition;
            path.Add(lastPosition);
            path.Reverse();

            if (display) Invoke("clearDisplay", this.displayActiveTime);
            return path;
        }
        else
        {
            // No path was found in 10 000 steps => there exists no path or path is too long
            return null;
        }
    }
    public List<Vector2> findShortestPath(Vector3 s, Vector3 f, Dictionary<Vector3Int, string> virtualObstacles = null, int maxCounter = 10000)
    {
        Vector3Int start = Vector3Int.FloorToInt(s);
        Vector3Int finish = Vector3Int.FloorToInt(f);

        return findShortestPath(start, finish, virtualObstacles, maxCounter);
    }

    public List<Vector2> findPathVirtualObstacles(Vector3 s, Vector3 f)
    {
        // Find shortest path length
        Vector3Int start = Vector3Int.FloorToInt(s);
        Vector3Int finish = Vector3Int.FloorToInt(f);
        List<Vector2> shortestPath = findShortestPath(start, finish);
        float shortestPathLength = getTotalPathLength(shortestPath);

        int counter = 0;
        while(counter < 1)
        {
            (Vector2, Vector2) tuple = getMinAndMax(shortestPath);
            int minX = (int)tuple.Item1.x;
            int maxX = (int)tuple.Item2.x;
            int widthDif = maxX - minX;
            if (widthDif < 10)
            {
                minX -= 10;
                maxX += 10;
            }
            else if (widthDif > 20)
            {
                minX += widthDif/5;
                maxX -= widthDif/5;
            }

            int minY = (int)tuple.Item1.y;
            int maxY = (int)tuple.Item2.y;
            int heightDif = maxY - minY;
            if (heightDif < 10)
            {
                minY -= 10;
                maxY += 10;
            }
            else if (heightDif > 20)
            {
                minY += widthDif / 5;
                maxY -= widthDif / 5;
            }

            /*Dictionary<Vector3Int, string> virtualObstacles = generateVirtualObstaclesMiddle(1f, minX, maxX, minY, maxY);*/
            Dictionary<Vector3Int, string> virtualObstacles = generateVirtualObstaclesPath(shortestPath, 3, 10, 0.7f);

            // Find path with added obstacles
            List<Vector2> path = findShortestPath(start, finish, virtualObstacles);
            if(path != null)
            {
                float pathLength = getTotalPathLength(path);

                // Return path if not too long
                if (pathLength < shortestPathLength * 2)
                {
                    return path;
                }
            }

            // Add counter
            counter++;
        }

        // Searching a random path has failed too many times
        // Returning the shortest path
        return shortestPath;
    }
    private Dictionary<Vector3Int, string> generateVirtualObstaclesEven(float chance, int minX, int maxX, int minY, int maxY, bool display = false)
    {
        // Chance must be between 0 and 1
        chance = Mathf.Clamp(chance, 0, 1);

        // Reset virtual obstacles
        Dictionary<Vector3Int, string> virtualObstacles = new Dictionary<Vector3Int, string>();

        // Erase previous display
        if (display) displayTilemap.ClearAllTiles();

        // Placing virtual obstacles
        for (int width = minX; width < maxX; width++)
        {
            for (int height = minY; height < maxY; height++)
            {
                Vector3Int tilePosition = new Vector3Int(width, height, 0);
                float random = UnityEngine.Random.Range(0f, 1f);
                if (random < chance)
                {
                    virtualObstacles.Add(tilePosition, "o");

                    if(display) displayTilemap.SetTile(tilePosition, displayErrorTile);
                }
            }
        }
        return virtualObstacles;
    }
    private Dictionary<Vector3Int, string> generateVirtualObstaclesPath(List<Vector2> path, int size, int frequency, float chance, bool display = false, bool clear = true)
    {
        if (frequency == 0) return null;
        if (clear) this.displayTilemap.ClearAllTiles();
        int counter = 0;
        Dictionary<Vector3Int, string> result = new Dictionary<Vector3Int, string>();
        foreach (Vector2 position in path)
        {
            if(counter == frequency)
            {
                Dictionary<Vector3Int, string> virtualObstacles = generateVirtualObstaclesMiddle(chance, (int)position.x - size, (int)position.x + size, (int)position.y - size, (int)position.y + size, display, false);
                foreach (KeyValuePair<Vector3Int, string> obstacle in virtualObstacles)
                {
                    result.TryAdd(obstacle.Key, obstacle.Value);
                }
                counter = 0;
            }
            else
            {
                counter++;
            }
        }

        return result;
    }
    private Dictionary<Vector3Int, string> generateVirtualObstaclesMiddle(float chance, int minX, int maxX, int minY, int maxY, bool display = false, bool clear = true)
    {
        // Chance must be between 0 and 1
        chance = Mathf.Clamp(chance, 0, 1);

        // Reset virtual obstacles
        Dictionary<Vector3Int, string> virtualObstacles = new Dictionary<Vector3Int, string>();

        // Erase previous display
        if (clear) displayTilemap.ClearAllTiles();

        // Storing width and height
        float widthDist = (maxX - minX) / 2;
        float heightDist = (maxY - minY) / 2;

        // Placing virtual obstacles
        for (int width = minX; width < maxX; width++)
        {
            for (int height = minY; height < maxY; height++)
            {
                Vector3Int tilePosition = new Vector3Int(width, height, 0);

                // Calculating local chance, the chance gets lower further away from the center
                float currentWidthDist = Mathf.Abs(width - minX - widthDist);
                float currentHeightDist = Mathf.Abs(height - minY - heightDist);
                float percentage = (((currentWidthDist / widthDist) + (currentHeightDist / heightDist)) / 2);

                float random = UnityEngine.Random.Range(0f, 1f);
                // Local chance is calculated with exponential funcion
                float localChance = chance * Mathf.Exp(-3 * percentage);

                if (random < localChance)
                {
                    virtualObstacles.Add(tilePosition, "o");

                    if (display) displayTilemap.SetTile(tilePosition, displayErrorTile);
                }
            }
        }
        return virtualObstacles;
    }
    private float getTotalPathLength(List<Vector2> path)
    {
        float length = 0;
        for(int i = 0; i < path.Count-1; i++)
        {
            Vector2 currentPosition = path[i];
            Vector2 nextPosition = path[i + 1];

            length += Vector2.Distance(currentPosition, nextPosition);
        }
        return length;
    }
    private (Vector2, Vector2) getMinAndMax(List<Vector2> path)
    {
        if (path.Count == 0) return (new Vector2(0,0), new Vector2(0, 0));
        float minX = path[0].x;
        float minY = path[0].y;
        float maxX = path[0].x;
        float maxY = path[0].y;
        foreach(Vector2 pos in path)
        {
            if(pos.x < minX)
            {
                minX = pos.x;
            }
            if(pos.x > maxX)
            {
                maxX = pos.x;
            }

            if (pos.y < minY)
            {
                minY = pos.y;
            }
            if (pos.y > maxY)
            {
                maxY = pos.y;
            }
        }
        return (new Vector2(minX, minY), new Vector2(maxX, maxY));
    }

    public List<Vector2> findPathRandomGreedy(Vector3 s, Vector3 f, Dictionary<Vector3Int, string> virtualObstacles, int maxCounter)
    {
        Vector3Int start = Vector3Int.FloorToInt(s);
        Vector3Int finish = Vector3Int.FloorToInt(f);

        int counter = 0;
        List<Vector2> path = new List<Vector2>();
        Vector3Int currentPosition = start;
        while (currentPosition != finish && counter < maxCounter)
        {
            path.Add(new Vector2(currentPosition.x, currentPosition.y));

            List<Vector3Int> neighbors = getNeighbors(currentPosition, virtualObstacles);

            // Check Random or Greedy
            int random = UnityEngine.Random.Range(0, 10);
            if(random < 9)
            {
                // Greedy
                Vector3Int closest = getClosestNeighbor(neighbors, finish);
                currentPosition = closest;
            }
            else
            {
                // Random
                random = UnityEngine.Random.Range(0, neighbors.Count);
                currentPosition = neighbors[random];
            }

            counter++;
        }
        if(counter < maxCounter)
        {
            path.Add(new Vector2(finish.x, finish.y));
            Debug.Log("found random path");
            return path;
        }
        else
        {
            Debug.Log("did not find random path");
            Debug.Log("distance to finish: " + Vector2.Distance(path[path.Count-1], f));
            if (Vector2.Distance(path[path.Count - 1], f) < 10)
            {
                List<Vector2> extend = findShortestPath(path[path.Count - 1], f, virtualObstacles);
                if(extend != null)
                {
                    Debug.Log("extended path to finish");
                    path.AddRange(extend);
                    return path;
                }
            }
            return null;
        }
    }

    private Vector3Int getClosestNeighbor(List<Vector3Int> neighbors, Vector3Int f)
    {
        if (neighbors.Count == 0) throw new Exception("List of neighbors is empty");
        Vector3Int closest = neighbors[0];
        float closestDistance = Vector3Int.Distance(closest, f);
        foreach (Vector3Int neighbor in neighbors)
        {
            float distance = Vector3Int.Distance(neighbor, f);
            if(distance < closestDistance)
            {
                closest = neighbor;
                closestDistance = distance;
            }
        }
        return closest;
    }

    /// <summary>
    /// This will initialize the pathfinding
    /// </summary>
    private void findPathDebug()
    {
        if (obstacleTilemap.GetTile(debugStart))
        {
            Debug.Log("Start is an obstacle");
            resetDebug();
            return;
        }
        else if (obstacleTilemap.GetTile(debugFinish))
        {
            Debug.Log("Finish is an obstacle");
            resetDebug();
            return;
        }
        else
        {
            this.counter = 0;
            this.storedNodes = new Dictionary<int, PathFindingNode>();
            this.expandedNodes = new Dictionary<int, int>();
            this.avlTree = new AVL();

            PathFindingNode firstNode = new PathFindingNode(0, 0, debugStart, null);
            avlTree.Add(firstNode);
            storedNodes.Add(firstNode.GetHashCode(), firstNode);

            this.currentNode = (PathFindingNode)avlTree.PopMinValue();
            displayTilemap.SetTile(currentNode.tilePosition, displaySearchedTile);
        }
    }

    private void resetDebug()
    {
        this.debugState = DebugState.Done;
        this.debugFinished = false;
        this.debugStart = new Vector3Int(0, 0, 0);
        this.debugFinish = new Vector3Int(0, 0, 0);
    }

    private void clearDisplay()
    {
        this.displayTilemap.ClearAllTiles();
    }

    /// <summary>
    /// This will update the pathfinding with one tile
    /// </summary>
    private void findPathStep()
    {
        try
        {
            displayTilemap.SetTile(currentNode.tilePosition, displaySearchedTile);
            if (currentNode.tilePosition != debugFinish && counter < 10000)
            {
                counter++;
                List<Vector3Int> neighbors = getNeighbors(currentNode.tilePosition);
                foreach (Vector3Int neighborPosition in neighbors)
                {
                    float distanceToFinish = Vector3Int.Distance(neighborPosition, debugFinish);
                    float distancePath = currentNode.distancePath + Vector3Int.Distance(currentNode.tilePosition, neighborPosition);
                    PathFindingNode neighborNode = new PathFindingNode(distancePath, distanceToFinish, neighborPosition, currentNode);
                    int hashCode = neighborNode.GetHashCode();

                    if (!expandedNodes.ContainsKey(hashCode))
                    {
                        if (storedNodes.ContainsKey(hashCode))
                        {
                            PathFindingNode node = storedNodes[hashCode];
                            if (distancePath < node.distancePath)
                            {
                                avlTree.Delete(node);
                                storedNodes.Remove(hashCode);

                                avlTree.Add(neighborNode);
                                storedNodes.Add(hashCode, neighborNode);
                            }
                        }
                        else
                        {
                            avlTree.Add(neighborNode);
                            storedNodes.Add(neighborNode.GetHashCode(), neighborNode);
                        }
                    }
                }
                currentNode = (PathFindingNode)avlTree.PopMinValue();
                expandedNodes.Add(currentNode.GetHashCode(),0);
                storedNodes.Remove(currentNode.GetHashCode());
            }
            else if (counter < 10000)
            {
                List<Vector3> path = new List<Vector3>();
                while (currentNode.previousNode != null)
                {
                    path.Add(currentNode.tilePosition + new Vector3(0.5f,0.5f,0));
                    displayTilemap.SetTile(currentNode.tilePosition, displayPathTile);
                    currentNode = currentNode.previousNode;
                }
                path.Reverse();

                string resultString = "Found path:";
                foreach (Vector3 position in path)
                {
                    resultString += " " + position + " ";
                }
                Debug.Log(resultString);
                this.debugFinished = true;
                
            }
            else
            {
                Debug.Log("No path was found");
            }
        }
        catch(Exception exception)
        {
            displayTilemap.SetTile(currentNode.tilePosition, displayErrorTile);

            Debug.Log("Counter: " + counter);
            Debug.Log(exception.Message);
            Debug.Log(exception.StackTrace);

            throw exception;
        }
    }

    /// <summary>
    /// This will get all valid neighbours of a certain position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private List<Vector3Int> getNeighbors(Vector3Int position, Dictionary<Vector3Int, string> virtualObstacles = null)
    {
        List<Vector3Int> result = new List<Vector3Int>();
        List<Vector3Int> neighborPositionsNormal = new List<Vector3Int>() { position + new Vector3Int(1, 0, 0), position + new Vector3Int(0, 1, 0), position + new Vector3Int(-1, 0, 0), position + new Vector3Int(0, -1, 0) };
        List<Vector3Int> neighborPositionsDiagonal = new List<Vector3Int>() { position + new Vector3Int(1, 1, 0), position + new Vector3Int(-1, 1, 0), position + new Vector3Int(1, -1, 0), position + new Vector3Int(-1, -1, 0) };

        foreach (Vector3Int neighborPosition in neighborPositionsNormal)
        {
            if (obstacleTilemap.GetTile(neighborPosition) == null && (virtualObstacles == null || !virtualObstacles.ContainsKey(neighborPosition)))
            {
                result.Add(neighborPosition);
            }
        }
        foreach (Vector3Int neighborPosition in neighborPositionsDiagonal)
        {
            Vector3Int side1 = new Vector3Int(neighborPosition.x, position.y, 0);
            Vector3Int side2 = new Vector3Int(position.x, neighborPosition.y, 0);
            if (obstacleTilemap.GetTile(neighborPosition) == null && (obstacleTilemap.GetTile(side1) == null || obstacleTilemap.GetTile(side2) == null) && (virtualObstacles == null || !virtualObstacles.ContainsKey(neighborPosition)))
            {
                result.Add(neighborPosition);
            }
        }
        return result;
    }
}
