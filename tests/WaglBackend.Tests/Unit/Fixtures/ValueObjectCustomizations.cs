using AutoFixture;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Tests.Unit.Fixtures;

public class ValueObjectCustomizations : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // Value object customizations
        fixture.Register(() => SessionId.Create());
        fixture.Register(() => RoomId.Create());
        fixture.Register(() => UserId.Create());
        fixture.Register(() => InviteToken.Create());
        fixture.Register(() => TierLevel.Tier1);
        fixture.Register(() => Email.Create("test@example.com"));

        // Create SessionId from Guid for when needed
        fixture.Register<SessionId>(() => SessionId.From(fixture.Create<Guid>()));
        fixture.Register<RoomId>(() => RoomId.From(fixture.Create<Guid>()));
        fixture.Register<UserId>(() => UserId.From(fixture.Create<Guid>()));

        // Prevent recursion issues with entities that reference each other
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

    }
}