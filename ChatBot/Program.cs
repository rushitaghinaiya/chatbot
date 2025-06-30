using ChatBot.Controllers;
using ChatBot.Models.Common;
using ChatBot.Models.Configuration;
using ChatBot.Models.Services;
using ChatBot.Repository;
using Microsoft.OpenApi.Models;
using VRMDBCommon2023;

var builder = WebApplication.CreateBuilder(args);

ConfigurationManager configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddHttpClient<MedlinePlusController>();
builder.Services.AddTransient<IUserSignUp>(s => new UserSignupRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IQuestion>(s => new QuestionRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IAdmin>(s => new AdminRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
//builder.Services.AddTransient<IUser>(s => new UserRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.Configure<AppSettings>(configuration.GetSection("ApplicationSettings"));
builder.Services.Configure<MedicareConfig>(
builder.Configuration.GetSection(MedicareConfig.SectionName));
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
// Validate configuration on startup
builder.Services.AddOptions<MedicareConfig>()
    .Bind(builder.Configuration.GetSection(MedicareConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Include XML comments for better documentation

// CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("allowCors", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Your Angular app's origin
               .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// **** IMPORTANT: CORS must be placed here ****
app.UseCors("allowCors");
app.UseAuthorization();

app.MapControllers();

app.Run();
