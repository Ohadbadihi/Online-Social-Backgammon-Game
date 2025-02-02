using FinalProjApi.Data;
using FinalProjApi.Hubs;
using FinalProjApi.Models;
using FinalProjApi.Repository.UserRpository;
using FinalProjApi.Service;
using FinalProjApi.Service.Game;
using FinalProjApi.Service.TokenJwt;
using FinalProjApi.Service.UserLogic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"]!;
builder.Services.AddDbContext<DataBaseContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyReact", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials()
              .WithOrigins("http://localhost:5173");
    });
});


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!))
    };


    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["JWT"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<IGameService>(sp => new GameService(sp));

builder.Services.AddHostedService<InviteToPlayCleanUpService>();

builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsStaging() || app.Environment.IsProduction())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    using (var scope = app.Services.CreateScope())
    {
        var dataBaseContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
        dataBaseContext.Database.EnsureDeleted();
        dataBaseContext.Database.EnsureCreated();
    }

}

app.UseHttpsRedirection();

app.UseCors("AllowMyReact");

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.MapHub<HomeHub>("/homeHub");
app.MapHub<ChatHub>("/chatHub");
app.MapHub<GameHub>("/gamehub");

app.Run();
