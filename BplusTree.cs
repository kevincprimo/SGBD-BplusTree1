using System;
using System.Collections.Generic;

public class BPlusTree
{
    private readonly int order; // Ordem da árvore B+ (máximo de filhos por nó interno)
    private readonly BufferManager bufferManager;
    private int rootNodeId;

    public BPlusTree(int order, BufferManager bufferManager)
    {
        this.order = order;
        this.bufferManager = bufferManager;
        this.rootNodeId = -1; // Árvore inicialmente vazia
    }

    public void Insert(int key, int recordPointer)
    {
        if (rootNodeId == -1)
        {
            LeafNode root = new LeafNode();
            root.Keys.Add(key);
            root.RecordPointers.Add(recordPointer);
            rootNodeId = bufferManager.SaveNode(root);
            root.NodeId = rootNodeId;
        }
        else
        {
            BPlusNode rootNode = bufferManager.LoadNode(rootNodeId);
            InsertRecursive(rootNode, key, recordPointer);
        }
    }

    private void InsertRecursive(BPlusNode node, int key, int recordPointer)
    {
        if (node is LeafNode leaf)
        {
            if (leaf.Keys.Count >= order - 1)
            {
                SplitLeaf(leaf, key, recordPointer);
            }
            else
            {
                int i = 0;
                while (i < leaf.Keys.Count && key > leaf.Keys[i]) i++;
                leaf.Keys.Insert(i, key);
                leaf.RecordPointers.Insert(i, recordPointer);
                bufferManager.SaveNode(leaf);
            }
        }
        else if (node is InternalNode internalNode)
        {
            int i = 0;
            while (i < internalNode.Keys.Count && key > internalNode.Keys[i]) i++;
            int childId = internalNode.ChildrenNodeIds[i];
            BPlusNode childNode = bufferManager.LoadNode(childId);
            InsertRecursive(childNode, key, recordPointer);
        }
    }

    private void SplitLeaf(LeafNode leaf, int key, int recordPointer)
    {
        // Inserir chave antes de dividir
        int i = 0;
        while (i < leaf.Keys.Count && key > leaf.Keys[i]) i++;
        leaf.Keys.Insert(i, key);
        leaf.RecordPointers.Insert(i, recordPointer);

        // Criar nova folha com metade superior
        int mid = leaf.Keys.Count / 2;
        LeafNode newLeaf = new LeafNode();
        newLeaf.Keys.AddRange(leaf.Keys.GetRange(mid, leaf.Keys.Count - mid));
        newLeaf.RecordPointers.AddRange(leaf.RecordPointers.GetRange(mid, leaf.RecordPointers.Count - mid));

        // Remover da folha original
        leaf.Keys.RemoveRange(mid, leaf.Keys.Count - mid);
        leaf.RecordPointers.RemoveRange(mid, leaf.RecordPointers.Count - mid);

        
        // Encadeamento
        newLeaf.NextLeafNodeId = leaf.NextLeafNodeId;
        int newLeafId = bufferManager.SaveNode(newLeaf);
        newLeaf.NodeId = newLeafId;
        leaf.NextLeafNodeId = newLeafId;

        bufferManager.SaveNode(leaf);
        bufferManager.SaveNode(newLeaf);

        // Promover menor chave da nova folha
        PromoteKey(leaf, newLeaf.Keys[0], newLeafId);
    }

    private void PromoteKey(BPlusNode leftChild, int key, int rightChildId)
    {
        int parentId = FindParent(rootNodeId, leftChild.NodeId);

        if (parentId == -1)
        {
            // Nova raiz
            InternalNode newRoot = new InternalNode();
            newRoot.Keys.Add(key);
            newRoot.ChildrenNodeIds.Add(leftChild.NodeId);
            newRoot.ChildrenNodeIds.Add(rightChildId);

            int newRootId = bufferManager.SaveNode(newRoot);
            newRoot.NodeId = newRootId;
            rootNodeId = newRootId;
            return;
        }

        // Atualizar pai
        BPlusNode parentNode = bufferManager.LoadNode(parentId);
        if (parentNode is InternalNode internalParent)
        {
            int i = 0;
            while (i < internalParent.Keys.Count && key > internalParent.Keys[i]) i++;
            internalParent.Keys.Insert(i, key);
            internalParent.ChildrenNodeIds.Insert(i + 1, rightChildId);

            if (internalParent.Keys.Count >= order)
            {
                SplitInternal(internalParent);
            }
            else
            {
                bufferManager.SaveNode(internalParent);
            }
        }
    }

    private int FindParent(int currentNodeId, int childId)
    {
        BPlusNode node = bufferManager.LoadNode(currentNodeId);
        if (node is InternalNode internalNode)
        {
            foreach (int child in internalNode.ChildrenNodeIds)
            {
                if (child == childId)
                    return currentNodeId;

                BPlusNode childNode = bufferManager.LoadNode(child);
                if (childNode is InternalNode)
                {
                    int found = FindParent(child, childId);
                    if (found != -1)
                        return found;
                }
            }
        }
        return -1;
    }

    private void SplitInternal(InternalNode node)
    {
        int midIndex = node.Keys.Count / 2;
        int promoteKey = node.Keys[midIndex];

        InternalNode newInternal = new InternalNode();
        newInternal.Keys.AddRange(node.Keys.GetRange(midIndex + 1, node.Keys.Count - midIndex - 1));
        newInternal.ChildrenNodeIds.AddRange(node.ChildrenNodeIds.GetRange(midIndex + 1, node.ChildrenNodeIds.Count - midIndex - 1));

        node.Keys.RemoveRange(midIndex, node.Keys.Count - midIndex);
        node.ChildrenNodeIds.RemoveRange(midIndex + 1, node.ChildrenNodeIds.Count - midIndex - 1);

        int newInternalId = bufferManager.SaveNode(newInternal);
        newInternal.NodeId = newInternalId;
        bufferManager.SaveNode(node);

        PromoteKey(node, promoteKey, newInternalId);
    }

    public List<int> Search(int key)
    {
        BPlusNode current = bufferManager.LoadNode(rootNodeId);

        while (current is InternalNode internalNode)
        {
            int i = 0;
            while (i < internalNode.Keys.Count && key > internalNode.Keys[i]) i++;
            int childId = internalNode.ChildrenNodeIds[i];
            current = bufferManager.LoadNode(childId);
        }

        if (current is LeafNode leaf)
        {
            List<int> results = new List<int>();
            for (int i = 0; i < leaf.Keys.Count; i++)
            {
                if (leaf.Keys[i] == key)
                    results.Add(leaf.RecordPointers[i]);
            }
            return results;
        }

        return new List<int>();
    }

    public int GetHeight()
    {
        int height = 0;
        int nodeId = rootNodeId;

        while (true)
        {
            height++;
            BPlusNode node = bufferManager.LoadNode(nodeId);
            if (node is InternalNode internalNode)
                nodeId = internalNode.ChildrenNodeIds[0];
            else
                break;
        }

        return height;
    }
}
