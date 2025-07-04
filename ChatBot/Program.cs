using ChatBot.Controllers;
using ChatBot.Models.Common;
using ChatBot.Models.Configuration;
using ChatBot.Models.Services;
using ChatBot.Repository;
using Microsoft.OpenApi.Models;
using VRMDBCommon2023;
using Serilog;


var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;
// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Add services to the container.
builder.Services.AddHttpClient<MedlinePlusController>();

// Register repositories with SQL Server connection string
builder.Services.AddTransient<IUserSignUp>(s => new UserSignupRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IQuestion>(s => new QuestionRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IAdmin>(s => new AdminRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IMedicine>(s => new MedicineRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IUser>(s => new UserRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IExceptionLog>(s => new ExceptionLogRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));

builder.Services.Configure<AppSettings>(configuration.GetSection("ApplicationSettings"));
builder.Services.Configure<MedicareConfig>(
    builder.Configuration.GetSection(MedicareConfig.SectionName));

// Remove the using alias and call ValidateOnStart directly

var medicareConfigOptions = builder.Services.AddOptions<MedicareConfig>()
    .Bind(builder.Configuration.GetSection(MedicareConfig.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddSingleton(medicareConfigOptions);

builder.Services.AddControllers();

// Swagger/OpenAPI configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ChatBot API",
        Version = "v1",
        Description = "API for ChatBot application with SQL Server backend"
    });

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("allowCors", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Host.UseSerilog();  // Integrate Serilog into Host

var app = builder.Build();

// Configure the HTTP request pipeline
// Always enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatBot API V1");
    c.RoutePrefix = "swagger"; // This makes Swagger available at /swagger
});

app.UseHttpsRedirection();
app.UseCors("allowCors");
app.UseAuthorization();
app.MapControllers();
// Basic route for testing logging
app.MapGet("/", () =>
{
    Log.Information("Home page visited at {Time}", DateTime.Now);
    return "Hello, Serilog Logging!";
});
app.Run();
