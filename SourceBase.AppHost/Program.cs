var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SourceBase_Api>("sourcebase-api");

builder.Build().Run();
