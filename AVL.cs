using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
public class AVL
{
    public abstract class Node : ICloneable
    {
        public Node left;
        public Node right;
        public Vector3Int tilePosition;

        public Node(Vector3Int tilePosition)
        {
            this.tilePosition = tilePosition;
        }
        public abstract object Clone();

        public abstract float getCost();

        public override abstract int GetHashCode();
    }

    Node root;
    public AVL()
    {
    }

    /// <summary>
    /// Adds a new Node to the tree
    /// </summary>
    /// <param name="newItem"></param>
    public void Add(Node newItem)
    {
        if (root == null)
        {
            root = newItem;
        }
        else
        {
            root = RecursiveInsert(root, newItem);
        }
    }
    private Node RecursiveInsert(Node current, Node n)
    {
        if (current == null)
        {
            current = n;
            return current;
        }
        else if (n.getCost() < current.getCost())
        {
            current.left = RecursiveInsert(current.left, n);
            current = balance_tree(current);
        }
        else if (n.getCost() >= current.getCost())
        {
            current.right = RecursiveInsert(current.right, n);
            current = balance_tree(current);
        }
        return current;
    }

    public bool isEmpty()
    {
        return this.root == null;
    }

    /// <summary>
    /// Removes and return the minimum value in the tree
    /// </summary>
    /// <returns></returns>
    public Node PopMinValue()
    {
        if (this.root == null) return null;
        Node currentNode = this.root;
        while (currentNode.left != null)
        {
            currentNode = currentNode.left;
        }
        Node result = (Node)currentNode.Clone();
        Delete(currentNode);
        result.left = null;
        result.right = null;
        return result;
    }

