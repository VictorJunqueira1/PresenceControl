using ControlePresenca.Application;
using ControlePresenca.Infrastructure;
using ControlePresenca.Web.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebConfiguration(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseWebConfiguration();

app.Run();