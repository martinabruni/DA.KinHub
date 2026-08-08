using DA.KinHub.Business.Common;
using DA.KinHub.Business.Identity;
using DA.KinHub.Business.KinList;
using DA.KinHub.Domain.Families;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Business.Tests;

public sealed class FamilySettingsServiceTests
{
    private static readonly Guid FamilyId = Guid.NewGuid();

    [Fact]
    public async Task MembersPageClampsPageSizeAndMapsItems()
    {
        var anchor = new FamilyMemberPageAnchor(DateTimeOffset.UtcNow, Guid.NewGuid());
        var applicationUserId = Guid.NewGuid();
        var repository = new StubMemberRepository(new FamilyMemberEntriesPage([new FamilyMemberEntry(applicationUserId, "Ada", "A", anchor)], false));
        var service = CreateService(memberRepository: repository);

        var result = await service.GetFamilyMembersPageAsync(FamilyId, applicationUserId, 999, null, CancellationToken.None);

        Assert.Equal(50, result.EffectivePageSize);
        Assert.Equal("Ada", Assert.Single(result.Items).DisplayName);
        Assert.True(Assert.Single(result.Items).IsCurrentUser);
        Assert.Equal(50, repository.PageSize);
    }

    [Fact]
    public async Task MembersPageDoesNotMarkAnotherMemberAsCurrentUser()
    {
        var anchor = new FamilyMemberPageAnchor(DateTimeOffset.UtcNow, Guid.NewGuid());
        var service = CreateService(memberRepository: new StubMemberRepository(new FamilyMemberEntriesPage([
            new FamilyMemberEntry(Guid.NewGuid(), "Ada", "A", anchor)
        ], false)));

        var result = await service.GetFamilyMembersPageAsync(FamilyId, Guid.NewGuid(), 20, null, CancellationToken.None);

        Assert.False(Assert.Single(result.Items).IsCurrentUser);
    }

    [Fact]
    public async Task InvalidCursorPageSizeIsMappedToValidationError()
    {
        var codec = new StubMemberCodec
        {
            Decoded = new DecodedFamilyMemberCursor(FamilyPageCursorDirection.Next, 10, new FamilyMemberPageAnchor(DateTimeOffset.UtcNow, Guid.NewGuid()))
        };
        var service = CreateService(memberCodec: codec);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() => service.GetFamilyMembersPageAsync(FamilyId, Guid.NewGuid(), 20, "cursor", CancellationToken.None));

        Assert.Equal(BusinessErrorCodes.PaginationCursorInvalid, exception.Code);
    }

    [Fact]
    public async Task EmptyInitialMembersPageIsReportedAsInconsistent()
    {
        var service = CreateService(memberRepository: new StubMemberRepository(new FamilyMemberEntriesPage([], false)));

        var exception = await Assert.ThrowsAsync<BusinessConflictException>(() => service.GetFamilyMembersPageAsync(FamilyId, Guid.NewGuid(), 20, null, CancellationToken.None));

        Assert.Equal(BusinessErrorCodes.FamilyStateInconsistent, exception.Code);
    }

    [Fact]
    public async Task InvitationsUseCurrentTimeAndMapActiveStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var invitationId = Guid.NewGuid();
        var repository = new StubInvitationRepository(new FamilyInvitationEntriesPage([
            new FamilyInvitationEntry(invitationId, new FamilyInvitationCreatorEntry("Ada", "A"), now, now.AddHours(1), new FamilyInvitationPageAnchor(now, invitationId))
        ], false));
        var service = CreateService(invitationRepository: repository, timeProvider: new FixedTimeProvider(now));

        var result = await service.GetActiveFamilyInvitationsPageAsync(FamilyId, 20, null, CancellationToken.None);

        Assert.Equal(now, repository.Now);
        Assert.Equal("active", Assert.Single(result.Items).Status);
    }

    private static FamilySettingsService CreateService(
        IFamilyDetailsRepository? detailsRepository = null,
        IFamilyMemberPageRepository? memberRepository = null,
        IFamilyInvitationPageRepository? invitationRepository = null,
        IFamilyMemberCursorCodec? memberCodec = null,
        IFamilyInvitationCursorCodec? invitationCodec = null,
        TimeProvider? timeProvider = null)
        => new(
            detailsRepository ?? new StubDetailsRepository(),
            memberRepository ?? new StubMemberRepository(new FamilyMemberEntriesPage([], false)),
            invitationRepository ?? new StubInvitationRepository(new FamilyInvitationEntriesPage([], false)),
            memberCodec ?? new StubMemberCodec(),
            invitationCodec ?? new StubInvitationCodec(),
            Options.Create(new PaginationReadOptions { ReadMax = 50 }),
            timeProvider ?? TimeProvider.System);

    private sealed class StubDetailsRepository : IFamilyDetailsRepository
    {
        public Task<FamilyDetailsEntry?> GetFamilyDetailsAsync(Guid familyId, CancellationToken cancellationToken) => Task.FromResult<FamilyDetailsEntry?>(new("Famiglia"));
    }

    private sealed class StubMemberRepository(FamilyMemberEntriesPage page) : IFamilyMemberPageRepository
    {
        public int PageSize { get; private set; }

        public Task<FamilyMemberEntriesPage> GetFamilyMembersPageAsync(Guid familyId, FamilyPageCursorDirection direction, FamilyMemberPageAnchor? anchor, int effectivePageSize, CancellationToken cancellationToken)
        {
            PageSize = effectivePageSize;
            return Task.FromResult(page);
        }
    }

    private sealed class StubInvitationRepository(FamilyInvitationEntriesPage page) : IFamilyInvitationPageRepository
    {
        public DateTimeOffset Now { get; private set; }

        public Task<FamilyInvitationEntriesPage> GetActiveFamilyInvitationsPageAsync(Guid familyId, FamilyPageCursorDirection direction, FamilyInvitationPageAnchor? anchor, int effectivePageSize, DateTimeOffset nowUtc, CancellationToken cancellationToken)
        {
            Now = nowUtc;
            return Task.FromResult(page);
        }
    }

    private sealed class StubMemberCodec : IFamilyMemberCursorCodec
    {
        public DecodedFamilyMemberCursor Decoded { get; init; } = new(FamilyPageCursorDirection.Next, 50, new FamilyMemberPageAnchor(DateTimeOffset.UtcNow, Guid.NewGuid()));
        public string Encode(Guid familyId, FamilyPageCursorDirection direction, int effectivePageSize, FamilyMemberPageAnchor anchor) => "encoded";
        public DecodedFamilyMemberCursor Decode(string opaqueCursor, Guid familyId) => Decoded;
    }

    private sealed class StubInvitationCodec : IFamilyInvitationCursorCodec
    {
        public string Encode(Guid familyId, FamilyPageCursorDirection direction, int effectivePageSize, FamilyInvitationPageAnchor anchor) => "encoded";
        public DecodedFamilyInvitationCursor Decode(string opaqueCursor, Guid familyId) => throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
