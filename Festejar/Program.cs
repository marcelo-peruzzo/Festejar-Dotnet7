using Festejar.Context;
using Festejar.Respositories.Interfaces;
using Festejar.Respositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Configura a sessão aqui
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	options.Cookie.Name = ".YourApp.Session";
	options.IdleTimeout = TimeSpan.FromSeconds(3600); // Sessão expira após 1 hora
	options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddRazorPages();

// Configurando Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false).AddEntityFrameworkStores<AppDbContext>();

//builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<AppDbContext>()
//    .AddDefaultTokenProviders();




// Register as Interfaces do contexto DB
builder.Services.AddScoped<ICidadesRepository, CidadesRepository>();
builder.Services.AddScoped<ICasasRepository, CasasRepository>();
builder.Services.AddScoped<IDiariasRepository, DiariasRepository>();
builder.Services.AddScoped<IImagens_casasRepository, Imagens_casasRepository>();

// implementação da interface IEmailSender como um serviço
builder.Services.AddTransient<IEmailSender, EmailSender>();

var app = builder.Build();

app.UseSession();

// Ensure the database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
