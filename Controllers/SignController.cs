using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NTQQ_SignServer.Models;
using NTQQ_SignServer.Services;
using System.Text.Json;

namespace NTQQ_SignServer.Controllers;

[ApiController]
[Route("api/sign")]
public class SignController : ControllerBase
{
    private readonly ISignService _signService;
    private readonly ILogger<SignController> _logger;

    public SignController(ISignService signService, ILogger<SignController> logger)
    {
        _signService = signService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SignResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Sign([FromBody] SignRequest request)
    {
        _logger.LogInformation("收到签名请求: Cmd={Cmd}, Seq={Seq}, DataLength={Length}", 
            request.Cmd, request.Seq, request.Src.Length);

        try
        {
            if (string.IsNullOrEmpty(request.Cmd))
            {
                return BadRequest(new ErrorResponse { Error = "Cmd参数不能为空" });
            }

            if (string.IsNullOrEmpty(request.Src))
            {
                return BadRequest(new ErrorResponse { Error = "Src参数不能为空" });
            }

            if (request.Seq < 0)
            {
                return BadRequest(new ErrorResponse { Error = "Seq参数必须大于等于0" });
            }

            var response = await _signService.SignAsync(request);
            
            _logger.LogInformation("签名请求处理成功: TokenLength={TokenLength}, ExtraLength={ExtraLength}, SignLength={SignLength}",
                response.Value.Token.Length, response.Value.Extra.Length, response.Value.Sign.Length);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "签名请求参数错误");
            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "签名服务未初始化");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                new ErrorResponse { Error = "签名服务未就绪，请稍后重试" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "签名处理过程中发生错误");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ErrorResponse { Error = "签名服务内部错误" });
        }
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        _logger.LogDebug("健康检查请求");
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        _logger.LogDebug("状态检查请求");
        return Ok(new 
        { 
            service = "NTQQ Sign Server",
            version = "1.0.0",
            status = "running",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("appinfo")]
    public IActionResult GetAppInfo()
    {
        _logger.LogDebug("应用信息请求");
        
        try
        {
            var appInfoPath = "./appinfo.json";
            if (!System.IO.File.Exists(appInfoPath))
            {
                appInfoPath = "./QQApp/appinfo.json";
                if (!System.IO.File.Exists(appInfoPath))
                {
                    _logger.LogWarning("appinfo.json文件不存在");
                    return NotFound(new ErrorResponse { Error = "应用信息文件不存在" });
                }
            }

            var jsonContent = System.IO.File.ReadAllText(appInfoPath);
            
            var appInfo = JsonSerializer.Deserialize<JsonElement>(jsonContent);
            
            _logger.LogDebug("应用信息读取成功");
            return Ok(appInfo);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "appinfo.json文件格式错误");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ErrorResponse { Error = "应用信息文件格式错误" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取应用信息时发生错误");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ErrorResponse { Error = "读取应用信息失败" });
        }
    }
}