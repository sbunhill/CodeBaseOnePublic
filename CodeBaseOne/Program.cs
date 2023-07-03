using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CodeBaseOne.EfCore;
using CodeBaseOne.Services.Concrete;
using CodeBaseOne.Services.Interfaces;
using System.Diagnostics;
using System.Text;
using CodeBaseOne.Middlewares;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();     

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CodeBase One - API base project",
        Description = "base API project with Authentication and Authorization built from scratch - with 1 addition demo entity",
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var appSettingsToken = builder.Configuration.GetSection("AppSettings:Token").Value;
if (appSettingsToken == null) { throw new Exception("AppSettings token is null"); }
TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettingsToken)),
    ValidateIssuer = false, // come back to this
    ValidateAudience = false,// come back to this
    ClockSkew = TimeSpan.FromSeconds(1) // expiry time variability issue
};

// because we want to be able to log remote client IP even when app is proxied 
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddSingleton(tokenValidationParameters);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

    .AddJwtBearer(options => {
        options.TokenValidationParameters = tokenValidationParameters;

        // we want to be able to easily access the bearer token without needing to inspect the headers
        options.SaveToken = true; 
    });


builder.Services.AddDbContext<EF_DataContext>(
    // config context to use Postgres provider
    o => o.UseNpgsql(builder.Configuration.GetConnectionString("CodeBaseOne"))
    );

// DI
builder.Services.AddTransient<IProductRepository, ProductRepository>();
builder.Services.AddTransient<IAuthRepository, AuthRepository>();
// we add the service for automapper - we register automapper and pass the assemblies as arguments
// those assemblies will then be scanned for profiles with mapping configurations
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // in Development the log file is created in AppData or in the Docker container itself
    // in Production - would want to decide where to log
    var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var tracePath = Path.Join(path, $"Log_CodeBaseOne_{DateTime.Now.ToString("yyyyMMdd-HHmm")}.txt");
    Trace.Listeners.Add(new TextWriterTraceListener(tracePath));
    Trace.AutoFlush = true;
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();
