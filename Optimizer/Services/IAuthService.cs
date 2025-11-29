using System.Threading;
using System.Threading.Tasks;

namespace Optimizer.Services
{
    public interface IAuthService
    {
        Task<AuthResult> AuthenticateAsync(CancellationToken ct);
    }

    public enum AuthResult
    {
        Success,
        Cancelled,
        Failed
    }
}