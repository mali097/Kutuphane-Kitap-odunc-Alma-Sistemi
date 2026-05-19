namespace LibrarySystem.UI.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Role { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("createdDate")]
    public DateTime? CreatedAt { get; set; }
}
