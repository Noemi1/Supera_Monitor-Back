using System.Globalization;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Supera_Monitor_Back.CRM4U;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Middlewares;
using Supera_Monitor_Back.Models.Email;
using Supera_Monitor_Back.Services;
using Supera_Monitor_Back.Services.Email;
using Supera_Monitor_Back.Services.Eventos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    })
    .AddNewtonsoftJson(x =>
    {
        x.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        x.SerializerSettings.Culture = new CultureInfo("pt-BR", false);
    });
builder.Services.AddHttpContextAccessor();

#region SQL

builder.Services.AddDbContext<DataContext>();
builder.Services.AddDbContext<_CRM4UContext>();

#endregion

builder.Services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);

#region SERVICES
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILogService, LogService>();

builder.Services.AddScoped<IEmailTemplateFactory, EmailTemplateFactory>();

builder.Services.AddScoped<IAlunoService, AlunoService>();
builder.Services.AddScoped<IChecklistService, ChecklistService>();
builder.Services.AddScoped<IJornadaSuperaService, JornadaSuperaService>();
builder.Services.AddScoped<IListaEsperaService, ListaEsperaService>();
builder.Services.AddScoped<IPessoaService, PessoaService>();
builder.Services.AddScoped<IProfessorService, ProfessorService>();
builder.Services.AddScoped<IRoteiroService, RoteiroService>();
builder.Services.AddScoped<IRestricaoService, RestricaoService>();
builder.Services.AddScoped<ISalaService, SalaService>();
builder.Services.AddScoped<ITurmaService, TurmaService>();

builder.Services.AddScoped<ICalendarioService, CalendarioService>();
builder.Services.AddScoped<IAulaService, AulaService>();
builder.Services.AddScoped<IEventoService, EventoService>();
builder.Services.AddScoped<IMonitoramentoService, MonitoramentoService>();

#endregion

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment()) {
app.UseSwagger();
app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors(option => option
    .SetIsOriginAllowed(x => true)
    .WithOrigins("http://localhost:4200", "https://localhost:4200", "https://supera-monitor-front.vercel.app", "https://supera-monitor-back-e4hwhteuewdmd8ea.canadacentral-01.azurewebsites.net")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()
    .WithExposedHeaders("Content-Disposition")
);

app.UseAuthorization();

app.MapControllers();

app.UseMiddleware<JwtMiddleware>();

app.Run();

public partial class Program { }
