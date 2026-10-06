using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace GymBook.Api.Feedback;

/// <summary>
/// Where feature requests and bug reports go: a private GitHub repository's issues, put on a GitHub project board. From
/// GitHub__* in .env; feedback is off while the token, owner or repository is unset.
/// <para>
/// The token is a classic personal access token with the "repo" and "project" scopes (fine-grained tokens can't reach a
/// project owned by a user), of an account that's an admin of the repository: deleting an issue takes one.
/// </para>
/// </summary>
public class GitHubOptions
{
    public string Token { get; set; } = "";
    /// <summary>The repository's owner (user or organization) and name. It must be private: bug reports are.</summary>
    public string Owner { get; set; } = "";
    public string Repo { get; set; } = "";
    /// <summary>
    /// The project board every ticket is added to: its owner (a user or organization; the repository's owner when unset)
    /// and its number, from its address (github.com/users/NAME/projects/NUMBER). 0: no board. Its single-select "Status"
    /// field is shown in the app as where each ticket is, and a "Votes" number field, if it has one, is kept up to date.
    /// </summary>
    public string ProjectOwner { get; set; } = "";
    public int ProjectNumber { get; set; }
    /// <summary>The branch attachments are committed to, made from the default branch if it doesn't exist.</summary>
    public string AttachmentsBranch { get; set; } = "feedback-attachments";

    public bool IsConfigured => Token.Length > 0 && Owner.Length > 0 && Repo.Length > 0;
}

/// <summary>Something the user should be told: a status for the response and what went wrong.</summary>
public class FeedbackException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public record GitHubIssue(
    int Number, string NodeId, string Title, string Body, string BodyHtml, bool IsClosed, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, int CommentCount, IReadOnlyList<string> Labels, string? Status, string? ProjectItemId);

public record GitHubComment(long Id, string Body, string BodyHtml, DateTimeOffset CreatedAt, bool Edited);

/// <summary>
/// The GitHub calls feedback needs: issues and their comments (REST to write, GraphQL to read, with the HTML GitHub
/// shows), the project board, and attachment files in the repository. One token for everything, so who wrote what is
/// kept by the API itself (see FeedbackService).
/// </summary>
public sealed class GitHubClient
{
    public const string FeatureLabel = "feature request";
    public const string BugLabel = "bug";
    /// <summary>The folder attachments go in, on <see cref="GitHubOptions.AttachmentsBranch"/>.</summary>
    public const string FilesFolder = "feedback-files";

    readonly GitHubOptions _options;
    readonly ILogger<GitHubClient> _log;
    readonly HttpClient _http = new() { BaseAddress = new Uri("https://api.github.com/"), Timeout = TimeSpan.FromSeconds(30) };
    readonly SemaphoreSlim _setupGate = new(1, 1);
    Board? _board;

    /// <summary>The project board and its fields, looked up once.</summary>
    sealed record Board(string? ProjectId, string? VotesFieldId);

    public GitHubClient(GitHubOptions options, ILogger<GitHubClient> log)
    {
        (_options, _log) = (options, log);
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GymBook-Api", "1.0"));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        if (options.Token.Length > 0)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Token);
    }

    public GitHubOptions Options => _options;

    string RepoPath => $"repos/{Uri.EscapeDataString(_options.Owner)}/{Uri.EscapeDataString(_options.Repo)}";

    // ---- Setup ----

    /// <summary>
    /// Checks once that feedback can work: configured, the repository private, its labels and attachments branch there,
    /// and the project board found. A failure is tried again on the next call.
    /// </summary>
    async Task<Board> ReadyAsync(CancellationToken ct)
    {
        if (_board is { } ready)
            return ready;
        if (!_options.IsConfigured)
            throw new FeedbackException(503, "Feedback isn't available right now.");
        await _setupGate.WaitAsync(ct);
        try
        {
            if (_board is { } again)
                return again;
            var repo = await RestAsync(HttpMethod.Get, RepoPath, null, ct) ?? throw Unavailable();
            if (repo["private"]?.GetValue<bool>() != true)
            {
                // Bug reports are private: never send them to a repository everyone can read.
                _log.LogError("Feedback is off: GitHub repository {Owner}/{Repo} isn't private", _options.Owner, _options.Repo);
                throw Unavailable();
            }
            await EnsureLabelAsync(FeatureLabel, "a2eeef", "Asked for in Gym Book", ct);
            await EnsureLabelAsync(BugLabel, "d73a4a", "Reported in Gym Book", ct);
            await EnsureBranchAsync(repo["default_branch"]?.GetValue<string>() ?? "main", ct);
            _board = await FindBoardAsync(ct);
            return _board;
        }
        finally
        {
            _setupGate.Release();
        }
    }

    static FeedbackException Unavailable() => new(503, "Feedback isn't available right now.");