    /// <summary>
    /// Deletes a node from the tree
    /// </summary>
    /// <param name="node"></param>
    public void Delete(Node node)
    {//and here
        root = Delete(root, node, null);
    }
    private Node Delete(Node current, Node node, Node parent)
    {
        if (current == null)
        { return null; }
        else
        {
            //left subtree
            if (node.getCost() < current.getCost())
            {
                current.left = Delete(current.left, node, current);
                if (balance_factor(current) == -2)//here
                {
                    if (balance_factor(current.right) <= 0)
                    {
                        current = RotateRR(current);
                    }
                    else
                    {
                        current = RotateRL(current);
                    }
                }
            }
            //right subtree
            else if (node.getCost() > current.getCost())
            {
                current.right = Delete(current.right, node, current);
                if (balance_factor(current) == 2)
                {
                    if (balance_factor(current.left) >= 0)
                    {
                        current = RotateLL(current);
                    }
                    else
                    {
                        current = RotateLR(current);
                    }
                }
            }
            else if (node.getCost() == current.getCost() && node.tilePosition != current.tilePosition)
            {
                if (Contains(current.right, node))
                {
                    current.right = Delete(current.right, node, current);
                    if (balance_factor(current) == 2)
                    {
                        if (balance_factor(current.left) >= 0)
                        {
                            current = RotateLL(current);
                        }
                        else
                        {
                            current = RotateLR(current);
                        }
                    }
                }
                else if (Contains(current.left, node))
                {
                    current.left = Delete(current.left, node, current);
                    if (balance_factor(current) == -2)//here
                    {
                        if (balance_factor(current.right) <= 0)
                        {
                            current = RotateRR(current);
                        }
                        else
                        {
                            current = RotateRL(current);
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.Log(node.tilePosition);
                    //displayTree();
                    //printAllHash();
                    //checkIfSameHashcodeInTree(node);
                    throw new Exception("Node not in tree");
                }
            }
            //if target is found
            else if (node.getCost() == current.getCost() && node.tilePosition == current.tilePosition)
            {
                if (current.right != null)
                {
                    // Get replace for current
                    Node replace = current.right;
                    while (replace.left != null)
                    {
                        replace = replace.left;
                    }

                    // Delete replace from lower tree and update left and right
                    replace.right = Delete(current.right, replace, current);
                    replace.left = current.left;

                    // Assigning current as replace so parent will be updated
                    current = replace;
                    if (balance_factor(current) == 2)//rebalancing
                    {
                        if (balance_factor(current.left) >= 0)
                        {
                            current = RotateLL(current);
                        }
                        else { current = RotateLR(current); }
                    }
                }
                else
                {   //if current.left != null
                    return current.left;
                }
            }
        }
        return current;
    }
    public bool contains(Vector3Int tilePosition)
    {
        return this.contains(this.root, tilePosition);
    }
    private bool contains(Node root, Vector3Int tilePosition)
    {
        if (root == null) return false;
        if(root.tilePosition != tilePosition)
        {
            bool left = contains(root.left, tilePosition);
            bool right = contains(root.right, tilePosition);
            if(left || right)
            {
                return true;
            }
            return false;
        }
        return true;
    }

    private void checkIfSameHashcodeInTree(Node search)
    {
        List<List<Node>> treeInfo = getTreeInfo(root);

        foreach (List<Node> nodes in treeInfo)
        {
            foreach (Node node in nodes)
            {
                if(node.GetHashCode() == search.GetHashCode())
                {
                    UnityEngine.Debug.Log("Node with same hascode found");
                    UnityEngine.Debug.Log("Original node location: " + search.tilePosition);
                    UnityEngine.Debug.Log("Original node hashcode: " + search.GetHashCode());
                    UnityEngine.Debug.Log("Other node location: " + node.tilePosition);
                    UnityEngine.Debug.Log("Other node hashcode: " + node.GetHashCode());
                    return;
                }
            }
        }
    }
    private void printAllHash()
    {
        List<List<Node>> treeInfo = getTreeInfo(root);

        foreach (List<Node> nodes in treeInfo)
        {
            string result = "";
            foreach (Node node in nodes)
            {
                result += node.GetHashCode();
            }
            UnityEngine.Debug.Log(result);
        }
    }

    private void displayTree()
    {
        List<List<Node>> treeInfo = getTreeInfo(root);

        foreach (List<Node> nodes in treeInfo)
        {
            string result = "";
            foreach (Node node in nodes)
            {
                result += node.tilePosition.ToString();
            }
            UnityEngine.Debug.Log(result);
        }
    }

    private List<List<Node>> getTreeInfo(Node root)
    {
        if(root.left == null && root.right == null)
        {
            List<Node> thisNode = new List<Node>();
            thisNode.Add(root);

            List<List<Node>> result = new List<List<Node>>();
            result.Add(thisNode);

            return result;
        }
        else if (root.left != null && root.right != null)
        {
            List<Node> thisNode = new List<Node>();
            thisNode.Add(root);

            List<List<Node>> childResultLeft = getTreeInfo(root.left);
            List<List<Node>> childResultRight = getTreeInfo(root.right);

            List<List<Node>> result = new List<List<Node>>();
            result.Add(thisNode);
            for (int i = 0; i < Math.Max(childResultLeft.Count, childResultRight.Count); i++)
            {
                List<Node> nodesLeft = new List<Node>();
                if(i < childResultLeft.Count)
                {
                    nodesLeft = childResultLeft[i];
                }

                List<Node> nodesRight = new List<Node>();
                if (i < childResultRight.Count)
                {
                    nodesRight = childResultRight[i];
                }

                List<Node> nodes = new List<Node>();
                nodes.AddRange(nodesLeft);
                nodes.AddRange(nodesRight);

                result.Add(nodes);
            }

            return result;
        }
        else if (root.left != null)
        {
            List<Node> thisNode = new List<Node>();
            thisNode.Add(root);

            List<List<Node>> childResult = getTreeInfo(root.left);

            List<List<Node>> result = new List<List<Node>>();
            result.Add(thisNode);
            result.AddRange(childResult);

            return result;
        }
        else if (root.right != null)
        {
            List<Node> thisNode = new List<Node>();
            thisNode.Add(root);

            List<List<Node>> childResult = getTreeInfo(root.right);

            List<List<Node>> result = new List<List<Node>>();
            result.Add(thisNode);
            foreach (List<Node> nodes in childResult)
            {
                result.Add(nodes);
            }

            return result;
        }
        else
        {
            UnityEngine.Debug.Log("error ocurred");
            return new List<List<Node>>();
        }
    }

    private bool Contains(Node root, Node find)
    {
        if(root == find) return true;
        if (root == null) return false;
        else
        {
            bool left = Contains(root.left,find);
            if (left) return left;
            return Contains(root.right, find);
        }
    }
    private int max(int l, int r)
    {
        return l > r ? l : r;
    }
    private int getHeight(Node current)
    {
        int height = 0;
        if (current != null)
        {
            int l = getHeight(current.left);
            int r = getHeight(current.right);
            int m = max(l, r);
            height = m + 1;
        }
        return height;
    }
    private Node balance_tree(Node current)
    {
        int b_factor = balance_factor(current);
        if (b_factor > 1)
        {
            if (balance_factor(current.left) > 0)
            {
                current = RotateLL(current);
            }
            else
            {
                current = RotateLR(current);
            }
        }
        else if (b_factor < -1)
        {
            if (balance_factor(current.right) > 0)
            {
                current = RotateRL(current);
            }
            else
            {
                current = RotateRR(current);
            }
        }
        return current;
    }
    private int balance_factor(Node current)
    {
        int l = getHeight(current.left);
        int r = getHeight(current.right);
        int b_factor = l - r;
        return b_factor;
    }
    private Node RotateRR(Node parent)
    {
        Node pivot = parent.right;
        parent.right = pivot.left;
        pivot.left = parent;
        return pivot;
    }
    private Node RotateLL(Node parent)
    {
        Node pivot = parent.left;
        parent.left = pivot.right;
        pivot.right = parent;
        return pivot;
    }
    private Node RotateLR(Node parent)
    {
        Node pivot = parent.left;
        parent.left = RotateRR(pivot);
        return RotateLL(parent);
    }
    private Node RotateRL(Node parent)
    {
        Node pivot = parent.right;
        parent.right = RotateLL(pivot);
        return RotateRR(parent);
    }
}
