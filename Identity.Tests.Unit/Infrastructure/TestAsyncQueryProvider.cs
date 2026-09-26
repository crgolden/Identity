namespace Identity.Tests.Unit.Infrastructure;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = ((IQueryProvider)this).Execute(expression);
        var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))
            ?? throw new InvalidOperationException($"{nameof(Task)}.{nameof(Task.FromResult)} was not found via reflection.");
        var invocationResult = fromResultMethod
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, [executionResult])
            ?? throw new InvalidOperationException($"{nameof(Task)}.{nameof(Task.FromResult)} unexpectedly returned null.");

        return (TResult)invocationResult;
    }
}
