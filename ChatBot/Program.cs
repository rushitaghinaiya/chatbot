using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Repository;
using VRMDBCommon2023;

var builder = WebApplication.CreateBuilder(args);

ConfigurationManager configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddTransient<IUserSignUp>(s => new UserSignupRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IQuestion>(s => new QuestionRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IAdmin>(s => new AdminRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.AddTransient<IUser>(s => new UserRepository(configuration["ConnectionStrings:ChatbotDB"].ReturnString()));
builder.Services.Configure<AppSettings>(configuration.GetSection("ApplicationSettings"));
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
