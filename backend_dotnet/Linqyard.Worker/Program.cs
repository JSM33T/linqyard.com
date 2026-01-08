using Linqyard.Worker;
using Linqyard.Worker.Configuration;
using Linqyard.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure options
builder.Services.Configure<IpGeolocationOptions>(
    builder.Configuration.GetSection("IpGeolocation"));
builder.Services.Configure<GeolocationEnrichmentOptions>(
    builder.Configuration.GetSection("GeolocationEnrichment"));

// Register services
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IpGeolocationService>();
builder.Services.AddSingleton<IIpGeolocationService, IpGeolocationService>();
builder.Services.AddScoped<GeolocationEnrichmentService>();

// Register hosted service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
