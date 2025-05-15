public abstract class BPlusNode
{
    public bool IsLeaf { get; protected set; }
    public int NodeId { get; set; } // Representa o "ID" da linha no arquivo de índice (como um ponteiro)
}