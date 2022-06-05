using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AVL
{
    public class Node
    {
        public Node previousNode;
        public Vector3Int tilePosition;
        public float distanceToFinish;
        public float distancePath;

        public Node left;
        public Node right;
        public Node(float distancePath, float distanceToFinish, Vector3Int tilePosition, Node previousNode)
        {
            this.tilePosition = tilePosition;
            this.previousNode = previousNode;

            this.distancePath = distancePath;
            this.distanceToFinish = distanceToFinish;
        }

        public float getData()
        {
            return this.distancePath + this.distanceToFinish;
        }

        public override int GetHashCode()
        {
            return this.tilePosition.x * 100000 + tilePosition.y;
        }
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
        else if (n.getData() < current.getData())
        {
            current.left = RecursiveInsert(current.left, n);
            current = balance_tree(current);
        }
        else if (n.getData() >= current.getData())
        {
            current.right = RecursiveInsert(current.right, n);
            current = balance_tree(current);
        }
        return current;
    }

    /// <summary>
    /// Removes and return the minimum value in the tree
    /// </summary>
    /// <returns></returns>
    public Node PopMinValue()
    {
        Node currentNode = this.root;
        while (currentNode.left != null)
        {
            currentNode = currentNode.left;
        }
        Node result = new Node(currentNode.distancePath, currentNode.distanceToFinish, currentNode.tilePosition, currentNode.previousNode);
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
            if (node.getData() < current.getData())
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
            else if (node.getData() > current.getData())
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
            else if (node.getData() == current.getData() && node.tilePosition != current.tilePosition)
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
                    throw new Exception("Node not in tree");
                }
            }
            //if target is found
            else if (node.getData() == current.getData() && node.tilePosition == current.tilePosition)
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