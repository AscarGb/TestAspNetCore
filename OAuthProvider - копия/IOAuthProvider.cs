using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuthProvider
{
   public interface IOAuthProvider
    {
        Task ByPassword(OAuthProviderContext oAuthProviderContext);
        Task ByRefreshToken(OAuthProviderContext oAuthProviderContext);
    }
}
