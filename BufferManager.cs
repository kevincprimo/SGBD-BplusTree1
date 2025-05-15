using System;
using System.Collections.Generic;

public class BufferManager
{
    private Dictionary<int, BPlusNode> nodeStorage = new Dictionary<int, BPlusNode>();
    private int nextNodeId = 0;
    public int GenerateNewNodeId()
{
    return nextNodeId++;
}

    public int SaveNode(BPlusNode node)
    {
        if (node.NodeId == -1)
        {
            node.NodeId = GenerateNewNodeId();
        }
        nodeStorage[node.NodeId] = node;
        return node.NodeId;
    }


    public BPlusNode LoadNode(int nodeId){
    if (nodeId == -1)
        throw new Exception("Tentativa de carregar nó inválido: ID = -1");

    if (nodeStorage.ContainsKey(nodeId))
    {
        return nodeStorage[nodeId];
    }

    throw new Exception($"Nó com ID {nodeId} não encontrado.");
    }

}
