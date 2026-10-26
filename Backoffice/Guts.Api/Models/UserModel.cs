using System.Text.Json.Serialization;

namespace Guts.Api.Models;

public class UserModel
{
    [JsonRequired]
    public int Id { get; set; }
    public string FullName { get; set; }
}