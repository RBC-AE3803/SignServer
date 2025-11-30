namespace NTQQ_SignServer.Models;

public class SignRequest
{
    public string Cmd { get; set; } = string.Empty;
    public string Src { get; set; } = string.Empty;
    public int Seq { get; set; }
}

public class ValueResponse
{
    public string Token { get; set; } = string.Empty;
    public string Extra { get; set; } = string.Empty;
    public string Sign { get; set; } = string.Empty;
}

public class SignResponse
{
    public string Platform { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public ValueResponse Value { get; set; } = new();
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}

public class AppInfo
{
    public string Os { get; set; } = string.Empty;
    public string VendorOs { get; set; } = string.Empty;
    public string Kernel { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public int MiscBitmap { get; set; }
    public string PtVersion { get; set; } = string.Empty;
    public int SsoVersion { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string WtLoginSdk { get; set; } = string.Empty;
    public long AppId { get; set; }
    public long SubAppId { get; set; }
    public long AppIdQrCode { get; set; }
    public int AppClientVersion { get; set; }
    public long MainSigMap { get; set; }
    public int SubSigMap { get; set; }
    public int NTLoginType { get; set; }
}