namespace SimpleGameDev
{
public class OverlayGroup
{
    public string Name { get; }
    public int Order { get; }

    public OverlayGroup(string name, int order)
    {
        Name = name;
        Order = order;
    }
}

}
