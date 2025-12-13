# NTQQ-SignerServer

这是一个基于 ASP.NET Core 10 重写的 NTQQ 签名服务器

## 项目结构

```
NTQQ-SignServer/
├── Controllers/           # API 控制器
│   └── SignController.cs
├── Models/               # 数据模型
│   ├── SignRequest.cs
│   └── AppSettings.cs
├── Services/             # 业务服务
│   ├── ISignService.cs
│   ├── SignService.cs
│   └── StartupService.cs
├── Program.cs            # 程序入口
├── appsettings.json      # 配置文件
├── NTQQ-SignServer.csproj # 项目文件
└── README.md            # 说明文档
```

## 快速开始

### 1. 安装依赖

确保已安装 .NET 10 SDK：


### 2. 配置签名服务

编辑 `appsettings.json` 文件，配置签名服务参数：

```json
{
  "AppSettings": {
    "SignService": {
      "Host": "127.0.0.1",
      "Port": 8080,
      "Libs": ["libgnutls.so.30", "./libsymbols.so"],
      "Offset": "0x0",
      "MaxDataLength": 1048576,
      "TimeoutMs": 5000
    }
  }
}
```

### 3. 运行服务器

```bash
# 进入项目目录
cd NTQQ-SignServer

# 恢复 NuGet 包
dotnet restore

# 运行服务器
dotnet run
```

服务器将在 `http://localhost:8080` 启动。

## API 接口

### 签名接口

**POST** `/api/sign`

请求体：
```json
{
  "cmd": "wtlogin.login",
  "src": "0102030405060708",
  "seq": 1
}
```

响应：
```json
{
  "value": {
    "token": "A1B2C3D4E5F6...",
    "extra": "G7H8I9J0K1L2...",
    "sign": "M3N4O5P6Q7R8..."
  }
}
```

### 应用信息接口

**GET** `/api/sign/appinfo`

返回从appinfo.json读取的应用配置信息。

**GET** `/api/sign/appinfo_v2`

返回从appinfo.json读取的应用配置信息，并转译为Lagrange v2格式。

### 健康检查

**GET** `/api/sign/health`

返回服务器健康状态。

**GET** `/api/sign/status`

返回签名服务状态。

## 配置说明

### SignService 配置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| Host | string | 127.0.0.1 | 服务器监听地址 |
| Port | int | 8080 | 服务器监听端口 |
| Libs | string[] | ["libgnutls.so.30", "./libsymbols.so"] | 依赖库列表 |
| Offset | string | "0x0" | 签名函数偏移量 |
| MaxDataLength | int | 1048576 | 最大数据长度（1MB） |
| TimeoutMs | int | 5000 | 超时时间（毫秒） |

### 日志配置

日志级别可以在 `appsettings.json` 中配置：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "NTQQ_SignServer": "Information"
    }
  }
}
```


## 故障排除

### 常见问题

1. **端口被占用**
   ```bash
   # 查找占用端口的进程
   netstat -tulpn | grep 8080
   
   # 或者修改 appsettings.json 中的端口号
   ```

2. **依赖库加载失败**
   - 确保 `libgnutls.so.30` 和 `libsymbols.so` 在系统路径中
   - 或者修改 Libs 配置为正确的库路径

3. **签名函数偏移量错误**
   - 检查 Offset 配置是否正确
   - 确保 wrapper.node 文件存在且版本匹配

### 日志查看

服务器启动时会输出详细的日志信息，可以通过以下方式查看：

```bash
# 查看控制台输出
dotnet run

# 或者查看系统日志
journalctl -u ntqq-signserver
```

## 开发说明

### 添加新的 API 接口

1. 在 `Controllers/` 目录下创建新的控制器
2. 在 `Models/` 目录下定义请求/响应模型
3. 在 `Services/` 目录下实现业务逻辑

### 修改配置

所有配置都在 `appsettings.json` 中管理，支持开发和生产环境的不同配置。

## 许可证

本项目基于 AGPL-3.0 许可证开源。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进这个项目。