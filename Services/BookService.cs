using dotnet_bookish_starter.Models;
using dotnet_bookish_starter.Dtos;
using Dapper;
using Microsoft.Data.SqlClient;

namespace dotnet_bookish_starter.Services;

public class BookService : IBookService
{
    private readonly string _connectionString;

    public BookService(IConfiguration config)
    { 
        _connectionString = config.GetConnectionString("DbConnectionString") ?? "";
    }

    public async Task<IEnumerable<Book>> getBooks(string? title, string? author)
    {
        await using var connection = new SqlConnection(_connectionString);
        string sqlCommand;

        if (!string.IsNullOrEmpty(author))
        {
            sqlCommand = @"SELECT * FROM dbo.Books 
                JOIN dbo.BooksAuthors ON Books.BookID = BooksAuthors.BookID
                JOIN dbo.Authors ON BooksAuthors.AuthorID = Authors.AuthorID
                WHERE Authors.Name LIKE @AuthorName
                ORDER BY Books.Title ASC";
            
            return await connection.QueryAsync<Book>(sqlCommand, new { AuthorName = "%" + author + "%" });
        }

        if (!string.IsNullOrEmpty(title))
        {
            sqlCommand = "SELECT * FROM dbo.Books WHERE Title LIKE @Title ORDER BY Title ASC";
            return await connection.QueryAsync<Book>(sqlCommand, new { Title = "%" + title + "%" });
        }

        sqlCommand = "SELECT * FROM dbo.Books ORDER BY Title ASC";
        return await connection.QueryAsync<Book>(sqlCommand);
    }

    public async Task<BookAvailabilityDTO?> getBookAvailability(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        
        string numberOfBooksSql = "SELECT NumberOfCopies FROM dbo.Books WHERE BookID = @BookID";
        
        int totalCopies = await connection.QueryFirstOrDefaultAsync<int>(numberOfBooksSql, new { BookID = id });
        
        string loansSql = @"
            SELECT USers.Name AS UserName, Loans.DueDate 
            FROM dbo.Loans 
            JOIN dbo.Users ON Loans.UserID = Users.UserID
            WHERE Loans.BookID = @BookID AND Loans.ReturnDate IS NULL";
        
        var activeLoans = (await connection.QueryAsync<BorrowerDetailsDTO>(loansSql, new { BookID = id })).ToList();
        
        int availableCopies = totalCopies - activeLoans.Count;

        return new BookAvailabilityDTO
        {
            TotalCopies = totalCopies,
            AvailableCopies = availableCopies,
            BorrowedBy = activeLoans
        };
    }

    public async Task<int> addBook(BookCreateDTO body)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            string insertBookSql = @"
                INSERT INTO dbo.Books (Title, ISBN, NumberOfCopies)
                VALUES (@Title, @ISBN, @NumberOfCopies);
                SELECT CAST(SCOPE_IDENTITY() as int);";
            
            int newBookId = await connection.QuerySingleAsync<int>(insertBookSql, body, transaction);
            
            if (body.AuthorIds != null && body.AuthorIds.Any())
            {
                string insertRelationSql = "INSERT INTO dbo.BooksAuthors (BookID, AuthorID) VALUES (@BookID, @AuthorID);";
                
                foreach (var authorId in body.AuthorIds)
                {
                    await connection.ExecuteAsync(insertRelationSql, new { BookID = newBookId, AuthorID = authorId }, transaction);
                }
            }
            
            await transaction.CommitAsync();
            return newBookId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw; 
        }
    }

    public async Task<bool> modifyBook(int id, BookUpdateDTO body)
    {
        await using var connection = new SqlConnection(_connectionString);
        
        string sql = @"
            UPDATE dbo.Books 
            SET Title = @Title, ISBN = @ISBN, NumberOfCopies = @NumberOfCopies
            WHERE BookID = @BookID";
        
        int rowsAffected = await connection.ExecuteAsync(sql, new { 
            Title = body.Title, 
            ISBN = body.ISBN, 
            NumberOfCopies = body.NumberOfCopies, 
            BookID = id 
        });

        return rowsAffected > 0;
    }

    public async Task<bool> deleteBook(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        
        string sql = "DELETE FROM dbo.Books WHERE BookID = @BookID";
        int rowsAffected = await connection.ExecuteAsync(sql, new { BookID = id });

        return rowsAffected > 0;
    }
}