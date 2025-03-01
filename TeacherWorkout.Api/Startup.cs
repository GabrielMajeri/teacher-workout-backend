using System;
using System.Linq;
using GraphQL.Server;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeacherWorkout.Api.GraphQL;
using TeacherWorkout.Data;

namespace TeacherWorkout.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                    {
                        builder.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });

            services.AddControllers();

            services.AddSingleton<TeacherWorkoutQuery>();
            services.AddSingleton<TeacherWorkoutMutation>();
            services.AddSingleton<ISchema, TeacherWorkoutSchema>();
            AddGraphQLNamespaces(services);

            services.AddHttpContextAccessor();
            services.AddGraphQL(options =>
                {
                    options.EnableMetrics = true;
                })
                .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = true)
                .AddSystemTextJson();

            services.AddDbContext<TeacherWorkoutContext>(options =>
            options.UseNpgsql(Configuration.GetConnectionString("TeacherWorkoutContext")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, TeacherWorkoutContext db)
        {
            app.UseCors();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHttpsRedirection();
            }

            db.Database.Migrate();

            app.UseGraphQL<ISchema>();
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static void AddGraphQLNamespaces(IServiceCollection services)
        {
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsClass)
                .Where(t => t.Namespace != null && t.Namespace.StartsWith("TeacherWorkout.Api.GraphQL.Types"))
                .ToList()
                .ForEach(t => services.AddSingleton(t));
        }
    }
}
