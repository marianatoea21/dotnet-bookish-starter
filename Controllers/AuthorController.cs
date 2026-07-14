using dotnet_bookish_starter.Models;
using dotnet_bookish_starter.Dtos;
using Microsoft.AspNetCore.Mvc; // aici se afla clasa abstracta ControllerBase
using Dapper; // ia rezultatul unui query si il transforma intr o lista
using Microsoft.Data.SqlClient; // clasa SqlConnection 

namespace dotnet_bookish_starter.Controllers;

[ApiController]
[Route("authors")]
public class AuthorController : ControllerBase
{
    private readonly string _connectionString; // unde se afla serverul de baza de date, cum se numeste baza de date .. este drumul catre baza de date

    public AuthorController(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DbConnectionString") ?? "";
    }

    [HttpGet]
    public async Task<IActionResult> getAuthors()
    {
        await using var connection = new SqlConnection(_connectionString);

        string sqlCommand = "SELECT * FROM dbo.Authors ORDER By Name";
        var allAuthors = await connection.QueryAsync(sqlCommand);
        
        return Ok(allAuthors);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> getAuthorById(int id)
    {
        await using var connection = new SqlConnection(_connectionString);

        // numele autorului cu id ul x
        string authorNameSql = "SELECT Name FROM dbo.Authors WHERE AuthorID = @AuthorID";
        string authorName = await connection.QueryFirstOrDefaultAsync<string>(authorNameSql, new { AuthorID = id }); // ruleaza query ul si ia prima valoare de tip string

        if (string.IsNullOrEmpty(authorName))
        {
            return NotFound($"Autorul cu ID-ul {id} nu a fost gasit!");
        }
        
        // cartile autorului cu id ul x
        string authorBooksSql = @"
              SELECT B.* FROM dbo.Books B 
              JOIN dbo.BooksAuthors BA on BA.BookID = B.BookID
              WHERE BA.AuthorID = @AuthorID
        ";

        var authorBooks = await connection.QueryAsync<Book>(authorBooksSql, new { AuthorID = id }); // QueryAsync returneaza o colectie sau o lista mereu

        var authorDetails = new AuthorDetailsDTO
        {
            AuthorID = id,
            Name = authorName,
            Books = authorBooks.ToList()
        };
        
        return Ok(authorDetails);
    }

    [HttpPost]
    public async Task<IActionResult> addAuthor([FromBody] string authorName)
    {
        if (string.IsNullOrWhiteSpace(authorName))
        {
            return BadRequest("Numele autorului nu poate fi gol!");
        }
        
        await using var connection = new SqlConnection(_connectionString);
        string sqlCommand = @"
            INSERT INTO dbo.Authors (Name) VALUES (@Name);
            SELECT CAST(SCOPE_IDENTITY() as int);
        ";

        int newAuthorID = await connection.QuerySingleAsync<int>(sqlCommand, new { Name = authorName });
        return StatusCode(201, new { message = "Autorul a fost adaugat!", authorId = newAuthorID});
    }
}