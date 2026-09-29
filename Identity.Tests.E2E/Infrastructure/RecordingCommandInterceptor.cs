namespace Identity.Tests.E2E.Infrastructure;

using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class RecordingCommandInterceptor : DbCommandInterceptor
{
    private readonly ConcurrentQueue<IReadOnlyList<object?>> _parameterValues = new();

    public IReadOnlyList<IReadOnlyList<object?>> CommandsCarrying(string parameterValue) =>
        [.. _parameterValues.Where(values => values.Contains(parameterValue))];

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Record(command);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void Record(DbCommand command) =>
        _parameterValues.Enqueue([.. command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value)]);
}
