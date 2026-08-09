using System.Net.Http.Json;
using System.Text.Json;

namespace Backend.Workflow;

public class CamundaWorkflowService(HttpClient httpClient, IConfiguration configuration, ILogger<CamundaWorkflowService> logger) : IWorkflowService
{
    private readonly string _processKey = configuration["Camunda:ProcessDefinitionKey"] ?? "school-paper-request";

    public async Task<string> StartRequestProcessAsync(int requestId, CancellationToken cancellationToken)
    {
        var payload = new
        {
            businessKey = $"request-{requestId}",
            variables = new { requestId = new { value = requestId, type = "Integer" } }
        };
        using var response = await httpClient.PostAsJsonAsync($"process-definition/key/{_processKey}/start", payload, cancellationToken);
        await EnsureSuccessAsync(response, "start workflow", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ProcessResult>(cancellationToken: cancellationToken);
        return result?.Id ?? throw new WorkflowException("Camunda did not return a process instance ID.");
    }

    public async Task CompleteReviewTaskAsync(string processInstanceId, string decision, CancellationToken cancellationToken)
    {
        using var taskResponse = await httpClient.GetAsync($"task?processInstanceId={Uri.EscapeDataString(processInstanceId)}&taskDefinitionKey=adminReview", cancellationToken);
        await EnsureSuccessAsync(taskResponse, "find admin review task", cancellationToken);
        var tasks = await taskResponse.Content.ReadFromJsonAsync<List<TaskResult>>(cancellationToken: cancellationToken);
        var task = tasks?.SingleOrDefault() ?? throw new WorkflowException("The admin review task was not found or was already completed.");
        var payload = new { variables = new { decision = new { value = decision, type = "String" } } };
        using var completeResponse = await httpClient.PostAsJsonAsync($"task/{task.Id}/complete", payload, cancellationToken);
        await EnsureSuccessAsync(completeResponse, "complete admin review task", cancellationToken);
    }

    public async Task CancelProcessAsync(string processInstanceId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.DeleteAsync($"process-instance/{Uri.EscapeDataString(processInstanceId)}", cancellationToken);
            if (!response.IsSuccessStatusCode) logger.LogWarning("Could not compensate workflow {ProcessId}; Camunda returned {StatusCode}.", processInstanceId, response.StatusCode);
        }
        catch (Exception ex) { logger.LogWarning(ex, "Could not compensate workflow {ProcessId}.", processInstanceId); }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new WorkflowException($"Could not {operation}. Camunda returned {(int)response.StatusCode}: {Truncate(body)}");
    }

    private static string Truncate(string value) => value.Length <= 300 ? value : value[..300];
    private sealed record ProcessResult(string Id);
    private sealed record TaskResult(string Id);
}

public class WorkflowException(string message) : Exception(message);
