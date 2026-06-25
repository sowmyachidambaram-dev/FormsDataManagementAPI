using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using FormsDataManagementAPI.Data;
using FormsDataManagementAPI.DTOs;
using FormsDataManagementAPI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FormsDataManagementAPI.Tests.Controllers;

// Always authenticates as a fixed test user — replaces JWT in the test host.
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string UserId = "test-user-id";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, UserId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// Spins up the full ASP.NET Core pipeline with:
//  - InMemory EF database (no SQL Server needed)
//  - IFormService replaced by a Moq mock
//  - JWT replaced by TestAuthHandler (always authenticated)
public sealed class FormsApiFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "test-signing-key-minimum-32-characters-xxxx";

    public Mock<IFormService> ServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(cfg =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(o =>
                o.UseInMemoryDatabase("integration-tests"));

            services.RemoveAll<IFormService>();
            services.AddScoped(_ => ServiceMock.Object);

            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = "Test";
                o.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }
}

public class FormsControllerTests : IClassFixture<FormsApiFactory>
{
    private readonly HttpClient _client;
    private readonly Mock<IFormService> _serviceMock;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public FormsControllerTests(FormsApiFactory factory)
    {
        _serviceMock = factory.ServiceMock;
        _serviceMock.Reset();
        _client = factory.CreateClient();
    }

    // ── POST /api/forms ────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_returns_201_with_valid_request()
    {
        var request = new CreateFormRequest("My subject", "desc", null, 3, true);
        var expected = new FormDataResponse(Guid.NewGuid(), "My subject", "desc", null, 3, true, DateTime.UtcNow, null, TestAuthHandler.UserId);

        _serviceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateFormRequest>(), TestAuthHandler.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await _client.PostAsJsonAsync("/api/forms", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<FormDataResponse>(JsonOptions);
        body!.Id.Should().Be(expected.Id);
        body.Subject.Should().Be("My subject");
    }

    [Fact]
    public async Task Create_returns_400_when_subject_is_empty()
    {
        var request = new CreateFormRequest("", null, null, null, null);

        var response = await _client.PostAsJsonAsync("/api/forms", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _serviceMock.Verify(
            s => s.CreateAsync(It.IsAny<CreateFormRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_returns_400_when_priority_out_of_range()
    {
        var request = new CreateFormRequest("Valid subject", null, null, 99, null);

        var response = await _client.PostAsJsonAsync("/api/forms", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/forms/{id} ────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_returns_200_when_found()
    {
        var id = Guid.NewGuid();
        var expected = new FormDataResponse(id, "Test form", null, null, null, null, DateTime.UtcNow, null, TestAuthHandler.UserId);

        _serviceMock
            .Setup(s => s.GetByIdAsync(id, TestAuthHandler.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await _client.GetAsync($"/api/forms/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FormDataResponse>(JsonOptions);
        body!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetById_returns_404_when_not_found()
    {
        var id = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.GetByIdAsync(id, TestAuthHandler.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FormDataResponse?)null);

        var response = await _client.GetAsync($"/api/forms/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/forms ─────────────────────────────────────────────────────────

    [Fact]
    public async Task List_returns_200_with_paged_results()
    {
        var pagedResult = new PagedResult<FormDataResponse>(
            new[] { new FormDataResponse(Guid.NewGuid(), "Form A", null, null, null, null, DateTime.UtcNow, null, TestAuthHandler.UserId) },
            TotalCount: 1, Page: 1, PageSize: 20);

        _serviceMock
            .Setup(s => s.ListAsync(It.IsAny<FormListQuery>(), TestAuthHandler.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var response = await _client.GetAsync("/api/forms");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<FormDataResponse>>(JsonOptions);
        body!.TotalCount.Should().Be(1);
        body.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task List_returns_400_when_page_is_zero()
    {
        var response = await _client.GetAsync("/api/forms?page=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_returns_400_when_page_size_exceeds_maximum()
    {
        var response = await _client.GetAsync("/api/forms?pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/forms/{id} ────────────────────────────────────────────────────

    [Fact]
    public async Task Update_returns_200_on_success()
    {
        var id = Guid.NewGuid();
        var request = new UpdateFormRequest("Updated subject", null, null, 5, null);
        var expected = new FormDataResponse(id, "Updated subject", null, null, 5, null, DateTime.UtcNow, DateTime.UtcNow, TestAuthHandler.UserId);

        _serviceMock
            .Setup(s => s.UpdateAsync(id, It.IsAny<UpdateFormRequest>(), TestAuthHandler.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await _client.PutAsJsonAsync($"/api/forms/{id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FormDataResponse>(JsonOptions);
        body!.Subject.Should().Be("Updated subject");
        body.Priority.Should().Be(5);
    }

    [Fact]
    public async Task Update_returns_404_when_not_found()
    {
        var id = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.UpdateAsync(id, It.IsAny<UpdateFormRequest>(), TestAuthHandler.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FormDataResponse?)null);

        var response = await _client.PutAsJsonAsync($"/api/forms/{id}", new UpdateFormRequest("Subject", null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_returns_400_when_subject_is_empty()
    {
        var id = Guid.NewGuid();
        var request = new UpdateFormRequest("", null, null, null, null);

        var response = await _client.PutAsJsonAsync($"/api/forms/{id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _serviceMock.Verify(
            s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateFormRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── DELETE /api/forms/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task Delete_returns_204_on_success()
    {
        var id = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.DeleteAsync(id, TestAuthHandler.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = await _client.DeleteAsync($"/api/forms/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_returns_404_when_not_found()
    {
        var id = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.DeleteAsync(id, TestAuthHandler.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = await _client.DeleteAsync($"/api/forms/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Auth ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        using var unauthFactory = new UnauthenticatedFactory();
        var unauthClient = unauthFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await unauthClient.GetAsync($"/api/forms/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

// Minimal factory without TestAuthHandler — used only for the 401 test.
// Uses real JWT validation, so requests without a token are rejected.
file sealed class UnauthenticatedFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(cfg =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = FormsApiFactory.TestJwtKey,
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(o =>
                o.UseInMemoryDatabase("unauth-tests"));
        });
    }
}
