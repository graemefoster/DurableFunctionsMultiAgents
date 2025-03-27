using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var signalR = builder.AddProject<DurableAgentSignalR>("DurableAgentSignalR"); 

var functions = builder.AddAzureFunctionsProject<DurableAgentFunctions>("DurableAgentFunctions")
    .WaitFor(signalR);

var client = builder.AddNpmApp("DurableAgentClient", "../storyteller.client", "dev")
    .WithHttpsEndpoint(63084, targetPort:55321)
    .WaitFor(functions);

builder.Build().Run();
