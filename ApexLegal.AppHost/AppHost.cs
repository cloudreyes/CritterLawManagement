var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var db = postgres.AddDatabase("apexlegaldb");

var api = builder.AddProject<Projects.ApexLegal_Api>("api")
    .WithReference(db)
    .WaitFor(db);

builder.AddProject<Projects.ApexLegal_Web>("web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
