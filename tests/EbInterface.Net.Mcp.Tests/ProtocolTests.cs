using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace EbInterface.Mcp.Tests
{
    /// <summary>Startet den echten Server als eigenen Prozess und spricht ihn über MCP (stdio) an – wie Claude es tut.</summary>
    public class ProtocolTests
    {
        [Fact]
        public async Task Server_ListsToolsAndAnswersCalls()
        {
            string serverDll = FindServerDll();
            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "ebinterface",
                Command = "dotnet",
                Arguments = new[] { serverDll },
            });

            await using McpClient client = await McpClient.CreateAsync(transport);

            var tools = await client.ListToolsAsync();
            Assert.Equal(
                new[] { "explain_code", "read_invoice", "upgrade_invoice", "validate_invoice" },
                tools.Select(t => t.Name).OrderBy(n => n).ToArray());
            Assert.True(tools.Single(t => t.Name == "validate_invoice").ProtocolTool.Annotations?.ReadOnlyHint);

            CallToolResult result = await client.CallToolAsync(
                "validate_invoice",
                new Dictionary<string, object?>
                {
                    ["path"] = Path.Combine(AppContext.BaseDirectory, "TestData", "6p1", "gueltig-bestellnummer.xml"),
                    ["profile"] = "erechnung",
                    ["referenceDate"] = "2026-09-25",
                });

            string text = result.Content.OfType<TextContentBlock>().Single().Text;
            Assert.Contains("Ergebnis: gültig", text);
        }

        /// <summary>Die Server-DLL aus dem Build-Ausgabeordner des MCP-Projekts (gleiche Konfiguration wie die Tests).</summary>
        private static string FindServerDll()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string configuration = dir.Parent!.Name; // …/bin/<Konfiguration>/net8.0/
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "EbInterface.Net.sln")))
                dir = dir.Parent;

            Assert.NotNull(dir);
            string dll = Path.Combine(dir!.FullName, "src", "EbInterface.Net.Mcp", "bin", configuration, "net8.0", "EbInterface.Net.Mcp.dll");
            Assert.True(File.Exists(dll), $"Server nicht gebaut: {dll}");
            return dll;
        }
    }
}
