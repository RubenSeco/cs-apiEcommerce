using System.Text;
using ApiEcommerce.Constants;
using ApiEcommerce.Repository;
using ApiEcommerce.Repository.IRepository;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var dbConnectionString = builder.Configuration.GetConnectionString("ConexionSql");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(dbConnectionString));

builder.Services.AddResponseCaching(options =>
{
  options.MaximumBodySize = 1024 * 1024;
  options.UseCaseSensitivePaths = true;
}
);
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddAutoMapper(typeof(Program).Assembly);

var secretKey = builder.Configuration.GetValue<string>("ApiSettings:SecretKey");

if (string.IsNullOrEmpty(secretKey))
{
  throw new InvalidOperationException("SecretKey no configurada");
}

builder.Services.AddAuthentication(options =>
{
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
  options.RequireHttpsMetadata = false;
  options.SaveToken = true;
  // options.Authority = "https://localhost:7280/";
  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
    ValidateIssuer = false,
    ValidateAudience = false,
  };
});

builder.Services.AddControllers(options =>
{
  options.CacheProfiles.Add(CacheProfiles.Cache1, CacheProfiles.Profile1);
  options.CacheProfiles.Add(CacheProfiles.Cache2, CacheProfiles.Profile2);
}

);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(
options =>
{
  options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Description = "Nuestra API utiliza la Autenticación JWT usando el esquema Bearer. \n\r\n\r" +
                  "Ingresa la palabra a continuación el token generado en login.\n\r\n\r" +
                  "Ejemplo: \"12345abcdef\"",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.Http,
    Scheme = "Bearer"
  });
  options.AddSecurityRequirement(new OpenApiSecurityRequirement()
  {
      {
        new OpenApiSecurityScheme
        {
          Reference = new OpenApiReference
          {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
          },
          Scheme = "oauth2",
          Name = "Bearer",
          In = ParameterLocation.Header
        },
        new List<string>()
      }
  });
  options.SwaggerDoc("v1", new OpenApiInfo
  {
    Version = "v1",
    Title = "Api Ecommerce",
    Description = "API para gestionar productos y usuarios",
    TermsOfService = new Uri("http://example.com/terms"),
    Contact = new OpenApiContact
    {
      Name = "Desarrollador",
      Url = new Uri("htttps://devtalles.com")
    },
    License = new OpenApiLicense
    {
      Name = "MIT",
      Url = new Uri("https://opensource.org/license/mit/")
    }
  });
  options.SwaggerDoc("v2", new OpenApiInfo
  {
    Version = "v2",
    Title = "Api Ecommerce V2",
    Description = "API para gestionar productos y usuarios",
    TermsOfService = new Uri("http://example.com/terms"),
    Contact = new OpenApiContact
    {
      Name = "Desarrollador",
      Url = new Uri("htttps://devtalles.com")
    },
    License = new OpenApiLicense
    {
      Name = "MIT",
      Url = new Uri("https://opensource.org/license/mit/")
    }
  });
}
);

var apiVersioningBuilder = builder.Services.AddApiVersioning(option =>
{
  option.AssumeDefaultVersionWhenUnspecified = true;
  option.DefaultApiVersion = new ApiVersion(1, 0);
  option.ReportApiVersions = true;
  // option.ApiVersionReader = ApiVersionReader.Combine(new QueryStringApiVersionReader("api-version"));

});

apiVersioningBuilder.AddApiExplorer(option =>
{
  option.GroupNameFormat = "'v'VVV"; //v1, v2, v3, ...
  option.SubstituteApiVersionInUrl = true; // api/v{version}/products

});

builder.Services.AddCors(options =>
{
  options.AddPolicy(PolicyNames.AllowCors, builder =>
  {
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
  });
});
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI(options =>
  {
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiEcommerce v1");
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "ApiEcommerce v2");
  });
}
app.UseHttpsRedirection();
app.UseCors(PolicyNames.AllowCors);
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
