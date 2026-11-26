using Kinof.Agent;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kinof.Agent;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        LoadAgentKeyFile();

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddWindowsService(options => options.ServiceName = "KinofAgent");
        builder.Services.AddSingleton<AgentApiClient>();
        builder.Services.AddSingleton<AgentSessionState>();
        builder.Services.AddSingleton<HostsWebsiteBlocker>();
        builder.Services.AddSingleton<ProgramProcessBlocker>();
        builder.Services.AddSingleton<LoginForm>();
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        var form = host.Services.GetRequiredService<LoginForm>();
        host.Start();
        try
        {
            Application.Run(form);
        }
        finally
        {
            host.StopAsync().GetAwaiter().GetResult();
            host.Dispose();
        }
    }

    /// <summary>
    /// A key file beside the exe lets a lab PC start by double-click.
    /// An explicit Kinof__ApiKey environment variable still wins.
    /// </summary>
    private static void LoadAgentKeyFile()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Kinof__ApiKey")))
            return;

        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        var path = Path.Combine(exeDir, "agent-api.key");
        if (!File.Exists(path))
            return;

        var key = File.ReadAllText(path).Trim().TrimStart('\uFEFF');
        if (key.Length == 0)
            return;

        Environment.SetEnvironmentVariable("Kinof__ApiKey", key);
    }
}
