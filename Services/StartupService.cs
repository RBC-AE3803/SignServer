using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NTQQ_SignServer.Services;

public class StartupService : IHostedService
{
    private readonly ISignService _signService;
    private readonly ILogger<StartupService> _logger;

    public StartupService(ISignService signService, ILogger<StartupService> logger)
    {
        _signService = signService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在启动签名服务...");
        
        try
        {
            var success = _signService.Initialize();
            if (success)
            {
                _logger.LogInformation("签名服务启动成功");
            }
            else
            {
                _logger.LogError("签名服务启动失败");
                throw new InvalidOperationException("签名服务初始化失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "签名服务启动过程中发生严重错误");
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止签名服务...");
        
        try
        {
            _signService.Unload();
            _logger.LogInformation("签名服务已停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止签名服务时发生错误");
        }

        await Task.CompletedTask;
    }
}