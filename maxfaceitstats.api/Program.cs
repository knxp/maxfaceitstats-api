using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using maxfaceitstats.api.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("MaxFaceitStatsPolicy", policy =>
    {
        policy.WithOrigins("https://www.maxfaceitstats.com")
            .WithMethods("POST", "OPTIONS")  // Add OPTIONS for preflight
            .WithHeaders("Content-Type", "Authorization") // Add Authorization
            .DisallowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cache preflight
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

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

if (app.Environment.IsProduction())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Check Azure configuration" });
        });
    });
}

app.Run();
