using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using NLog;

namespace Amtsschimmel.Services;

public sealed record UpdateInfo(string TagName, Version Version);

/// <summary>
/// Prüft GitHub-Releases auf neuere Versionen. Proxy-fähig nach dem Checkmk-Cockpit-Muster:
/// DefaultProxyCredentials, damit authentifizierende Unternehmens-Proxys (407) funktionieren.
/// </summary>
public sealed class UpdateCheckService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string ApiUrl = "https://api.github.com/repos/Kroste/Amtsschimmel/releases/latest";
    public const string ReleasesPage = "https://github.com/Kroste/Amtsschimmel/releases";

    /// <summary>Liefert Update-Infos, wenn eine neuere Version existiert; sonst null. Wirft nie.</summary>
    public async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                UseProxy = true,
                DefaultProxyCredentials = CredentialCache.DefaultCredentials,
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Amtsschimmel-UpdateCheck");

            using var response = await client.GetAsync(ApiUrl).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            var tag = json.RootElement.GetProperty("tag_name").GetString() ?? "";

            var latest = ParseVersion(tag);
            var current = ParseVersion(CurrentVersionString());
            if (latest is not null && current is not null && latest > current)
            {
                Log.Info("Update verfügbar: {Tag} (installiert: {Current})", tag, current);
                return new UpdateInfo(tag, latest);
            }
            Log.Info("Update-Check: aktuell ({Current}).", current);
            return null;
        }
        catch (Exception ex)
        {
            // Offline, Rate-Limit, Proxy-Probleme — alles kein Grund, die App zu stören.
            Log.Warn(ex, "Update-Check fehlgeschlagen.");
            return null;
        }
    }

    private static string CurrentVersionString() =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

    /// <summary>Parst "v1.2.3", "1.2.3+sha" oder "1.2.3-rc1" zu einer Version (nur numerischer Kern).</summary>
    public static Version? ParseVersion(string input)
    {
        var s = input.Trim().TrimStart('v', 'V');
        foreach (var separator in new[] { '+', '-' })
        {
            var idx = s.IndexOf(separator);
            if (idx > 0)
            {
                s = s[..idx];
            }
        }
        if (s.Count(ch => ch == '.') == 1)
        {
            s += ".0"; // "1.2" → "1.2.0"
        }
        return Version.TryParse(s, out var version) ? version : null;
    }
}
