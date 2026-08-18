var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
}
else
{
    app.UseHsts();
}
app.UseHttpsRedirection();

app.Run();
