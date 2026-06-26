FormsDataManagementAPI


Description
Design and implement a REST API endpoint for storing and retrieving form submission data. The solution should demonstrate proper API design, database interaction, validation, error handling, and security considerations appropriate for a production system.


Problem
Given the following data model:
public class FormData
{
    public Guid Id { get; set; }
    public string Subject { get; set; }
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public int? Priority { get; set; }  // Must be between 1 and 10
    public bool? Critical { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; }
}
Requirements
1. Implement the following REST endpoints:
   * POST /api/forms - Create a new form entry
   * GET /api/forms/{id} - Retrieve a form by ID
   * GET /api/forms - List forms with pagination and filtering
   * PUT /api/forms/{id} - Update an existing form
   * DELETE /api/forms/{id} - Soft delete a form
2. Validation:
   * Subject is required and must be 1-200 characters
   * Priority must be an integer between 1 and 10 (if provided)
   * DueDate must be a valid date in the future (if provided)
   * Return appropriate validation error responses
3. Database Considerations:
   * Design the database schema
   * Implement repository pattern or equivalent abstraction
   * Handle concurrent updates appropriately
4. Error Handling:
   * Return appropriate HTTP status codes
   * Provide meaningful error messages without exposing internal details
   * Handle database connection failures gracefully
5. Security:
   * Prevent SQL injection
   * Implement input sanitization
   * Consider authorization (can use a UserCanModify, UserCanView stub which doesn’t need implemented)
Evaluation Criteria
* API design and RESTful conventions
* Code organization and separation of concerns
* Validation completeness and error response quality
* Database query efficiency
* Exception handling strategy
* Security awareness
* Unit test coverage for critical paths
Starter Code (C# / .NET)
[ApiController]
[Route("api/[controller]")]
public class FormsController : ControllerBase
{
    private readonly IFormDataRepository _repository;
    private readonly ILogger<FormsController> _logger;


    public FormsController(IFormDataRepository repository, ILogger<FormsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFormRequest request)
    {
        // Implement
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Implement
    }


    [HttpGet]
    public async Task<IActionResult> List([FromQuery] FormListQuery query)
    {
        // Implement
    }


    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFormRequest request)
    {
        // Implement
    }


    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Implement
    }
}


public interface IFormDataRepository
{
    // Define interface methods
}


public record CreateFormRequest(/* Define properties */);
public record UpdateFormRequest(/* Define properties */);
public record FormListQuery(int Page = 1, int PageSize = 20, string? SubjectFilter = null);

FormsDataManagementAPI.Tests
This sectiion has unit tests for Repository, Service , Validator classes. It has integration test to test controller.

TokenGenerator
It is a console project to generate a JWT token by feeding in user information. Ideally the token will get geenerated when client will first send request to server.
