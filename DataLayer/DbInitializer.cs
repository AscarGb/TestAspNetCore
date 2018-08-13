using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;

namespace DataLayer
{
    public class DbInitializer
    {
        UserManager<ApplicationUser> _userManager;
        RoleManager<IdentityRole> _roleManager;
        IOptions<ConfigurationManager> _configurationManager;
        AuthContext _context;
        Helper _helper;

        public DbInitializer(IOptions<ConfigurationManager> configurationManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            Helper helper,
            AuthContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configurationManager = configurationManager;
            _context = context;
            _helper = helper;
        }

        public void Initialize()
        {
            _context.Database.EnsureCreated();

            if (!_context.Clients.Any())
            {
                ApplicationUser user = new ApplicationUser { UserName = "admin" };

                var r = _userManager.CreateAsync(user, _configurationManager.Value.defaultAdminPsw).GetAwaiter().GetResult();

                if (r.Succeeded)
                {
                    _roleManager.CreateAsync(new IdentityRole { Name = ServerRoles.Admin }).GetAwaiter().GetResult();
                    _roleManager.CreateAsync(new IdentityRole { Name = ServerRoles.User }).GetAwaiter().GetResult();

                    _userManager.AddToRoleAsync(user, ServerRoles.Admin).GetAwaiter().GetResult();

                    _context.Clients.Add(new Client
                    {
                        Id = "ngAuth",
                        Secret = _helper.GetHash(_configurationManager.Value.ngAuthSecret),
                        Name = "Web App",
                        ApplicationType = ApplicationTypes.JavaScript,
                        Active = true,
                        AllowedOrigin = "http://localhost:8082/",
                        RefreshTokenLifeTime = 60 * 24
                    });


                    _context.Clients.Add(new Client
                    {
                        Id = "App",
                        Secret = _helper.GetHash(_configurationManager.Value.AppSecret),
                        Name = "Desktop App",
                        ApplicationType = ApplicationTypes.NativeConfidential,
                        Active = true,
                        AllowedOrigin = "*",
                        RefreshTokenLifeTime = 60 * 24
                    });

                    _context.SaveChanges();
                }
                else
                {
                    throw new Exception(string.Join("; ", r.Errors.Select(a => a.Description)));
                }
            }
        }
    }
}
