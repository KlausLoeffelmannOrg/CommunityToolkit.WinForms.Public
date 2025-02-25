﻿using Chatty.DataEntities;
using CommunityToolkit.WinForms.AsyncSupport;
using CommunityToolkit.WinForms.Controls.Blazor;
using System.ComponentModel;
using System.Text;

namespace Chatty.DataProcessing;

/// <summary>
///  Processes conversations and handles various operations related to conversation files and listings.
/// </summary>
/// <remarks>
///  <para>
///   This class is responsible for managing conversation data, including saving and loading conversations from files.
///  </para>
///  <para>
///   It also handles the processing of paragraphs within a conversation, identifying and managing code listings.
///  </para>
/// </remarks>
public class ConversationProcessor
{
    /// <summary>
    /// Event triggered when a new listing file is added.
    /// </summary>
    public event AsyncEventHandler<AsyncListingFileAddedEventArgs>? AsyncListingFileAdded;

    private readonly List<ListingFile> _textFiles = [];
    private StringBuilder? _currentListingBuilder;
    private string? _currentListingType;
    private readonly StringBuilder _currentConvItemMarkdown = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationProcessor"/> class.
    /// </summary>
    /// <param name="conversation">The conversation to be processed.</param>
    /// <param name="basePath">The base path for storing conversation files.</param>
    /// <remarks>
    /// <para>Sets up the conversation processor with the provided conversation and base path.</para>
    /// <para>
    /// Subscribes to the <see cref="Conversation.PropertyChanged"/> event if the conversation 
    /// title is not set or starts with a zero-width space.
    /// </para>
    /// </remarks>
    public ConversationProcessor(Conversation conversation, string basePath)
    {
        Conversation = conversation;
        BasePath = basePath;

        if (string.IsNullOrEmpty(Conversation.Title)
            || Conversation.Title[0] == '\u200B')
        {
            Conversation.PropertyChanged += Conversation_PropertyChanged;
        }
        else
        {
            TryGetConversationBaseFolder(false);
        }

        void Conversation_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Conversation.Title))
            {
                if (!string.IsNullOrEmpty(Conversation.Title)
                    && Conversation.Title[0] != '\u200B')
                {
                    TryGetConversationBaseFolder(true);
                    Conversation.PropertyChanged -= Conversation_PropertyChanged;
                }
            }
        }
    }

    /// <summary>
    ///  Gets the conversation being processed.
    /// </summary>
    public Conversation Conversation { get; }

    /// <summary>
    ///  Gets the base path for storing conversation files.
    /// </summary>
    public string BasePath { get; }

    /// <summary>
    ///  Gets or sets the base folder for the conversation.
    /// </summary>
    public string? ConversationBaseFolder { get; internal set; }

    /// <summary>
    ///  Gets or sets the description of the current listing.
    /// </summary>
    public string? CurrentListingDescription { get; internal set; }

    /// <summary>
    ///  Gets or sets the filename of the current listing.
    /// </summary>
    public string? CurrentListingFilename { get; internal set; }

    /// <summary>
    ///  Attempts to get the base folder for the conversation.
    /// </summary>
    /// <remarks>
    /// <para>Generates a unique filename from the conversation title and ensures it does not contain invalid characters.</para>
    /// <para>Creates the necessary folder structure for storing the conversation file.</para>
    /// </remarks>
    private void TryGetConversationBaseFolder(bool createNewName)
    {
        string uniqueFilename;

        if (createNewName)
        {
            string? title = Conversation.Title.Replace(".", "_");
            uniqueFilename = GenerateUniqueFullName(title, ".cjson");
            Conversation.Filename = Path.GetFileName(uniqueFilename)!;
        }
        else
        {
            uniqueFilename = Conversation.Filename;
        }

        string newBasePath = Path.GetDirectoryName(uniqueFilename)!;
        ConversationBaseFolder = newBasePath;

        // Generates a unique filename from the conversation title, ensures it does not contain invalid characters
        // and should the filename already exist, it will append a counter to the filename, until it finds
        // a unique filename.
        string GenerateUniqueFullName(string title, string? extension = default)
        {
            string sanitizedTitle = string.Concat(title.Split(Path.GetInvalidFileNameChars()));
            string fileName = sanitizedTitle;
            string folderPath = GetFolderFullPathFromFilename(BasePath, fileName);

            Directory.CreateDirectory(folderPath);

            int counter = 1;

            while (File.Exists(Path.Combine(folderPath, fileName + extension)))
            {
                fileName = $"{sanitizedTitle}_{counter}";
                counter++;
            }

            return Path.Combine(folderPath, fileName + extension);
        }
    }

    /// <summary>
    ///  Gets the full path of the folder from the filename.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <param name="fileName">The filename.</param>
    /// <returns>The full path of the folder.</returns>
    private static string GetFolderFullPathFromFilename(string basePath, string fileName)
    {
        string folderName = Path.GetFileNameWithoutExtension(fileName);
        string folderPath = Path.Combine(basePath, folderName);
        return folderPath;
    }

    /// <summary>
    ///  Saves the conversation asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <remarks>
    /// <para>Opens a file stream to write the conversation data to a file in JSON format.</para>
    /// <para>Flushes the file stream asynchronously to ensure all data is written to the file.</para>
    /// </remarks>
    public async Task SaveConversationAsync()
    {
        using FileStream fileStream = new(
                path: Path.Combine(ConversationBaseFolder!, Conversation.Filename),
                mode: FileMode.Create,
                access: FileAccess.Write);

        Conversation.WriteJSon(fileStream);

        await fileStream.FlushAsync();
    }

    /// <summary>
    ///  Creates a <see cref="ConversationProcessor"/> from a file asynchronously.
    /// </summary>
    /// <param name="basePath">The base path for storing conversation files.</param>
    /// <param name="filename">The filename of the conversation file.</param>
    /// <returns>A task representing the asynchronous operation, with a <see cref="ConversationProcessor"/> as the result.</returns>
    /// <remarks>
    /// <para>Reads the conversation data from the specified file and creates a new <see cref="ConversationProcessor"/> instance.</para>
    /// </remarks>
    public static async Task<ConversationProcessor> FromFileAsync(string basePath, string filename)
    {
        filename = Path.GetFileName(filename);
        string folderPath = GetFolderFullPathFromFilename(basePath, filename);

        string json = await File.ReadAllTextAsync(Path.Combine(folderPath, filename));
        var conversation = Conversation.FromJson(json);

        return new ConversationProcessor(conversation, basePath);
    }

    /// <summary>
    ///  Handles a new paragraph in the conversation.
    /// </summary>
    /// <param name="paragraph">The paragraph text.</param>
    /// <param name="textPosition">The position of the text in the conversation.</param>
    /// <param name="isLastParagraph">Indicates whether this is the last paragraph.</param>
    /// <remarks>
    /// <para>Processes the paragraph to identify and manage code listings.</para>
    /// <para>Adds the paragraph to the current listing if it is part of a code block.</para>
    /// <para>Creates a new <see cref="ConversationItem"/> for the paragraph if it is the last paragraph.</para>
    /// </remarks>
    public async Task HandleNewParagraphAsync(string paragraph, int textPosition, bool isLastParagraph)
    {
        _currentConvItemMarkdown.Append(paragraph);

        string trimmedParagraph = paragraph.Trim();

        if (trimmedParagraph.StartsWith("```"))
        {
            // Handle listing start or end
            if (trimmedParagraph.Length > 3 && _currentListingBuilder == null)
            {
                // This is a start of a new listing with a specific type
                _currentListingType = trimmedParagraph[3..].Trim();
                _currentListingBuilder = new StringBuilder();
            }
            else if (_currentListingBuilder != null)
            {
                // This is the end of the current listing
                string listingContent = _currentListingBuilder.ToString();
                var listingFile = new ListingFile(BasePath, listingContent);

                await OnListingFileAddedAsync(new(listingFile));

                _currentListingBuilder = null;
                _currentListingType = null;
            }
        }
        else
        {
            if (_currentListingBuilder is not null)
            {
                // Add paragraph to the current listing
                // Note: The paragraph is not trimmed, so we only Append
                // to not add additional whitespace.
                _currentListingBuilder?.Append(paragraph);
            }
        }

        if (isLastParagraph)
        {
            var conversationItem = new ConversationItem
            {
                MarkdownContent = _currentConvItemMarkdown.ToString(),
                HtmlContent = $"{Conversation.ResponseInProgress}",
                IsResponse = true
            };

            Conversation.ConversationItems.Add(conversationItem);
            _currentConvItemMarkdown.Clear();
            Conversation.ResponseInProgress = string.Empty;
        }
    }

    protected virtual Task OnListingFileAddedAsync(AsyncListingFileAddedEventArgs e)
        => AsyncListingFileAdded?.Invoke(this, e) ?? Task.CompletedTask;
}
