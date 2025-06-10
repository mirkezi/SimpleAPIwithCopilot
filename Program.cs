using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

/* TOKEN:

eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsImp0aSI6IjczOTM1OGNlLTFlZjUtNGRmNC05N2FiLTAwMmYwNzdlNWQ2MiIsImV4cCI6MTc0OTU5NDc0MywiaXNzIjoieW91ci1hcHAiLCJhdWQiOiJ5b3VyLXVzZXJzIn0.mawlb-65mFDXi_eRbFT4ytwv0VbVjAEY_CP-BpGLM38

*/
var builder = WebApplication.CreateBuilder(args);

// 🔹 Configure JWT Authentication
var key = Encoding.UTF8.GetBytes("this_is_a_very_long_secret_key_for_authentication_which_is_32_bytes_or_more");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "your-app",
            ValidAudience = "your-users",
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

var users = new List<User>
    {
        new User { Id = 1, Name = "John Doe", Email = "john.doe@example.com" },
        new User { Id = 2, Name = "Jane Smith", Email = "jane.smith@example.com" }
    };

// 🔹 Token Generation Endpoint
app.MapPost("/login", (UserLogin user) =>
{
    if (user.Username == "admin" && user.Password == "password") // Replace with DB validation
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "your-app",
            audience: "your-users",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Results.Ok(new { Token = tokenString });
    }

    return Results.Unauthorized();
});

// 🔹 Apply Authentication & Authorization Middleware
// 🔹 Custom Middleware for Exception Handling
app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var errorResponse = new
        {
            Message = "An unexpected error occurred.",
            Details = ex.Message
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    // Log the HTTP method and request path
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");

    await next.Invoke();

    // Log the response status code
    Console.WriteLine($"Response: {context.Response.StatusCode}");
});

// GET: Retrieve all users
app.MapGet("/users", () =>
{
    try
    {
        return Results.Ok(users.ToList()); // Defensive copy to avoid modification issues
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error retrieving users: {ex.Message}");
        return Results.Problem($"Error retrieving users: {ex.Message}");
    }
}).RequireAuthorization();

// GET: Retrieve a specific user by ID
app.MapGet("/users/{id:int}", (int id) =>
{
    try
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        return user is not null ? Results.Ok(user) : Results.NotFound();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error retrieving user {id}: {ex.Message}");
        return Results.Problem($"Error retrieving user {id}: {ex.Message}");
    }
}).RequireAuthorization();

// POST: Add a new user
app.MapPost("/users", (User newUser) =>
{
    try
    {
        newUser.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
        users.Add(newUser);
        return Results.Created($"/users/{newUser.Id}", newUser);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error adding user: {ex.Message}");
        return Results.Problem($"Error adding user: {ex.Message}");
    }
}).RequireAuthorization();

// PUT: Update an existing user's details
app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    try
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if (user is null) return Results.NotFound();

        user.Name = updatedUser.Name;
        user.Email = updatedUser.Email;
        return Results.Ok(user);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error updating user {id}: {ex.Message}");
        return Results.Problem($"Error updating user {id}: {ex.Message}");
    }
}).RequireAuthorization();

// DELETE: Remove a user by ID
app.MapDelete("/users/{id:int}", (int id) =>
{
    try
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if (user is null) return Results.NotFound();

        users.Remove(user);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deleting user {id}: {ex.Message}");
        return Results.Problem($"Error deleting user {id}: {ex.Message}");
    }
}).RequireAuthorization();

app.Run();

// 🔹 Models
record UserLogin
{
    public string Username { get; set; }
    public string Password { get; set; }
}

record User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}