using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flashcards.Features.Flashcard.Model;

public abstract record FlashcardCard
{
    public FlashcardCard(string format)
    {
        Format = format;
    }

    public string Format { get; private init; }

    public string? FileLocation { get; set; }

    /// <returns>Returns the file content without format data, i.e. the file without the first line.</returns>
    public abstract string ToFileContent();
}