using System.Text;
using System.Text.Json.Serialization;
using BloggingPlatformApi;
using BloggingPlatformApi.Data;
using BloggingPlatformApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddRoles<IdentityRole>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))

        };
    });

builder.Services.AddSwaggerGen(swagger =>
{
    swagger.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Blogging Platform Api",
    });
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme

            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }, Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminAuthorModeratorUserPolicy", o =>
    {
        o.RequireAuthenticatedUser();
        o.RequireRole("admin", "author", "moderator", "user");
    })
    .AddPolicy("AdminAuthorModeratorPolicy", o =>
    {
        o.RequireAuthenticatedUser();
        o.RequireRole("admin", "author", "moderator");
    })
    .AddPolicy("AdminAuthorPolicy", o =>
    {
        o.RequireAuthenticatedUser();
        o.RequireRole("admin", "author");
    })
    .AddPolicy("AdminModeratorPolicy", o =>
    {
        o.RequireAuthenticatedUser();
        o.RequireRole("admin", "moderator");
    })
    .AddPolicy("UserPolicy", o =>
    {
        o.RequireAuthenticatedUser();
        o.RequireRole("user");
    })
    .AddPolicy("ModeratorPolicy", o =>
    {
        o.RequireAuthenticatedUser();
        o.RequireRole("moderator");
    })
    .AddPolicy("AuthorPolicy", o =>
    {
        o.RequireAuthenticatedUser();
        o.RequireRole("author");
    })
    .AddPolicy("AdminPolicy", o =>
    {
        o.RequireAuthenticatedUser();
        o.RequireRole("admin");
    });

builder.Services.AddScoped<AuthService>();
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(app => app.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationsHub");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
    RequestPath = "/static"
});

app.Run();
