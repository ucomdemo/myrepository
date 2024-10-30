using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Net.Http.Headers;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


app.MapGet("/", () => "Hello World! from extension");

// make sure you change the App Name below
string yourGitHubAppName = "vvext";
string githubCopilotCompletionsUrl =
    "https://api.githubcopilot.com/chat/completions";


app.MapPost("/agent", async (
    [FromHeader(Name = "X-GitHub-Token")] string githubToken,
    [FromBody] Request userRequest) =>
{

    var octokitClient =
        new GitHubClient(
            new Octokit.ProductHeaderValue(yourGitHubAppName))
        {
            Credentials = new Credentials(githubToken)
        };
    var user = await octokitClient.User.Current();
    userRequest.Messages.Insert(0, new Message
    {
        Role = "system",
        Content =
        "每一個回應都以用戶的名稱開始， " +
        $"如 @{user.Login}"
    });
    userRequest.Messages.Insert(0, new Message
    {
        Role = "system",
        Content =
            "你是一個AI助理，以五歲小朋友的語氣回應使用者的提問。" 
    });
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", githubToken);
    userRequest.Stream = true;

    var copilotLLMResponse = await httpClient.PostAsJsonAsync(
    githubCopilotCompletionsUrl, userRequest);

    var responseStream =
    await copilotLLMResponse.Content.ReadAsStreamAsync();
    return Results.Stream(responseStream, "application/json");
});

app.MapGet("/callback", () => "你可以關掉這個分頁，回到先前的畫面。");
app.Run();
