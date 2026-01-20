using System.Reflection;
using System.Text;
using Accounts.Infrastructure;
using Accounts.Infrastructure.Persistence;
using AccountTransactions.Infrastructure;
using AccountTransactions.Infrastructure.Persistence;
using Api.Interfaces;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Transfers.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAccountsModule(builder.Configuration);
builder.Services.AddAccountTransactionsModule(builder.Configuration);
builder.Services.AddTransfersModule();

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Banking API",
        Version = "v1",
        Description = "A simple banking API for account management and transfers",
    });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
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
            Array.Empty<string>()
        }
    });
    
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

await ApplyMigrationsAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();


static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 5;
    
    for (int retry = 0; retry < maxRetries; retry++)
    {
        try
        {
            logger.LogInformation("Applying database migrations (attempt {Attempt}/{Max})...", 
                retry + 1, maxRetries);

            var accountDbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
            await accountDbContext.Database.MigrateAsync();

            var transactionDbContext = scope.ServiceProvider.GetRequiredService<AccountTransactionDbContext>();
            await transactionDbContext.Database.MigrateAsync();

            logger.LogInformation("Database migrations applied successfully");
            return;
        }
        catch (Exception ex) when (retry < maxRetries - 1)
        {
            logger.LogWarning(ex, "Migration failed, retrying in {Delay}s...", Math.Pow(2, retry));
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retry)));
        }
    }
}