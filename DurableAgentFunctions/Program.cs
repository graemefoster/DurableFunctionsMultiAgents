using System.ClientModel;
using DurableAgentFunctions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddDurableTaskClient(c => { c.UseGrpc(); });

var settings= new AgentSettings(
    Environment.GetEnvironmentVariable("SIGNALR_URL")!,
    Environment.GetEnvironmentVariable("AOAI_URL")!,
    Environment.GetEnvironmentVariable("AOAI_KEY")!,
    Environment.GetEnvironmentVariable("AOAI_DEPLOYMENT")!
    );

builder.Services.AddSingleton(settings);

await using var signalrHub = new HubConnectionBuilder()
    .WithUrl(settings.SignalrUrl)
    .WithAutomaticReconnect()
    .Build();

signalrHub.StartAsync().Wait();
builder.Services.AddSingleton(signalrHub);

var chatClient = new Azure.AI.OpenAI.AzureOpenAIClient(
        new Uri(settings.AoaiUrl),
        new ApiKeyCredential(settings.AoaiKey))
    .AsChatClient(settings.AoaiDeploymentName)
    .AsBuilder()
    .UseFunctionInvocation()
    .UseOpenTelemetry()
    .Build();

builder.Services.AddSingleton(chatClient);

builder.ConfigureFunctionsWebApplication();

builder.Build().Run();