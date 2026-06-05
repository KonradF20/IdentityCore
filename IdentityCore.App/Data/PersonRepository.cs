using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using IdentityCore.App.Models;

namespace IdentityCore.App.Data;

public class PersonRepository
{
    public List<Person> GetAll()
    {
        var persons = new List<Person>();

        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
            SELECT
                Id,
                PersonCode,
                FirstName,
                LastName,
                DateOfBirth,
                Gender,
                Department,
                Status,
                Description,
                ProfileImagePath,
                FaceImagePath,
                FingerprintImagePath,
                CreatedAt
            FROM dbo.Persons
            ORDER BY Id;
        ";

        using var command = new SqlCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            persons.Add(new Person
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                PersonCode = reader.GetString(reader.GetOrdinal("PersonCode")),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                LastName = reader.GetString(reader.GetOrdinal("LastName")),

                DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),

                Gender = reader.IsDBNull(reader.GetOrdinal("Gender"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("Gender")),

                Department = reader.IsDBNull(reader.GetOrdinal("Department"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("Department")),

                Status = reader.GetString(reader.GetOrdinal("Status")),

                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("Description")),

                ProfileImagePath = reader.IsDBNull(reader.GetOrdinal("ProfileImagePath"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("ProfileImagePath")),

                FaceImagePath = reader.IsDBNull(reader.GetOrdinal("FaceImagePath"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("FaceImagePath")),

                FingerprintImagePath = reader.IsDBNull(reader.GetOrdinal("FingerprintImagePath"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("FingerprintImagePath")),

                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
        }

        return persons;
    }

    public int Add(Person person)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
        INSERT INTO dbo.Persons
        (
            PersonCode,
            FirstName,
            LastName,
            DateOfBirth,
            Gender,
            Department,
            Status,
            Description,
            ProfileImagePath,
            FaceImagePath,
            FingerprintImagePath
        )
        OUTPUT INSERTED.Id
        VALUES
        (
            @PersonCode,
            @FirstName,
            @LastName,
            @DateOfBirth,
            @Gender,
            @Department,
            @Status,
            @Description,
            @ProfileImagePath,
            @FaceImagePath,
            @FingerprintImagePath
        );
    ";

        using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@PersonCode", person.PersonCode);
        command.Parameters.AddWithValue("@FirstName", person.FirstName);
        command.Parameters.AddWithValue("@LastName", person.LastName);
        command.Parameters.AddWithValue("@DateOfBirth", person.DateOfBirth.HasValue ? person.DateOfBirth.Value : DBNull.Value);
        command.Parameters.AddWithValue("@Gender", string.IsNullOrWhiteSpace(person.Gender) ? DBNull.Value : person.Gender);
        command.Parameters.AddWithValue("@Department", string.IsNullOrWhiteSpace(person.Department) ? DBNull.Value : person.Department);
        command.Parameters.AddWithValue("@Status", person.Status);
        command.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(person.Description) ? DBNull.Value : person.Description);
        command.Parameters.AddWithValue("@ProfileImagePath", string.IsNullOrWhiteSpace(person.ProfileImagePath) ? DBNull.Value : person.ProfileImagePath);
        command.Parameters.AddWithValue("@FaceImagePath", string.IsNullOrWhiteSpace(person.FaceImagePath) ? DBNull.Value : person.FaceImagePath);
        command.Parameters.AddWithValue("@FingerprintImagePath", string.IsNullOrWhiteSpace(person.FingerprintImagePath) ? DBNull.Value : person.FingerprintImagePath);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Update(Person person)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
        UPDATE dbo.Persons
        SET
            FirstName = @FirstName,
            LastName = @LastName,
            DateOfBirth = @DateOfBirth,
            Gender = @Gender,
            Department = @Department,
            Status = @Status,
            Description = @Description,
            ProfileImagePath = @ProfileImagePath,
            FaceImagePath = @FaceImagePath,
            FingerprintImagePath = @FingerprintImagePath
        WHERE Id = @Id;
    ";

        using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Id", person.Id);
        command.Parameters.AddWithValue("@FirstName", person.FirstName);
        command.Parameters.AddWithValue("@LastName", person.LastName);
        command.Parameters.AddWithValue("@DateOfBirth", person.DateOfBirth.HasValue ? person.DateOfBirth.Value : DBNull.Value);
        command.Parameters.AddWithValue("@Gender", string.IsNullOrWhiteSpace(person.Gender) ? DBNull.Value : person.Gender);
        command.Parameters.AddWithValue("@Department", string.IsNullOrWhiteSpace(person.Department) ? DBNull.Value : person.Department);
        command.Parameters.AddWithValue("@Status", person.Status);
        command.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(person.Description) ? DBNull.Value : person.Description);
        command.Parameters.AddWithValue("@ProfileImagePath", string.IsNullOrWhiteSpace(person.ProfileImagePath) ? DBNull.Value : person.ProfileImagePath);
        command.Parameters.AddWithValue("@FaceImagePath", string.IsNullOrWhiteSpace(person.FaceImagePath) ? DBNull.Value : person.FaceImagePath);
        command.Parameters.AddWithValue("@FingerprintImagePath", string.IsNullOrWhiteSpace(person.FingerprintImagePath) ? DBNull.Value : person.FingerprintImagePath);

        command.ExecuteNonQuery();
    }


    public Person? GetById(int personId)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
            SELECT
                Id,
                PersonCode,
                FirstName,
                LastName,
                DateOfBirth,
                Gender,
                Department,
                Status,
                Description,
                ProfileImagePath,
                FaceImagePath,
                FingerprintImagePath,
                CreatedAt
            FROM dbo.Persons
            WHERE Id = @Id;
        ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", personId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return new Person
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            PersonCode = reader.GetString(reader.GetOrdinal("PersonCode")),
            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
            LastName = reader.GetString(reader.GetOrdinal("LastName")),

            DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),

            Gender = reader.IsDBNull(reader.GetOrdinal("Gender"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("Gender")),

            Department = reader.IsDBNull(reader.GetOrdinal("Department"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("Department")),

            Status = reader.GetString(reader.GetOrdinal("Status")),

            Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("Description")),

            ProfileImagePath = reader.IsDBNull(reader.GetOrdinal("ProfileImagePath"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("ProfileImagePath")),

            FaceImagePath = reader.IsDBNull(reader.GetOrdinal("FaceImagePath"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("FaceImagePath")),

            FingerprintImagePath = reader.IsDBNull(reader.GetOrdinal("FingerprintImagePath"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("FingerprintImagePath")),

            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    public void Delete(int personId)
    {
        using var connection = DatabaseConnection.CreateConnection();
        connection.Open();

        const string query = @"
        DELETE FROM dbo.Persons
        WHERE Id = @Id;
    ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", personId);

        command.ExecuteNonQuery();
    }
}
