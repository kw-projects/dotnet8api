using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

[ApiController]
[EnableCors("AllowAll")]
[Authorize]
[Route("[controller]")]
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TaskController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetAll()
    {


        // Debug: Log current time and token expiry
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new JsonWebTokenHandler();
            var jsonToken = handler.ReadJsonWebToken(token);

            Console.WriteLine($"Current UTC Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Token Expires: {jsonToken.ValidTo:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Time until expiry: {jsonToken.ValidTo.Subtract(DateTime.UtcNow).TotalMinutes:F2} minutes");
        }


        var items = await _taskService.GetAllAsync();
        
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItem?>> GetById(int id)
    {
        var item = await _taskService.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound();
        }
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] TaskItem item)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        await _taskService.CreateAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] TaskItem item)
    {
        if (id != item.Id)
        {
            return BadRequest();
        }
        await _taskService.UpdateAsync(id, item);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        await _taskService.DeleteAsync(id);
        return NoContent();
    }
    
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetUserTasks(int userId)
    {
        if (userId <= 0)
        {
            return BadRequest("Invalid user ID");
        }

        var userTasks = await _taskService.GetTasksByUserIdAsync(userId);
        return Ok(userTasks);
    }
}