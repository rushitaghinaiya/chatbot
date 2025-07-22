using ChatBot.Controllers;
using ChatBot.Models.Common;
using ChatBot.Models.Configuration;
using ChatBot.Models.Services;
using ChatBot.Repository;
using Microsoft.OpenApi.Models;
using VRMDBCommon2023;
using Serilog;
using ChatBot.Middleware;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Add services to the container
builder.Services.AddHttpClient<MedlinePlusController>();

builder.Services.AddTransient<IUserSignUp>(s => new UserSignupRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IQuestion>(s => new QuestionRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IAdmin>(s => new AdminRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IMedicine>(s => new MedicineRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IUser>(s => new UserRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IExceptionLog>(s => new ExceptionLogRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));

builder.Services.Configure<AppSettings>(configuration.GetSection("ApplicationSettings"));
builder.Services.Configure<MedicareConfig>(builder.Configuration.GetSection(MedicareConfig.SectionName));
builder.Services.AddSingleton(builder.Services.AddOptions<MedicareConfig>()
    .Bind(builder.Configuration.GetSection(MedicareConfig.SectionName))
    .ValidateDataAnnotations());

builder.Services.AddControllers();

// Swagger Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ChatBot API",
        Version = "v1",
        Description = "API for ChatBot application with SQL Server backend"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("allowCors", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Host.UseSerilog();

var app = builder.Build();

// ? Correct Order: PathBase BEFORE Swagger & others
app.UsePathBase("/api");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatBot API v1");
    c.RoutePrefix = "swagger";  // Available at /api/swagger
});

app.UseHttpsRedirection();
app.UseCors("allowCors");
//app.UseMiddleware<SessionTrackingMiddleware>();
//app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () =>
{
    Log.Information("Home page visited at {Time}", DateTime.Now);
    return "Hello, Serilog Logging!";
});

app.Run();
