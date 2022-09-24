using System.Drawing;
using System.Text;

namespace Flashcards.Features.Flashcard.Model;

public record FlashcardPlaintextV1Card : FlashcardCard
{
    public const string FormatTag = "PlaintextV1";

    public FlashcardPlaintextV1Card() : base(FormatTag)
    {
    }

    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    /// <param name="content">File content without format data, i.e. the file without the first line.</param>
	public static bool TryParseFromFile(string content, out FlashcardPlaintextV1Card card)
    {
        card = new();

        var questionStartIndex = content.IndexOf("#QUESTION#") + "#QUESTION#".Length;
        var questionEndIndex = content.IndexOf("#ANSWER#");
        var answerStartIndex = questionEndIndex + "#ANSWER#".Length;
        var answerEndIndex = content.Length;

        if (questionStartIndex < 0 || questionEndIndex < 0 || answerStartIndex < 0)
            return false;

        card.Question = content.Substring(questionStartIndex, questionEndIndex - questionStartIndex);
        card.Answer = content.Substring(answerStartIndex, answerEndIndex - answerStartIndex);
        return true;
    }

    /// <inheritdoc/>
    public override string ToFileContent()
    {
        StringBuilder strBuilder = new();

        strBuilder.Append("#QUESTION#");
        strBuilder.Append(Question);
        strBuilder.AppendLine();
        strBuilder.Append("#ANSWER#");
        strBuilder.Append(Answer);

        return strBuilder.ToString();
    }
}
