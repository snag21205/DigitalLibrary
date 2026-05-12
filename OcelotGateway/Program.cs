using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using MMLib.SwaggerForOcelot.DependencyInjection; // Thêm dòng này

var builder = WebApplication.CreateBuilder(args);

// Cấu hình CORS cho Blazor
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("http://localhost:5500")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Load file ocelot.json
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Đăng ký Ocelot VÀ Swagger cho Ocelot
builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddSwaggerForOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors("AllowBlazor");

// QUAN TRỌNG: Thứ tự middleware
app.UseSwagger();                               // 1. Tạo swagger.json
app.UseSwaggerForOcelotUI();                    // 2. Giao diện Swagger cho Gateway (thay thế UseSwaggerUI mặc định)
await app.UseOcelot();                          // 3. Middleware Ocelot

app.Run();