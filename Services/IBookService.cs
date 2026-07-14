using dotnet_bookish_starter.Models;
using dotnet_bookish_starter.Dtos;

namespace dotnet_bookish_starter.Services;

public interface IBookService
{
    Task<IEnumerable<Book>> getBooks(string? title, string? author);
    Task<BookAvailabilityDTO?> getBookAvailability(int id);
    Task<int> addBook(BookCreateDTO body);
    Task<bool> modifyBook(int id, BookUpdateDTO body);
    Task<bool> deleteBook(int id);
}