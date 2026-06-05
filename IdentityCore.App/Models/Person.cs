using System;

namespace IdentityCore.App.Models;

public class Person
{
    public int Id { get; set; }

    public string PersonCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public DateTime? DateOfBirth { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Status { get; set; } = "Aktywny";

    public string Description { get; set; } = string.Empty;

    public string ProfileImagePath { get; set; } = string.Empty;

    public string FaceImagePath { get; set; } = string.Empty;

    public string FingerprintImagePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