    async Task EnsureLabelAsync(string name, string color, string description, CancellationToken ct)
    {
        if (await RestAsync(HttpMethod.Get, $"{RepoPath}/labels/{Uri.EscapeDataString(name)}", null, ct, notFoundIsNull: true) != null)
            return;
        await RestAsync(HttpMethod.Post, $"{RepoPath}/labels", new { name, color, description }, ct);
    }

    async Task EnsureBranchAsync(string defaultBranch, CancellationToken ct)
    {
        var branch = _options.AttachmentsBranch;
        if (await RestAsync(HttpMethod.Get, $"{RepoPath}/branches/{Uri.EscapeDataString(branch)}", null, ct, notFoundIsNull: true) != null)
            return;
        var head = await RestAsync(HttpMethod.Get, $"{RepoPath}/git/ref/heads/{Uri.EscapeDataString(defaultBranch)}", null, ct, notFoundIsNull: true);
        if (head?["object"]?["sha"]?.GetValue<string>() is not { } sha)
        {
            _log.LogError("Feedback attachments: GitHub repository {Owner}/{Repo} has no commits to branch from; add a README", _options.Owner, _options.Repo);
            throw Unavailable();
        }
        await RestAsync(HttpMethod.Post, $"{RepoPath}/git/refs", new { @ref = "refs/heads/" + branch, sha }, ct);
    }

    async Task<Board> FindBoardAsync(CancellationToken ct)
    {
        if (_options.ProjectNumber <= 0)
            return new Board(null, null);
        var owner = _options.ProjectOwner.Length > 0 ? _options.ProjectOwner : _options.Owner;
        const string fields = "projectV2(number: $number) { id fields(first: 50) { nodes { ... on ProjectV2FieldCommon { id name dataType } } } }";
        // A user's board or an organization's: whichever the owner is (asking for the other is an error).
        foreach (var kind in new[] { "user", "organization" })
        {
            var data = await GraphQlAsync($"query($login: String!, $number: Int!) {{ {kind}(login: $login) {{ {fields} }} }}",
                new { login = owner, number = _options.ProjectNumber }, ct, allowErrors: true);
            if (data?[kind]?["projectV2"] is not { } project)
                continue;
            var votes = project["fields"]?["nodes"]?.AsArray()
                .FirstOrDefault(f => f?["name"]?.GetValue<string>() is { } n && n.Equals("Votes", StringComparison.OrdinalIgnoreCase)
                    && f["dataType"]?.GetValue<string>() == "NUMBER");
            return new Board(project["id"]!.GetValue<string>(), votes?["id"]?.GetValue<string>());
        }
        _log.LogWarning("Feedback: GitHub project {Owner} #{Number} not found; tickets won't be put on a board", owner, _options.ProjectNumber);
        return new Board(null, null);
    }

    // ---- Issues ----

    const string IssueFields = """
        number id title body state createdAt updatedAt comments { totalCount } labels(first: 20) { nodes { name } }
        projectItems(first: 10) { nodes { id project { id } fieldValueByName(name: "Status") { ... on ProjectV2ItemFieldSingleSelectValue { name } } } }
        """;

    /// <summary>Issues by number (with their text when <paramref name="full"/>); ones that don't exist (any more) are left out.</summary>
    public async Task<List<GitHubIssue>> GetIssuesAsync(IReadOnlyCollection<int> numbers, bool full, CancellationToken ct)
    {
        var board = await ReadyAsync(ct);
        var issues = new List<GitHubIssue>();
        foreach (var chunk in numbers.Distinct().Chunk(50))
        {
            var fields = full ? IssueFields + " bodyHTML" : IssueFields;
            var query = "query($o: String!, $r: String!) { repository(owner: $o, name: $r) { "
                + string.Concat(chunk.Select(n => $"i{n}: issue(number: {n}) {{ {fields} }} ")) + "} }";
            // A deleted issue is an error for its part only.
            var data = await GraphQlAsync(query, new { o = _options.Owner, r = _options.Repo }, ct, allowErrors: true);
            foreach (var n in chunk)
                if (data?["repository"]?[$"i{n}"] is JsonObject node)
                    issues.Add(ReadIssue(node, board));
        }
        return issues;
    }

