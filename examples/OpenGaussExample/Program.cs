using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add OpenGauss server
var opengauss = builder.AddOpenGauss("opengauss")
    .WithDataVolume();

// Add a database
var database = opengauss.AddDatabase("mydb");

Console.WriteLine("OpenGauss example configured successfully!");
Console.WriteLine($"Server resource: {opengauss.Resource.Name}");
Console.WriteLine($"Database resource: {database.Resource.Name}");
Console.WriteLine($"Database name: {database.Resource.DatabaseName}");

// Uncomment the following line to actually run the application
// builder.Build().Run();
