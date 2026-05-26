// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
namespace Rock.CloudPrint.Service;

/// <summary>
/// An <see cref="ILoggerProvider"/> that captures Rock.CloudPrint log entries
/// into an <see cref="InMemoryLogSink"/> for display in the web UI.
/// ASP.NET Core request/response logs are intentionally excluded to avoid
/// noise from the dashboard's 2-second status polling.
/// </summary>
internal class InMemoryLoggerProvider : ILoggerProvider
{
    private readonly InMemoryLogSink _sink;

    public InMemoryLoggerProvider( InMemoryLogSink sink )
    {
        _sink = sink;
    }

    /// <inheritdoc/>
    public ILogger CreateLogger( string categoryName )
    {
        return new InMemoryLogger( categoryName, _sink );
    }

    /// <inheritdoc/>
    public void Dispose() { }
}

/// <summary>
/// Writes log messages from Rock.CloudPrint categories into the
/// shared <see cref="InMemoryLogSink"/>.
/// </summary>
internal class InMemoryLogger : ILogger
{
    private readonly string _categoryName;
    private readonly InMemoryLogSink _sink;

    public InMemoryLogger( string categoryName, InMemoryLogSink sink )
    {
        _categoryName = categoryName;
        _sink = sink;
    }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>( TState state ) where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled( LogLevel logLevel ) => logLevel >= LogLevel.Information;

    /// <inheritdoc/>
    public void Log<TState>( LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter )
    {
        if ( !IsEnabled( logLevel ) )
        {
            return;
        }

        // Only capture Rock.CloudPrint logs — ASP.NET Core request logs are too
        // noisy and would drown out meaningful proxy events.
        if ( !_categoryName.StartsWith( "Rock.CloudPrint" ) )
        {
            return;
        }

        var message = formatter( state, exception );

        if ( exception != null )
        {
            message += Environment.NewLine + exception.Message;
        }

        _sink.Add( new LogEntry
        {
            Timestamp = DateTimeOffset.Now,
            Level = logLevel.ToString(),
            Category = _categoryName.Replace( "Rock.CloudPrint.Service.", string.Empty ),
            Message = message
        } );
    }
}
