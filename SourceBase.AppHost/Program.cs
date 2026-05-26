var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.SourceBase_Api>("sourcebase-api");

builder.AddProject<Projects.SourceBase_Web>("sourcebase-web").WithReference(api).WaitFor(api);

builder.Build().Run();
