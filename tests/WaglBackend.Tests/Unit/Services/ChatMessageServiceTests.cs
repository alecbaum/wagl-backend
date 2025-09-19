using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Domain.Organisms.Services;
using WaglBackend.Infrastructure.Services;
using WaglBackend.Tests.Unit.Fixtures;
using Xunit;

namespace WaglBackend.Tests.Unit.Services;

public class ChatMessageServiceTests
{
    private readonly Mock<IChatMessageRepository> _mockMessageRepository;
    private readonly Mock<IParticipantRepository> _mockParticipantRepository;
    private readonly Mock<IUAIIntegrationService> _mockUAIIntegrationService;
    private readonly Mock<ISystemParticipantService> _mockSystemParticipantService;
    private readonly Mock<ILogger<ChatMessageService>> _mockLogger;
    private readonly ChatMessageService _service;
    private readonly Fixture _fixture;

    public ChatMessageServiceTests()
    {
        _mockMessageRepository = new Mock<IChatMessageRepository>();
        _mockParticipantRepository = new Mock<IParticipantRepository>();
        _mockUAIIntegrationService = new Mock<IUAIIntegrationService>();
        _mockSystemParticipantService = new Mock<ISystemParticipantService>();
        _mockLogger = new Mock<ILogger<ChatMessageService>>();
        _service = new ChatMessageService(
            _mockMessageRepository.Object,
            _mockParticipantRepository.Object,
            _mockUAIIntegrationService.Object,
            _mockSystemParticipantService.Object,
            _mockLogger.Object);
        _fixture = new Fixture();
        _fixture.Customize(new ValueObjectCustomizations());
    }

