using dotnet_bookish_starter.Dtos;
using dotnet_bookish_starter.Services;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_bookish_starter.Controllers;

[ApiController]
[Route("books")] 
public class BookController : ControllerBase
{
    private readonly IBookService _bookService;
    
    public BookController(IBookService bookService)
    {
         _bookService = bookService;
    }

    [HttpGet]
    public async Task<IActionResult> getBooks([FromQuery] string? title, [FromQuery] string? author)
    {
        var books = await _bookService.getBooks(title, author);
        return Ok(books);
    }

    [HttpGet("{id}/availability")]
    public async Task<IActionResult> getBookAvailability(int id)
    {
        var availability = await _bookService.getBookAvailability(id);
        
        if (availability == null)
            return NotFound($"book with ID:{id} was not found");
            
        return Ok(availability);
    }
    
    [HttpPost]
    public async Task<IActionResult> addBook([FromBody] BookCreateDTO body)
    {
        try
        {
            int newBookId = await _bookService.addBook(body);
            return StatusCode(201, new { message = "the book was added", bookId = newBookId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"the book couldn't be added: {ex.Message}");
        }
    }
    
    [HttpPatch("{id}")]
    public async Task<IActionResult> modifyBook(int id, [FromBody] BookUpdateDTO body)
    {
        bool updated = await _bookService.modifyBook(id, body);

        if (!updated)
            return NotFound($"the book with ID:{id} was not found");

        return Ok(new { message = $"the book with ID:{id} was modified" });
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> deleteBook(int id)
    {
        bool deleted = await _bookService.deleteBook(id);

        if (!deleted)
            return NotFound($"the book with ID:{id} was not found");

        return Ok(new { message = $"the book with ID:{id} was deleted" });
    }
}
