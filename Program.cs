using Microsoft.EntityFrameworkCore;
using TWTodos.Data;
using TWTodos.Services; // Add this
using Microsoft.AspNetCore.Authentication.JwtBearer; // Add this
using Microsoft.IdentityModel.Tokens; // Add this
using System.Text; // Add this
using Microsoft.AspNetCore.Identity; // For IPasswordHasher, PasswordHasher
using TWTodos.Models;             // For Usuario (Adjust if your namespace is different)
using TWTodos.Data;               // For CajuTalkContext (Likely needed for AddDbContext)
using TWTodos.Services;           // For ITokenService, TokenService
using Microsoft.AspNetCore.Authentication.JwtBearer; // For JWT Authentication setup
using Microsoft.IdentityModel.Tokens; // For SymmetricSecurityKey, TokenValidationParameters
using System.Text;                // For Encoding
// Add other necessary usings like Microsoft.EntityFrameworkCore

var builder = WebApplication.CreateBuilder(args);

// ... other builder setup ...

// *** Register ITokenService ***
builder.Services.AddScoped<ITokenService, TokenService>();

// *** Configure JWT Authentication (Needed for validating tokens later) ***
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
    });

// Add Authorization services (usually added by default, but ensure it's there)
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<CajuTalkContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

var app = builder.Build();

app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();