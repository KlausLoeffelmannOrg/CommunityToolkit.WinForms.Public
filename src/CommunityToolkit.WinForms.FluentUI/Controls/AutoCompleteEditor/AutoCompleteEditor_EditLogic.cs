using CommunityToolkit.WinForms.AsyncSupport;

namespace CommunityToolkit.WinForms.FluentUI.Controls;

public partial class AutoCompleteEditor : RichTextBox
{
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Escape)
        {
            RejectSuggestion();
        }
        else if (e.KeyCode == Keys.Enter && !e.Control && !e.Alt && !e.Shift)
        {
            e.SuppressKeyPress = true; // Prevent the Enter key from being input as text
            SendCommand();
        }

        else if (e.KeyCode == Keys.Right && e.Control)
        {
            AcceptSuggestionWordWise();
        }
        else if (e.KeyCode == Keys.Space && e.Control)
        {
            ForceNewSuggestion();
        }
        else
        {
            StartSuggestionTimer();
        }
    }

    private void SendCommand()
    {
        var eArgs = new AsyncSendCommandEventArgs(
            this.Text,
            this.Rtf);

        _oldParagraph = CurrentParagraph;
        _currentSuggestion = string.Empty;

        AsyncExtensions.RunAsync(this, OnSendCommandAsync, eArgs);
    }

    protected virtual async Task OnSendCommandAsync(AsyncSendCommandEventArgs eArgs)
    {
        if (AsyncSendCommand is null)
        {
            return;
        }

        AsyncEventInvoker<AsyncSendCommandEventArgs> invoker = new(
            asyncEventHandler: AsyncSendCommand,
            autoDisableWhenBusy: true,
            parallelInvoke: false);

        await invoker.RaiseEventAsync(this, eArgs);
    }

    private void StartSuggestionTimer()
    {
        _suggestionTimer.Interval = (int) MinTimeSuggestionRequestSensitivity * 1000;
        _suggestionTimer.Start();
    }

    private void SuggestionTimer_Tick(object? sender, EventArgs e)
    {
        _suggestionTimer.Stop();
        RequestSuggestion();
    }

    private void RequestSuggestion()
    {
        if (Text.Length >= MinCharCountSuggestionRequestSensitivity 
            && Text.Length <= MaxCharCountSuggestionRequestSensitivity)
        {
            var args = new AsyncRequestAutoCompleteSuggestionEventArgs(
                currentParagraph: CurrentParagraph,
                oldParagraph: OldParagraph,
                isCursorAtEnd: IsCursorAtEnd,
                cursorLocation: CursorLocation,
                suggestionText: string.Empty);

            OnRequestAutoCompleteSuggestion(args);
            _currentSuggestion = args.SuggestionText;

            ShowSuggestion();
        }
    }

    private void ShowSuggestion()
    {
        if (!string.IsNullOrEmpty(_currentSuggestion))
        {
            SelectionColor = SuggestionColor;
            SelectedText = $"[TAB->] {_currentSuggestion}";
            SelectionColor = StandardEditColor;
        }
    }

    private string GetCurrentParagraph()
    {
        int start = Text.LastIndexOf('\n', SelectionStart - 1) + 1;
        int end = Text.IndexOf('\n', SelectionStart);

        if (end == -1)
        {
            end = Text.Length;
        }

        return Text[start..end];
    }

    private void RejectSuggestion()
    {
        _currentSuggestion = string.Empty;
        // Logic to remove the suggestion text from the editor
    }

    private void AcceptSuggestionWordWise()
    {
        if (!string.IsNullOrEmpty(_currentSuggestion))
        {
            int cursorPos = SelectionStart;

            string remainingText = Text[cursorPos..];
            string[] words = remainingText.Split(
                [' ', '\n'],
                options: StringSplitOptions.RemoveEmptyEntries);

            if (words.Length > 0 && _currentSuggestion.StartsWith(words[0]))
            {
                string nextWord = words[0];
                int nextWordLength = nextWord.Length;

                // Change the color of the next word to the standard edit color
                SelectionStart = cursorPos;
                SelectionLength = nextWordLength;
                SelectionColor = StandardEditColor;

                // Update the text to include the next word
                SelectedText = nextWord;

                // Update the current suggestion by removing the accepted word
                _currentSuggestion = _currentSuggestion[nextWordLength..].TrimStart();

                // Move the cursor to the end of the accepted word
                SelectionStart = cursorPos + nextWordLength;
                SelectionLength = 0;
                SelectionColor = StandardEditColor;
            }
        }
    }

    private void ForceNewSuggestion()
    {
        RequestSuggestion();
    }

    private string CurrentParagraph => GetCurrentParagraph();
    private string? CurrentSuggestion => _currentSuggestion;
    private string OldParagraph => _oldParagraph;
    private bool IsCursorAtEnd => SelectionStart == Text.Length;

    private int CursorLocation
    {
        get
        {
            int paragraphStart = Text.LastIndexOf('\n', SelectionStart - 1) + 1;
            return SelectionStart - paragraphStart;
        }
    }
}
