using DymoScaleService.Api.Interfaces;
using DymoScaleService.Api.Middlewares;
using DymoScaleService.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// hosting windows service 
builder.Host.UseWindowsService();
builder.Services.AddWindowsService();

// add services
builder.Services.AddScoped<IDymoScaleUsbService, DymoScaleUsbService>();

//serilog configuration
//https://waqasahmeddev.medium.com/structured-logging-with-serilog-in-net-core-6-best-practices-and-setup-99aff5893f33
//https://www.anmalkov.com/blog/use-serilog-with-minimal-api-or-aspnet-6
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config).CreateLogger();

// remove default logging providers
builder.Logging.ClearProviders();

// register Serilog
builder.Logging.AddSerilog(logger);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.MapGet("/dymoscaleapi/getweight", (IDymoScaleUsbService _dymoService) => _dymoService.GetWeight());

app.MapGet("/dymoscaleapi/hello", () => $"Dymo Scale Service API listening on {config.GetValue<string>("Kestrel:Endpoints:Http:Url")}");

app.Logger.LogInformation("DymoScaleService application started");

app.Run();

// 
// async await