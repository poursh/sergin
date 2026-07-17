using Sergin.SharedKernel.Domain;

namespace Sergin.UserAccess.Domain.Users;

public class User : AggregateRoot<UserInternalId>
{
    private User() { }

    public UserName UserName { get; private set; }
    public bool IsActive { get; private set; }

    public static User Create(UserName userName)
    {
        return new User
        {
            Id = new UserInternalId(Guid.CreateVersion7()),
            UserName = userName,
            IsActive = true
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

public sealed record UserInternalId(Guid Value);
public sealed record UserName(string Value);
