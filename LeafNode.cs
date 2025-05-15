using System.Collections.Generic;

public class LeafNode : BPlusNode
{
    public new List<int> Keys { get; set; } = new List<int>();
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