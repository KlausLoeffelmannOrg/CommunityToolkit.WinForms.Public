﻿using CommunityToolkit.WinForms.ConversationView.Components;
using CommunityToolkit.WinForms.DebuggingAids;
using Markdig;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace CommunityToolkit.WinForms.Controls.Blazor;

/// <summary>
///  Represents a custom conversation view control that extends 
///  the BlazorWebView class.
/// </summary>
public partial class ConversationView : BlazorWebView
{
    public event EventHandler<ConversationTitleChangedEventArgs>? ConversationTitleChanged;
    public event EventHandler<ConversationItemAddedEventArgs>? ConversationItemAdded;

    private readonly Conversation _conversation;

    private readonly IServiceProvider? _serviceProvider;
    private readonly HexAsciiDumper _hexDumper = new();

    /// <summary>
    ///  Initializes a new instance of the ConversationView class.
    /// </summary>
    public ConversationView()
    {
        _serviceProvider = this.GetServiceProvider();

        if (_serviceProvider is not null)
        {
            Services = _serviceProvider;
        }

        _conversation = new Conversation()
        {
            Id = Guid.NewGuid(),
            BackColor = SystemColors.ControlLightLight.ToWebColor(),
            ForeColor = SystemColors.ControlText.ToWebColor(),
            Title = Conversation.GetDefaultTitle()
        };

        _conversation.PropertyChanged += ViewModel_PropertyChanged;
        _conversation.ConversationItems.CollectionChanged += ConversationItems_CollectionChanged;

        WebView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
        WebView.NavigationCompleted += WebView_NavigationCompleted;
        WebView.NavigationStarting += WebView_NavigationStarting;
    }

    /// <summary>
    ///  Gets or sets the unique identifier for the conversation.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Conversation Conversation
    {
        get => _conversation;
        set
        {
            if (object.Equals(_conversation, value))
            {
                return;
            }

            // Fast-fail if the value is null
            ArgumentNullException.ThrowIfNull(value);

            // We do not replacing the conversation, but updating it.
            _conversation.ConversationItems.Clear();

            // Let's clone the classes content first:
            _conversation.Id = value.Id;
            _conversation.Title = value.Title;
            _conversation.DateCreated = value.DateCreated;
            _conversation.DateLastEdited = value.DateLastEdited;
            _conversation.Summary = value.Summary;
            _conversation.Keywords = value.Keywords;
            _conversation.TokenCount = value.TokenCount;
            _conversation.Model = value.Model;
            _conversation.IdPersonality = value.IdPersonality;

            // Now we add the conversation items:
            foreach (var item in value.ConversationItems)
            {
                _conversation.ConversationItems.Add(item);
            }
        }
    }

    /// <summary>
    ///  Adds a conversation item to the conversation view.
    /// </summary>
    /// <param name="text">The text content of the conversation item.</param>
    /// <param name="isResponse">Indicates whether the item is a response.</param>
    /// <returns>The added conversation item.</returns>
    public ConversationItem AddConversationItem(string text, bool isResponse)
    {
        var item = new ConversationItem
        {
            BackColor = SystemColors.ControlLight.ToWebColor(),
            ForeColor = ForeColor.ToWebColor(),
            MarkdownContent = text,
            HtmlContent = Markdown.ToHtml(text),
            DateCreated = DateTimeOffset.Now,
            FirstResponseDuration = TimeSpan.Zero,
            IsResponse = isResponse
        };

        _conversation?.ConversationItems.Add(item);

        return item;
    }

    /// <summary>
    ///  Clears the conversation history.
    /// </summary>
    public void ClearHistory()
        => _conversation?.ConversationItems.Clear();

