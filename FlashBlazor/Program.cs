using Blazored.LocalStorage;
using FlashBlazor;
using FlashBlazor.Components;
using FlashBlazor.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;

#region 应用构建器与配置

//如果是开发环境则使用开发环境配置
var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

var baseDirectory = Path.GetDirectoryName(AppContext.BaseDirectory)!;

if (isDevelopment)
{
    baseDirectory = Path.Combine(Directory.GetCurrentDirectory());
}

// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(baseDirectory, "Logs/AllLogs/Log.txt"), rollingInterval: RollingInterval.Day)
    .WriteTo.File(Path.Combine(baseDirectory, "Logs/Information/Log-Information-.txt"),
        restrictedToMinimumLevel: LogEventLevel.Information, rollingInterval: RollingInterval.Day)
    .WriteTo.File(Path.Combine(baseDirectory, "Logs/Warning/Log-Warning-.txt"),
        restrictedToMinimumLevel: LogEventLevel.Warning, rollingInterval: RollingInterval.Day)
    .WriteTo.File(Path.Combine(baseDirectory, "Logs/Error/Log-Error-.txt"),
        restrictedToMinimumLevel: LogEventLevel.Error, rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = baseDirectory,
});

// 使用Serilog作为日志提供程序
builder.Host.UseSerilog();

#endregion

#region 响应压缩配置

builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    // 这里可以添加更多 MIME 类型 
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
        { "text/plain", "text/html", "application/json" });
});

#endregion

#region 跨域设置配置

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        corsPolicyBuilder => corsPolicyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

#endregion

#region JWT配置

var section = builder.Configuration.GetSection("TokenOptions");
var tokenOptions = section.Get<TokenOptions>()!;

// 检查配置并生成随机值
if (string.IsNullOrEmpty(tokenOptions.SecretKey))
    tokenOptions.SecretKey = Guid.NewGuid().ToString();
if (string.IsNullOrEmpty(tokenOptions.Issuer))
    tokenOptions.Issuer = Guid.NewGuid().ToString();
if (string.IsNullOrEmpty(tokenOptions.Audience))
    tokenOptions.Audience = Guid.NewGuid().ToString();

builder.Services.Configure<TokenOptions>(options =>
{
    options.SecretKey = tokenOptions.SecretKey;
    options.Issuer = tokenOptions.Issuer;
    options.Audience = tokenOptions.Audience;
    options.ExpireMinutes = tokenOptions.ExpireMinutes;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

#endregion

#region 依赖注入

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // 如果在开发环境中使用了代理服务器，需要添加下面的代码以防止环回网络地址的错误
    // options.KnownNetworks.Clear();
    // options.KnownProxies.Clear();
});

// 将服务添加到容器中。
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();

#endregion

#region 数据库连接配置

var databaseOptions = (builder.Configuration.GetSection("DefaultConnection").Get<string>() ?? "").ToLower();

if (databaseOptions == nameof(DatabaseType.Mysql).ToLower())
{
    Console.WriteLine("使用MySQL数据库");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(builder.Configuration.GetConnectionString("MySqlConnection"),
            ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MySqlConnection"))));
}
else if (databaseOptions == nameof(DatabaseType.Postgresql).ToLower())
{
    Console.WriteLine("使用PostgreSQL数据库");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSqlConnection")));
}
else if (databaseOptions == nameof(DatabaseType.Sqlite).ToLower())
{
    Console.WriteLine("使用Sqlite数据库");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")));
}
else if (databaseOptions == nameof(DatabaseType.Sqlserver).ToLower())
{
    Console.WriteLine("使用SqlServer数据库");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection")));
}
else
{
    Console.WriteLine("使用Sqlite数据库");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={Path.Combine(baseDirectory, "App.db")}"));
}

#endregion

#region Scrutor自动注入

builder.Services.Scan(scan => scan
        // 指定要扫描的程序集（假设服务和仓库都在当前程序集）
        .FromAssemblyOf<Program>()

        // 自动注册 MarkerAddScoped 特性的类，作为 Scoped 服务
        .AddClasses(classes => classes.WithAttribute<AddScopedAttribute>())
        .AsSelf()
        .WithScopedLifetime()

        // 自动注册 MarkerAddScopedAsImplementedInterfaces 特性的类，作为 Scoped 服务，并作为实现的接口注册
        .AddClasses(classes => classes.WithAttribute<AddScopedAsImplementedInterfacesAttribute>())
        .AsImplementedInterfaces()
        .WithScopedLifetime()

        // 自动注册 MarkerAddTransient 特性的类，作为 Transient 服务
        .AddClasses(classes => classes.WithAttribute<AddTransientAttribute>())
        .AsSelf()
        .WithTransientLifetime()

        // 自动注册 MarkerAddTransientAsImplementedInterfaces 特性的类，作为 Transient 服务，并作为实现的接口注册
        .AddClasses(classes => classes.WithAttribute<AddTransientAsImplementedInterfacesAttribute>())
        .AsImplementedInterfaces()
        .WithTransientLifetime()

        // 自动注册 MarkerAddSingleton 特性的类，作为 Singleton 服务
        .AddClasses(classes => classes.WithAttribute<AddSingletonAttribute>())
        .AsSelf()
        .WithSingletonLifetime()

        // 自动注册 MarkerAddSingletonAsImplementedInterfaces 特性的类，作为 Singleton 服务，并作为实现的接口注册
        .AddClasses(classes => classes.WithAttribute<AddSingletonAsImplementedInterfacesAttribute>())
        .AsImplementedInterfaces()
        .WithSingletonLifetime()

        // 自动注册 MarkerAddHostedService 特性的类，作为 HostedService 服务
        .AddClasses(classes => classes.WithAttribute<AddHostedServiceAttribute>())
        .As<IHostedService>()
        .WithSingletonLifetime()
    );

#endregion

var app = builder.Build();

#region 使用响应压缩中间件

app.UseResponseCompression();

#endregion

#region 基础中间件配置

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

#endregion

#region 数据库初始化

using var serviceScope = app.Services.CreateScope();
var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
context.Database.EnsureCreated();

#endregion

#region 错误路由配置

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithRedirects("/Error/{0}");

#endregion

#region 前端配置

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

#endregion

app.Run();

// 关闭和刷新日志
Log.CloseAndFlush();
