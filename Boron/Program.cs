using System.Security;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Http()

var app = builder.Build();

app.Run();
