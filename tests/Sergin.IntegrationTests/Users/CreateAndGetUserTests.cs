using System.Net;
using System.Net.Http.Json;
using Sergin.SharedKernel.Application;
using Sergin.UserAccess.Application.Users.Commands.Create;
using Sergin.UserAccess.Application.Users.Commands.GetList;
using Sergin.UserAccess.Presentation.WebApi.Users.Endpoints.Create;

namespace Sergin.IntegrationTests.Users;

[Collection(nameof(IntegrationTestCollection))]
public sealed class CreateAndGetUserTests(SerginApiFactory factory)
{
    [Fact]
    public async Task CreateUser_ThenListUsers_IncludesCreatedUser()
    {
        HttpClient client = factory.CreateClient();
        string userName = $"integration-test-{Guid.CreateVersion7()}";

        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/ua/users", new NewUserModel(userName));
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        CreateUserCommandResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateUserCommandResponse>();
        Assert.NotNull(created);

        HttpResponseMessage listResponse = await client.GetAsync("/ua/users?pageSize=100");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        ListQueryResponse<GetUserListItem>? list =
            await listResponse.Content.ReadFromJsonAsync<ListQueryResponse<GetUserListItem>>();
        Assert.NotNull(list);
        Assert.Contains(list.Data, item => item.Id == created.Id && item.UserName == userName);
    }
}
