using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MatBlazorLab.Data;
using AutoMapper;
using MatBlazorLab.Helpers;
using MatBlazorLab.Services;
using MatBlazorLab.Adaptors;
using EFCoreModel.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace MatBlazorLab
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<WeatherForecastService>();

            services.AddDbContext<SchoolContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("SchoolContext")));

            // Server Side Blazor doesn't register HttpClient by default
            if (!services.Any(x => x.ServiceType == typeof(HttpClient)))
            {
                // Setup HttpClient for server side in a client side compatible fashion
                services.AddScoped<HttpClient>(s =>
                {
                    // Creating the URI helper needs to wait until the JS Runtime is initialized, so defer it.
                    var uriHelper = s.GetRequiredService<NavigationManager>();
                    return new HttpClient
                    {
                        BaseAddress = new Uri(uriHelper.BaseUri)
                    };
                });
            }
            services.AddAutoMapper(c => c.AddProfile<AutoMapping>(), typeof(Startup));

            RegisterInspectionService(services);
        }

        private static void RegisterInspectionService(IServiceCollection services)
        {
            #region ���U�A��
            services.AddScoped<IPersonService, PersonService>();
            services.AddScoped<PersonAdaptor>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<DepartmentAdaptor>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<CourseAdaptor>();
            services.AddScoped<IOutlineService, OutlineService>();
            services.AddScoped<OutlineAdaptor>();
            services.AddScoped<IStudentGradeService, StudentGradeService>();
            services.AddScoped<StudentGradeAdaptor>();
            #endregion

            #region ���U Razor Model
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
