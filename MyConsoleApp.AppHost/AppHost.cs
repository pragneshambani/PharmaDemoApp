var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.MyConsoleApp_Server>("server");

builder.AddNpmApp("frontend", "../myconsoleapp.client")
    .WithReference(server)
    .WithHttpsEndpoint(env: "DEV_SERVER_PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();