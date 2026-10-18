using System.Text.Json.Serialization;

namespace Guts.Api.Models.ExamModels;

public class ExamCreationModel
{
    [JsonRequired]
    public int CourseId { get; set; }
    public string Name { get; set; }
}