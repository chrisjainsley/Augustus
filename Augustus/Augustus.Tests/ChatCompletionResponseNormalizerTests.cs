using System.Text.Json;
using System.Text.Json.Nodes;

namespace Augustus.Tests;

public class ChatCompletionResponseNormalizerTests
{
    private const string ChatCompletionPath = "/v1/chat/completions";
    private const string AzureChatCompletionPath = "/openai/deployments/gpt-4/chat/completions";

    #region Path Detection

    [Theory]
    [InlineData("/v1/chat/completions")]
    [InlineData("/openai/deployments/gpt-4/chat/completions")]
    [InlineData("/openai/deployments/my-model/chat/completions")]
    public void IsChatCompletionPath_ReturnsTrue_ForChatCompletionPaths(string path)
    {
        ChatCompletionResponseNormalizer.IsChatCompletionPath(path).Should().BeTrue();
    }

    [Theory]
    [InlineData("/v1/models")]
    [InlineData("/v1/embeddings")]
    [InlineData("/v1/completions")]
    [InlineData("/api/customers")]
    public void IsChatCompletionPath_ReturnsFalse_ForNonChatCompletionPaths(string path)
    {
        ChatCompletionResponseNormalizer.IsChatCompletionPath(path).Should().BeFalse();
    }

    #endregion

    #region Pass-through

