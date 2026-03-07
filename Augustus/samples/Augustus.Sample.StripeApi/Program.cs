using Augustus.Sample.StripeApi.Models;
using Augustus.Sample.StripeApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<StripeClient>(client =>
{
    var baseUrl = builder.Configuration["Stripe:BaseUrl"] ?? "https://api.stripe.com";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

app.MapPost("/payments", async (CreatePaymentRequest req, StripeClient stripe) =>
{
    var result = await stripe.CreateChargeAsync(req.Amount, req.Currency, req.Source);
    return Results.Ok(result);
});

app.MapGet("/customers/{id}", async (string id, StripeClient stripe) =>
{
    var result = await stripe.GetCustomerAsync(id);
    return Results.Ok(result);
});

app.MapPost("/customers", async (CreateCustomerRequest req, StripeClient stripe) =>
{
    var result = await stripe.CreateCustomerAsync(req.Name, req.Email);
    return Results.Ok(result);
});

app.Run();
