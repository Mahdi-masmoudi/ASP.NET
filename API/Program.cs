using API.Data;
using API.Repositories.Interfaces;
using API.Repositories.Implementations;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using System.Text;

namespace API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ----- DbContexts -----
            builder.Services.AddDbContext<OltpDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("OltpConnection"))
            );

            // ----- Repository Pattern & Unit of Work -----
            builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ----- Services -----
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IImageService, ImageService>();

            // ----- JWT Authentication -----
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]!;

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.IncludeErrorDetails = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");

                        var hasAuthHeader = context.Request.Headers.Authorization.Count > 0;
                        var scheme = hasAuthHeader
                            ? context.Request.Headers.Authorization.ToString().Split(' ', 2)[0]
                            : "(none)";

                        logger.LogError(context.Exception, "JWT authentication failed. Authorization header present={HasAuthHeader}; scheme={Scheme}",
                            hasAuthHeader,
                            scheme);

                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");

                        logger.LogWarning("JWT challenge. Error={Error}; ErrorDescription={ErrorDescription}; Authorization header present={HasAuthHeader}",
                            context.Error,
                            context.ErrorDescription,
                            context.Request.Headers.Authorization.Count > 0);

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");

                        logger.LogInformation("JWT validated for {NameIdentifier}",
                            context.Principal?.FindFirst("nameid")?.Value
                            ?? context.Principal?.FindFirst("sub")?.Value
                            ?? "(unknown)");

                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization();

            // ----- CORS -----
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader(); 
                });
            });

            // ----- Controllers & Swagger -----
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Entrez votre token JWT : Bearer {token}"
                });
                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            // ----- Seed SuperAdmin si la base est vide -----
            await DbInitializer.InitializeAsync(app.Services);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(@"C:\imagesAngular\assets\images"),
                RequestPath = "/assets/images"
            });

            app.UseStaticFiles(); // Servir les fichiers depuis wwwroot/assets/images
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
