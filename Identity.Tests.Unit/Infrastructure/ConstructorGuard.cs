namespace Identity.Tests.Unit.Infrastructure;

using System.Reflection;

internal static class ConstructorGuard
{
    internal static (Exception? Thrown, string? ExpectedParamName) Construct<T>(object?[] arguments)
    {
        if (arguments.Count(argument => argument is null) != 1)
        {
            throw new InvalidOperationException("Exactly one constructor argument must be null.");
        }

        var constructor = typeof(T).GetConstructors().Single(candidate => candidate.GetParameters().Length == arguments.Length);
        var expectedParamName = constructor.GetParameters()[Array.IndexOf(arguments, null)].Name;
        try
        {
            _ = constructor.Invoke(arguments);
            return (null, expectedParamName);
        }
        catch (TargetInvocationException exception)
        {
            return (exception.InnerException, expectedParamName);
        }
    }
}
