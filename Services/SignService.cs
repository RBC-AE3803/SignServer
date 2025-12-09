using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTQQ_SignServer.Models;
using System.Text.Json;
namespace NTQQ_SignServer.Services;

public class SignService : ISignService
{
    private readonly ILogger<SignService> _logger;
    private readonly AppSettings _appSettings;
    private bool _initialized = false;
    private IntPtr _moduleHandle = IntPtr.Zero;
    private IntPtr _signFunction = IntPtr.Zero;
    
    public SignService(ILogger<SignService> logger, IOptions<AppSettings> appSettings)
    {
        _logger = logger;
        _appSettings = appSettings.Value;
        _logger.LogInformation("签名服务配置已加载:Host={Host}, Port={Port}", 
            _appSettings.SignService.Host, _appSettings.SignService.Port);
    }
    private (string platform, string version) ReadAppInfo()
    {
        string platform = "Linux";
        string version = "3.2.21-42086";
        
        var appInfoPath = "./appinfo.json";
        if (!System.IO.File.Exists(appInfoPath))
        {
            appInfoPath = "./QQApp/appinfo.json";
            if (!System.IO.File.Exists(appInfoPath))
            {
                _logger.LogWarning("appinfo.json文件不存在，使用默认值");
                return (platform, version);
            }
        }
        
        try
        {
            var jsonContent = System.IO.File.ReadAllText(appInfoPath);
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;
            
            var properties = root.EnumerateObject().ToDictionary(
                p => p.Name, 
                p => p.Value, 
                StringComparer.OrdinalIgnoreCase);
            
            if (properties.TryGetValue("Os", out var osValue))
            {
                platform = osValue.GetString() ?? "Unknown";
            }
            
            if (properties.TryGetValue("CurrentVersion", out var versionValue))
            {
                version = versionValue.GetString() ?? "Unknown";
            }
            
            return (platform, version);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取appinfo.json文件失败，使用默认值");
            return (platform, version);
        }
    }
    public bool Initialize()
    {
        try
        {
            _logger.LogInformation("正在启动签名服务初始化...");
            ValidateConfiguration();
            
            // 测试模式
            if (_appSettings.SignService.Libs.Length == 0)
            {
                _logger.LogWarning("未配置库文件，运行在测试模式");
                _initialized = true;
                _logger.LogInformation("签名服务在测试模式下初始化成功");
                return true;
            }
            
            foreach (var lib in _appSettings.SignService.Libs)
            {
                var handle = NativeMethods.dlopen(lib, NativeMethods.RTLD_LAZY | NativeMethods.RTLD_GLOBAL);
                if (handle == IntPtr.Zero)
                {
                    var error = NativeMethods.dlerror();
                    _logger.LogError("加载库 {Lib} 失败: {Error}", lib, error);
                    return false;
                }
                _logger.LogDebug("成功加载库: {Lib}", lib);
            }
            
            _moduleHandle = NativeMethods.dlopen("./QQApp/wrapper.node", NativeMethods.RTLD_LAZY);
            if (_moduleHandle == IntPtr.Zero)
            {
                var error = NativeMethods.dlerror();
                _logger.LogError("加载 wrapper.node 失败: {Error}", error);
                return false;
            }
            
            var moduleBase = GetModuleBaseAddress("wrapper.node");
            if (moduleBase == 0)
            {
                _logger.LogError("无法获取模块基地址");
                NativeMethods.dlclose(_moduleHandle);
                return false;
            }
            
            var offset = ParseOffset(_appSettings.SignService.Offset);
            _signFunction = CalculateFunctionPointer(moduleBase, offset);
            
            if ((ulong)_signFunction < 0x1000)
            {
                _logger.LogError("无效的函数指针: 0x{0}", ((ulong)_signFunction).ToString("X"));
                NativeMethods.dlclose(_moduleHandle);
                return false;
            }
            
            _initialized = true;
            _logger.LogInformation("签名服务初始化成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "签名服务初始化过程中发生异常");
            return false;
        }
    }
    
    public async Task<SignResponse> SignAsync(SignRequest request)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("签名服务尚未初始化");
        }
        
        _logger.LogInformation("开始签名处理: Cmd={Cmd}, Seq={Seq}, DataLength={Length}", 
            request.Cmd, request.Seq, request.Src.Length);
        
