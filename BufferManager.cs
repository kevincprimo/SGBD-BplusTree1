using System;
using System.Collections.Generic;

public class BufferManager
{
    private Dictionary<int, BPlusNode> nodeStorage = new Dictionary<int, BPlusNode>();
    private int nextNodeId = 0;

    public int SaveNode(BPlusNode node)
    {
        if (node.NodeId == -1)
        {
            node.NodeId = nextNodeId++;
        }

        nodeStorage[node.NodeId] = node;
        return node.NodeId;
    }

    public BPlusNode LoadNode(int nodeId)
    {
        if (nodeStorage.ContainsKey(nodeId))
        {
            return nodeStorage[nodeId];
        }

        throw new Exception($"Nó com ID {nodeId} não encontrado.");
    }
}
