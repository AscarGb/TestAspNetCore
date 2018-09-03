using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLayer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Types;

namespace TestCore
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration config)
        {
            Configuration = config;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            IConfigurationSection authOptionsSection = Configuration.GetSection("AuthOptions");

            //конфиги из файла
            services.AddOptions();
            services.Configure<ConfigurationManager>(Configuration.GetSection("ConfigurationManager"));
            services.Configure<AuthOptions>(authOptionsSection);

            services.AddTransient<Helper>();

            services.AddTransient<DbInitializer>();

            //внедрение контекста бд
            services.AddDbContext<AuthContext>(options =>
               options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            //внедрение пользователя и ролей
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AuthContext>();

            //внедрение репозитория данных
            services.AddTransient<IAuthRepository, AuthRepository>();

            services.AddCors();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                AuthOptions authOptions = authOptionsSection.Get<AuthOptions>();

                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // укзывает, будет ли валидироваться издатель при валидации токена
                    ValidateIssuer = false,
                    // строка, представляющая издателя
                    ValidIssuer = authOptions.Issuer,

                    // будет ли валидироваться потребитель токена
                    ValidateAudience = false,
                    // установка потребителя токена
                    ValidAudience = authOptions.Audience,
                    // будет ли валидироваться время существования
                    ValidateLifetime = true,

                    // установка ключа безопасности
                    IssuerSigningKey = authOptions.GetSymmetricSecurityKey(),
                    // валидация ключа безопасности
                    ValidateIssuerSigningKey = true,                    
                };
            });

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors(builder => builder.AllowAnyOrigin());
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseMvc();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
