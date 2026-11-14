using DocuMind.Application.Common.Models;
using FluentAssertions;
using Xunit;

namespace DocuMind.Application.Tests;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult_WithError()
    {
        // Arrange
        const string errorMessage = "Document processing failed";

        // Act
        var result = Result.Failure(errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public void TypedResult_Success_ShouldReturnValue()
    {
        // Arrange
        const string payload = "sample-doc-id";

        // Act
        var result = Result<string>.Success(payload);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(payload);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void TypedResult_Failure_AccessingValue_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var result = Result<string>.Failure("Invalid operation");

        // Act
        var action = () => { _ = result.Value; };

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot access the value of a failed result*");
    }
}
