# GitHub Copilot Instructions - TaskManagerApi

## 🌟 Project Overview

This is a learning-focused .NET 8 Web API project integrating PostgreSQL. The goal is to apply modern, production-aligned patterns using a simple and maintainable architecture.

## 🔧 Technology Stack

* .NET 8 Web API (minimal hosting model)
* ASP.NET Core
* Entity Framework Core with Npgsql provider
* PostgreSQL (hosted on Neon.tech)
* Dependency Injection using built-in .NET container
* RESTful API principles

## 🧱 Architecture Patterns

* Service Layer for business logic
* Optional Repository Pattern for abstraction (without Unit of Work)
* Dependency Injection everywhere (constructor injection)
* Async/await for all I/O
* SOLID principle adherence

## 🧑‍💻 Code Style Preferences

* Prefer explicit async/await (no `.Result`, `.Wait()`)
* Use constructor injection for all dependencies
* Use `ILogger<T>` for logging
* Apply correct HTTP status codes in controllers
* Use strongly-typed config with `IOptions<T>`
* Follow RESTful naming for all routes and resources

## 🧠 Entity Framework Core Guidelines

* Code-First approach with Migrations
* Use Fluent API in `OnModelCreating` (avoid data annotations)
* Use `DbSet<T>` properties for entities
* Always use async methods (`ToListAsync`, `FindAsync`, etc.)
* Dispose `DbContext` properly (scoped lifetime via DI)

## 📡 API Design Guidelines

* Inherit controllers from `ControllerBase` (not `Controller`)
* Use `[ApiController]` attribute
* Match HTTP verbs: `GET`, `POST`, `PUT`, `DELETE`
* Return `ActionResult<T>` from controller actions
* Use `CreatedAtAction` for POST responses
* Consistently apply status codes: `200`, `201`, `400`, `404`, `500`

## 🛡️ Error Handling

* Use global exception middleware (e.g., `UseExceptionHandler`)
* Create custom exception types for domain-specific errors
* Log exceptions with contextual info
* Return consistent and structured error response objects

## 📄 Database Context

* Store connection string in `appsettings.json` (no hardcoding)
* Use Neon PostgreSQL (DB name: `TaskManagerDb`)
* Configure `DbContext` with `AddDbContext<T>()`
* Register `DbContext` as scoped

## 🗒️ Naming Conventions

| Type        | Convention                           |
| ----------- | ------------------------------------ |
| Controllers | PascalCase, end with `Controller`    |
| Services    | Interface `I<name>` + Impl `<name>`  |
| Models      | PascalCase, match DB structure       |
| Routes      | kebab-case (e.g., `/api/task-items`) |
| Tables      | PascalCase (`Tasks`, `Users`, etc.)  |

## 🔐 Security Best Practices

* Store secrets securely (`UserSecrets`, Azure Key Vault)
* Use SSL/TLS for DB connection (`SslMode=Require`)
* Validate all incoming API model inputs
* Plan for authentication/authorization (JWT, etc.)

## 🧪 Testing Strategy

* Write **unit tests** for services (use Moq if needed)
* Write **integration tests** for API endpoints
* Use **TestContainers** or SQLite for DB testing (future)
* Follow **Arrange-Act-Assert** pattern

## ✅ Recommended Code Patterns

### Program.cs (Service Registration)

```csharp
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<ITaskService, TaskService>();
```

### Controller Pattern

```csharp
[ApiController]
[Route("api/task-items")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetAll()
    {
        var items = await _taskService.GetAllAsync();
        return Ok(items);
    }
}
```

### Service Pattern

```csharp
public class TaskService : ITaskService
{
    private readonly TaskDbContext _context;
    private readonly ILogger<TaskService> _logger;

    public TaskService(TaskDbContext context, ILogger<TaskService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        return await _context.Tasks.ToListAsync();
    }
}
```

### EF Core Usage

```csharp
await _context.Tasks.ToListAsync();
await _context.Tasks.FindAsync(id);
await _context.SaveChangesAsync();
```

## 🚫 Avoid These Patterns

* Synchronous calls (`.Result`, `.Wait()`)
* Full Repository + Unit of Work complexity
* Over-abstraction or inheritance hierarchies
* Hardcoded secrets in config
* Mixing business logic in controllers

## 🎓 Learning Context

* Developer transitioning from .NET Framework to modern .NET
* Emphasis on production-level patterns
* Learning goal: prepare for real-world development and interviews
