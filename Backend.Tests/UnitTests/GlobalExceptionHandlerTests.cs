using Backend.Middleware;
using FluentAssertions;

namespace Backend.Tests.UnitTests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public void TrimmedStackTrace_ReturnsNull_WhenStackTraceIsNull()
    {
        // Act
        var result = GlobalExceptionHandler.TrimmedStackTrace(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TrimmedStackTrace_ReturnsNull_WhenStackTraceIsEmpty()
    {
        // Act
        var result = GlobalExceptionHandler.TrimmedStackTrace(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TrimmedStackTrace_ReturnsNull_WhenStackTraceIsWhitespace()
    {
        // Act
        var result = GlobalExceptionHandler.TrimmedStackTrace("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TrimmedStackTrace_TrimsToBackendLine_WhenBackendLineExists()
    {
        // Arrange
        var stackTrace = $"Line 1{Environment.NewLine}Line 2{Environment.NewLine}at Backend.SomeClass.Method(){Environment.NewLine}at System.SomeMethod()";

        // Act
        var result = GlobalExceptionHandler.TrimmedStackTrace(stackTrace);

        // Assert
        var expected = $"Line 1{Environment.NewLine}Line 2{Environment.NewLine}at Backend.SomeClass.Method()";
        result.Should().Be(expected);
    }

    [Fact]
    public void TrimmedStackTrace_ReturnsAllLines_WhenNoBackendLineAndFewLines()
    {
        // Arrange
        var stackTrace = $"Line 1{Environment.NewLine}Line 2{Environment.NewLine}Line 3";

        // Act
        var result = GlobalExceptionHandler.TrimmedStackTrace(stackTrace);

        // Assert
        result.Should().Be(stackTrace);
    }

    [Fact]
    public void TrimmedStackTrace_TrimsToTenLines_WhenNoBackendLineAndManyLines()
    {
        // Arrange
        var lines = new string[15];
        for (int i = 0; i < 15; i++)
        {
            lines[i] = $"Line {i + 1}";
        }
        var stackTrace = string.Join(Environment.NewLine, lines);

        // Act
        var result = GlobalExceptionHandler.TrimmedStackTrace(stackTrace);

        // Assert
        var expectedLines = lines.Take(10);
        var expected = string.Join(Environment.NewLine, expectedLines);
        result.Should().Be(expected);
    }
}