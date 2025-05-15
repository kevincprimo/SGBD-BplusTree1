using System.Collections.Generic;

public class InternalNode : BPlusNode
{
    public List<int> Keys { get; set; }
    public List<int> ChildrenNodeIds { get; set; } // IDs dos filhos no disco

    public InternalNode()
    {
        IsLeaf = false;
        Keys = new List<int>();
        ChildrenNodeIds = new List<int>();
    }
}