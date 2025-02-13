using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using maxfaceitstats.api.Middleware;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure CORS for maxfaceitstats.com
builder.Services.AddCors(options =>
{
    options.AddPolicy("MaxFaceitStatsPolicy", policy =>
    {
        policy.WithOrigins("https://www.maxfaceitstats.com")
            .WithMethods("POST")  // Only allow POST for token endpoint
            .WithHeaders("Content-Type")
            .DisallowCredentials();
    });
});

// Configure forwarded headers for Azure App Service
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Add this after your existing services
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["JWT:SigningKey"] ?? throw new Exception("JWT:SigningKey is not configured");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "https://maxfaceitstats-api.azurewebsites.net",
            ValidAudience = "https://www.maxfaceitstats.com",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHsts();

// Use forwarded headers
app.UseForwardedHeaders();

// Always redirect to HTTPS in production
app.UseHttpsRedirection();

// Use CORS before routing
app.UseCors("MaxFaceitStatsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.Run();
