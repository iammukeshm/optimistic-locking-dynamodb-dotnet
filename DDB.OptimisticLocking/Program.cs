using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using DDB.OptimisticLocking.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/{id}", async (IDynamoDBContext context, int id) =>
{
    var product = await context.LoadAsync<Product>(id);
    if (product == null) return Results.NotFound(id);
    return Results.Ok(product);
});

app.MapPut("/", async (IDynamoDBContext context, Product product) =>
{
    await context.SaveAsync(product);
    return Results.Ok();

});

app.MapPut("/skip", async (IDynamoDBContext context, Product product) =>
{
    await context.SaveAsync(product, new DynamoDBOperationConfig()
    {
        SkipVersionCheck = true
    });
    return Results.Ok();

});

app.MapPut("/lowlevel", async (IAmazonDynamoDB ddb, Product product) =>
{
    var currentVersion = product.Version;
    product.Version++;

    var request = new UpdateItemRequest
    {
        TableName = "products",
        Key = new Dictionary<string, AttributeValue>() { { "id", new AttributeValue { N = product.Id.ToString() } } },
        UpdateExpression = "set #available_stock = :available_stock, #Version = :newVersion",
        ConditionExpression = "#Version = :currentVersion",
        ExpressionAttributeNames = new Dictionary<string, string>
        {
            { "#available_stock", "available_stock" },
            { "#Version", "Version" }
        },
        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":available_stock", new AttributeValue { N = product.AvailableStock.ToString() } },
            { ":currentVersion", new AttributeValue { N = currentVersion.ToString() } },
            { ":newVersion", new AttributeValue { N = product.Version.ToString() } }
        },
    };
    var response = await ddb.UpdateItemAsync(request);
    return Results.Ok();

});

app.Run();