using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add DMDB server with default settings
var dmdb = builder.AddDmdb("dmdb");

// Add a database
var database = dmdb.AddDatabase("testdb");

// You can also customize passwords
var password = builder.AddParameter("user-password", value: "Test@1234", secret: true);
var dbaPassword = builder.AddParameter("dba-password", value: "SYSDBA_abc123", secret: true);

var customDmdb = builder.AddDmdb("dmdb-custom")
    .WithPassword(password)
    .WithDbaPassword(dbaPassword)
    .WithHostPort(5236);
    //.WithDataVolume();

var customDatabase = customDmdb.AddDatabase("mydb");

// Uncomment the following line to actually run the application
// builder.Build().Run();

Console.WriteLine("DMDB example setup complete. Uncomment the last line to run the application.");
