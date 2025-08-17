using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
namespace TaskManagerApi.IntegrationTests;

public class TaskTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TaskTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task GetAllTasks_ReturnsOkResult_WithListOfTasks()
    {
        // Arrange
        var client = _factory.CreateClient();
        // acquire a valid access token
        var accessToken = await LoginHelper.LoginAsync(client);

        // Act
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.GetAsync("/task");

        // Assert
        response.EnsureSuccessStatusCode();
        var tasks = await response.Content.ReadFromJsonAsync<IEnumerable<TaskItem>>();
        Assert.NotNull(tasks);
    }

    [Fact]
    public async Task GetTaskById_ReturnsOkResult_WithTask()
    {
        // Arrange
        var client = _factory.CreateClient();
        // acquire a valid access token
        var accessToken = await LoginHelper.LoginAsync(client);
        var taskId = 1;

        // Act
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.GetAsync($"/task/{taskId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var task = await response.Content.ReadFromJsonAsync<TaskItem>();
        Assert.NotNull(task);
    }

    [Fact]
    public async Task CreateTask_ReturnsCreatedResult_WithTask()
    {
        // Arrange
        var client = _factory.CreateClient();
        // acquire a valid access token
        var accessToken = await LoginHelper.LoginAsync(client);
        var newTask = new TaskItem
        {
            Title = "New Task",
            Description = "Task Description",
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        //create a new task
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.PostAsJsonAsync("/task", newTask);

        // Assert
        response.EnsureSuccessStatusCode();
        var createdTask = await response.Content.ReadFromJsonAsync<TaskItem>();
        Assert.NotNull(createdTask);
    }

    [Fact]
    public async Task UpdateTask_ReturnsNoContentResult()
    {
        // Arrange
        var client = _factory.CreateClient();
        // acquire a valid access token
        var accessToken = await LoginHelper.LoginAsync(client);
        var updatedTask = new TaskItem
        {
            Id = 1,
            Title = "Updated Task",
            Description = "Updated Description",
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.PutAsJsonAsync($"/task/{updatedTask.Id}", updatedTask);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task DeleteTask_ReturnsNoContentResult()
    {
        // Arrange
        var client = _factory.CreateClient();
        // acquire a valid access token
        var accessToken = await LoginHelper.LoginAsync(client);
        var taskId = 1;

        // Act
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.DeleteAsync($"/task/{taskId}");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetUserTasks_ReturnsOkResult_WithListOfTasks()
    {
        // Arrange
        var client = _factory.CreateClient();
        // acquire a valid access token
        var accessToken = await LoginHelper.LoginAsync(client);
        var userId = 1;

        // Act
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.GetAsync($"/task/user/{userId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var tasks = await response.Content.ReadFromJsonAsync<IEnumerable<TaskItem>>();
        Assert.NotNull(tasks);
    }
}