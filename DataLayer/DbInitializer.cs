using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Types;

namespace DataLayer
{
    public static class DbInitializer
    {
        static UserManager<ApplicationUser> _userManager;
        static RoleManager<IdentityRole> _roleManager;
        static IOptions<ConfigurationManager> _configurationManager;
        public static void InitializeAsync(AuthContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
          Helper helper, IOptions<ConfigurationManager> configurationManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configurationManager = configurationManager;

            if (context.Database.EnsureCreated())
            {
                context.Clients.Add(new Client
                {
                    Id = "ngAuth",
                    Secret = helper.GetHash(configurationManager.Value.ngAuthSecret),
                    Name = "Web App",
                    ApplicationType = ApplicationTypes.JavaScript,
                    Active = true,
                    AllowedOrigin = "http://localhost:8082/",
                    RefreshTokenLifeTime = 60 * 24
                });

                context.Clients.Add(new Client
                {
                    Id = "App",
                    Secret = helper.GetHash(configurationManager.Value.AppSecret),
                    Name = "Desktop App",
                    ApplicationType = ApplicationTypes.NativeConfidential,
                    Active = true,
                    AllowedOrigin = "*",
                    RefreshTokenLifeTime = 60 * 24
                });

                ApplicationUser user = new ApplicationUser { UserName = "admin" };

                _userManager.CreateAsync(user, configurationManager.Value.defaultAdminPsw).GetAwaiter().GetResult();

                _roleManager.CreateAsync(new IdentityRole { Name = ServerRoles.Admin }).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole { Name = ServerRoles.User }).GetAwaiter().GetResult();

                _userManager.AddToRoleAsync(user, ServerRoles.Admin).GetAwaiter().GetResult();
            }
        }
    }
}
