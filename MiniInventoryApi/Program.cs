using MiniInventoryApi.Models;
using MiniInventoryApi.Services;

#region Variables

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<StokService>();
var app = builder.Build();

#endregion

#region Middleware

app.Use(async (context, next) =>
{
    var watch = System.Diagnostics.Stopwatch.StartNew();
    Console.WriteLine("Request started"); //TODO log yaz (dosya)
    
    await next();
    
    watch.Stop();
    Console.WriteLine($"Request completed in {watch.ElapsedMilliseconds}ms"); //TODO log yaz (dosya)
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

app.MapGet("/durum", () =>
{
    if (app.Environment.IsDevelopment())
    {
        return Results.Ok("Development environment");
    }
    return Results.Ok("Production environment");
}); // /durum request -> development or production

app.MapGet("/urunler", (StokService Stoks) =>
{
    Console.WriteLine("Get /urunler request");
    Console.WriteLine($"{Stoks.Stoks.Count} sayıda ürün döndürüldü");
    return Results.Ok(Stoks.Stoks);
}); // /urunler request -> return all products

app.MapPost("/urunler", (StokService Stoks, Stok yeniUrun, IConfiguration Configuration) =>
{
    Console.WriteLine("Post /urunler request");
    int limit = app.Configuration.GetValue<int>("Rules:MaxAmount");
    if (Stoks.Stoks.Count >= limit)
    {
        Console.WriteLine("Max amount reached");
        return Results.BadRequest("Max amount reached");
    }

    yeniUrun.Id = Stoks.Stoks.Count + 1;
    Stoks.Stoks.Add(yeniUrun);
    Console.WriteLine($"New product added: Id: {yeniUrun.Id}, Name:{yeniUrun.Name}, Price:{yeniUrun.Price}, Description:{yeniUrun.Description}");
    return Results.Created($"/urunler/{yeniUrun.Id}", yeniUrun);
}); // /urunler request -> add new product

app.MapDelete("/urunler", (StokService Stoks) =>
{
    Console.WriteLine("Delete /urunler request");
    Stoks.Stoks.Clear();
    Console.WriteLine("All products deleted");
    return Results.NoContent();
}); // /urunler request -> delete all products

#endregion

app.Run();