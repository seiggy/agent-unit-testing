using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentEvalsWorkshop.Tests.Helpers
{
    public static class AgentRunResponseExtensions
    {
        public static ChatResponse ToChatResponse(this AgentRunResponse agentRunResponse)
        {
            return new ChatResponse
            {
                AdditionalProperties = agentRunResponse.AdditionalProperties,
                CreatedAt = agentRunResponse.CreatedAt,
                ContinuationToken = agentRunResponse.ContinuationToken,
                ConversationId = agentRunResponse.AgentId,
                FinishReason = ChatFinishReason.Stop,
                RawRepresentation = agentRunResponse.RawRepresentation,
                ResponseId = agentRunResponse.ResponseId,
                Messages = agentRunResponse.Messages,
                Usage = agentRunResponse.Usage
            };
        }
    }
}