        try
        {
            ValidateSignRequest(request);
            var srcBytes = HexToBytes(request.Src);
            var result = CallSignFunction(request.Cmd, srcBytes, request.Seq);
            
            var (platform, version) = ReadAppInfo();
            
            var response = new SignResponse
            {
                Platform = platform,
                Version = version,
                Value = new ValueResponse
                {
                    Token = BytesToHex(result.Token),
                    Extra = BytesToHex(result.Extra),
                    Sign = BytesToHex(result.Sign)
                }
            };
            
            _logger.LogInformation("签名处理完成");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "签名处理失败");
            throw;
        }
    }
    
    public void Unload()
    {
        if (_moduleHandle != IntPtr.Zero)
        {
            NativeMethods.dlclose(_moduleHandle);
            _moduleHandle = IntPtr.Zero;
            _signFunction = IntPtr.Zero;
            _logger.LogInformation("签名模块已卸载");
        }
        _initialized = false;
    }
    
    private void ValidateSignRequest(SignRequest request)
    {
        if (string.IsNullOrEmpty(request.Cmd))
        {
            throw new ArgumentException("Cmd 参数不能为空");
        }
        
        if (string.IsNullOrEmpty(request.Src))
        {
            throw new ArgumentException("Src 参数不能为空");
        }
        
        if (request.Seq < 0)
        {
            throw new ArgumentException("Seq 参数必须大于或等于 0");
        }
        
        if (!IsHexString(request.Src))
        {
            throw new ArgumentException("Src 参数必须是有效的十六进制字符串");
        }
        
        if (request.Src.Length > 1024 * 1024)
        {
            throw new ArgumentException("数据长度超出限制");
        }
    }
    
    private bool IsHexString(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        
        foreach (char c in input)
        {
            if (!char.IsDigit(c) && !(c >= 'a' && c <= 'f') && !(c >= 'A' && c <= 'F'))
            {
                return false;
            }
        }
        return true;
    }
    
    private byte[] HexToBytes(string hex)
    {
        if (hex.Length % 2 != 0)
        {
            throw new ArgumentException("十六进制字符串长度必须为偶数");
        }
        
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            var hexByte = hex.Substring(i * 2, 2);
            bytes[i] = Convert.ToByte(hexByte, 16);
        }
        return bytes;
    }
    
    private string BytesToHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
    }
    
    private ulong GetModuleBaseAddress(string moduleName)
    {
        try
        {
            var state = new DlIteratePhdrState(moduleName);
            var handle = GCHandle.Alloc(state);
            try
            {
                int ret = NativeMethods.dl_iterate_phdr(DlIteratePhdrCallback, GCHandle.ToIntPtr(handle));
                if (ret != 0 && state.Found && state.BaseAddress != 0)
                {
                    _logger.LogDebug("找到模块 {ModuleName} 基地址: 0x{BaseAddress:X16}", moduleName, state.BaseAddress);
                    return state.BaseAddress;
                }
                _logger.LogError("未找到模块 {ModuleName} 的基地址", moduleName);
                return 0;
            }
            finally
            {
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用 dl_iterate_phdr 获取模块基地址失败");
            return 0;
        }
    }
    
    private void ValidateConfiguration()
    {
        if (_appSettings.SignService.Libs == null || _appSettings.SignService.Libs.Length == 0)
        {
            throw new InvalidOperationException("配置错误: Libs 列表不能为空");
        }

        if (string.IsNullOrEmpty(_appSettings.SignService.Offset) || _appSettings.SignService.Offset == "0" || _appSettings.SignService.Offset == "0x0")
        {
            throw new InvalidOperationException("配置错误: Offset 不能为零");
        }

        if (_appSettings.SignService.MaxDataLength <= 0 || _appSettings.SignService.MaxDataLength > 10 * 1024 * 1024)
        {
            throw new InvalidOperationException("配置错误: MaxDataLength 必须在合理范围内");
        }

        if (_appSettings.SignService.TimeoutMs <= 0 || _appSettings.SignService.TimeoutMs > 30000)
        {
            throw new InvalidOperationException("配置错误: TimeoutMs 必须在合理范围内");
        }
    }
    
    private ulong ParseOffset(string offsetString)
    {
        if (string.IsNullOrEmpty(offsetString))
        {
            throw new ArgumentException("Offset 字符串不能为空");
        }
        
        try
        {
            if (offsetString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToUInt64(offsetString, 16);
            }
            else
            {
                return Convert.ToUInt64(offsetString);
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"无效的 Offset 格式: {offsetString}", ex);
        }
    }
    
    private IntPtr CalculateFunctionPointer(ulong moduleBase, ulong offset)
    {
        if (ulong.MaxValue - moduleBase < offset)
        {
            throw new OverflowException("计算函数指针时发生算术溢出");
        }
        
        var result = moduleBase + offset;
        
        if (result < 0x1000 || result > 0x7FFFFFFFFFFFFFFF)
        {
            throw new ArgumentOutOfRangeException($"计算出的函数指针超出有效范围: 0x{result:X}");
        }
        
        return (IntPtr)result;
    }
    
    private SignResult CallSignFunction(string cmd, byte[] srcData, int seq)
    {
        if (srcData.Length > _appSettings.SignService.MaxDataLength)
        {
            throw new ArgumentException($"输入数据过长，允许的最大长度: {_appSettings.SignService.MaxDataLength}");
        }
        
        // 测试模式
        if (_appSettings.SignService.Libs.Length == 0)
        {
            _logger.LogWarning("运行在测试模式，返回模拟签名数据");
            var token = GenerateRandomBytes(32);
            var extra = GenerateRandomBytes(64);
            var sign = GenerateRandomBytes(64);
            
            return new SignResult(token, extra, sign);
        }
        
        try
        {
            _logger.LogDebug("调用签名函数: Cmd={Cmd}, Seq={Seq}, DataLength={Length}", 
                cmd, seq, srcData.Length);
            
            var signFunc = Marshal.GetDelegateForFunctionPointer<SignFunctionDelegate>(_signFunction);

            var inputBuffer = Marshal.AllocCoTaskMem(srcData.Length);
            Marshal.Copy(srcData, 0, inputBuffer, srcData.Length);
            
            var outputBuffer = Marshal.AllocCoTaskMem(0x300);
            
            try
            {
                var result = signFunc(
                    cmd, 
                    inputBuffer, 
                    srcData.Length, 
                    seq, 
                    outputBuffer);
                
                if (result != 0)
                {
                    throw new InvalidOperationException($"签名函数返回错误代码: {result}");
                }
                
                var outputBytes = new byte[0x300];
                Marshal.Copy(outputBuffer, outputBytes, 0, 0x300);

                var tokenLen = outputBytes[0x0FF];
                var extraLen = outputBytes[0x1FF];
                var signLen = outputBytes[0x2FF];
                
                var token = new byte[tokenLen];
                var extra = new byte[extraLen];
                var sign = new byte[signLen];
                
                Array.Copy(outputBytes, 0, token, 0, tokenLen);
                Array.Copy(outputBytes, 0x100, extra, 0, extraLen);
                Array.Copy(outputBytes, 0x200, sign, 0, signLen);
                
                _logger.LogDebug("签名函数调用成功，Token长度={TokenLen}, Extra长度={ExtraLen}, Sign长度={SignLen}", 
                    tokenLen, extraLen, signLen);
                return new SignResult(token, extra, sign);
            }
            finally
            {
                Marshal.FreeCoTaskMem(inputBuffer);
                Marshal.FreeCoTaskMem(outputBuffer);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用签名函数失败");
            throw;
        }
    }

    private sealed class DlIteratePhdrState
    {
        public string ModuleName { get; }
        public ulong BaseAddress { get; set; }
        public bool Found { get; set; }
        public DlIteratePhdrState(string moduleName)
        {
            ModuleName = moduleName;
            BaseAddress = 0;
            Found = false;
        }
    }

    private static int DlIteratePhdrCallback(ref NativeMethods.DlPhdrInfo info, IntPtr size, IntPtr data)
    {
        try
        {
            var handle = GCHandle.FromIntPtr(data);
            var state = (DlIteratePhdrState)handle.Target!;
            string? name = Marshal.PtrToStringAnsi(info.dlpi_name);
            if (!string.IsNullOrEmpty(name) && name.Contains(state.ModuleName, StringComparison.Ordinal))
            {
                state.BaseAddress = info.dlpi_addr;
                state.Found = true;
                return 1;
            }
        }
        catch
        {
        }
        return 0;
    }
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SignFunctionDelegate(
        string cmd,
        IntPtr srcData,
        int dataLength,
        int seq,
        IntPtr outputBuf);
    
    private byte[] GenerateRandomBytes(int length)
    {
        if (length <= 0 || length > 1024 * 1024)
        {
            throw new ArgumentException($"无效的缓冲区长度: {length}");
        }
        
        var random = new Random();
        var bytes = new byte[length];
        random.NextBytes(bytes);
        return bytes;
    }
    
    private record SignResult(byte[] Token, byte[] Extra, byte[] Sign);
}

[SuppressUnmanagedCodeSecurity]
internal static class NativeMethods
{
    public const int RTLD_LAZY = 0x00001;
    public const int RTLD_NOW = 0x00002;
    public const int RTLD_GLOBAL = 0x00100;
    public const int RTLD_LOCAL = 0x00000;
    
    [DllImport("libdl.so.2")]
    public static extern IntPtr dlopen(string filename, int flags);
    
    [DllImport("libdl.so.2")]
    public static extern int dlclose(IntPtr handle);
    
    [DllImport("libdl.so.2")]
    public static extern IntPtr dlsym(IntPtr handle, string symbol);
    
    [DllImport("libdl.so.2")]
    public static extern string dlerror();
    
    [StructLayout(LayoutKind.Sequential)]
    public struct DlPhdrInfo
    {
        public ulong dlpi_addr;
        public IntPtr dlpi_name;
        public IntPtr dlpi_phdr;
        public ushort dlpi_phnum;
    }
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int DlIteratePhdrCallback(ref DlPhdrInfo info, IntPtr size, IntPtr data);
    
    [DllImport("libc.so.6")]
    public static extern int dl_iterate_phdr(DlIteratePhdrCallback callback, IntPtr data);
}
