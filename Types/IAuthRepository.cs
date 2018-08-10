using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Types
{
    public interface IAuthRepository
    {
        Task<bool> AddRefreshToken(RefreshToken token);
        Client FindClient(string clientId);
        Task<RefreshToken> FindRefreshToken(string refreshTokenId);
        Task<IdentityUser> FindUser(string userName);
        Task<IdentityUser> FindUser(string userName, string password);
        List<RefreshToken> GetAllRefreshTokens();
        Task<IList<string>> GetRoles(ApplicationUser user);
        Task<IdentityResult> RegisterUser(UserModel userModel);
        Task<bool> RemoveRefreshToken(RefreshToken refreshToken);
        Task<bool> RemoveRefreshToken(string refreshTokenId);
    }
}