    /// <summary>Every issue with <paramref name="label"/>, newest first, without their text.</summary>
    public async Task<List<GitHubIssue>> ListIssuesAsync(string label, CancellationToken ct)
    {
        var board = await ReadyAsync(ct);
        var issues = new List<GitHubIssue>();
        string? cursor = null;
        for (var page = 0; page < 10; page++)
        {
            var data = await GraphQlAsync($$"""
                query($o: String!, $r: String!, $label: String!, $after: String) {
                  repository(owner: $o, name: $r) {
                    issues(first: 100, after: $after, labels: [$label], orderBy: { field: CREATED_AT, direction: DESC }) {
                      nodes { {{IssueFields}} }
                      pageInfo { hasNextPage endCursor }
                    }
                  }
                }
                """, new { o = _options.Owner, r = _options.Repo, label, after = cursor }, ct);
            var connection = data?["repository"]?["issues"];
            foreach (var node in connection?["nodes"]?.AsArray() ?? new JsonArray())
                if (node is JsonObject issue)
                    issues.Add(ReadIssue(issue, board));
            if (connection?["pageInfo"]?["hasNextPage"]?.GetValue<bool>() != true)
                break;
            cursor = connection["pageInfo"]!["endCursor"]!.GetValue<string>();
        }
        return issues;
    }

    static GitHubIssue ReadIssue(JsonObject node, Board board)
    {
        var item = node["projectItems"]?["nodes"]?.AsArray()
            .FirstOrDefault(i => board.ProjectId != null && i?["project"]?["id"]?.GetValue<string>() == board.ProjectId);
        return new GitHubIssue(
            node["number"]!.GetValue<int>(),
            node["id"]!.GetValue<string>(),
            node["title"]?.GetValue<string>() ?? "",
            node["body"]?.GetValue<string>() ?? "",
            node["bodyHTML"]?.GetValue<string>() ?? "",
            node["state"]?.GetValue<string>() == "CLOSED",
            node["createdAt"]!.GetValue<DateTimeOffset>(),
            node["updatedAt"]!.GetValue<DateTimeOffset>(),
            node["comments"]?["totalCount"]?.GetValue<int>() ?? 0,
            node["labels"]?["nodes"]?.AsArray().Select(l => l?["name"]?.GetValue<string>() ?? "").ToList() ?? [],
            item?["fieldValueByName"]?["name"]?.GetValue<string>(),
            item?["id"]?.GetValue<string>());
    }

