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
        var root = new LeafNode();
        root.Keys.Add(key);
        root.RecordPointers.Add(recordPointer);
        bufferManager.SaveNode(root);
        rootNodeId = root.NodeId;
        return;
    }

    BPlusNode rootNode = bufferManager.LoadNode(rootNodeId);
    var (promotedKey, newNodeId) = InsertRecursive(rootNode, key, recordPointer);

    if (promotedKey.HasValue && newNodeId.HasValue)
    {
        // Criar nova raiz
        var newRoot = new InternalNode();
        newRoot.Keys.Add(promotedKey.Value);
        newRoot.ChildrenNodeIds.Add(rootNodeId);
        newRoot.ChildrenNodeIds.Add(newNodeId.Value);
        bufferManager.SaveNode(newRoot);
        rootNodeId = newRoot.NodeId;
    }
}


    private (int? promotedKey, int? newNodeId) InsertRecursive(BPlusNode node, int key, int recordPointer)
{
    if (node is LeafNode leaf)
    {
            if (leaf.Keys.Count >= order - 1)
            {
                var (promotedKey, newNodeId) = SplitLeaf(leaf, key, recordPointer);
                PromoteKey(leaf, promotedKey, newNodeId);
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

        var (promotedKey, newChildId) = InsertRecursive(childNode, key, recordPointer);

        if (promotedKey.HasValue && newChildId.HasValue)
        {
            if (internalNode.Keys.Count >= order - 1)
            {
                // Split do nó interno
                var (newInternalKey, newInternalNodeId) = SplitInternal(internalNode, promotedKey.Value, newChildId.Value);
                return (newInternalKey, newInternalNodeId);
            }
            else
            {
                // Inserir normalmente no nó interno
                internalNode.Keys.Insert(i, promotedKey.Value);
                internalNode.ChildrenNodeIds.Insert(i + 1, newChildId.Value);
                bufferManager.SaveNode(internalNode);
            }
        }

        return (null, null); // nada foi promovido
    }

    return (null, null);
}

    private (int promotedKey, int newNodeId) SplitLeaf(LeafNode leaf, int key, int recordPointer)
    {
        // Inserir a nova chave e ponteiro
        int i = 0;
        while (i < leaf.Keys.Count && key > leaf.Keys[i]) i++;
        leaf.Keys.Insert(i, key);
        leaf.RecordPointers.Insert(i, recordPointer);

        // Criar nova folha e dividir
        var newLeaf = new LeafNode();
        int mid = leaf.Keys.Count / 2;

        newLeaf.Keys.AddRange(leaf.Keys.GetRange(mid, leaf.Keys.Count - mid));
        newLeaf.RecordPointers.AddRange(leaf.RecordPointers.GetRange(mid, leaf.RecordPointers.Count - mid));

        leaf.Keys.RemoveRange(mid, leaf.Keys.Count - mid);
        leaf.RecordPointers.RemoveRange(mid, leaf.RecordPointers.Count - mid);

        // Salvar nova folha primeiro para obter o ID real
        int newLeafId = bufferManager.SaveNode(newLeaf);
        newLeaf.NodeId = newLeafId;

        // Atualizar ponteiro de próxima folha
        newLeaf.NextLeafNodeId = leaf.NextLeafNodeId;
        leaf.NextLeafNodeId = newLeafId;

        // Salvar ambos novamente
        bufferManager.SaveNode(newLeaf);
        bufferManager.SaveNode(leaf);

        // Retornar a menor chave da nova folha e o ID dela
        return (newLeaf.Keys[0], newLeafId);
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
                SplitInternal(internalParent, key, rightChildId); // <- corrigido aqui
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

    private (int promotedKey, int newInternalNodeId) SplitInternal(InternalNode node, int keyToInsert, int newChildId)
    {
        // Etapa 1: Inserir a nova chave e filho nas listas temporárias
        List<int> tempKeys = new List<int>(node.Keys);
        List<int> tempChildren = new List<int>(node.ChildrenNodeIds);

        int insertIndex = 0;
        while (insertIndex < tempKeys.Count && keyToInsert > tempKeys[insertIndex])
            insertIndex++;

        tempKeys.Insert(insertIndex, keyToInsert);
        tempChildren.Insert(insertIndex + 1, newChildId);

        // Etapa 2: Definir ponto de split
        int midIndex = tempKeys.Count / 2;
        int promotedKey = tempKeys[midIndex];

        // Etapa 3: Criar novo nó interno com metade da direita
        InternalNode newInternal = new InternalNode();
        newInternal.Keys.AddRange(tempKeys.GetRange(midIndex + 1, tempKeys.Count - (midIndex + 1)));
        newInternal.ChildrenNodeIds.AddRange(tempChildren.GetRange(midIndex + 1, tempChildren.Count - (midIndex + 1)));

        // Etapa 4: Atualizar o nó original com metade da esquerda
        node.Keys = tempKeys.GetRange(0, midIndex);
        node.ChildrenNodeIds = tempChildren.GetRange(0, midIndex + 1);

        // Etapa 5: Salvar os nós no bufferManager
        bufferManager.SaveNode(node);
        int newNodeId = bufferManager.SaveNode(newInternal);
        newInternal.NodeId = newNodeId; // <-- atribui corretamente

        return (promotedKey, newNodeId);
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

    public int GetHeight(){
    int height = 0;
    int currentId = rootNodeId;

    while (currentId != -1){
        height++;
        var node = bufferManager.LoadNode(currentId);

        if (node is InternalNode internalNode)
        {
            // Sempre desce para o primeiro filho
            currentId = internalNode.ChildrenNodeIds[0];
        }
        else
        {
            // É uma folha, então terminamos
            break;
        }
    }

    return height;
    }

}
