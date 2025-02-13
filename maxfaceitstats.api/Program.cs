using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using maxfaceitstats.api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for maxfaceitstats.com
builder.Services.AddCors(options =>
{
    options.AddPolicy("MaxFaceitStatsPolicy", policy =>
    {
        policy.WithOrigins("https://maxfaceitstats.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(origin => true); // Allow any origin during development
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
        // Configure JWT validation parameters
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Add your validation parameters here
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

// Use forwarded headers
app.UseForwardedHeaders();

// Always redirect to HTTPS in production
app.UseHttpsRedirection();

// Use CORS before routing
app.UseCors("MaxFaceitStatsPolicy");

app.UseAuthorization();
app.MapControllers();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.Run();