    /// <summary>Opens an issue and puts it on the board. Returns its number.</summary>
    public async Task<int> CreateIssueAsync(string title, string body, string label, CancellationToken ct)
    {
        var board = await ReadyAsync(ct);
        var issue = await RestAsync(HttpMethod.Post, $"{RepoPath}/issues", new { title, body, labels = new[] { label } }, ct) ?? throw Unavailable();
        var number = issue["number"]!.GetValue<int>();
        if (board.ProjectId != null)
        {
            try
            {
                await GraphQlAsync("mutation($p: ID!, $c: ID!) { addProjectV2ItemById(input: { projectId: $p, contentId: $c }) { item { id } } }",
                    new { p = board.ProjectId, c = issue["node_id"]!.GetValue<string>() }, ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // The issue is there all the same; it can go on the board by hand.
                _log.LogWarning(e, "Feedback: couldn't add issue #{Number} to the project board", number);
            }
        }
        return number;
    }

    public async Task UpdateIssueAsync(int number, string title, string body, CancellationToken ct)
    {
        await ReadyAsync(ct);
        await RestAsync(HttpMethod.Patch, $"{RepoPath}/issues/{number}", new { title, body }, ct);
    }

    /// <summary>Deletes the issue for good (the token's account must be an admin of the repository).</summary>
    public async Task DeleteIssueAsync(string nodeId, CancellationToken ct)
    {
        await ReadyAsync(ct);
        await GraphQlAsync("mutation($id: ID!) { deleteIssue(input: { issueId: $id }) { clientMutationId } }", new { id = nodeId }, ct);
    }

    /// <summary>The board's "Votes" field for the issue, when the board has one and the issue is on it.</summary>
    public async Task SetVotesAsync(GitHubIssue issue, int votes, CancellationToken ct)
    {
        var board = await ReadyAsync(ct);
        if (board.ProjectId == null || board.VotesFieldId == null || issue.ProjectItemId == null)
            return;
        await GraphQlAsync("""
            mutation($p: ID!, $i: ID!, $f: ID!, $v: Float!) {
              updateProjectV2ItemFieldValue(input: { projectId: $p, itemId: $i, fieldId: $f, value: { number: $v } }) { clientMutationId }
            }
            """, new { p = board.ProjectId, i = issue.ProjectItemId, f = board.VotesFieldId, v = (double)votes }, ct);
    }

    // ---- Comments ----

    public async Task<List<GitHubComment>> GetCommentsAsync(int number, CancellationToken ct)
    {
        await ReadyAsync(ct);
        var comments = new List<GitHubComment>();
        string? cursor = null;
        for (var page = 0; page < 10; page++)
        {
            var data = await GraphQlAsync("""
                query($o: String!, $r: String!, $n: Int!, $after: String) {
                  repository(owner: $o, name: $r) {
                    issue(number: $n) {
                      comments(first: 100, after: $after) {
                        nodes { databaseId body bodyHTML createdAt lastEditedAt }
                        pageInfo { hasNextPage endCursor }
                      }
                    }
                  }
                }
                """, new { o = _options.Owner, r = _options.Repo, n = number, after = cursor }, ct);
            var connection = data?["repository"]?["issue"]?["comments"];
            foreach (var node in connection?["nodes"]?.AsArray() ?? new JsonArray())
                if (node is JsonObject c)
                    comments.Add(new GitHubComment(
                        c["databaseId"]!.GetValue<long>(),
                        c["body"]?.GetValue<string>() ?? "",
                        c["bodyHTML"]?.GetValue<string>() ?? "",
                        c["createdAt"]!.GetValue<DateTimeOffset>(),
                        c["lastEditedAt"] != null));
            if (connection?["pageInfo"]?["hasNextPage"]?.GetValue<bool>() != true)
                break;
            cursor = connection["pageInfo"]!["endCursor"]!.GetValue<string>();
        }
        return comments;
    }

    /// <summary>Returns the new comment's id.</summary>
    public async Task<long> AddCommentAsync(int number, string body, CancellationToken ct)
    {
        await ReadyAsync(ct);
        var comment = await RestAsync(HttpMethod.Post, $"{RepoPath}/issues/{number}/comments", new { body }, ct) ?? throw Unavailable();
        return comment["id"]!.GetValue<long>();
    }

    public async Task UpdateCommentAsync(long id, string body, CancellationToken ct)
    {
        await ReadyAsync(ct);
        await RestAsync(HttpMethod.Patch, $"{RepoPath}/issues/comments/{id}", new { body }, ct);
    }

    public async Task DeleteCommentAsync(long id, CancellationToken ct)
    {
        await ReadyAsync(ct);
        await RestAsync(HttpMethod.Delete, $"{RepoPath}/issues/comments/{id}", null, ct, notFoundIsNull: true);
    }

    // ---- Attachments and Markdown ----

    /// <summary>Commits a file to <see cref="GitHubOptions.AttachmentsBranch"/> at <paramref name="path"/>.</summary>
    public async Task PutFileAsync(string path, byte[] content, CancellationToken ct)
    {
        await ReadyAsync(ct);
        await RestAsync(HttpMethod.Put, $"{RepoPath}/contents/{path}",
            new { message = "Attachment from Gym Book", content = Convert.ToBase64String(content), branch = _options.AttachmentsBranch }, ct);
    }

    /// <summary>A file from <see cref="GitHubOptions.AttachmentsBranch"/>; null when there's none.</summary>
    public async Task<byte[]?> GetFileAsync(string path, CancellationToken ct)
    {
        await ReadyAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{RepoPath}/contents/{path}?ref={Uri.EscapeDataString(_options.AttachmentsBranch)}");
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw+json"));
        using var response = await _http.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    /// <summary>Markdown as GitHub shows it in this repository, HTML.</summary>
    public async Task<string> RenderMarkdownAsync(string markdown, CancellationToken ct)
    {
        await ReadyAsync(ct);
        using var response = await _http.PostAsJsonAsync("markdown", new { text = markdown, mode = "gfm", context = $"{_options.Owner}/{_options.Repo}" }, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    // ---- Calls ----

    async Task<JsonNode?> RestAsync(HttpMethod method, string path, object? body, CancellationToken ct, bool notFoundIsNull = false)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body != null)
            request.Content = JsonContent.Create(body);
        using var response = await _http.SendAsync(request, ct);
        if (notFoundIsNull && response.StatusCode == HttpStatusCode.NotFound)
            return null;
        await EnsureSuccessAsync(response, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        return text.Length == 0 ? null : JsonNode.Parse(text);
    }

    /// <summary>
    /// A GraphQL query or mutation's data. Errors throw, unless <paramref name="allowErrors"/> (then whatever data came
    /// back is returned, e.g. with an issue that doesn't exist left null).
    /// </summary>
    async Task<JsonNode?> GraphQlAsync(string query, object variables, CancellationToken ct, bool allowErrors = false)
    {
        using var response = await _http.PostAsJsonAsync("graphql", new { query, variables }, ct);
        await EnsureSuccessAsync(response, ct);
        var result = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));
        if (result?["errors"] is JsonArray { Count: > 0 } errors && !allowErrors)
        {
            _log.LogError("GitHub GraphQL error: {Errors}", errors.ToJsonString());
            throw Unavailable();
        }
        return result?["data"];
    }

    async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;
        var text = await response.Content.ReadAsStringAsync(ct);
        _log.LogError("GitHub {Method} {Path} failed: {Status} {Body}", response.RequestMessage?.Method, response.RequestMessage?.RequestUri?.AbsolutePath,
            (int)response.StatusCode, text.Length > 500 ? text[..500] : text);
        throw response.StatusCode == HttpStatusCode.NotFound
            ? new FeedbackException(404, "That ticket doesn't exist any more.")
            : Unavailable();
    }
}
