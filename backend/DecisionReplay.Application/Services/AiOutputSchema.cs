using System.Text.Json;
using System.Text.Json.Nodes;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public static class AiOutputSchema
{
    public const string Version = "decision-analysis-v1";
    public static string Analysis(DecisionContextSnapshot context)
    {
        var schema = JsonNode.Parse("""
            {
              "type":"object", "additionalProperties":false,
              "required":["assumptions","risks","missingInformation","alternatives","analysis","confidence"],
              "$defs":{
                "evidenceIds":{"type":"array","maxItems":50,"items":{"type":"string"}},
                "claim":{"type":"object","additionalProperties":false,"required":["text","evidenceIds","isHypothesis"],
                  "properties":{"text":{"type":"string","minLength":1,"maxLength":500},"evidenceIds":{"$ref":"#/$defs/evidenceIds"},"isHypothesis":{"type":"boolean"}}},
                "alternative":{"type":"object","additionalProperties":false,"required":["name","tradeoff","evidenceIds","isHypothesis"],
                  "properties":{"name":{"type":"string","minLength":1,"maxLength":200},"tradeoff":{"type":"string","minLength":1,"maxLength":500},"evidenceIds":{"$ref":"#/$defs/evidenceIds"},"isHypothesis":{"type":"boolean"}}}
              },
              "properties":{
                "assumptions":{"type":"array","maxItems":3,"items":{"$ref":"#/$defs/claim"}},
                "risks":{"type":"array","maxItems":3,"items":{"$ref":"#/$defs/claim"}},
                "missingInformation":{"type":"array","maxItems":4,"items":{"type":"string","minLength":1,"maxLength":500}},
                "alternatives":{"type":"array","minItems":1,"maxItems":2,"items":{"$ref":"#/$defs/alternative"}},
                "analysis":{"type":"string","minLength":1,"maxLength":1200},
                "confidence":{"type":"number","minimum":0,"maximum":1}
              }
            }
            """)!;
        var ids = context.Evidence.Select(item => item.Id).ToArray();
        if (ids.Length == 0) schema["$defs"]!["evidenceIds"]!["maxItems"] = 0;
        else schema["$defs"]!["evidenceIds"]!["items"]!["enum"] = JsonSerializer.SerializeToNode(ids);
        return schema.ToJsonString();
    }
}

