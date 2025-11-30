using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NTQQ_SignServer.Services;
using NTQQ_SignServer.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSingleton<ISignService, SignService>();
builder.Services.AddHostedService<StartupService>();


builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{

}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// 服务器你健不健康！！！！！！！！！！
app.MapGet("/health", () => "Sign Server is running");
app.MapGet("/", () => "{code:200,message:Sign Server is running}");
Console.ForegroundColor = ConsoleColor.Magenta;
        if (Console.BufferWidth >= 45)
        {
            Console.WriteLine(
                $$"""
                                _   _ _____ ___   ___       ____  _             ____                           
                | \ | |_   _/ _ \ / _ \     / ___|(_) __ _ _ __ / ___|  ___ _ ____   _____ _ __ 
                |  \| | | || | | | | | |____\___ \| |/ _` | '_ \\___ \ / _ \ '__\ \ / / _ \ '__|
                | |\  | | || |_| | |_| |_____|__) | | (_| | | | |___) |  __/ |   \ V /  __/ |   
                |_| \_| |_| \__\_\\__\_\    |____/|_|\__, |_| |_|____/ \___|_|    \_/ \___|_|   
                                                    |___/                                      
                """
            );
        }
        else
            Console.WriteLine("NTQQ-SignServer");
app.Logger.LogInformation("NTQQ Sign Server starting...");

var port = appSettings?.SignService?.Port ?? 8080;
var host = appSettings?.SignService?.Host ?? "127.0.0.1";

app.Logger.LogInformation("服务将监听在 {Host}:{Port}", host, port);
await Task.Delay(2000);
app.Run($"http://{host}:{port}");