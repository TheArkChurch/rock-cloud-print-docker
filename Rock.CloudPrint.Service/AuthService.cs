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
using System.Security.Cryptography;

using Microsoft.Extensions.Options;

namespace Rock.CloudPrint.Service;

/// <summary>
/// Manages bearer tokens for the web UI authentication layer.
/// Tokens live in memory only — they are invalidated on container restart
/// or whenever the PIN/password is changed via <see cref="RevokeAll"/>.
/// </summary>
internal class AuthService
{
    private readonly HashSet<string> _validTokens = new();
    private readonly object _lock = new();

    /// <summary>
    /// Returns <c>true</c> if a PIN or password has been configured via
    /// either the settings file or an environment variable.
    /// </summary>
    public bool IsPasswordConfigured( IOptionsMonitor<CloudPrintOptions> options ) =>
        !string.IsNullOrWhiteSpace( options.CurrentValue.Password );

    /// <summary>
    /// Returns <c>true</c> if <paramref name="password"/> matches the
    /// currently configured value. Always returns <c>false</c> when no
    /// password is configured.
    /// </summary>
    public bool ValidatePassword( string password, IOptionsMonitor<CloudPrintOptions> options ) =>
        !string.IsNullOrWhiteSpace( options.CurrentValue.Password ) &&
        password == options.CurrentValue.Password;

    /// <summary>
    /// Generates a cryptographically random 64-character hex bearer token,
    /// registers it as valid, and returns it.
    /// </summary>
    public string IssueToken()
    {
        var token = Convert.ToHexString( RandomNumberGenerator.GetBytes( 32 ) ).ToLower();
        lock ( _lock ) { _validTokens.Add( token ); }
        return token;
    }

    /// <summary>
    /// Returns <c>true</c> if the token was issued by this instance and has
    /// not been revoked.
    /// </summary>
    public bool ValidateToken( string token )
    {
        if ( string.IsNullOrWhiteSpace( token ) ) return false;
        lock ( _lock ) { return _validTokens.Contains( token ); }
    }

    /// <summary>Revokes a single token (used on explicit logout).</summary>
    public void RevokeToken( string token )
    {
        lock ( _lock ) { _validTokens.Remove( token ); }
    }

    /// <summary>
    /// Revokes all active tokens. Called whenever the PIN changes so that
    /// existing sessions must re-authenticate with the new value.
    /// </summary>
    public void RevokeAll()
    {
        lock ( _lock ) { _validTokens.Clear(); }
    }
}
