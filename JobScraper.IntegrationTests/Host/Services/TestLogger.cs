using System.Text;
using Microsoft.Extensions.Logging;

namespace JobScraper.IntegrationTests.Host.Services;

internal sealed class TestLoggerProvider : ILoggerProvider
{
    /// <summary> Test output used in unit test </summary>
    public ITestOutputHelper? TestOutput { get; set; }

    public void Dispose()
    { }

    public ILogger CreateLogger(string categoryName) => new TestLogger(GetOutput);

    /// <summary> Method called in TestLogger for late getting test output </summary>
    private ITestOutputHelper? GetOutput() => TestOutput;
}

internal sealed class TestLogger(Func<ITestOutputHelper?>? testOutputGetter) : ILogger
{
    private readonly LoggerExternalScopeProvider scopeProvider = new();

    /// <summary> Get delegate to override test output after creating logger instances </summary>
    public Func<ITestOutputHelper?>? TestOutputGetter { get; set; } = testOutputGetter;

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => scopeProvider.Push(state);

    public void Log<TState>(LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var testOutputHelper = TestOutputGetter?.Invoke() ?? TestContext.Current.TestOutputHelper;
        if (testOutputHelper is null)
            return;

        var sb = new StringBuilder();

        sb.Append(GetLogLevelString(logLevel)).Append(' ');

        sb.Append(formatter(state, exception));

        if (exception is not null)
            sb.Append('\n').Append(exception);

        // Append scopes
        scopeProvider.ForEachScope((scope, st) =>
            {
                st.Append("\n => ");
                st.Append(scope);
            },
            sb);

        try
        {
            testOutputHelper.WriteLine(sb.ToString());
        }
        catch
        {
            // This can happen when the test is not active
        }
    }

    private static string GetLogLevelString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace       => "trce",
        LogLevel.Debug       => "dbug",
        LogLevel.Information => "info",
        LogLevel.Warning     => "warn",
        LogLevel.Error       => "fail",
        LogLevel.Critical    => "crit",
        _                    => throw new ArgumentOutOfRangeException(nameof(logLevel)),
    };
}
