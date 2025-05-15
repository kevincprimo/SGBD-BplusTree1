using System.Collections.Generic;

public class LeafNode : BPlusNode
{
    public List<int> Keys { get; set; }
    public List<int> RecordPointers { get; set; }
    public int NextLeafNodeId { get; set; }

    public LeafNode()
    {
        IsLeaf = true;
        Keys = new List<int>();
        RecordPointers = new List<int>();
        NextLeafNodeId = -1;
    }
}