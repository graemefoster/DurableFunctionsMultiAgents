using System.ClientModel;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using SemanticKernelWriterMultiAgent;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();
//

var fiddlerRevocationListCheckingBypassHttpClient = new HttpClient();

builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(
        Environment.GetEnvironmentVariable("AOAI_DEPLOYMENT"),
        Environment.GetEnvironmentVariable("AOAI_URL"),
        Environment.GetEnvironmentVariable("AOAI_KEY"),
        httpClient:fiddlerRevocationListCheckingBypassHttpClient);

builder.Services.AddSingleton(new GroupChatCache());


Console.WriteLine("Ready!");

builder.Build().Run();