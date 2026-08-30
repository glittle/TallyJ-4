using System.Text;
using Backend.Identity;
using Backend.Middleware;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

/// <summary>
/// Authentication, identity, authorization, and Swagger setup.
/// Extracted from Program.cs top-level local functions — no behavior change.
/// </summary>
public static class ProgramAuthSetup
{
    public static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var path = context.HttpContext.Request.Path;
                    var accessToken = context.Request.Query["access_token"];

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                        return Task.CompletedTask;
                    }

                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader) &&
                        authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader["Bearer ".Length..].Trim();
                        return Task.CompletedTask;
                    }

                    if (SecureCookieMiddleware.IsVoterScopedPath(path))
                    {
                        var voterCookie = context.Request.Cookies[SecureCookieMiddleware.VoterTokenCookieName];
                        if (!string.IsNullOrEmpty(voterCookie))
                        {
                            context.Token = voterCookie;
                            return Task.CompletedTask;
                        }
                    }

                    var tokenCookie = context.Request.Cookies[SecureCookieMiddleware.AccessTokenCookieName];
                    if (!string.IsNullOrEmpty(tokenCookie))
                    {
                        context.Token = tokenCookie;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        var googleClientSecret = configuration["GoogleClientSecret"]; // server only
        var googleClientId = configuration["ClientEnv:googleClientId"]; // client as well

        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret)
            && !googleClientId.StartsWith('<') && !googleClientSecret.StartsWith('<'))
        {
            services.AddAuthentication()
                .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    options.CallbackPath = "/signin-google";
                    options.SaveTokens = true;
                    options.SignInScheme = IdentityConstants.ExternalScheme;
                    options.Events.OnRemoteFailure = context =>
                    {
                        context.Response.Redirect("/login?error=" + context.Failure?.Message);
                        context.HandleResponse();
                        return Task.CompletedTask;
                    };
                });
            Log.Information("Google authentication configured successfully");
        }
        else
        {
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            if (isDevelopment)
            {
                Log.Warning("Google authentication not configured - ClientId or ClientSecret is missing or using placeholder values. Google login will not be available.");
            }
            else
            {
                Log.Information("Google authentication not configured. Google login will not be available.");
            }
        }
    }

    public static void ConfigureIdentityOptions(IServiceCollection services)
    {
        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 12;
            options.Password.RequiredUniqueChars = 1;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
        });
    }

    public static void ConfigureAuthorization(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("ElectionAccess", policy =>
                policy.Requirements.Add(new Backend.Authorization.ElectionAccessRequirement()));

            options.AddPolicy("TellerAccess", policy =>
                policy.Requirements.Add(new Backend.Authorization.TellerAccessRequirement()));

            options.AddPolicy("HeadTellerAccess", policy =>
                policy.Requirements.Add(new Backend.Authorization.HeadTellerAccessRequirement()));

            options.AddPolicy("FullTellerAccess", policy =>
                policy.Requirements.Add(new Backend.Authorization.FullTellerAccessRequirement()));

            options.AddPolicy("SuperAdmin", policy =>
                policy.Requirements.Add(new Backend.Authorization.SuperAdminRequirement()));

            options.AddPolicy("OnlineVoter", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("voterType", "online")
                      .RequireClaim("voterId"));
        });

        services.AddScoped<IAuthorizationHandler, Backend.Authorization.ElectionAccessHandler>();
        services.AddScoped<IAuthorizationHandler, Backend.Authorization.TellerAccessHandler>();
        services.AddScoped<IAuthorizationHandler, Backend.Authorization.HeadTellerAccessHandler>();
        services.AddScoped<IAuthorizationHandler, Backend.Authorization.FullTellerAccessHandler>();
        services.AddScoped<IAuthorizationHandler, Backend.Authorization.SuperAdminHandler>();
    }

    public static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.UseAllOfForInheritance();
            options.CustomSchemaIds(type =>
            {
                static string GetSchemaId(Type t)
                {
                    if (!t.IsGenericType)
                    {
                        var ns = t.Namespace?.Split('.').LastOrDefault();
                        return string.IsNullOrEmpty(ns) ? t.Name : $"{ns}_{t.Name}";
                    }

                    var typeName = t.Name.Substring(0, t.Name.IndexOf('`'));
                    var genericArgs = string.Join("", t.GetGenericArguments().Select(GetSchemaId));
                    return $"{typeName}{genericArgs}";
                }

                return GetSchemaId(type);
            });

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TallyJ4 API",
                Version = "v1",
                Description = "Election management and vote tallying system API"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
            });

            options.DocInclusionPredicate((docName, apiDesc) =>
            {
                return apiDesc.RelativePath?.StartsWith("api/") == true;
            });
        });
    }
}
