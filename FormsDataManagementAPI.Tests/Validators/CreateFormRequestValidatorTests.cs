using FluentAssertions;
using FormsDataManagementAPI.DTOs;
using FormsDataManagementAPI.Validators;

namespace FormsDataManagementAPI.Tests.Validators;

public class CreateFormRequestValidatorTests
{
    private readonly CreateFormRequestValidator _validator = new();

    [Fact]
    public async Task Valid_request_passes()
    {
        var request = new CreateFormRequest("Buy milk", null, DateTime.UtcNow.AddDays(1), 5, null);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_subject_fails(string subject)
    {
        var request = new CreateFormRequest(subject, null, null, null, null);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFormRequest.Subject));
    }

    [Fact]
    public async Task Subject_exceeding_200_chars_fails()
    {
        var subject = new string('x', 201);
        var request = new CreateFormRequest(subject, null, null, null, null);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFormRequest.Subject));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public async Task Priority_out_of_range_fails(int priority)
    {
        var request = new CreateFormRequest("Subject", null, null, priority, null);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFormRequest.Priority));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Priority_in_range_passes(int priority)
    {
        var request = new CreateFormRequest("Subject", null, null, priority, null);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Null_priority_passes()
    {
        var request = new CreateFormRequest("Subject", null, null, null, null);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Past_due_date_fails()
    {
        var request = new CreateFormRequest("Subject", null, DateTime.UtcNow.AddDays(-1), null, null);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFormRequest.DueDate));
    }

    [Fact]
    public async Task Future_due_date_passes()
    {
        var request = new CreateFormRequest("Subject", null, DateTime.UtcNow.AddDays(1), null, null);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Null_due_date_passes()
    {
        var request = new CreateFormRequest("Subject", null, null, null, null);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }
}
