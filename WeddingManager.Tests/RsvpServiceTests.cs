using System.Text.Json;
using Moq;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class RsvpServiceTests
{
    private static readonly Guid WeddingId = Guid.NewGuid();

    [Fact]
    public async Task GetFlowStateAsync_ReturnsNoFlows_WhenNoneExist()
    {
        var ctx = new Context();
        ctx.FlowRepo.Setup(r => r.GetByWeddingIdAsync(WeddingId)).ReturnsAsync(new List<InvitationFlow>());

        var result = await ctx.Service.GetFlowStateAsync("slug");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.HasFlows);
    }

    [Fact]
    public async Task GetFlowStateAsync_ReturnsOpenFlowDirectly()
    {
        var ctx = new Context();
        ctx.FlowRepo.Setup(r => r.GetByWeddingIdAsync(WeddingId))
            .ReturnsAsync(new List<InvitationFlow> { Flow(passcode: null) });

        var result = await ctx.Service.GetFlowStateAsync("slug");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.HasFlows);
        Assert.False(result.Value.RequiresPasscode);
        Assert.NotNull(result.Value.Flow);
    }

    [Fact]
    public async Task GetFlowStateAsync_RequiresPasscode_WhenAllFlowsGated()
    {
        var ctx = new Context();
        ctx.FlowRepo.Setup(r => r.GetByWeddingIdAsync(WeddingId))
            .ReturnsAsync(new List<InvitationFlow> { Flow(passcode: "abc") });

        var result = await ctx.Service.GetFlowStateAsync("slug");

        Assert.True(result.Value!.RequiresPasscode);
        Assert.Null(result.Value.Flow);
    }

    [Fact]
    public async Task UnlockAsync_ReturnsUnauthorized_OnWrongPasscode()
    {
        var ctx = new Context();
        ctx.FlowRepo.Setup(r => r.GetByPasscodeAsync(WeddingId, "wrong")).ReturnsAsync((InvitationFlow?)null);

        var result = await ctx.Service.UnlockAsync("slug", "wrong");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Unauthorized, result.Errors[0].Code);
    }

    [Fact]
    public async Task UnlockAsync_ReturnsFlow_OnCorrectPasscode()
    {
        var ctx = new Context();
        ctx.FlowRepo.Setup(r => r.GetByPasscodeAsync(WeddingId, "secret")).ReturnsAsync(Flow(passcode: "secret"));

        var result = await ctx.Service.UnlockAsync("slug", "secret");

        Assert.True(result.IsSuccess);
        Assert.Equal(WeddingId, result.Value!.WeddingId);
    }

    [Fact]
    public async Task SubmitAsync_ReturnsConflict_WhenDuplicate()
    {
        var ctx = new Context();
        var flow = Flow(passcode: "secret");
        ctx.FlowRepo.Setup(r => r.GetByIdAsync(flow.Id)).ReturnsAsync(flow);
        ctx.GuestRepo.Setup(r => r.FindByDedupeKeyAsync(WeddingId, "jane@x.com", "Jane", "Doe"))
            .ReturnsAsync(new Guest { Id = Guid.NewGuid(), Name = "Jane" });

        var result = await ctx.Service.SubmitAsync("slug", flow.Id, ValidSubmit());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
        ctx.ResponseRepo.Verify(r => r.SubmitAsync(It.IsAny<IReadOnlyCollection<Guest>>(),
            It.IsAny<IReadOnlyCollection<RsvpResponse>>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_CreatesGuestAndResponse()
    {
        var ctx = new Context();
        var flow = Flow(passcode: "secret");
        ctx.FlowRepo.Setup(r => r.GetByIdAsync(flow.Id)).ReturnsAsync(flow);
        IReadOnlyCollection<Guest>? capturedGuests = null;
        IReadOnlyCollection<RsvpResponse>? capturedResponses = null;
        ctx.ResponseRepo
            .Setup(r => r.SubmitAsync(It.IsAny<IReadOnlyCollection<Guest>>(), It.IsAny<IReadOnlyCollection<RsvpResponse>>()))
            .Callback<IReadOnlyCollection<Guest>, IReadOnlyCollection<RsvpResponse>>((g, r) =>
            {
                capturedGuests = g;
                capturedResponses = r;
            })
            .ReturnsAsync(true);

        var result = await ctx.Service.SubmitAsync("slug", flow.Id, ValidSubmit());

        Assert.True(result.IsSuccess);
        Assert.Single(capturedGuests!);
        Assert.Single(capturedResponses!);
        Assert.Equal("Jane", capturedGuests!.First().Name);
        Assert.Equal(RsvpStatus.Accepted, capturedGuests!.First().RsvpStatus);
    }

    [Fact]
    public async Task SubmitAsync_CreatesPlusOneGuestAndResponse()
    {
        var ctx = new Context();
        var flow = Flow(passcode: "secret");
        flow.IncludePlusOne = true;
        ctx.FlowRepo.Setup(r => r.GetByIdAsync(flow.Id)).ReturnsAsync(flow);
        IReadOnlyCollection<Guest>? capturedGuests = null;
        ctx.ResponseRepo
            .Setup(r => r.SubmitAsync(It.IsAny<IReadOnlyCollection<Guest>>(), It.IsAny<IReadOnlyCollection<RsvpResponse>>()))
            .Callback<IReadOnlyCollection<Guest>, IReadOnlyCollection<RsvpResponse>>((g, _) => capturedGuests = g)
            .ReturnsAsync(true);

        var submit = ValidSubmit();
        submit.PlusOneAttending = true;
        submit.PlusOneName = "John";

        var result = await ctx.Service.SubmitAsync("slug", flow.Id, submit);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.PlusOneGuestId);
        Assert.Equal(2, capturedGuests!.Count);
        var plusOne = capturedGuests!.First(g => g.PlusOneOfGuestId != null);
        Assert.Equal("John", plusOne.Name);
        Assert.Equal("jane@x.com", plusOne.Email);
    }

    [Fact]
    public async Task SubmitAsync_RejectsRequiredQuestionMissing()
    {
        var ctx = new Context();
        var flow = Flow(passcode: "secret");
        flow.CustomQuestions =
        [
            new QuestionDefinition { Id = Guid.NewGuid(), Type = RsvpQuestionType.FreeText, Label = "Song", Required = true }
        ];
        ctx.FlowRepo.Setup(r => r.GetByIdAsync(flow.Id)).ReturnsAsync(flow);

        var result = await ctx.Service.SubmitAsync("slug", flow.Id, ValidSubmit());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task SubmitAsync_RejectsEventNotInFlow()
    {
        var ctx = new Context();
        var flow = Flow(passcode: "secret");
        ctx.FlowRepo.Setup(r => r.GetByIdAsync(flow.Id)).ReturnsAsync(flow);

        var submit = ValidSubmit();
        submit.AttendingEventIds = [Guid.NewGuid()];

        var result = await ctx.Service.SubmitAsync("slug", flow.Id, submit);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    private static InvitationFlow Flow(string? passcode) => new()
    {
        Id = Guid.NewGuid(),
        WeddingId = WeddingId,
        Name = "Flow",
        Passcode = passcode
    };

    private static RsvpFlowSubmitRequestDto ValidSubmit() => new()
    {
        Name = "Jane",
        Surname = "Doe",
        Email = "jane@x.com",
        Status = RsvpResponseStatus.Attending,
        AttendingEventIds = new List<Guid>(),
        CustomAnswers = new Dictionary<string, JsonElement>()
    };

    private sealed class Context
    {
        public Mock<IWeddingRepository> WeddingRepo { get; } = new();
        public Mock<IInvitationFlowRepository> FlowRepo { get; } = new();
        public Mock<IRsvpResponseRepository> ResponseRepo { get; } = new();
        public Mock<IGuestRepository> GuestRepo { get; } = new();
        public Mock<IEventRepository> EventRepo { get; } = new();
        public RsvpService Service { get; }

        public Context()
        {
            WeddingRepo.Setup(r => r.GetByIdOrSlugAsync(It.IsAny<string>()))
                .ReturnsAsync(new Wedding { Id = WeddingId, Title = "Test", Slug = "slug" });
            EventRepo.Setup(r => r.GetByWeddingIdUnscopedAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Event>());
            Service = new RsvpService(WeddingRepo.Object, FlowRepo.Object, ResponseRepo.Object,
                GuestRepo.Object, EventRepo.Object);
        }
    }
}
