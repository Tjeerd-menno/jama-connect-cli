using System.CommandLine;
using JamaConnect.Infrastructure.JamaConnect;

namespace JamaConnect.Cli.Commands;

internal static class Execution
{
    public static async Task RunAsync(CliRuntime runtime, ParseResult parseResult, Func<CliContext, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        var context = runtime.Create(parseResult);
        try
        {
            await action(context, cancellationToken).ConfigureAwait(false);
        }
        catch (JamaApiException ex)
        {
            CliOutput.WriteError(context, "jama.api_error", ex.Message, ex.Details, retryable: (int)ex.StatusCode is 429 or 502 or 503 or 504);
            Environment.ExitCode = (int)ex.StatusCode == 404 ? 3 : ((int)ex.StatusCode is 429 or 502 or 503 or 504 ? 6 : 5);
        }
        catch (InvalidOperationException ex)
        {
            CliOutput.WriteError(context, "validation.error", ex.Message);
            Environment.ExitCode = 4;
        }
        catch (IOException ex)
        {
            CliOutput.WriteError(context, "io.error", ex.Message);
            Environment.ExitCode = 8;
        }
    }
}