    [Fact]
    public void NonChatCompletionPath_ReturnsJsonUnchanged()
    {
        var json = """{"object":"list","data":[]}""";
        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, "/v1/models");
        result.Should().Be(json);
    }

    [Fact]
    public void ValidChatCompletion_PreservesStructure()
    {
        var json = CreateValidChatCompletionJson();
        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);

        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.GetProperty("choices").ValueKind.Should().Be(JsonValueKind.Array);
        root.GetProperty("choices")[0].GetProperty("message").ValueKind.Should().Be(JsonValueKind.Object);
        root.GetProperty("choices")[0].GetProperty("message").GetProperty("role").GetString().Should().Be("assistant");
        root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString().Should().Be("Hello!");
        root.GetProperty("usage").GetProperty("prompt_tokens").ValueKind.Should().Be(JsonValueKind.Number);
    }

    [Fact]
    public void InvalidJson_ReturnsUnchanged()
    {
        var badJson = "this is not json {{{";
        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(badJson, ChatCompletionPath);
        result.Should().Be(badJson);
    }

    [Fact]
    public void NullOrEmpty_ReturnsUnchanged()
    {
        ChatCompletionResponseNormalizer.NormalizeIfChatCompletion("", ChatCompletionPath).Should().Be("");
        ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(null!, ChatCompletionPath).Should().BeNull();
    }

    #endregion

    #region Choices normalization

    [Fact]
    public void NonArrayChoices_IsReplacedWithEmptyArray()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": {"unexpected": "object"},
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("choices").ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetProperty("choices").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void StringifiedChoicesArray_IsParsedToArray()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": "[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"Hi\"},\"finish_reason\":\"stop\"}]",
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("choices").ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetProperty("choices").GetArrayLength().Should().Be(1);
    }

    #endregion

    #region Message normalization

    [Fact]
    public void StringMessage_IsWrappedAsObject()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": "Hello world",
              "finish_reason": "stop"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");
        message.ValueKind.Should().Be(JsonValueKind.Object);
        message.GetProperty("role").GetString().Should().Be("assistant");
        message.GetProperty("content").GetString().Should().Be("Hello world");
    }

    #endregion

    #region Numeric field normalization

    [Fact]
    public void StringIndex_IsConvertedToNumber()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": "0",
              "message": {"role": "assistant", "content": "Hi"},
              "finish_reason": "stop"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("choices")[0].GetProperty("index").ValueKind.Should().Be(JsonValueKind.Number);
    }

    [Fact]
    public void StringCreated_IsConvertedToNumber()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": "1700000000",
          "model": "gpt-4",
          "choices": [{"index": 0, "message": {"role": "assistant", "content": "Hi"}, "finish_reason": "stop"}],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("created").ValueKind.Should().Be(JsonValueKind.Number);
    }

    [Fact]
    public void StringTokenCounts_AreConvertedToNumbers()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [{"index": 0, "message": {"role": "assistant", "content": "Hi"}, "finish_reason": "stop"}],
          "usage": {"prompt_tokens": "10", "completion_tokens": "5", "total_tokens": "15"}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var usage = doc.RootElement.GetProperty("usage");
        usage.GetProperty("prompt_tokens").ValueKind.Should().Be(JsonValueKind.Number);
        usage.GetProperty("completion_tokens").ValueKind.Should().Be(JsonValueKind.Number);
        usage.GetProperty("total_tokens").ValueKind.Should().Be(JsonValueKind.Number);
    }

    #endregion

    #region Tool calls normalization

    [Fact]
    public void ObjectArguments_AreSerializedToString()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {
                "role": "assistant",
                "content": null,
                "tool_calls": [
                  {
                    "id": "call_abc",
                    "type": "function",
                    "function": {
                      "name": "get_weather",
                      "arguments": {"location": "London", "unit": "celsius"}
                    }
                  }
                ]
              },
              "finish_reason": "tool_calls"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var args = doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("tool_calls")[0]
            .GetProperty("function")
            .GetProperty("arguments");
        args.ValueKind.Should().Be(JsonValueKind.String);

        // The serialized string should be valid JSON containing the original object
        var parsedArgs = JsonDocument.Parse(args.GetString()!);
        parsedArgs.RootElement.GetProperty("location").GetString().Should().Be("London");
    }

    [Fact]
    public void StringArguments_AreLeftUnchanged()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {
                "role": "assistant",
                "content": null,
                "tool_calls": [
                  {
                    "id": "call_abc",
                    "type": "function",
                    "function": {
                      "name": "get_weather",
                      "arguments": "{\"location\":\"London\"}"
                    }
                  }
                ]
              },
              "finish_reason": "tool_calls"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var args = doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("tool_calls")[0]
            .GetProperty("function")
            .GetProperty("arguments");
        args.ValueKind.Should().Be(JsonValueKind.String);
        args.GetString().Should().Contain("London");
    }

    #endregion

    #region Content filter results normalization

    [Fact]
    public void StringContentFilterResults_IsExpandedToObject()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {"role": "assistant", "content": "Hi"},
              "finish_reason": "stop",
              "content_filter_results": "safe"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, AzureChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var filter = doc.RootElement.GetProperty("choices")[0].GetProperty("content_filter_results");
        filter.ValueKind.Should().Be(JsonValueKind.Object);
        filter.GetProperty("hate").GetProperty("filtered").GetBoolean().Should().BeFalse();
    }

    #endregion

    #region Logprobs normalization

    [Fact]
    public void StringLogprobs_IsSetToNull()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {"role": "assistant", "content": "Hi"},
              "finish_reason": "stop",
              "logprobs": "none"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("choices")[0].GetProperty("logprobs").ValueKind.Should().Be(JsonValueKind.Null);
    }

    #endregion

    #region Usage normalization

    [Fact]
    public void StringifiedNullUsage_IsReplacedWithDefault()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [{"index": 0, "message": {"role": "assistant", "content": "Hi"}, "finish_reason": "stop"}],
          "usage": "null"
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var usage = doc.RootElement.GetProperty("usage");
        usage.ValueKind.Should().Be(JsonValueKind.Object);
        usage.GetProperty("prompt_tokens").GetInt32().Should().Be(0);
    }

    [Fact]
    public void StringUsage_IsParsedToObject()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [{"index": 0, "message": {"role": "assistant", "content": "Hi"}, "finish_reason": "stop"}],
          "usage": "{\"prompt_tokens\":10,\"completion_tokens\":5,\"total_tokens\":15}"
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var usage = doc.RootElement.GetProperty("usage");
        usage.ValueKind.Should().Be(JsonValueKind.Object);
        usage.GetProperty("prompt_tokens").GetInt32().Should().Be(10);
    }

    [Fact]
    public void NonObjectUsage_IsReplacedWithDefault()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [{"index": 0, "message": {"role": "assistant", "content": "Hi"}, "finish_reason": "stop"}],
          "usage": [1, 2, 3]
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var usage = doc.RootElement.GetProperty("usage");
        usage.ValueKind.Should().Be(JsonValueKind.Object);
        usage.GetProperty("prompt_tokens").GetInt32().Should().Be(0);
        usage.GetProperty("completion_tokens").GetInt32().Should().Be(0);
        usage.GetProperty("total_tokens").GetInt32().Should().Be(0);
    }

    [Fact]
    public void MissingUsage_GetsDefault()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [{"index": 0, "message": {"role": "assistant", "content": "Hi"}, "finish_reason": "stop"}]
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("usage").ValueKind.Should().Be(JsonValueKind.Object);
    }

    #endregion

    #region Stringified tool_calls normalization

    [Fact]
    public void StringifiedToolCallsArray_IsRehydratedToArray()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {
                "role": "assistant",
                "content": null,
                "tool_calls": "[{\"id\":\"call_abc\",\"type\":\"function\",\"function\":{\"name\":\"apply_rule\",\"arguments\":\"{\\\"rule_name\\\":\\\"test\\\"}\"}}]"
              },
              "finish_reason": "tool_calls"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var toolCalls = doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("tool_calls");
        toolCalls.ValueKind.Should().Be(JsonValueKind.Array);
        toolCalls.GetArrayLength().Should().Be(1);
        toolCalls[0].GetProperty("function").GetProperty("name").GetString().Should().Be("apply_rule");
    }

    [Fact]
    public void StringifiedFunctionObject_IsRehydratedToObject()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {
                "role": "assistant",
                "content": null,
                "tool_calls": [
                  {
                    "id": "call_abc",
                    "type": "function",
                    "function": "{\"name\":\"remove_rule\",\"arguments\":\"{\\\"rule_id\\\":\\\"123\\\"}\"}"
                  }
                ]
              },
              "finish_reason": "tool_calls"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var func = doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("tool_calls")[0]
            .GetProperty("function");
        func.ValueKind.Should().Be(JsonValueKind.Object);
        func.GetProperty("name").GetString().Should().Be("remove_rule");
        func.GetProperty("arguments").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public void UnparseableStringChoiceItem_IsReplacedWithDefaultChoice()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": ["not valid json {{{"],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var choice = doc.RootElement.GetProperty("choices")[0];
        choice.ValueKind.Should().Be(JsonValueKind.Object);
        choice.GetProperty("finish_reason").GetString().Should().Be("stop");
        choice.GetProperty("message").GetProperty("role").GetString().Should().Be("assistant");
    }

    [Fact]
    public void ScalarStringChoiceItem_IsReplacedWithDefaultChoice()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": ["null"],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var choice = doc.RootElement.GetProperty("choices")[0];
        choice.ValueKind.Should().Be(JsonValueKind.Object);
        choice.GetProperty("finish_reason").GetString().Should().Be("stop");
    }

    #endregion

    #region Stringified choice items normalization

    [Fact]
    public void StringifiedChoiceItem_IsRehydratedToObject()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": ["{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"Hi\"},\"finish_reason\":\"stop\"}"],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var choice = doc.RootElement.GetProperty("choices")[0];
        choice.ValueKind.Should().Be(JsonValueKind.Object);
        choice.GetProperty("message").GetProperty("content").GetString().Should().Be("Hi");
    }

    #endregion

    #region Non-array tool_calls normalization

    [Fact]
    public void ObjectToolCalls_IsReplacedWithEmptyArray()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {
                "role": "assistant",
                "content": null,
                "tool_calls": {"unexpected": "object"}
              },
              "finish_reason": "tool_calls"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var toolCalls = doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("tool_calls");
        toolCalls.ValueKind.Should().Be(JsonValueKind.Array);
        toolCalls.GetArrayLength().Should().Be(0);
    }

    #endregion

    #region Null function normalization

    [Fact]
    public void NullFunction_IsReplacedWithDefault()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {
                "role": "assistant",
                "content": null,
                "tool_calls": [
                  {
                    "id": "call_abc",
                    "type": "function",
                    "function": null
                  }
                ]
              },
              "finish_reason": "tool_calls"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var func = doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("tool_calls")[0]
            .GetProperty("function");
        func.ValueKind.Should().Be(JsonValueKind.Object);
        func.GetProperty("name").GetString().Should().Be("unknown");
        func.GetProperty("arguments").GetString().Should().Be("{}");
    }

    #endregion

    #region Arguments primitive coercion

    [Fact]
    public void NumericArguments_IsCoercedToString()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {
                "role": "assistant",
                "content": null,
                "tool_calls": [
                  {
                    "id": "call_abc",
                    "type": "function",
                    "function": {
                      "name": "get_count",
                      "arguments": 42
                    }
                  }
                ]
              },
              "finish_reason": "tool_calls"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var args = doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("tool_calls")[0]
            .GetProperty("function")
            .GetProperty("arguments");
        args.ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public void BooleanArguments_IsCoercedToString()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {
                "role": "assistant",
                "content": null,
                "tool_calls": [
                  {
                    "id": "call_abc",
                    "type": "function",
                    "function": {
                      "name": "get_flag",
                      "arguments": true
                    }
                  }
                ]
              },
              "finish_reason": "tool_calls"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var args = doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("tool_calls")[0]
            .GetProperty("function")
            .GetProperty("arguments");
        args.ValueKind.Should().Be(JsonValueKind.String);
    }

    #endregion

    #region Content type coercion

    [Fact]
    public void NumericContent_IsCoercedToString()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {"role": "assistant", "content": 42},
              "finish_reason": "stop"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content");
        content.ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public void BooleanContent_IsCoercedToString()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {"role": "assistant", "content": true},
              "finish_reason": "stop"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content");
        content.ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public void StringifiedMessageObject_IsRehydratedAndNormalized()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": "{\"role\":\"assistant\",\"content\":\"Hello from stringified message\"}",
              "finish_reason": "stop"
            }
          ],
          "usage": {"prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, ChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");
        message.ValueKind.Should().Be(JsonValueKind.Object);
        message.GetProperty("role").GetString().Should().Be("assistant");
        message.GetProperty("content").GetString().Should().Be("Hello from stringified message");
    }

    #endregion

    #region Azure OpenAI path detection

    [Fact]
    public void AzureOpenAIPath_TriggersNormalization()
    {
        var json = """
        {
          "id": "chatcmpl-abc",
          "object": "chat.completion",
          "created": "1700000000",
          "model": "gpt-4",
          "choices": [{"index": "0", "message": {"role": "assistant", "content": "Hi"}, "finish_reason": "stop"}],
          "usage": {"prompt_tokens": "10", "completion_tokens": "5", "total_tokens": "15"}
        }
        """;

        var result = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(json, AzureChatCompletionPath);
        var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("created").ValueKind.Should().Be(JsonValueKind.Number);
        doc.RootElement.GetProperty("choices")[0].GetProperty("index").ValueKind.Should().Be(JsonValueKind.Number);
    }

    #endregion

    private static string CreateValidChatCompletionJson()
    {
        return """
        {
          "id": "chatcmpl-abc123",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4",
          "choices": [
            {
              "index": 0,
              "message": {
                "role": "assistant",
                "content": "Hello!"
              },
              "logprobs": null,
              "finish_reason": "stop"
            }
          ],
          "usage": {
            "prompt_tokens": 10,
            "completion_tokens": 5,
            "total_tokens": 15
          },
          "system_fingerprint": "fp_abc123"
        }
        """;
    }
}
