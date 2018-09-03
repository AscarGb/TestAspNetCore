using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Types;

namespace TestCore.Controllers
{
    [Produces("application/json")]
    [Route("api/Token")]
    public class TokenController : Controller
    {
        IAuthRepository _repo = null;
        AuthOptions _authOptions = null;
        Helper _helper = null;

        public TokenController(IAuthRepository authRepository, AuthOptions authOptions, Helper helper)
        {
            _repo = authRepository;
            _authOptions = authOptions;
            _helper = helper;
        }

        [HttpPost("/token")]
        public async Task<IActionResult> Token()
        {
            string grant_type = Request.Form["grant_type"];
            string client_id = Request.Form["client_id"];

            switch (grant_type)
            {
                case "password":
                    {
                        var username = Request.Form["username"];
                        var password = Request.Form["password"];

                        var identity = await GetIdentity(username, password, client_id);
                        if (identity == null)
                        {
                            return BadRequest("Invalid username or password.");
                        }

                        var encodedJwt = CreateJWT(identity);
                        var refresh_token = CreateRefreshToken(client_id, identity);

                        if(refresh_token == null)
                        {
                            return BadRequest("Invalid identity");
                        }

                        return Ok(new
                        {
                            access_token = encodedJwt,
                            refresh_token
                        });
                    };
                case "refresh_token":
                    {
                        var refresh_token = Request.Form["refresh_token"];

                        ClaimsIdentity identity = await GrantRefreshToken(client_id, refresh_token);
                        var encodedJwt = CreateJWT(identity);

                        if (identity == null)
                        {
                            return BadRequest("Invalid refresh_token");
                        }

                        return Ok(new
                        {
                            access_token = encodedJwt,
                            refresh_token
                        });                       
                    };
                default:
                    {
                        return BadRequest("Invalid grant_type.");
                    };
            }
        }

       string CreateJWT(ClaimsIdentity identity)
        {
            var now = DateTime.UtcNow;
            // создаем JWT-токен
            var jwt = new JwtSecurityToken(
                    issuer: _authOptions.Issuer,
                    audience: _authOptions.Audience,
                    notBefore: now,
                    claims: identity.Claims,
                    expires: now.Add(TimeSpan.FromSeconds(_authOptions.LifetimeSeconds)),
                    signingCredentials: new SigningCredentials(_authOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return encodedJwt;
        }

        private async Task<ClaimsIdentity> GetIdentity(string username, string password, string clientid)
        {
            ApplicationUser user = await _repo.FindUser(username, password);
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

            // если пользователя не найдено
            return null;
        }

        async Task<string> CreateRefreshToken(string clientid, ClaimsIdentity claimsIdentity)
        {
            if (string.IsNullOrEmpty(clientid))
            {
                return null;
            }

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

            token.ProtectedTicket = JsonConvert.SerializeObject(claimsIdentity);

            var result = await _repo.AddRefreshToken(token);

            if (result)
            {
                return refreshTokenId;
            }

            return null;
        }

        async Task<ClaimsIdentity> GrantRefreshToken(string currentClient, string refreshTokenId)
        {
            string hashedTokenId = _helper.GetHash(refreshTokenId);
            ClaimsIdentity ProtectedTicket = null;

            var refreshToken = await _repo.FindRefreshToken(hashedTokenId);

            if (refreshToken != null)
            {
                //Get protectedTicket from refreshToken class
                ProtectedTicket = JsonConvert.DeserializeObject<ClaimsIdentity>(refreshToken.ProtectedTicket);

                var originalClient = ProtectedTicket.FindFirst(c => c.Type == "client_id").Value;

                if (originalClient != currentClient)
                {
                    return null;
                }

                return ProtectedTicket;

            }
            else
            {
                return null;
            }

        }
    }
}