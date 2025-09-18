using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Domain.Organisms.Services;
using WaglBackend.Infrastructure.Services;
using WaglBackend.Tests.Unit.Fixtures;
using Xunit;

namespace WaglBackend.Tests.Unit.Services;

public class InviteManagementServiceTests
{
    private readonly Mock<ISessionInviteRepository> _mockInviteRepository;
    private readonly Mock<IChatSessionRepository> _mockChatSessionRepository;
    private readonly Mock<IRoomAllocationService> _mockRoomAllocationService;
    private readonly Mock<IParticipantTrackingService> _mockParticipantTrackingService;
    private readonly Mock<ILogger<InviteManagementService>> _mockLogger;
    private readonly InviteManagementService _service;
    private readonly Fixture _fixture;

    public InviteManagementServiceTests()
    {
        _mockInviteRepository = new Mock<ISessionInviteRepository>();
        _mockChatSessionRepository = new Mock<IChatSessionRepository>();
        _mockRoomAllocationService = new Mock<IRoomAllocationService>();
        _mockParticipantTrackingService = new Mock<IParticipantTrackingService>();
        _mockLogger = new Mock<ILogger<InviteManagementService>>();
        _service = new InviteManagementService(
            _mockInviteRepository.Object,
            _mockChatSessionRepository.Object,
            _mockRoomAllocationService.Object,
            _mockParticipantTrackingService.Object,
            _mockLogger.Object);
        _fixture = new Fixture();
        _fixture.Customize(new ValueObjectCustomizations());
    }

