using MiniInventoryApi.Models;
using MiniInventoryApi.Data;
using Microsoft.EntityFrameworkCore;

#region Variables

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

#endregion

#region Middleware

app.Use(async (context, next) =>
{
    var watch = System.Diagnostics.Stopwatch.StartNew();
    Console.WriteLine("Request started");
    
    await next();
    
    watch.Stop();
    Console.WriteLine($"Request completed in {watch.ElapsedMilliseconds}ms");
}); //log startTime, endTime. First middleware

app.Use(async (context, next) =>
{
    var truePassword = app.Configuration["Security:Password"];
    
    if (!context.Request.Headers.TryGetValue("Password", out var incomingPassword))
    {
        context.Response.StatusCode = 401;
        Console.WriteLine("Unauthorized request");
        return;
    }

    if (incomingPassword.ToString() != truePassword)
    {
        context.Response.StatusCode = 401;
        Console.WriteLine("Unauthorized request");
        return;
    }
    await next();
}); //auth middleware. Second middleware

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/admin"))
    {
        context.Response.StatusCode = 403;
        Console.WriteLine($"Admin request: {context.Request.Method} {context.Request.Path}");
        Console.WriteLine("Request aborted");
        return;
    }
    await next();
    Console.WriteLine("Api work is finished, returned");
}); // /admin request denied. Third middleware

#endregion

#region Endpoints

app.MapGet("/kategori", async (AppDbContext context) =>
{
    var kategoriler = await context.Kategoriler.ToListAsync();
    return Results.Ok(kategoriler);
}); // /kategori request -> add new category

app.MapPost("/kategori", async (AppDbContext context, Kategori Kategori) =>
{
    await context.Kategoriler.AddAsync(Kategori);
    await context.SaveChangesAsync();
    return Results.Created($"/kategori/{Kategori.Id}", Kategori);
});

app.MapGet("/durum", () =>
{
    if (app.Environment.IsDevelopment())
    {
        return Results.Ok("Development environment");
    }
    return Results.Ok("Production environment");
}); // /durum request -> development or production

app.MapGet("/urunler", async (AppDbContext context) =>
{
    var urunler = await context.Stoklar
                                        .Include(x => x.Kategori) 
                                        .ToListAsync();
    return Results.Ok(urunler);
}); // /urunler request -> return all products

app.MapPost("/urunler", async (AppDbContext context, Stok yeniUrun, IConfiguration config) =>
{
    int mevcutSayi = await context.Stoklar.CountAsync();
    int limit = config.GetValue<int>("Rules:MaxAmount");

    if (mevcutSayi >= limit)
        return Results.BadRequest("Limit doldu!");

    var kategoriVarMi = await context.Kategoriler.AnyAsync(k => k.Id == yeniUrun.KategoriId);
    if (!kategoriVarMi)
    {
        return Results.BadRequest($"HATA: {yeniUrun.KategoriId} ID'li bir kategori bulunamadı! Önce kategori ekleyin.");
    }

    await context.Stoklar.AddAsync(yeniUrun);
    await context.SaveChangesAsync();

    return Results.Created($"/urunler/{yeniUrun.Id}", yeniUrun);
}); // /urunler request -> add new product

app.MapDelete("/urunler", async (AppDbContext context) =>
{
    await context.Stoklar.ExecuteDeleteAsync();
    return Results.NoContent();
}); // /urunler request -> delete all products

#endregion

app.Run();