    /// <summary>
    ///  Converts the conversation items to a JSON string.
    /// </summary>
    /// <returns>A JSON string representing the conversation items.</returns>
    public string ToJson()
    {
        try
        {
            using MemoryStream stream = new();
            _conversation.WriteJSon(stream);

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async IAsyncEnumerable<string> UpdateCurrentResponseAsync(
        IAsyncEnumerable<string> asyncEnumerable)
    {
        if (_conversation is null)
        {
            yield break;
        }

        StringBuilder builtUpHtmlParagraphs = new();
        StringBuilder builtUpMarkdown = new();
        StringBuilder currentParagraph = new();
        StringBuilder localHtml = new();
        bool lockHtmlParagraphs = false;

        int listingCount = 0;

        await foreach (string word in asyncEnumerable)
        {
            currentParagraph.Append(word);

            if (word.EndsWith("```\n") && listingCount == 0
                || word.StartsWith("```") && word.Length > 3 && word[3] is not '\n')
            {
                if (listingCount == 0)
                {
                    builtUpHtmlParagraphs.AppendLine($"<listing>{word[3..].Trim()}</listing>");
                    builtUpHtmlParagraphs.Append("<pre><code>");
                    lockHtmlParagraphs = true;
                }

                listingCount++;
            }

            if (word.EndsWith("```\n") && listingCount > 0)
            {
                listingCount--;

                if (listingCount == 0)
                {
                    builtUpHtmlParagraphs.Append("</code></pre>");
                    localHtml.Clear();
                }
            }

            // Convert the accumulated text to HTML
            string localMarkdown = currentParagraph.ToString();

            if (listingCount == 0)
            {
                localHtml.Clear();
                localHtml.Append(Markdown.ToHtml(localMarkdown));
            }
            else
            {
                if (!lockHtmlParagraphs)
                {
                    // Watch out for special characters in the code block!
                    string htmlFriendlyWord = word.Replace("<", "&lt;").Replace(">", "&gt;");
                    localHtml.Append(htmlFriendlyWord);
                }
                else
                {
                    lockHtmlParagraphs = false;
                }
            }

            if (word.EndsWith('\n'))
            {
                builtUpHtmlParagraphs.Append(localHtml);
                builtUpMarkdown.Append(localMarkdown);
                currentParagraph.Clear();
                localHtml.Clear();
            }

            _conversation.ResponseInProgress =
                builtUpHtmlParagraphs.ToString()
                + localHtml
                + (listingCount > 0
                    ? "</code></pre>"
                    : string.Empty);
        }
    }

    private void ConversationItems_CollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (ConversationItemAdded is not null
            && e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add
            && e.NewItems?.Count == 1
            && e.NewItems[0] is ConversationItem conversationItem)
        {
            OnConversationItemAdded(new ConversationItemAddedEventArgs(conversationItem));
        }

        _conversation.DateLastEdited = DateTimeOffset.Now;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Conversation.Title))
        {
            OnConversationTitleChanged(new ConversationTitleChangedEventArgs(_conversation.Title));
        }

        if (e.PropertyName != nameof(Conversation.DateLastEdited))
        {
            _conversation.DateLastEdited = DateTimeOffset.Now;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateRootComponent();

    }

    private void UpdateRootComponent()
    {
        if (IsAncestorSiteInDesignMode)
        {
            return;
        }

        // Create new dictionary of parameters for the component
        Dictionary<string, object?> parameters = new()
            {
                { nameof(ConversationRenderer.ViewModel), _conversation },
                { nameof(ConversationRenderer.BackColor), SystemColors.ControlDark.ToWebColor() }
            };

        RootComponent component = new(
            selector: "#app",
            componentType: typeof(ConversationRenderer),
            parameters: parameters);

        RootComponents.Clear();
        RootComponents.Add(component);
        HostPage = $"wwwroot/index.html";
    }

    /// <summary>
    /// Raises the <see cref="ConversationTitleChanged"/> event.
    /// </summary>
    /// <param name="conversationTitleChangedEventArgs">The event data.</param>
    protected virtual void OnConversationTitleChanged(ConversationTitleChangedEventArgs conversationTitleChangedEventArgs)
        => ConversationTitleChanged?.Invoke(this, conversationTitleChangedEventArgs);

    /// <summary>
    /// Paints the background of the control.
    /// </summary>
    /// <param name="e">A <see cref="PaintEventArgs"/> that contains the event data.</param>
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
        e.Graphics.Clear(SystemColors.ControlLightLight);
    }

    /// <summary>
    /// Raises the <see cref="ConversationItemAdded"/> event.
    /// </summary>
    /// <param name="conversationItemAddedEventArgs">The event data.</param>
    protected virtual void OnConversationItemAdded(ConversationItemAddedEventArgs conversationItemAddedEventArgs)
        => ConversationItemAdded?.Invoke(this, conversationItemAddedEventArgs);
}
