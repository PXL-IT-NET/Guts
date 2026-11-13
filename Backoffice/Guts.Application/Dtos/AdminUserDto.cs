using System.Collections.Generic;

namespace Guts.Application.Dtos;

public class AdminUserDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool IsLinkedToCourse { get; set; }
    public bool CanDelete { get; set; }
    public bool CanImpersonate { get; set; }
    public bool CanPromoteToAdmin { get; set; }
    public bool CanDemoteFromAdmin { get; set; }
}

public class ImpersonatableUserDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}
