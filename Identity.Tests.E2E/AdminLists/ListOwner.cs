namespace Identity.Tests.E2E.AdminLists;

public sealed class ListOwner
{
    private ListOwnerKind? _kind;
    private int? _id;

    public ListOwnerKind Kind => _kind ?? throw new InvalidOperationException("No record owns a list in this scenario.");

    public int Id => _id ?? throw new InvalidOperationException("No record owns a list in this scenario.");

    public void Set(ListOwnerKind kind, int id)
    {
        _kind = kind;
        _id = id;
    }
}
