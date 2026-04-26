using System.Runtime.CompilerServices;
using VerifyTests;

namespace WorkflowEditor.Tests.Client.TestKit;

internal static class VerifyConfig
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings.UseStrictJson();
        VerifierSettings.DontScrubDateTimes();
    }
}
