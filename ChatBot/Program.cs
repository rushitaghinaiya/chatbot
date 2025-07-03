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

builder.Services.AddTransient<IUserSignUp>(s => new UserSignupRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IQuestion>(s => new QuestionRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IAdmin>(s => new AdminRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IMedicine>(s => new MedicineRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));

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
        Title = "Medicare Knowledge Base API",
        Version = "v1",
        Description = "API for managing Medicare knowledge base files and Q&A operations"
        //// Include XML comments for better documentation
        //    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        //    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        //    if (File.Exists(xmlPath))
        //    {
        //        c.IncludeXmlComments(xmlPath);
        //    }
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    ;

});

// Include XML comments for better documentation

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
