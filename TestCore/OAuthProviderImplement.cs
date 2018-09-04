using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OAuthProvider;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Types;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;

namespace TestCore
{
    public class OAuthProviderImplement : IOAuthProvider
    {
        IServiceProvider _services;
        IOptions<AuthOptions> _authOptions = null;
        Helper _helper = null;

        public OAuthProviderImplement(IServiceProvider services, IOptions<AuthOptions> authOptions, Helper helper)
        {
            _services = services;
            _authOptions = authOptions;
            _helper = helper;
        }

        public async Task ByPassword(OAuthProviderContext context)
        {
            ClaimsIdentity identity = await GetIdentity(context.Username, context.ClientId, context.Password);
            if (identity == null)
            {
                context.SetError("User not found");
                return;
            }

            string encodedJwt = CreateJWT(identity);
            string refresh_token = await CreateRefreshToken(context.ClientId, identity);

            if (refresh_token == null)
            {
                context.SetError("Error while create refresh token");
                return;
            }

            context.SetToken(encodedJwt, refresh_token);
            return;
        }

        public async Task ByRefreshToken(OAuthProviderContext context)
        {
            ProtectedTicket protectedTicket = await GrantRefreshToken(context.RefreshToken);

            if (protectedTicket == null)
            {
                context.SetError("Invalid refresh token");
                return;
            }

            if (protectedTicket.clientid != context.ClientId)
            {
                context.SetError("Invalid client id");
                return;
            }

            ClaimsIdentity identity = await GetIdentity(protectedTicket.username, protectedTicket.clientid);

            if (identity == null)
            {
                context.SetError("User not found");
                return;
            }

            string encodedJwt = CreateJWT(identity);

            context.SetToken(encodedJwt, context.RefreshToken);
            return;
        }

        string CreateJWT(ClaimsIdentity identity)
        {
            var now = DateTime.UtcNow;
            // создаем JWT-токен
            var jwt = new JwtSecurityToken(
                    issuer: _authOptions.Value.Issuer,
                    audience: _authOptions.Value.Audience,
                    notBefore: now,
                    claims: identity.Claims,
                    expires: now.Add(TimeSpan.FromSeconds(_authOptions.Value.LifetimeSeconds)),
                    signingCredentials: new SigningCredentials(_authOptions.Value.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return encodedJwt;
        }

        async Task<ClaimsIdentity> GetIdentity(string username, string clientid, string password = null)
        {
            using (var serviceScope = _services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                IAuthRepository _repo = serviceScope.ServiceProvider.GetService<IAuthRepository>();

                ApplicationUser user = null;
                if (password != null)
                {
                    user = await _repo.FindUser(username, password);
                }
                else
                {
                    user = await _repo.FindUser(username);
                }

                if (user != null)
                {
                    var claims = new List<Claim>
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, user.UserName)
                };

                    foreach (var r in await _repo.GetRoles(user))
                    {
                        claims.Add(new Claim(ClaimsIdentity.DefaultRoleClaimType, r));
                    }

                    claims.Add(new Claim("client_id", clientid));

                    ClaimsIdentity claimsIdentity = new ClaimsIdentity(
                        claims,
                        "Password",
                        ClaimsIdentity.DefaultNameClaimType,
                        ClaimsIdentity.DefaultRoleClaimType);

                    return claimsIdentity;
                }
                else
                {

                }

                // если пользователя не найдено
                return null;
            }
        }

        async Task<string> CreateRefreshToken(string clientid, ClaimsIdentity claimsIdentity)
        {
            using (var serviceScope = _services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                IAuthRepository _repo = serviceScope.ServiceProvider.GetService<IAuthRepository>();
                Client client = _repo.FindClient(clientid);

                var refreshTokenId = _helper.GetHash(_helper.GenerateRandomCryptographicKey(100));

                var refreshTokenLifeTime = client.RefreshTokenLifeTime;

                var now = DateTime.UtcNow;

                var token = new RefreshToken()
                {
                    Id = _helper.GetHash(refreshTokenId),
                    ClientId = clientid,
                    Subject = claimsIdentity.Name,
                    IssuedUtc = now,
                    ExpiresUtc = now.AddMinutes(Convert.ToDouble(refreshTokenLifeTime))
                };

                token.ProtectedTicket = JsonConvert.SerializeObject(new ProtectedTicket { clientid = clientid, username = claimsIdentity.Name });

                var result = await _repo.AddRefreshToken(token);

                if (result)
                {
                    return refreshTokenId;
                }

                return null;
            }
        }

        async Task<ProtectedTicket> GrantRefreshToken(string refreshTokenId)
        {
            using (var serviceScope = _services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                IAuthRepository _repo = serviceScope.ServiceProvider.GetService<IAuthRepository>();
                string hashedTokenId = _helper.GetHash(refreshTokenId);
                ProtectedTicket protectedTicket = null;

                var refreshToken = await _repo.FindRefreshToken(hashedTokenId);

                if (refreshToken != null)
                {
                    //Get protectedTicket from refreshToken class
                    protectedTicket = JsonConvert.DeserializeObject<ProtectedTicket>(refreshToken.ProtectedTicket);

                    return protectedTicket;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
