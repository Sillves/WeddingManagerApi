using Moq;
using WeddingManager.Application.Mappings;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class InvitationFlowServiceTests
{
    private readonly ApplicationMapper _mapper = new();

    [Fact]
    public async Task CreateAsync_CreatesOpenFlow_WhenNoOtherFlowsExist()
    {
        var flowRepo = new Mock<IInvitationFlowRepository>();
        flowRepo.Setup(r => r.GetByWeddingIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<InvitationFlow>());
        var service = CreateService(flowRepo);

        var result = await service.CreateAsync(Guid.NewGuid(), new CreateInvitationFlowRequestDto { Name = "Everyone" });

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Passcode);
        flowRepo.Verify(r => r.AddAsync(It.IsAny<InvitationFlow>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RejectsOpenFlow_WhenOtherFlowsExist()
    {
        var weddingId = Guid.NewGuid();
        var flowRepo = new Mock<IInvitationFlowRepository>();
        flowRepo.Setup(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(new List<InvitationFlow> { new() { Id = Guid.NewGuid(), WeddingId = weddingId, Passcode = "abc" } });
        var service = CreateService(flowRepo);

        var result = await service.CreateAsync(weddingId, new CreateInvitationFlowRequestDto { Name = "Open", Passcode = null });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
        flowRepo.Verify(r => r.AddAsync(It.IsAny<InvitationFlow>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RejectsPasscodedFlow_WhenOpenFlowExists()
    {
        var weddingId = Guid.NewGuid();
        var flowRepo = new Mock<IInvitationFlowRepository>();
        flowRepo.Setup(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(new List<InvitationFlow> { new() { Id = Guid.NewGuid(), WeddingId = weddingId, Passcode = null } });
        var service = CreateService(flowRepo);

        var result = await service.CreateAsync(weddingId, new CreateInvitationFlowRequestDto { Name = "Family", Passcode = "secret" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicatePasscode()
    {
        var weddingId = Guid.NewGuid();
        var flowRepo = new Mock<IInvitationFlowRepository>();
        flowRepo.Setup(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(new List<InvitationFlow> { new() { Id = Guid.NewGuid(), WeddingId = weddingId, Passcode = "secret" } });
        var service = CreateService(flowRepo);

        var result = await service.CreateAsync(weddingId, new CreateInvitationFlowRequestDto { Name = "Friends", Passcode = "secret" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
    }

    [Fact]
    public async Task CreateAsync_AllowsSecondPasscodedFlow()
    {
        var weddingId = Guid.NewGuid();
        var flowRepo = new Mock<IInvitationFlowRepository>();
        flowRepo.Setup(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(new List<InvitationFlow> { new() { Id = Guid.NewGuid(), WeddingId = weddingId, Passcode = "first" } });
        var service = CreateService(flowRepo);

        var result = await service.CreateAsync(weddingId, new CreateInvitationFlowRequestDto { Name = "Friends", Passcode = "second" });

        Assert.True(result.IsSuccess);
        flowRepo.Verify(r => r.AddAsync(It.IsAny<InvitationFlow>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RejectsChoiceQuestionWithoutOptions()
    {
        var flowRepo = new Mock<IInvitationFlowRepository>();
        flowRepo.Setup(r => r.GetByWeddingIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<InvitationFlow>());
        var service = CreateService(flowRepo);

        var request = new CreateInvitationFlowRequestDto
        {
            Name = "Flow",
            CustomQuestions =
            [
                new QuestionDefinition { Type = RsvpQuestionType.SingleChoice, Label = "Pick", Options = [] }
            ]
        };

        var result = await service.CreateAsync(Guid.NewGuid(), request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsEventIdNotInWedding()
    {
        var weddingId = Guid.NewGuid();
        var flowRepo = new Mock<IInvitationFlowRepository>();
        flowRepo.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(new List<InvitationFlow>());
        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByWeddingIdUnscopedAsync(weddingId)).ReturnsAsync(new List<Event>());
        var service = CreateService(flowRepo, eventRepo: eventRepo);

        var request = new CreateInvitationFlowRequestDto { Name = "Flow", EventIds = [Guid.NewGuid()] };

        var result = await service.CreateAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task DeleteAsync_BlocksWhenResponsesExist_AndForceFalse()
    {
        var weddingId = Guid.NewGuid();
        var flowId = Guid.NewGuid();
        var flowRepo = new Mock<IInvitationFlowRepository>();
        flowRepo.Setup(r => r.GetByIdAsync(flowId))
            .ReturnsAsync(new InvitationFlow { Id = flowId, WeddingId = weddingId });
        var responseRepo = new Mock<IRsvpResponseRepository>();
        responseRepo.Setup(r => r.CountByFlowAsync(flowId)).ReturnsAsync(3);
        var service = CreateService(flowRepo, responseRepo);

        var result = await service.DeleteAsync(weddingId, flowId, force: false);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
        flowRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_DeletesWhenForced()
    {
        var weddingId = Guid.NewGuid();
        var flowId = Guid.NewGuid();
        var flowRepo = new Mock<IInvitationFlowRepository>();
        flowRepo.Setup(r => r.GetByIdAsync(flowId))
            .ReturnsAsync(new InvitationFlow { Id = flowId, WeddingId = weddingId });
        var responseRepo = new Mock<IRsvpResponseRepository>();
        responseRepo.Setup(r => r.CountByFlowAsync(flowId)).ReturnsAsync(3);
        var service = CreateService(flowRepo, responseRepo);

        var result = await service.DeleteAsync(weddingId, flowId, force: true);

        Assert.True(result.IsSuccess);
        flowRepo.Verify(r => r.DeleteAsync(flowId), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenFlowBelongsToAnotherWedding()
    {
        var flowId = Guid.NewGuid();
        var flowRepo = new Mock<IInvitationFlowRepository>();
        flowRepo.Setup(r => r.GetByIdAsync(flowId))
            .ReturnsAsync(new InvitationFlow { Id = flowId, WeddingId = Guid.NewGuid() });
        var service = CreateService(flowRepo);

        var result = await service.UpdateAsync(Guid.NewGuid(), flowId, new UpdateInvitationFlowRequestDto { Name = "X" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    private InvitationFlowService CreateService(
        Mock<IInvitationFlowRepository> flowRepo,
        Mock<IRsvpResponseRepository>? responseRepo = null,
        Mock<IEventRepository>? eventRepo = null)
    {
        responseRepo ??= new Mock<IRsvpResponseRepository>();
        responseRepo.Setup(r => r.GetCountsByWeddingAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
        eventRepo ??= new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByWeddingIdUnscopedAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Event>());
        return new InvitationFlowService(flowRepo.Object, responseRepo.Object, eventRepo.Object, _mapper);
    }
}
