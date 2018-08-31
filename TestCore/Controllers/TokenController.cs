using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Types;

namespace TestCore.Controllers
{
    [Produces("application/json")]
    [Route("api/Token")]
    public class TokenController : Controller
    {
        private IAuthRepository _repo = null;
        private AuthOptions _authOptions = null;

        public TokenController(IAuthRepository authRepository, AuthOptions authOptions)
        {
            _repo = authRepository;
            _authOptions = authOptions;
        }

        [HttpPost("/token")]
        public async Task<IActionResult> Token()
        {
            string grant_type = Request.Form["grant_type"];

            switch (grant_type)
            {
                case "password":
                    {
                        var username = Request.Form["username"];
                        var password = Request.Form["password"];

                        var identity = await GetIdentity(username, password);
                        if (identity == null)
                        {
                            return BadRequest("Invalid username or password.");
                        }

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

                        var response = new
                        {
                            access_token = encodedJwt,
                            username = identity.Name
                        };

                        return Ok(new { access_token = encodedJwt });
                    };
                case "refresh_token":
                    {
                        return BadRequest("Invalid grant_type.");
                    };
                default:
                    {
                        return BadRequest("Invalid grant_type.");
                    };
            }
        }

        private async Task<ClaimsIdentity> GetIdentity(string username, string password)
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


                ClaimsIdentity claimsIdentity = new ClaimsIdentity(
                    claims,
                    "Token",
                    ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);

                return claimsIdentity;
            }

            // если пользователя не найдено
            return null;
        }
    }
}