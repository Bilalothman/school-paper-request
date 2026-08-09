namespace Backend.Workflow;

public interface IWorkflowService
{
    Task<string> StartRequestProcessAsync(int requestId, CancellationToken cancellationToken);
    Task CompleteReviewTaskAsync(string processInstanceId, string decision, CancellationToken cancellationToken);
    Task CancelProcessAsync(string processInstanceId, CancellationToken cancellationToken);
}
