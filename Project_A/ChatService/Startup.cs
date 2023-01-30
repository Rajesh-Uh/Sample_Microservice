using Microsoft.EntityFrameworkCore;
using Precision.Data.Context;
using Precision.Providers.Interfaces.ChatService;
using Precision.Providers.Services.ChatService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Serilog;

internal class Startup
{
    private static void Main(string[] args)
    {
        var appConfiguration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .AddJsonFile($"appsettings.json", true)
            .Build();

        var builder = WebApplication.CreateBuilder(args);
        var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        var logger = new LoggerConfiguration().Enrich
                .FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

        builder.Services.AddLogging(lb => lb.AddSerilog(logger));

        builder.Services.AddDbContext<PrecisionDbContext>(
            options => SqlServerDbContextOptionsExtensions
                .UseSqlServer(options, appConfiguration["ConnectionString"])
                .EnableSensitiveDataLogging()
        );

        builder.Services.AddScoped<IChatService, ChatProvider>();
        builder.Services.AddScoped<IChatGroupService, ChatGroupProvider>();

        builder.Services.AddMemoryCache();
        builder.Services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Precision APIs",
                    Description = "",
                    Version = "v1"
                });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please Insert Token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
            });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: MyAllowSpecificOrigins,
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:5000")
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();
                    });
        });

        var securitykey = builder.Configuration.GetValue<string>("JwtConfig:Securitykey");
        builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }
            ).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = true;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securitykey)),
                    ValidateAudience = false,
                    ValidateIssuer = false
                };
            });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
                    {
                        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                        options.RoutePrefix = string.Empty;
                    });
            app.UseCors(MyAllowSpecificOrigins);
        }
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrecisionDbContext>();
            db.Database.Migrate();
        }
        app.UseAuthentication();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.UseStaticFiles();
        app.MapControllers();
        app.Run();
    }
}