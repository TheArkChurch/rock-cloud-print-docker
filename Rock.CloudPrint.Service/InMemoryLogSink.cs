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
/// A fixed-size circular buffer that stores recent log entries for display
/// in the web UI.
/// </summary>
internal class InMemoryLogSink
{
    private readonly Queue<LogEntry> _entries = new();
    private readonly object _lock = new();
    private const int MaxEntries = 250;

    /// <summary>
    /// Adds a new log entry, evicting the oldest entry when at capacity.
    /// </summary>
    public void Add( LogEntry entry )
    {
        lock ( _lock )
        {
            _entries.Enqueue( entry );

            if ( _entries.Count > MaxEntries )
            {
                _entries.Dequeue();
            }
        }
    }

    /// <summary>
    /// Returns a snapshot of all buffered entries in chronological order.
    /// </summary>
    public IReadOnlyList<LogEntry> GetEntries()
    {
        lock ( _lock )
        {
            return _entries.ToList();
        }
    }
}

/// <summary>
/// Represents a single captured log entry.
/// </summary>
internal class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }

    public string Level { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
