using Microsoft.EntityFrameworkCore;
using UrlShort.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlite(connStr));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/shorturl", async (UrlDto url, ApiDbContext db, HttpContext ctx) => 
{
    // validating the input url
    if(!Uri.TryCreate(url.Url, UriKind.Absolute, out var inputUrl))
    return Results.BadRequest("Invalid url has been provided");

    // craeating a short version of the provided url
    var random = new Random();
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890@ofsa";
    var randomStr = new string(Enumerable.Repeat(chars, 8)
    .Select(x => x[random.Next(x.Length)]).ToArray());

    // mapping the short url with the long url
    var sUrl = new UrlManagement()
    {
        Url = url.Url,
        ShortUrl = randomStr
    };
    
    //  saving the mapping to the db

    db.Urls.Add(sUrl);
    db.SaveChangesAsync();

    // construct url

    var result = $"{ctx.Request.Scheme}://{ctx.Request.Host}/{sUrl.ShortUrl}";

    return Results.Ok(new UrlShortResponseDto()
    {
        Url = result
    });
});

app.MapFallback(async (ApiDbContext db, HttpContext ctx) => 
{
    var path = ctx.Request.Path.ToUriComponent().Trim('/');
    var urlMatch = await db.Urls.FirstOrDefaultAsync(x => 
        x.ShortUrl.Trim() == path.Trim());

    if(urlMatch == null)
        return Results.BadRequest("Invalid request");

    return Results.Redirect(urlMatch.Url);
});

app.Run();

class ApiDbContext : DbContext
{
    public virtual DbSet<UrlManagement> Urls { get; set; }

    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {  }
}