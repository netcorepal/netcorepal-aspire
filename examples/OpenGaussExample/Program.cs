using System;
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("database-password", value: "Test@1234", secret: true);
// Add OpenGauss server
var opengauss = builder.AddOpenGauss("opengauss")
    .WithPassword(password)
    .WithPgWeb()
    .WithPgAdmin();
    //.WithDataVolume();

// Add a database
var database = opengauss.AddDatabase("mydb");

// Uncomment the following line to actually run the application
builder.Build().Run();