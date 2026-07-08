using Sergin.SharedKernel.Domain;

namespace Sergin.UserAccess.Domain.Users;

public class User : AggregateRoot<UserInternalId>
{
    private User() { }

    public UserName UserName { get; private set; }

    public static User Create(UserName userName)
    {
        return new User
        {
            Id = new UserInternalId(Guid.CreateVersion7()),
            UserName = userName
        };
    }
}

public sealed record UserInternalId(Guid Value);
public sealed record UserName(string Value);
