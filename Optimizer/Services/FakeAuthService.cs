using System;
using System.Threading;
using System.Threading.Tasks;

namespace Optimizer.Services
{
    public class FakeAuthService : IAuthService
    {
        private readonly Random _random = new Random();

        public async Task<AuthResult> AuthenticateAsync(CancellationToken ct)
        {
            try
            {
                // Simulate a random delay between 10-30 seconds
                int delayMilliseconds = _random.Next(10000, 30000);
                await Task.Delay(delayMilliseconds, ct);
                return AuthResult.Success;
            }
            catch (OperationCanceledException)
            {
                return AuthResult.Cancelled;
            }
            catch (Exception)
            {
                return AuthResult.Failed;
            }
        }
    }
}