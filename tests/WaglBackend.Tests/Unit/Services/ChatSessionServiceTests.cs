using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Infrastructure.Services;
using WaglBackend.Tests.Unit.Fixtures;
using Xunit;

namespace WaglBackend.Tests.Unit.Services;

public class ChatSessionServiceTests
{
    private readonly Mock<IChatSessionRepository> _mockChatSessionRepository;
    private readonly Mock<IChatRoomRepository> _mockChatRoomRepository;
    private readonly Mock<ILogger<ChatSessionService>> _mockLogger;
    private readonly ChatSessionService _service;
    private readonly Fixture _fixture;

    public ChatSessionServiceTests()
    {
        _mockChatSessionRepository = new Mock<IChatSessionRepository>();
        _mockChatRoomRepository = new Mock<IChatRoomRepository>();
        _mockLogger = new Mock<ILogger<ChatSessionService>>();
        _service = new ChatSessionService(
            _mockChatSessionRepository.Object,
            _mockChatRoomRepository.Object,
            _mockLogger.Object);
        _fixture = new Fixture();
        _fixture.Customize(new ValueObjectCustomizations());
    }

    [Theory]
    [AutoData]
    public async Task CreateSessionAsync_WithValidRequest_ShouldCreateSessionAndRooms(
        ChatSessionRequest request)
    {
        // Arrange
        request.MaxParticipants = 18; // Ensures 3 rooms
        request.DurationMinutes = 60;
        var userId = UserId.Create();

        var session = _fixture.Create<ChatSession>();
        var room = _fixture.Create<ChatRoom>();
        _mockChatSessionRepository.Setup(x => x.AddAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _mockChatRoomRepository.Setup(x => x.AddAsync(It.IsAny<ChatRoom>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        // Act
        var result = await _service.CreateSessionAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.MaxParticipants.Should().Be(request.MaxParticipants);
        result.DurationMinutes.Should().Be(request.DurationMinutes);
        result.Status.Should().Be(SessionStatus.Scheduled);

        _mockChatSessionRepository.Verify(x => x.AddAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockChatRoomRepository.Verify(x => x.AddAsync(It.IsAny<ChatRoom>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Theory]
    [AutoData]
    public async Task CreateSessionAsync_WithMaxParticipants6_ShouldCreateOneRoom(
        ChatSessionRequest request)
    {
        // Arrange
        request.MaxParticipants = 6;
        var userId = UserId.Create();

        var session = _fixture.Create<ChatSession>();
        var room = _fixture.Create<ChatRoom>();
        _mockChatSessionRepository.Setup(x => x.AddAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _mockChatRoomRepository.Setup(x => x.AddAsync(It.IsAny<ChatRoom>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        // Act
        var result = await _service.CreateSessionAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        _mockChatRoomRepository.Verify(x => x.AddAsync(It.IsAny<ChatRoom>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task StartSessionAsync_WithValidSessionId_ShouldUpdateStatusToActive(
        SessionId sessionId)
    {
        // Arrange
        var session = _fixture.Build<ChatSession>()
            .With(x => x.Id, sessionId)
            .With(x => x.Status, SessionStatus.Scheduled)
            .Create();

        _mockChatSessionRepository.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var updatedSession = _fixture.Create<ChatSession>();
        _mockChatSessionRepository.Setup(x => x.UpdateAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedSession);

        // Act
        var result = await _service.StartSessionAsync(sessionId);

        // Assert
        result.Should().BeTrue();
        _mockChatSessionRepository.Verify(x => x.UpdateAsync(It.Is<ChatSession>(s => s.Status == SessionStatus.Active), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task StartSessionAsync_WithNonExistentSession_ShouldReturnFalse(
        SessionId sessionId)
    {
        // Arrange
        _mockChatSessionRepository.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);

        // Act
        var result = await _service.StartSessionAsync(sessionId);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [AutoData]
    public async Task EndSessionAsync_WithActiveSession_ShouldUpdateStatusToEnded(
        SessionId sessionId)
    {
        // Arrange
        var session = _fixture.Build<ChatSession>()
            .With(x => x.Id, sessionId)
            .With(x => x.Status, SessionStatus.Active)
            .Create();

        _mockChatSessionRepository.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var updatedSession = _fixture.Create<ChatSession>();
        _mockChatSessionRepository.Setup(x => x.UpdateAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedSession);

        // Act
        var result = await _service.EndSessionAsync(sessionId);

        // Assert
        result.Should().BeTrue();
        _mockChatSessionRepository.Verify(x => x.UpdateAsync(It.Is<ChatSession>(s => s.Status == SessionStatus.Ended), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task GetSessionAsync_WithValidSessionId_ShouldReturnSession(
        SessionId sessionId)
    {
        // Arrange
        var session = _fixture.Build<ChatSession>()
            .With(x => x.Id, sessionId)
            .Create();

        _mockChatSessionRepository.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.GetSessionAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId.Value.ToString());
        result.Name.Should().Be(session.Name);
    }

    [Theory]
    [AutoData]
    public async Task GetSessionAsync_WithNonExistentSession_ShouldReturnNull(
        SessionId sessionId)
    {
        // Arrange
        _mockChatSessionRepository.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);

        // Act
        var result = await _service.GetSessionAsync(sessionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetScheduledSessionsAsync_ShouldReturnOnlyScheduledSessions()
    {
        // Arrange
        var sessions = new List<ChatSession>
        {
            _fixture.Build<ChatSession>().With(x => x.Status, SessionStatus.Scheduled).Create(),
            _fixture.Build<ChatSession>().With(x => x.Status, SessionStatus.Active).Create(),
            _fixture.Build<ChatSession>().With(x => x.Status, SessionStatus.Scheduled).Create(),
            _fixture.Build<ChatSession>().With(x => x.Status, SessionStatus.Ended).Create()
        };

        _mockChatSessionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _service.GetScheduledSessionsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.Status == SessionStatus.Scheduled);
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ShouldReturnOnlyActiveSessions()
    {
        // Arrange
        var sessions = new List<ChatSession>
        {
            _fixture.Build<ChatSession>().With(x => x.Status, SessionStatus.Scheduled).Create(),
            _fixture.Build<ChatSession>().With(x => x.Status, SessionStatus.Active).Create(),
            _fixture.Build<ChatSession>().With(x => x.Status, SessionStatus.Active).Create(),
            _fixture.Build<ChatSession>().With(x => x.Status, SessionStatus.Ended).Create()
        };

        _mockChatSessionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _service.GetActiveSessionsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.Status == SessionStatus.Active);
    }

    [Theory]
    [AutoData]
    public async Task DeleteSessionAsync_WithValidSessionId_ShouldDeleteSession(
        SessionId sessionId)
    {
        // Arrange
        var session = _fixture.Build<ChatSession>()
            .With(x => x.Id, sessionId)
            .Create();

        _mockChatSessionRepository.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _mockChatSessionRepository.Setup(x => x.DeleteAsync(sessionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteSessionAsync(sessionId);

        // Assert
        result.Should().BeTrue();
        _mockChatSessionRepository.Verify(x => x.DeleteAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task DeleteSessionAsync_WithNonExistentSession_ShouldReturnFalse(
        SessionId sessionId)
    {
        // Arrange
        _mockChatSessionRepository.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);

        // Act
        var result = await _service.DeleteSessionAsync(sessionId);

        // Assert
        result.Should().BeFalse();
    }
}