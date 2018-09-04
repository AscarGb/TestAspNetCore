
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace OAuthProvider
{
    class OAuthProviderMiddleware
    {
        RequestDelegate _next;
        IOAuthProvider _oAuthProvider;       

        public OAuthProviderMiddleware(RequestDelegate next, IOAuthProvider oAuthProvider)
        {
            _next = next;
            _oAuthProvider = oAuthProvider;
        }
        public async Task Invoke(HttpContext context)
        {
            OAuthProviderContext _oAuthProviderContext;

            string path = context.Request.Path.Value.ToLower().Trim();

            bool isPost = context.Request.Method.ToLower() == "post";

            if (path == "/token" && isPost)
            {
                var form = context.Request.Form;

                if (!form.ContainsKey("grant_type")) {
                    await context.BadRequest("invalid grant_type");
                    return;
                }
                if (!form.ContainsKey("client_id"))
                {
                    await context.BadRequest("invalid client_id");
                    return;
                }

                string grant_type = form["grant_type"];
                string client_id = form["client_id"];

                switch (grant_type)
                {
                    case "password":
                        {
                            if (!form.ContainsKey("username"))
                            {
                                await context.BadRequest("invalid username");
                                return;
                            }
                            if (!form.ContainsKey("password"))
                            {
                                await context.BadRequest("invalid password");
                                return;
                            }

                            string username = form["username"];
                            string password = form["password"];

                            _oAuthProviderContext = new OAuthProviderContext()
                            {
                                ClientId = client_id,
                                Username = username,
                                Password = password
                            };


                            await _oAuthProvider.ByPassword(_oAuthProviderContext);

                            if (_oAuthProviderContext.HasError)
                            {
                                await context.BadRequest(_oAuthProviderContext.Error);
                                return;
                            }
                            else
                            {
                                await context.WriteToken(_oAuthProviderContext);
                                return;
                            }

                        };
                    case "refresh_token":
                        {
                            if (!form.ContainsKey("refresh_token"))
                            {
                                await context.BadRequest("invalid refresh_token");
                                return;
                            }

                            string refresh_token = form["refresh_token"];

                            _oAuthProviderContext = new OAuthProviderContext()
                            {
                                ClientId = client_id,
                                RefreshToken = refresh_token
                            };

                            await _oAuthProvider.ByRefreshToken(_oAuthProviderContext);

                            if (_oAuthProviderContext.HasError)
                            {
                                await context.BadRequest(_oAuthProviderContext.Error);                                
                                return;
                            }
                            else
                            {
                                await context.WriteToken(_oAuthProviderContext);
                                return;
                            }
                        };
                    default:
                        {
                            await context.BadRequest("invalid grant_type");
                            return;
                        };
                }
            }
            else
            {
                await _next.Invoke(context);
            }
        }
    }

    public static class OAuthExtensions
    {
        public static IApplicationBuilder UseOAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OAuthProviderMiddleware>();
        }

        internal static async Task BadRequest(this HttpContext context, string Error)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync(Error);
        }

        internal static async Task WriteToken(this HttpContext context, OAuthProviderContext _oAuthProviderContext)
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(new
            {
                access_token = _oAuthProviderContext.AccessToken,
                refresh_token = _oAuthProviderContext.RefreshToken
            }));
        }
    }
}