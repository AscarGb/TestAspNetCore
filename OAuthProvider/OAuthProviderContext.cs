using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuthProvider
{
    public class OAuthProviderContext
    {
        public bool HasError { get; private set; }
        public string Error { get; private set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; set; }

        public string ClientId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public void SetError(string error)
        {
            Error = error;
            HasError = true;
        }
        public void SetToken(string access_token, string refresh_token)
        {
            AccessToken = access_token;
            RefreshToken = refresh_token;
        }
    }
}
