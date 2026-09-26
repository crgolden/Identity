namespace Identity.Tests.E2E.Infrastructure;

using System.Reflection;
using Xunit;
using Xunit.v3;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PlaywrightArtifactFinalizerAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        ArgumentNullException.ThrowIfNull(test);
        PlaywrightArtifactRecorder.Clear(test.UniqueID);
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        ArgumentNullException.ThrowIfNull(test);
        var state = TestContext.Current.TestState;
        PlaywrightArtifactRecorder.Finalize(test.UniqueID, state);
    }
}
