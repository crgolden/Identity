namespace Identity.Tests.Unit.Infrastructure;

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;

public static class MockDbSetHelper
{
    public static Mock<DbSet<T>> BuildMockDbSet<T>(IEnumerable<T> data)
        where T : class
    {
        var queryable = new TestAsyncEnumerable<T>(data);
        IQueryable<T> q = queryable;
        var mockSet = new Mock<DbSet<T>>(MockBehavior.Strict);

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(q.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(q.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(q.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => q.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(q.GetEnumerator()));

        return mockSet;
    }

    public static EntityEntry<T> DetachedEntry<T>()
        where T : class =>
        (EntityEntry<T>)RuntimeHelpers.GetUninitializedObject(typeof(EntityEntry<T>));
}
