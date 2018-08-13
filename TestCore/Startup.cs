using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLayer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            //конфиги из файла
            services.AddOptions();
            services.Configure<ConfigurationManager>(Configuration.GetSection("ConfigurationManager"));
            
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
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {            
            app.UseCors(builder => builder.AllowAnyOrigin());            
            app.UseDefaultFiles();
            app.UseStaticFiles();
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
