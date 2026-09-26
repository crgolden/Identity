namespace Identity.Tests.E2E.Synthetic;

using static Identity.Tests.E2E.Synthetic.WalkerRngConstants;

internal sealed class WalkerRng
{
    private uint _state;

    public WalkerRng(uint seed) => _state = seed;

    public double Next()
    {
        unchecked
        {
            _state += Increment;
            var t = _state;
            t = (t ^ (t >> FirstShift)) * (t | FirstMultiplicandOrBits);
            t ^= t + ((t ^ (t >> SecondShift)) * (t | SecondMultiplicandOrBits));
            return (t ^ (t >> FinalShift)) / UInt32Range;
        }
    }

    public int Int(int maxExclusive) => (int)(Next() * maxExclusive);

    public T Pick<T>(IReadOnlyList<T> items) =>
        items.Count == 0
            ? throw new InvalidOperationException("Cannot pick from an empty list.")
            : items[Int(items.Count)];
}