    [Theory]
    [AutoData]
    public async Task GenerateInviteAsync_WithValidRequest_ShouldCreateInvite(
        SessionInviteRequest request)
    {
        // Arrange
        var sessionId = SessionId.Create();
        request.SessionId = sessionId.Value.ToString();

        var session = _fixture.Build<ChatSession>()
            .With(x => x.Id, sessionId)
            .With(x => x.Status, SessionStatus.Scheduled)
            .Create();

        _mockChatSessionRepository.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _mockInviteRepository.Setup(x => x.AddAsync(It.IsAny<SessionInvite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<SessionInvite>());

        // Act
        var result = await _service.GenerateInviteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(request.SessionId);
        result.InviteeEmail.Should().Be(request.InviteeEmail);
        result.InviteeName.Should().Be(request.InviteeName);
        result.Token.Should().NotBeNullOrEmpty();
        result.IsConsumed.Should().BeFalse();

        _mockInviteRepository.Verify(x => x.AddAsync(It.IsAny<SessionInvite>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task GenerateBulkInvitesAsync_WithMultipleRecipients_ShouldCreateMultipleInvites(
        BulkSessionInviteRequest request)
    {
        // Arrange
        var sessionId = SessionId.Create();
        request.SessionId = sessionId.Value.ToString();
        request.Recipients = new List<InviteRecipient>
        {
            _fixture.Create<InviteRecipient>(),
            _fixture.Create<InviteRecipient>(),
            _fixture.Create<InviteRecipient>()
        };

        var session = _fixture.Build<ChatSession>()
            .With(x => x.Id, sessionId)
            .With(x => x.Status, SessionStatus.Scheduled)
            .Create();

        _mockChatSessionRepository.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _mockInviteRepository.Setup(x => x.AddAsync(It.IsAny<SessionInvite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<SessionInvite>());

        // Act
        var result = await _service.GenerateBulkInvitesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Invites.Should().HaveCount(3);
        result.TotalInvites.Should().Be(3);
        result.SuccessfulInvites.Should().Be(3);
        result.FailedInvites.Should().Be(0);

        _mockInviteRepository.Verify(x => x.AddAsync(It.IsAny<SessionInvite>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Theory]
    [AutoData]
    public async Task IsTokenValidAsync_WithValidToken_ShouldReturnTrue(
        InviteToken token)
    {
        // Arrange
        var invite = _fixture.Build<SessionInvite>()
            .With(x => x.Token, token)
            .With(x => x.IsConsumed, false)
            .With(x => x.IsExpired, false)
            .With(x => x.ExpiresAt, DateTime.UtcNow.AddHours(1))
            .Create();

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        var result = await _service.IsTokenValidAsync(token);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public async Task IsTokenValidAsync_WithExpiredToken_ShouldReturnFalse(
        InviteToken token)
    {
        // Arrange
        var invite = _fixture.Build<SessionInvite>()
            .With(x => x.Token, token)
            .With(x => x.IsConsumed, false)
            .With(x => x.IsExpired, true)
            .With(x => x.ExpiresAt, DateTime.UtcNow.AddHours(-1))
            .Create();

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        var result = await _service.IsTokenValidAsync(token);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [AutoData]
    public async Task IsTokenValidAsync_WithUsedToken_ShouldReturnFalse(
        InviteToken token)
    {
        // Arrange
        var invite = _fixture.Build<SessionInvite>()
            .With(x => x.Token, token)
            .With(x => x.IsConsumed, true)
            .With(x => x.IsExpired, false)
            .With(x => x.ExpiresAt, DateTime.UtcNow.AddHours(1))
            .Create();

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        var result = await _service.IsTokenValidAsync(token);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [AutoData]
    public async Task IsTokenValidAsync_WithNonExistentToken_ShouldReturnFalse(
        InviteToken token)
    {
        // Arrange
        _mockInviteRepository.Setup(x => x.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionInvite?)null);

        // Act
        var result = await _service.IsTokenValidAsync(token);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [AutoData]
    public async Task ConsumeInviteAsync_WithValidToken_ShouldMarkInviteAsUsed(
        InviteToken token, string displayName)
    {
        // Arrange
        var userId = UserId.Create();
        var invite = _fixture.Build<SessionInvite>()
            .With(x => x.Token, token)
            .With(x => x.IsConsumed, false)
            .With(x => x.IsExpired, false)
            .With(x => x.ExpiresAt, DateTime.UtcNow.AddHours(1))
            .Create();

        var roomJoinResponse = new RoomJoinResponse { Success = true, RoomId = "room1", ParticipantId = "part1" };
        var session = _fixture.Create<ChatSession>();

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);
        _mockChatSessionRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _mockRoomAllocationService.Setup(x => x.AllocateParticipantToRoomAsync(It.IsAny<SessionId>(), It.IsAny<string>(), It.IsAny<UserId?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roomJoinResponse);
        _mockInviteRepository.Setup(x => x.UpdateAsync(It.IsAny<SessionInvite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<SessionInvite>());

        // Act
        var result = await _service.ConsumeInviteAsync(token, displayName, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RoomId.Should().NotBeNullOrEmpty();

        _mockInviteRepository.Verify(x => x.UpdateAsync(It.Is<SessionInvite>(i => i.IsConsumed == true), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ConsumeInviteAsync_WithInvalidToken_ShouldReturnFailure(
        InviteToken token, string displayName)
    {
        // Arrange
        var userId = UserId.Create();

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionInvite?)null);

        // Act
        var result = await _service.ConsumeInviteAsync(token, displayName, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Theory]
    [AutoData]
    public async Task GetInviteAsync_WithValidToken_ShouldReturnInvite(
        InviteToken token)
    {
        // Arrange
        var invite = _fixture.Build<SessionInvite>()
            .With(x => x.Token, token)
            .Create();

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        var result = await _service.GetInviteAsync(token);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be(token.Value);
    }

    [Theory]
    [AutoData]
    public async Task GetActiveInvitesAsync_ShouldReturnActiveInvites(
        SessionId sessionId)
    {
        // Arrange
        var invites = new List<SessionInvite>
        {
            _fixture.Build<SessionInvite>().With(x => x.SessionId, sessionId).With(x => x.IsConsumed, false).With(x => x.IsExpired, false).Create(),
            _fixture.Build<SessionInvite>().With(x => x.SessionId, sessionId).With(x => x.IsConsumed, true).With(x => x.IsExpired, false).Create(),
            _fixture.Build<SessionInvite>().With(x => x.SessionId, sessionId).With(x => x.IsConsumed, false).With(x => x.IsExpired, false).Create()
        };

        _mockInviteRepository.Setup(x => x.GetInvitesBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invites);

        // Act
        var result = await _service.GetActiveInvitesAsync(sessionId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => !i.IsConsumed && !i.IsExpired);
    }

    [Theory]
    [AutoData]
    public async Task GenerateInviteUrlAsync_WithValidToken_ShouldReturnUrl(
        InviteToken token, string baseUrl)
    {
        // Arrange
        var expectedUrl = $"{baseUrl}/join/{token.Value}";

        // Act
        var result = await _service.GenerateInviteUrlAsync(token, baseUrl);

        // Assert
        result.Should().Be(expectedUrl);
    }

    [Theory]
    [AutoData]
    public async Task GetActiveInviteCountAsync_ShouldReturnCorrectCount(
        SessionId sessionId)
    {
        // Arrange
        var activeInvites = _fixture.Build<SessionInvite>()
            .With(x => x.SessionId, sessionId)
            .With(x => x.IsConsumed, false)
            .With(x => x.IsExpired, false)
            .With(x => x.ExpiresAt, DateTime.UtcNow.AddHours(1)) // Not expired by time
            .CreateMany(5)
            .ToList();

        var expiredInvites = _fixture.Build<SessionInvite>()
            .With(x => x.SessionId, sessionId)
            .With(x => x.IsConsumed, false)
            .With(x => x.IsExpired, false)
            .With(x => x.ExpiresAt, DateTime.UtcNow.AddHours(-1)) // Expired by time
            .CreateMany(2)
            .ToList();

        var consumedInvites = _fixture.Build<SessionInvite>()
            .With(x => x.SessionId, sessionId)
            .With(x => x.IsConsumed, true)
            .With(x => x.IsExpired, false)
            .With(x => x.ExpiresAt, DateTime.UtcNow.AddHours(1))
            .CreateMany(1)
            .ToList();

        var allInvites = activeInvites.Concat(expiredInvites).Concat(consumedInvites).ToList();

        _mockInviteRepository.Setup(x => x.GetInvitesBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allInvites);

        // Act
        var result = await _service.GetActiveInviteCountAsync(sessionId);

        // Assert
        result.Should().Be(5);
    }

    [Theory]
    [AutoData]
    public async Task GetInviteStatisticsAsync_ShouldReturnComprehensiveStatistics(
        SessionId sessionId)
    {
        // Arrange
        var invites = new List<SessionInvite>
        {
            _fixture.Build<SessionInvite>().With(x => x.SessionId, sessionId).With(x => x.IsConsumed, false).With(x => x.IsExpired, false).Create(),
            _fixture.Build<SessionInvite>().With(x => x.SessionId, sessionId).With(x => x.IsConsumed, true).With(x => x.IsExpired, false).Create(),
            _fixture.Build<SessionInvite>().With(x => x.SessionId, sessionId).With(x => x.IsConsumed, false).With(x => x.IsExpired, true).Create(),
            _fixture.Build<SessionInvite>().With(x => x.SessionId, sessionId).With(x => x.IsConsumed, true).With(x => x.IsExpired, false).Create()
        };

        _mockInviteRepository.Setup(x => x.GetBySessionIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invites);

        // Act
        var result = await _service.GetInviteStatisticsAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId.Value.ToString());
        result.TotalInvites.Should().Be(4);
        result.ActiveInvites.Should().Be(1);
        result.ConsumedInvites.Should().Be(2);
        result.ExpiredInvites.Should().Be(1);
        result.ConversionRate.Should().Be(0.5); // 2/4 = 50%
    }

    [Theory]
    [AutoData]
    public async Task ExpireInviteAsync_WithValidToken_ShouldMarkInviteAsExpired(
        InviteToken token)
    {
        // Arrange
        var invite = _fixture.Build<SessionInvite>()
            .With(x => x.Token, token)
            .With(x => x.IsExpired, false)
            .Create();

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);
        _mockInviteRepository.Setup(x => x.UpdateAsync(It.IsAny<SessionInvite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<SessionInvite>());

        // Act
        var result = await _service.ExpireInviteAsync(token);

        // Assert
        result.Should().BeTrue();
        _mockInviteRepository.Verify(x => x.UpdateAsync(It.Is<SessionInvite>(i => i.IsExpired == true), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task DeleteExpiredInvitesAsync_ShouldDeleteExpiredInvites()
    {
        // Arrange
        // DeleteExpiredInvitesAsync doesn't exist in repository, so we'll mock GetAllAsync instead
        var expiredInvites = _fixture.Build<SessionInvite>()
            .With(x => x.IsExpired, true)
            .CreateMany(3);
        _mockInviteRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredInvites);
        _mockInviteRepository.Setup(x => x.DeleteAsync(It.IsAny<SessionInvite>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteExpiredInvitesAsync();

        // Assert
        result.Should().BeTrue();
        _mockInviteRepository.Verify(x => x.DeleteAsync(It.IsAny<SessionInvite>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
}