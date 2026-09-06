namespace Identity.Tests.E2E.Synthetic;

internal sealed class WalkerRng
{
    private const uint Increment = 0x6d2b79f5;
    private const double UInt32Range = 4294967296d;

    private uint _state;

    public WalkerRng(uint seed) => _state = seed;

    public double Next()
    {
        unchecked
        {
            _state += Increment;
            var t = _state;
            t = (t ^ (t >> 15)) * (t | 1u);
            t ^= t + ((t ^ (t >> 7)) * (t | 61u));
            return (t ^ (t >> 14)) / UInt32Range;
        }
    }

    public int Int(int maxExclusive) => (int)(Next() * maxExclusive);

    public T Pick<T>(IReadOnlyList<T> items) =>
        items.Count == 0
            ? throw new InvalidOperationException("Cannot pick from an empty list.")
            : items[Int(items.Count)];
}