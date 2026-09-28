using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EbInterface.Mcp
{
    /// <summary>
    /// Lokaler MCP-Server über Standardein-/-ausgabe. Das Protokoll läuft über stdout; Protokollmeldungen daher nur
    /// auf stderr. Der Server baut keine Netzwerkverbindungen auf.
    /// </summary>
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

            builder.Services
                .AddMcpServer(options =>
                {
                    options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
                    {
                        Name = "ebinterface",
                        Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0",
                    };
                    options.ServerInstructions =
                        "Werkzeuge für ebInterface-Rechnungen (österreichischer E-Rechnungsstandard, Versionen 4.3 bis 6.1): " +
                        "prüfen (auch gegen die Regeln von e-Rechnung.gv.at), lesen, auf 6.1 aktualisieren und Fehlercodes erklären. " +
                        "Rechnungen werden über Dateipfade angegeben. Meldungen sind deutsch.";
                })
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            await builder.Build().RunAsync();
        }
    }
}
