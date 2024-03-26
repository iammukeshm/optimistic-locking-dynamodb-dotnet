using Amazon.DynamoDBv2.DataModel;

namespace DDB.OptimisticLocking.Models;

[DynamoDBTable("products")]
public class Product
{
    [DynamoDBHashKey("id")]
    public int? Id { get; set; }
    [DynamoDBProperty("available_stock")]
    public int AvailableStock { get; set; }
    [DynamoDBVersion]
    public int? Version { get; set; }
}
