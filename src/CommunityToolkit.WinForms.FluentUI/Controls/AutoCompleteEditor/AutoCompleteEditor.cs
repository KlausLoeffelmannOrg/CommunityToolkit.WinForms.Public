﻿using CommunityToolkit.WinForms.AsyncSupport;
using System.ComponentModel;

using Timer = System.Windows.Forms.Timer;

namespace CommunityToolkit.WinForms.FluentUI.Controls;

public partial class AutoCompleteEditor : RichTextBox
{
    public event AsyncEventHandler<AsyncRequestAutoCompleteSuggestionEventArgs>? AsyncRequestAutoCompleteSuggestion;
    public event AsyncEventHandler<AsyncSendCommandEventArgs>? AsyncSendCommand;

    private int _minCharCountSuggestionRequestSensitivity = 5;
    private int _maxCharCountSuggestionRequestSensitivity = 10;
    private float _minTimeSuggestionRequestSensitivity = 0;
    private bool _allowExistingRemainingTextReplacement;
    private Color _standardEditColor;
    private Color _suggestionColor;
    private Color _autoCorrectionColor;
    private Color _correctionCandidateColor;

    private Timer _suggestionTimer;
    private string _currentSuggestion = string.Empty;
    private string _oldParagraph = string.Empty;

    private static readonly Color DefaultLightModeStandardEditColor = Color.Black;
    private static readonly Color DefaultDarkModeStandardEditColor = Color.White;
    private static readonly Color DefaultLightModeSuggestionColor = Color.Blue;
    private static readonly Color DefaultDarkModeSuggestionColor = Color.LightBlue;
    private static readonly Color DefaultLightModeAutoCorrectionColor = Color.Green;
    private static readonly Color DefaultDarkModeAutoCorrectionColor = Color.LightGreen;
    private static readonly Color DefaultLightModeCorrectionCandidateColor = Color.Red;
    private static readonly Color DefaultDarkModeCorrectionCandidateColor = Color.OrangeRed;

    public AutoCompleteEditor()
    {
        _suggestionTimer = new Timer();
        _suggestionTimer.Tick += SuggestionTimer_Tick;

        if (Application.IsDarkModeEnabled)
        {
            _standardEditColor = DefaultDarkModeStandardEditColor;
            _suggestionColor = DefaultDarkModeSuggestionColor;
            _autoCorrectionColor = DefaultDarkModeAutoCorrectionColor;
            _correctionCandidateColor = DefaultDarkModeCorrectionCandidateColor;
        }
        else
        {
            _standardEditColor = DefaultLightModeStandardEditColor;
            _suggestionColor = DefaultLightModeSuggestionColor;
            _autoCorrectionColor = DefaultLightModeAutoCorrectionColor;
            _correctionCandidateColor = DefaultLightModeCorrectionCandidateColor;
        }
    }

    [Description("Minimum character count to trigger suggestion request.")]
    [DefaultValue(5)]
    [Category("Behavior")]
    public int MinCharCountSuggestionRequestSensitivity
    {
        get => _minCharCountSuggestionRequestSensitivity;
        set
        {
            if (value < 5 || value > 20)
                throw new ArgumentOutOfRangeException(nameof(value), "Valid values are between 5 and 20.");
            _minCharCountSuggestionRequestSensitivity = value;
        }
    }

    [Description("Maximum character count to trigger suggestion request.")]
    [DefaultValue(10)]
    [Category("Behavior")]
    public int MaxCharCountSuggestionRequestSensitivity
    {
        get => _maxCharCountSuggestionRequestSensitivity;
        set
        {
            if (value < 10 || value > 200)
                throw new ArgumentOutOfRangeException(nameof(value), "Valid values are between 10 and 200.");
            _maxCharCountSuggestionRequestSensitivity = value;
        }
    }

    [Description("Minimum time in seconds to trigger suggestion request.")]
    [DefaultValue(0f)]
    [Category("Behavior")]
    public float MinTimeSuggestionRequestSensitivity
    {
        get => _minTimeSuggestionRequestSensitivity;
        set
        {
            if (value < 0 || value > 9)
                throw new ArgumentOutOfRangeException(nameof(value), "Valid values are between 0 and 9 seconds.");
            _minTimeSuggestionRequestSensitivity = value;
        }
    }

    [Description("Allow replacement of existing remaining text.")]
    [DefaultValue(false)]
    [Category("Behavior")]
    public bool AllowExistingRemainingTextReplacement
    {
        get => _allowExistingRemainingTextReplacement;
        set => _allowExistingRemainingTextReplacement = value;
    }

    [Description("Color for standard text editing.")]
    [Category("Appearance")]
    public Color StandardEditColor
    {
        get => _standardEditColor;
        set => _standardEditColor = value;
    }

    [Description("Color for suggestions.")]
    [Category("Appearance")]
    public Color SuggestionColor
    {
        get => _suggestionColor;
        set => _suggestionColor = value;
    }

    [Description("Color for auto-corrections.")]
    [Category("Appearance")]
    public Color AutoCorrectionColor
    {
        get => _autoCorrectionColor;
        set => _autoCorrectionColor = value;
    }

    [Description("Color for correction candidates.")]
    [Category("Appearance")]
    public Color CorrectionCandidateColor
    {
        get => _correctionCandidateColor;
        set => _correctionCandidateColor = value;
    }

    private bool ShouldSerializeStandardEditColor()
    {
        return Application.IsDarkModeEnabled
            ? _standardEditColor != DefaultDarkModeStandardEditColor
            : _standardEditColor != DefaultLightModeStandardEditColor;
    }

    private bool ShouldSerializeSuggestionColor()
    {
        return Application.IsDarkModeEnabled
            ? _suggestionColor != DefaultDarkModeSuggestionColor
            : _suggestionColor != DefaultLightModeSuggestionColor;
    }

    private bool ShouldSerializeAutoCorrectionColor()
    {
        return Application.IsDarkModeEnabled
            ? _autoCorrectionColor != DefaultDarkModeAutoCorrectionColor
            : _autoCorrectionColor != DefaultLightModeAutoCorrectionColor;
    }

    private bool ShouldSerializeCorrectionCandidateColor()
    {
        return Application.IsDarkModeEnabled
            ? _correctionCandidateColor != DefaultDarkModeCorrectionCandidateColor
            : _correctionCandidateColor != DefaultLightModeCorrectionCandidateColor;
    }

    protected virtual void OnRequestAutoCompleteSuggestion(AsyncRequestAutoCompleteSuggestionEventArgs e)
    {
        AsyncRequestAutoCompleteSuggestion?.Invoke(this, e);
    }
}
