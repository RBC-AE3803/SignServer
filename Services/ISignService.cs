using NTQQ_SignServer.Models;

namespace NTQQ_SignServer.Services;

public interface ISignService
{
    Task<SignResponse> SignAsync(SignRequest request);
    bool Initialize();
    void Unload();
}