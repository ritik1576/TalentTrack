using Microsoft.EntityFrameworkCore;
using TalentTrack.Data;
using TalentTrack.Models;
using TalentTrack.Services;

var builder = WebApplication.CreateBuilder(args);

// Register PDFSharp Font Resolver
try
{
    PdfSharp.Fonts.GlobalFontSettings.FontResolver = new WindowsFontResolver();
}
catch (System.Exception)
{
    // Ignore if already set
}


// Add services to the container.
builder.Services.AddControllersWithViews();

// Database Connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ).UseSnakeCaseNamingConvention());

// Configure Email Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Register Email Service
builder.Services.AddTransient<IEmailService, EmailService>();

// Add Session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();   // must be before UseAuthorization

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}")
    .WithStaticAssets();

app.Run();