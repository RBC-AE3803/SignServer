namespace NTQQ_SignServer.Models;
//默认配置
public class AppSettings
{
    public SignServiceConfig SignService { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
}

public class SignServiceConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;
    public string[] Libs { get; set; } = Array.Empty<string>();
    public string Offset { get; set; } = "0x0";
    public int MaxDataLength { get; set; } = 1048576; // 1MB
    public int TimeoutMs { get; set; } = 5000;
}

public class LoggingConfig
{
    public string Level { get; set; } = "Information";
    public bool EnableFileLogging { get; set; } = false;
    public string LogFilePath { get; set; } = "logs/signserver.log";
}