using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static AVL;

public class PathFinding : MonoBehaviour
{
    [SerializeField]
    private bool debug = false;

    [SerializeField]
    private Vector3Int start = new Vector3Int(0,0,0);
    [SerializeField]
    private Vector3Int finish = new Vector3Int(0, 0, 0);

    [SerializeField]
    private Tilemap obstacleTilemap;
    [SerializeField]
    private Tilemap displayTilemap;
    [SerializeField]
    private Tile displayPathTile;
    [SerializeField]
    private Tile displaySearchedTile;
    [SerializeField]
    private float debugUpdateSpeed = 1;
    private float lastUpdate = 0;

    private AVL avlTree = new AVL();
    private Dictionary<int, Node> storedNodes = new Dictionary<int, Node>();
    private Node currentNode;
    private int counter = 0;
    private bool searching = false;

    private void Start()
    {
        if (debug) Invoke("findPathDebug", 1);
    }

    private void Update()
    {
        debugUpdate();
    }

    /// <summary>
    /// Will update the visual debug if debug is enabled
    /// </summary>
    private void debugUpdate()
    {
        if (searching && Time.time - lastUpdate > debugUpdateSpeed)
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
    public List<Vector3> findPath(Vector3Int start, Vector3Int finish)
    {
        int counter = 0;
        Dictionary<int, Node> storedNodes = new Dictionary<int, Node>();
        AVL avlTree = new AVL();

        Node firstNode = new Node(0, 0, start, null);
        avlTree.Add(firstNode);
        storedNodes.Add(firstNode.GetHashCode(),firstNode);

        Node currentNode = avlTree.PopMinValue();
        while(currentNode.tilePosition != finish && counter < 10000)
        {
            counter++;
            List<Vector3Int> neighbors = getNeighbors(currentNode.tilePosition);
            foreach (Vector3Int neighborPosition in neighbors)
            {
                float distanceToFinish = Vector3Int.Distance(neighborPosition, finish);
                float distancePath = currentNode.distancePath + Vector3Int.Distance(currentNode.tilePosition, neighborPosition);
                Node neighborNode = new Node(distancePath, distanceToFinish,  neighborPosition, currentNode);

                if (storedNodes.ContainsKey(neighborNode.GetHashCode()))
                {
                    Node node = storedNodes[neighborNode.GetHashCode()];
                    if (distancePath < node.distancePath)
                    {
                        avlTree.Delete(node);
                        storedNodes.Remove(neighborNode.GetHashCode());

                        avlTree.Add(neighborNode);
                        storedNodes.Add(neighborNode.GetHashCode(), neighborNode);
                    }
                }
                else
                {
                    avlTree.Add(neighborNode);
                    storedNodes.Add(neighborNode.GetHashCode(), neighborNode);
                }
            }
            currentNode = avlTree.PopMinValue();
        }

        List<Vector3> path = new List<Vector3>();
        while (currentNode.previousNode != null)
        {
            path.Add(currentNode.tilePosition + new Vector3(0.5f,0.5f,0));
            currentNode = currentNode.previousNode;
        }
        path.Add(currentNode.tilePosition);
        path.Reverse();

        return path;
    }

    /// <summary>
    /// This will initialize the pathfinding
    /// </summary>
    private void findPathDebug()
    {
        if (obstacleTilemap.GetTile(start))
        {
            searching = false;
            throw new Exception("Start is an obstacle");
        }
        else if (obstacleTilemap.GetTile(finish))
        {
            searching = false;
            throw new Exception("Finish is an obstacle");
        }
        else
        {
            this.counter = 0;
            this.storedNodes = new Dictionary<int, Node>();
            this.avlTree = new AVL();

            Node firstNode = new Node(0, 0, start, null);
            avlTree.Add(firstNode);
            storedNodes.Add(firstNode.GetHashCode(), firstNode);

            this.currentNode = avlTree.PopMinValue();
            this.searching = true;
            displayTilemap.SetTile(currentNode.tilePosition, displaySearchedTile);
        }
    }

    /// <summary>
    /// This will update the pathfinding with one tile
    /// </summary>
    private void findPathStep()
    {
        try
        {
            if (currentNode.tilePosition != finish && counter < 10000)
            {
                if (currentNode.tilePosition == new Vector3Int(13, 0, 0))
                {
                    int lol = 0;
                }
                counter++;
                List<Vector3Int> neighbors = getNeighbors(currentNode.tilePosition);
                foreach (Vector3Int neighborPosition in neighbors)
                {
                    if (neighborPosition == new Vector3Int(12, 0, 0))
                    {
                        int lol = 0;
                    }
                    float distanceToFinish = Vector3Int.Distance(neighborPosition, finish);
                    float distancePath = currentNode.distancePath + Vector3Int.Distance(currentNode.tilePosition, neighborPosition);
                    Node neighborNode = new Node(distancePath, distanceToFinish, neighborPosition, currentNode);
                    int hashCode = neighborNode.GetHashCode();

                    if (storedNodes.ContainsKey(hashCode))
                    {
                        Node node = storedNodes[hashCode];
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
                currentNode = avlTree.PopMinValue();
                displayTilemap.SetTile(currentNode.tilePosition, displaySearchedTile);
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
                searching = false;

                string resultString = "Found path:";
                foreach (Vector3 position in path)
                {
                    resultString += " " + position + " ";
                }
                Debug.Log(resultString);
            }
            else
            {
                Debug.Log("No path was found");
                searching = false;
            }
        }
        catch(Exception execption)
        {
            searching = false;
            Debug.Log("Counter: " + counter);
            Debug.Log(execption.Message);
            Debug.Log(execption.StackTrace);
            throw execption;
        }
    }

    /// <summary>
    /// This will get all valid neighbours of a certain position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private List<Vector3Int> getNeighbors(Vector3Int position)
    {
        List<Vector3Int> result = new List<Vector3Int>();
        List<Vector3Int> neighborPositionsNormal = new List<Vector3Int>() { position + new Vector3Int(1, 0, 0), position + new Vector3Int(0, 1, 0), position + new Vector3Int(-1, 0, 0), position + new Vector3Int(0, -1, 0)};

        List<Vector3Int> neighborPositionsDiagonal = new List<Vector3Int>() {position + new Vector3Int(1, 1, 0), position + new Vector3Int(-1, 1, 0), position + new Vector3Int(1, -1, 0), position + new Vector3Int(-1, -1, 0)};

        foreach (Vector3Int neighborPosition in neighborPositionsNormal)
        {
            if (obstacleTilemap.GetTile(neighborPosition) == null)
            {
                result.Add(neighborPosition);
            }
        }
        foreach (Vector3Int neighborPosition in neighborPositionsDiagonal)
        {
            Vector3Int side1 = new Vector3Int(neighborPosition.x, position.y, 0);
            Vector3Int side2 = new Vector3Int(position.x, neighborPosition.y, 0);
            if (obstacleTilemap.GetTile(neighborPosition) == null && (obstacleTilemap.GetTile(side1) == null || obstacleTilemap.GetTile(side2) == null))
            {
                result.Add(neighborPosition);
            }
        }
        return result;
    }
}
