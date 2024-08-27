using BeautySalon.Data;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
public class TestController : Controller
{
    private readonly ApplicationDbContext _context;

    public TestController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/TestDatabaseConnection")]
    public async Task<IActionResult> TestDatabaseConnection()
    {
        try
        {
            // Assuming you have a table called 'Users' in your database
            var users = await _context.Users.Take(5).ToListAsync();
            return Ok(new { success = true, data = users });
        }
        catch (Exception ex)
        {
            // Return an error message if the connection fails
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}