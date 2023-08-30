namespace Augustus;

using System;
using System.Threading.Tasks;

public partial class APISimulator
{
    private readonly string apiName;
    private readonly APISimulatorOptions options;
    private readonly WebHost webHost = new();

    public APISimulator(string apiName, APISimulatorOptions options)
    {
        this.apiName = apiName ?? throw new ArgumentNullException(nameof(apiName));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public void AddInstruction(string instruction)
    {
        InstructionsContainer.AddInstruction(instruction);
    }

    public void ClearInstructions()
    {
        InstructionsContainer.ClearInstructions();
    }

    public async Task StartAsync()
    {
        await webHost.StartAsync();
    }

    public async Task StopAsync()
    {
        await webHost.StopAsync();
    }

    public HttpClient CreateClient()
    {
        return webHost.CreateClient();
    }
}
