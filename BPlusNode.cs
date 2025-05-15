public abstract class BPlusNode
{
    public bool IsLeaf { get; protected set; }
    public int NodeId { get; set; } = -1;
    public List<int> Keys { get; set; } = new List<int>();
}