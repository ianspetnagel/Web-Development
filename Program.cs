using Microsoft.EntityFrameworkCore;
using ProductsMinimalAPISpetnagel.Models;
using ProductsMinimalAPISpetnagel.MyModels;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ProductsBcbsContext>(options => options.UseSqlServer(builder.Configuration["ConnectionStrings:ProductsDBConn"]));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder
            .WithOrigins("https://localhost:7123")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();
app.UseCors(); // abovv bold lines added to allow cross-origin requests

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();


//-------------------------Our API related functions---------------------------
// get all products
app.MapGet("/myapi/Products", (ProductsBcbsContext db) =>
{
    return db.Products.ToList();
});
// get all categories
app.MapGet("/myapi/categories", (ProductsBcbsContext db) =>
{
    return db.Categories.ToList();
});
// get products By Categoryid
app.MapGet("/myapi/productsbycatid", (ProductsBcbsContext db, int id) =>
{
    var prods = db.Products.Where(p => p.CategoryId == id).ToList<Product>();
    return Results.Ok(prods);
});
// product By Id
app.MapGet("/myapi/productbyid", (ProductsBcbsContext db, int id) =>
{
    var prod = db.Products.Find(id);
    return Results.Ok(prod);
});


// Get products with suppliers
app.MapGet("/myapi/productsuppliers", (ProductsBcbsContext db) =>
{
    var prodsSups = db.Products.Include(p => p.Suppliers)
        .Select(p => new MyProductSupplier
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            SupplierAddress = p.Suppliers.ToList().Count > 0 ? p.Suppliers.ToList()[0].Address : "", SupplierName = p.Suppliers.ToList().Count > 0 ? p.Suppliers.ToList()[0].SupplierName : "", Price = p.Price
        })
        .ToList<MyProductSupplier>();

    return Results.Ok(prodsSups);
});


// Add product
app.MapPost("/myapi/addproduct", (ProductsBcbsContext db, Product prod) =>
{
    db.Products.Add(prod);
    db.SaveChanges();
    return Results.Created($"/myapi/productsbyid/{prod.ProductId}", prod);
});

// Update product
app.MapPut("/myapi/updateproduct/", (ProductsBcbsContext db, Product prod) =>
{
    var prodindb = db.Products.FirstOrDefault(x => x.ProductId == prod.ProductId);

    if (prodindb != null)
    {
        prodindb.ProductName = prod.ProductName;
        prodindb.Price = prod.Price;
        prodindb.Description = prod.Description;
        prodindb.StockLevel = prod.StockLevel;
        prodindb.CategoryId = prod.CategoryId;

        db.Products.Update(prodindb);
        db.SaveChanges();

        return Results.NoContent();
    }
    else
    {
        return Results.NotFound();
    }
});

// Apply discount to a product
app.MapPut("/myapi/applydiscount/", (ProductsBcbsContext db, Product prod, double percentDiscount) =>
{
    var prodindb = db.Products.FirstOrDefault(x => x.ProductId == prod.ProductId);

    if (prodindb != null)
    {
        prodindb.Price = prodindb.Price - prodindb.Price * (decimal)(percentDiscount / 100.0);
        db.Products.Update(prodindb);
        db.SaveChanges();

        return Results.NoContent();
    }
    else
    {
        return Results.NotFound();
    }
});

// Delete product
app.MapDelete("/myapi/deleteproduct/", (ProductsBcbsContext db, int id) =>
{
    var prodindb = db.Products.Find(id);

    if (prodindb != null)
    {
        db.Products.Remove(prodindb);
        db.SaveChanges();

        return Results.NoContent();
    }
    else
    {
        return Results.NotFound();
    }
});
//-------------------------------------------------------







app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
