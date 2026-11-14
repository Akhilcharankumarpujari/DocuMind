using DocuMind.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace DocuMind.Infrastructure.Tests;

public class DomainExceptionTests
{
    [Fact]
    public void EntityNotFoundException_ShouldFormatMessageCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var ex = new EntityNotFoundException("Document", id);

        // Assert
        ex.EntityName.Should().Be("Document");
        ex.Key.Should().Be(id);
        ex.Message.Should().Be($"Entity 'Document' with identifier '{id}' was not found.");
    }

    [Fact]
    public void DomainException_ShouldStoreMessage()
    {
        // Arrange
        const string message = "PDF exceeds 20 MB size constraint";

        // Act
        var ex = new DomainException(message);

        // Assert
        ex.Message.Should().Be(message);
    }
}
