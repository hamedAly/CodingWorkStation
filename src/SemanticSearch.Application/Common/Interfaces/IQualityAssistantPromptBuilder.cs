using SemanticSearch.Application.Quality.Assistant.Models;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IQualityAssistantPromptBuilder
{
    AssistantPromptModel BuildProjectPlanPrompt(ProjectPlanRequestModel request);
    AssistantPromptModel BuildFindingFixPrompt(FindingFixRequestModel request);
    AssistantInferenceOptionsModel BuildInferenceOptions();
}
