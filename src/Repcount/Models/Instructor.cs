namespace Repcount.Models;

public class Instructor
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Bio { get; set; } = "";
    public List<GymClass> Classes { get; set; } = [];
}