    [Theory]
    [AutoData]
    public async Task SendMessageAsync_WithValidMessage_ShouldCreateAndReturnMessage(
        ChatMessageRequest request)
    {
        // Arrange
        var participantId = Guid.NewGuid();
        var roomId = RoomId.Create();
        var sessionId = SessionId.Create();

        // Fix the request to have a valid GUID for RoomId
        request.RoomId = roomId.Value.ToString();

        var participant = _fixture.Build<Participant>()
            .With(p => p.Id, participantId)
            .With(p => p.RoomId, roomId)
            .With(p => p.SessionId, sessionId)
            .With(p => p.IsActive, true)
            .Create();

        var message = _fixture.Build<ChatMessage>()
            .With(m => m.RoomId, roomId)
            .With(m => m.SessionId, sessionId)
            .With(m => m.ParticipantId, participantId)
            .Create();

        _mockParticipantRepository.Setup(x => x.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
        _mockMessageRepository.Setup(x => x.AddAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        // Act
        var result = await _service.SendMessageAsync(request, participantId);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be(request.Content);
        result.RoomId.Should().Be(request.RoomId);

        _mockMessageRepository.Verify(x => x.AddAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task GetRoomMessagesAsync_WithValidRoomId_ShouldReturnMessages(
        RoomId roomId)
    {
        // Arrange
        var messages = _fixture.Build<ChatMessage>()
            .With(x => x.RoomId, roomId)
            .CreateMany(5)
            .ToList();

        var participants = messages.Select(m =>
            _fixture.Build<Participant>()
                .With(p => p.Id, m.ParticipantId)
                .Create()).ToList();

        _mockMessageRepository.Setup(x => x.GetMessagesByRoomAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Setup participant lookups for each message
        foreach (var participant in participants)
        {
            _mockParticipantRepository.Setup(x => x.GetByIdAsync(participant.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);
        }

        // Act
        var result = await _service.GetRoomMessagesAsync(roomId);

        // Assert
        result.Should().HaveCount(5);
        result.Should().OnlyContain(m => m.RoomId == roomId.Value.ToString());
    }

    [Theory]
    [AutoData]
    public async Task GetRoomMessageCountAsync_WithValidRoomId_ShouldReturnCount(
        RoomId roomId)
    {
        // Arrange
        _mockMessageRepository.Setup(x => x.GetMessageCountByRoomAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);

        // Act
        var result = await _service.GetRoomMessageCountAsync(roomId);

        // Assert
        result.Should().Be(8);
    }

    [Theory]
    [AutoData]
    public async Task GetRecentRoomMessagesAsync_WithLimit_ShouldReturnLimitedMessages(
        RoomId roomId)
    {
        // Arrange
        var messages = _fixture.Build<ChatMessage>()
            .With(x => x.RoomId, roomId)
            .CreateMany(10)
            .OrderByDescending(m => m.SentAt)
            .ToList();

        _mockMessageRepository.Setup(x => x.GetByRoomIdAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _service.GetRecentRoomMessagesAsync(roomId, 5);

        // Assert
        result.Should().HaveCount(5);
        result.Should().OnlyContain(m => m.RoomId == roomId.Value.ToString());
    }

    [Theory]
    [AutoData]
    public async Task GetAverageMessageLengthAsync_WithMessages_ShouldReturnAverage(
        RoomId roomId)
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            _fixture.Build<ChatMessage>().With(x => x.RoomId, roomId).With(x => x.Content, "Hi").Create(), // 2 chars
            _fixture.Build<ChatMessage>().With(x => x.RoomId, roomId).With(x => x.Content, "Hello").Create(), // 5 chars
            _fixture.Build<ChatMessage>().With(x => x.RoomId, roomId).With(x => x.Content, "Hello World!").Create() // 12 chars
        };

        _mockMessageRepository.Setup(x => x.GetByRoomIdAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _service.GetAverageMessageLengthAsync(roomId);

        // Assert
        result.Should().BeApproximately(6.33, 0.1); // (2 + 5 + 12) / 3 = 6.33
    }

    [Theory]
    [AutoData]
    public async Task GetAverageMessageLengthAsync_WithNoMessages_ShouldReturnZero(
        RoomId roomId)
    {
        // Arrange
        _mockMessageRepository.Setup(x => x.GetByRoomIdAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessage>());

        // Act
        var result = await _service.GetAverageMessageLengthAsync(roomId);

        // Assert
        result.Should().Be(0);
    }

    [Theory]
    [AutoData]
    public async Task GetMessagesByParticipantAsync_WithValidParticipantId_ShouldReturnParticipantMessages(
        Guid participantId)
    {
        // Arrange
        var messages = _fixture.Build<ChatMessage>()
            .With(x => x.ParticipantId, participantId)
            .CreateMany(4)
            .ToList();

        _mockMessageRepository.Setup(x => x.GetMessagesByParticipantAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _service.GetMessagesByParticipantAsync(participantId);

        // Assert
        result.Should().HaveCount(4);
        result.Should().OnlyContain(m => m.ParticipantId == participantId.ToString());
    }

    [Theory]
    [AutoData]
    public async Task GetMessageCountByRoomAsync_ShouldReturnCorrectCount(
        RoomId roomId)
    {
        // Arrange
        var messages = _fixture.Build<ChatMessage>()
            .With(x => x.RoomId, roomId)
            .CreateMany(15)
            .ToList();

        _mockMessageRepository.Setup(x => x.GetByRoomIdAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _service.GetMessageCountByRoomAsync(roomId);

        // Assert
        result.Should().Be(15);
    }

    [Theory]
    [AutoData]
    public async Task DeleteMessageAsync_WithValidMessageId_ShouldDeleteMessage(
        Guid messageId)
    {
        // Arrange
        var participantId = Guid.NewGuid();

        _mockMessageRepository.Setup(x => x.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<ChatMessage>());

        // Act
        var result = await _service.DeleteMessageAsync(messageId, participantId);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public async Task GetMessageHistoryAsync_ShouldReturnFormattedHistory(
        RoomId roomId)
    {
        // Arrange
        var messages = _fixture.Build<ChatMessage>()
            .With(x => x.RoomId, roomId)
            .CreateMany(10)
            .ToList();

        var participants = messages.Select(m =>
            _fixture.Build<Participant>()
                .With(p => p.Id, m.ParticipantId)
                .Create()).ToList();

        _mockMessageRepository.Setup(x => x.GetMessageCountByRoomAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);
        _mockMessageRepository.Setup(x => x.GetMessagesByRoomPaginatedAsync(roomId, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Setup participant lookups for each message
        foreach (var participant in participants)
        {
            _mockParticipantRepository.Setup(x => x.GetByIdAsync(participant.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);
        }

        // Act
        var result = await _service.GetMessageHistoryAsync(roomId, 1, 50);

        // Assert
        result.Should().NotBeNull();
        result.RoomId.Should().Be(roomId.Value.ToString());
        result.Messages.Should().NotBeEmpty();
        result.TotalMessages.Should().Be(10);
    }
}