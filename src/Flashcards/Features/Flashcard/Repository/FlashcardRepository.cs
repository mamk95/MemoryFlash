namespace Flashcards.Features.Flashcard.Repository;

using Flashcards.Features.Flashcard.Model;
using System.Text.RegularExpressions;

public class FlashcardRepository
{
	private readonly string _directory;

	public FlashcardRepository()
	{
		var baseDir = Directory.GetCurrentDirectory();
		var flashcardDir = Path.Combine(baseDir, "flashcards");
		Directory.CreateDirectory(flashcardDir);
		_directory = flashcardDir;
	}

    public List<FlashcardGroup> GetAllGroupsWithoutCards()
    {
        List<FlashcardGroup> groups = new();
        var groupPaths = Directory.GetDirectories(_directory);

        foreach (var groupPath in groupPaths)
        {
            groups.Add(new(
                Name: new DirectoryInfo(groupPath).Name,
                Cards: new()
            ));
        }

        return groups;
    }

    public async Task<FlashcardGroup> GetGroupWithCards(string groupName)
    {
		groupName = SanitizeFolderName(groupName);

        return new(
                Name: groupName,
                Cards: await GetAllFlashcardsInPath(Path.Combine(_directory, groupName))
            );
    }

    public void CreateGroup(string groupName)
    {
        groupName = SanitizeFolderName(groupName);

        var groupPath = Path.Combine(_directory, groupName);
        Directory.CreateDirectory(groupPath);
    }

    public async Task CreateFlashcard(string groupName, FlashcardCard card)
    {
        groupName = SanitizeFolderName(groupName);

        var filename = Guid.NewGuid().ToString() + ".txt";
        var filepath = Path.Combine(_directory, groupName, filename);

        var formatLine = $"Flashcard|{card.Format}|{Guid.NewGuid()}";
        var fileContent = card.ToFileContent();

        await File.WriteAllTextAsync(filepath, formatLine + "\n" + fileContent);
    }

    public async Task EditFlashcard(FlashcardCard card)
    {
        if (card.FileLocation == null)
            throw new ArgumentException("Missing file location", nameof(card));

        if (!File.Exists(card.FileLocation))
            throw new ArgumentException("Wrong file location", nameof(card));

        if (card.FileLocation.Contains(_directory) != true)
            throw new ArgumentException("File location outside allowed area", nameof(card));

        var formatLine = $"Flashcard|{card.Format}|{card.UniqueId}";
        var fileContent = card.ToFileContent();

        await File.WriteAllTextAsync(card.FileLocation, formatLine + "\n" + fileContent);
    }

    private static async Task<List<FlashcardCard>> GetAllFlashcardsInPath(string path)
	{
		List<FlashcardCard> cards = new();

        var filePaths = Directory.EnumerateFiles(path, "*.txt");

		await Parallel.ForEachAsync(
			filePaths, 
			new ParallelOptions() { MaxDegreeOfParallelism = 20 }, 
			async (filePath, cancelToken) =>
			{
				using var stream = File.OpenRead(filePath);
				using var reader = new StreamReader(stream);

				var line1 = await reader.ReadLineAsync();
				var formatTags = line1?.Split('|'); // Flashcard|{Format}|{UniqueId/GUID}

                if (formatTags?.FirstOrDefault() != "Flashcard")
					return;

				var contentWithoutFormat = await reader.ReadToEndAsync();

                if (formatTags?.ElementAtOrDefault(1) == FlashcardPlaintextV1Card.FormatTag)
                {
                    if (FlashcardPlaintextV1Card.TryParseFromFile(contentWithoutFormat, out var plaintext))
                        cards.Add(plaintext with { FileLocation = filePath, UniqueId = formatTags[2] });
                }
                else if (formatTags?.ElementAtOrDefault(1) == FlashcardMarkdownV1Card.FormatTag)
                {
                    if (FlashcardMarkdownV1Card.TryParseFromFile(contentWithoutFormat, out var markdown))
                        cards.Add(markdown with { FileLocation = filePath, UniqueId = formatTags[2] });
                }
            });

		return cards;
	}

	private static string SanitizeFolderName(string foldername)
	{
        string clean = String.Concat(foldername.Split(Path.GetInvalidFileNameChars()));

		clean = Regex.Replace(clean, @"\.+", "."); // We use Regex replace to avoid this problem: ".....".Replace("..", ".") => "..."

		return clean;
    }
}
