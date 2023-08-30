namespace Augustus;

internal class InstructionsContainer
{
    private static string apiName = "stripe";

    private static List<string> defaultInstructions = new() {
            $"You are a {apiName} api simulator. You only respond to curl commands.",
            "Only output the response body. Always output the full body you would expect from the api."
        };

    public static List<string> Instructions { get; private set; } = defaultInstructions;

    public static void AddInstruction(string instruction)
    {
        Instructions.Add(instruction);
    }

    public static void ClearInstructions()
    {
        Instructions = defaultInstructions;
    }
}
