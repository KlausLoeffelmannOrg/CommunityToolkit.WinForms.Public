﻿using CommunityToolkit.DesktopGeneric.Mvvm;
using CommunityToolkit.WinForms.ComponentModel;
using System.ComponentModel;
using System.Globalization;
using DialogResult = CommunityToolkit.DesktopGeneric.Mvvm.DialogResult;
using WinFormsDialogResult = System.Windows.Forms.DialogResult;

namespace CommunityToolkit.WinForms.Extensions;

public static class FormExtensions
{
    /// <summary>
    ///  Centers the form on the specified screen or the screen containing the form.
    /// </summary>
    /// <param name="form">The form to be centered.</param>
    /// <param name="screen">The screen on which to center the form. If null, the screen containing the form is used.</param>
    /// <param name="horizontalFillGrade">The percentage of the horizontal space to fill. Default is 50%.</param>
    /// <param name="verticalFillGrade">The percentage of the vertical space to fill. Default is 50%.</param>
    /// <returns>A <see cref="Rectangle"/> representing the bounds of the centered form.</returns>
    public static Rectangle CenterOnScreen(this Form form, Screen? screen = null, int horizontalFillGrade = 50, int verticalFillGrade = 50)
    {
        Rectangle screenBounds = (screen ?? Screen.FromControl(form)).Bounds;

        int width = screenBounds.Width * horizontalFillGrade / 100;
        int height = screenBounds.Height * verticalFillGrade / 100;

        int x = screenBounds.X + (screenBounds.Width - width) / 2;
        int y = screenBounds.Y + (screenBounds.Height - height) / 2;

        return new Rectangle(x, y, width, height);
    }

    /// <summary>
    ///  Gets the bounds of the form that can be restored to.
    /// </summary>
    /// <param name="form">The form whose restorable bounds are to be retrieved.</param>
    /// <returns>A <see cref="Rectangle"/> representing the restorable bounds of the form.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the form is in an unknown window state.</exception>
    public static Rectangle GetRestorableBounds(this Form form)
        => form.WindowState switch
        {
            FormWindowState.Normal => form.Bounds,
            FormWindowState.Maximized => form.RestoreBounds,
            FormWindowState.Minimized => form.RestoreBounds,
            _ => throw new InvalidOperationException("Unknown window state.")
        };

    /// <summary>
    ///  Shows the form as a modal dialog box with a data context.
    /// </summary>
    /// <typeparam name="T">The type of the data context.</typeparam>
    /// <param name="form">The form to be shown as a dialog.</param>
    /// <param name="dialogDataContext">
    ///  The data context to be passed to the form. This parameter is passed by 
    ///  reference and will be updated with the form's data context after the dialog is closed.</param>
    /// <returns>
    ///  A <see cref="DialogResult{T}"/> containing the result of the dialog and
    ///  the updated data context.
    /// </returns>
    public async static Task<IModalDialogResult<T>> ShowDialogAsync<T>(this Form form, T? dialogDataContext)
        where T : class, INotifyPropertyChanged
    {
        if (dialogDataContext is not null)
        {
            form.DataContext = dialogDataContext;
        }

#pragma warning disable WFO5002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var winFormsDialogResult = await form.ShowDialogAsync();
#pragma warning restore WFO5002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        dialogDataContext = (T?)form.DataContext;

        DialogResult dialogResult = winFormsDialogResult switch
        {
            WinFormsDialogResult.OK => DialogResult.First,
            WinFormsDialogResult.Cancel => DialogResult.Second,
            WinFormsDialogResult.Yes => DialogResult.First,
            WinFormsDialogResult.No => DialogResult.Second,
            WinFormsDialogResult.Abort => DialogResult.First,
            WinFormsDialogResult.Retry => DialogResult.Second,
            WinFormsDialogResult.Ignore => DialogResult.Third,
            _ => DialogResult.None
        };

        return new ModalDialogResult<T>(dialogDataContext, dialogResult);
    }

    /// <summary>
    ///  Adds a value converter to an existing binding on a bindable component.
    /// </summary>
    /// <param name="bindableComponent">The bindable component to which the converter is added.</param>
    /// <param name="propertyName">The name of the property that is bound.</param>
    /// <param name="valueConverter">The value converter to be added to the binding.</param>
    /// <returns>The binding with the added value converter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> or <paramref name="valueConverter"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no binding is found for the specified property.</exception>
    public static Binding AddBindingConverter(
        this IBindableComponent bindableComponent,
        string propertyName,
        IValueConverter valueConverter)
    {
        ArgumentNullException.ThrowIfNull(propertyName, nameof(propertyName));
        ArgumentNullException.ThrowIfNull(valueConverter, nameof(valueConverter));

        if (bindableComponent.DataBindings[propertyName] is not Binding binding)
        {
            throw new InvalidOperationException($"No binding found for property '{propertyName}'.");
        }

        binding.Parse += Binding_Parse;
        binding.Format += Binding_Format;

        var managerBase = binding.BindingManagerBase;

        bindableComponent.Disposed += Control_Disposed;
        return binding;

        void Binding_Format(object? sender, ConvertEventArgs e)
        {
            e.Value = valueConverter.Convert(e.Value, e.DesiredType!, null, CultureInfo.CurrentCulture);
        }

        void Binding_Parse(object? sender, ConvertEventArgs e)
        {
            e.Value = valueConverter.ConvertBack(e.Value, e.DesiredType!, null, CultureInfo.CurrentCulture);
        }

        void Control_Disposed(object? sender, EventArgs e)
        {
            binding.Parse -= Binding_Parse;
            binding.Format -= Binding_Format;
        }
    }

    /// <summary>
    ///  Retrieves the direct child controls of the specified control.
    /// </summary>
    /// <param name="control">The control whose child controls are retrieved.</param>
    /// <returns>An enumerable of child controls.</returns>
    public static IEnumerable<Control> ChildControls(this Control control)
        => control.Controls.Cast<Control>();

    /// <summary>
    ///  Retrieves the direct child controls of the specified control that are of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the child controls to retrieve.</typeparam>
    /// <param name="control">The control whose child controls are retrieved.</param>
    /// <returns>An enumerable of child controls of the specified type.</returns>
    public static IEnumerable<T> ChildControls<T>(this Control control) where T : Control
        => control.Controls.Cast<T>();

    /// <summary>
    ///  Retrieves the first child control that matches the specified predicate.
    /// </summary>
    /// <param name="control">The control to search within.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>The first matching child control.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no child control is found.</exception>
    public static Control FirstChild(this Control control, Func<Control, bool>? predicate = default)
        => control.FirstChild<Control>(predicate);

    /// <summary>
    ///  Retrieves the first child control that matches the specified predicate.
    /// </summary>
    /// <typeparam name="T">The type of the child control to retrieve.</typeparam>
    /// <param name="control">The control to search within.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>The first matching child control.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no child control is found.</exception>
    public static T FirstChild<T>(this Control control, Func<T, bool>? predicate = default)
        where T : Control
        => control.FirstChildOrDefault(predicate) ?? throw new InvalidOperationException("No child control found.");

    /// <summary>
    ///  Retrieves the first child control that matches the specified predicate, or null if none is found.
    /// </summary>
    /// <typeparam name="T">The type of the child control to retrieve.</typeparam>
    /// <param name="control">The control to search within.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>The first matching child control, or null if none is found.</returns>
    public static T? FirstChildOrDefault<T>(this Control control, Func<T, bool>? predicate = default)
        where T : Control
    {
        foreach (Control childControl in control.ChildControls())
        {
            if (childControl is T t && (predicate == null || predicate(t)))
            {
                return t;
            }

            T? grandChildControl = childControl.FirstChildOrDefault(predicate);
            if (grandChildControl is not null)
            {
                return grandChildControl;
            }
        }

        return null;
    }

    /// <summary>
    ///  Enumerates the control's ascendant controls, up to the root of the control tree.
    /// </summary>
    /// <param name="control">The control to retrieve ascendants from.</param>
    /// <returns>An enumerable of ascendant controls.</returns>
    public static IEnumerable<Control> AscendantControls(this Control control)
        => control.AscendantControls<Control>();

    /// <summary>
    ///  Enumerates the control's ascendant controls of the specified type, up to the root of the control tree.
    /// </summary>
    /// <typeparam name="T">The type of the ascendant controls to retrieve.</typeparam>
    /// <param name="control">The control to retrieve ascendants from.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>An enumerable of ascendant controls of the specified type.</returns>
    public static IEnumerable<T> AscendantControls<T>(this Control control, Func<Control, bool>? predicate = default)
        where T : Control
    {
        Control? parent = control.Parent;

        while (parent is not null)
        {
            if (parent is T t && (predicate == null || predicate(t)))
            {
                yield return t;
            }
            parent = parent.Parent;
        }
    }

    /// <summary>
    ///  Retrieves the first ascendant control that matches the specified predicate.
    /// </summary>
    /// <param name="control">The control to search within.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>The first matching ascendant control.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no ascendant control is found.</exception>
    public static Control FirstAscendant(this Control control, Func<Control, bool>? predicate = default)
        => control.FirstAscendant<Control>(predicate);

    /// <summary>
    ///  Retrieves the first ascendant control that matches the specified predicate, or null if none is found.
    /// </summary>
    /// <param name="control">The control to search within.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>The first matching ascendant control, or null if none is found.</returns>
    public static Control? FirstAscendantOrDefault(this Control control, Func<Control, bool>? predicate = default)
        => control.FirstAscendantOrDefault<Control>(predicate);

    /// <summary>
    ///  Retrieves the first ascendant control of the specified type that matches the specified predicate.
    /// </summary>
    /// <typeparam name="T">The type of the ascendant control to retrieve.</typeparam>
    /// <param name="control">The control to search within.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>The first matching ascendant control.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no ascendant control is found.</exception>
    public static T FirstAscendant<T>(this Control control, Func<T, bool>? predicate = default)
        where T : Control
        => control.FirstAscendantOrDefault(predicate) ?? throw new InvalidOperationException("No ascendant control found.");

    /// <summary>
    ///  Retrieves the first ascendant control of the specified type that matches the specified predicate, or null if none is found.
    /// </summary>
    /// <typeparam name="T">The type of the ascendant control to retrieve.</typeparam>
    /// <param name="control">The control to search within.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>The first matching ascendant control, or null if none is found.</returns>
    public static T? FirstAscendantOrDefault<T>(this Control control, Func<T, bool>? predicate = default)
        where T : Control
    {
        foreach (Control parentControl in control.AscendantControls())
        {
            if (parentControl is T t && (predicate == null || predicate(t)))
            {
                return t;
            }
        }
        return null;
    }

    /// <summary>
    ///  Retrieves the root control in the control tree.
    /// </summary>
    /// <param name="control">The starting control.</param>
    /// <returns>The root control.</returns>
    public static Control Root(this Control control)
    {
        Control? parent = control.Parent;
        while (parent is not null)
        {
            control = parent;
            parent = parent.Parent;
        }

        return control;
    }

    /// <summary>
    ///  Enumerates all descendant controls of the specified control.
    /// </summary>
    /// <param name="control">The control to retrieve descendants from.</param>
    /// <returns>An enumerable of descendant controls.</returns>
    public static IEnumerable<Control> DescendantControls(this Control control)
        => control.DescendantControls<Control>();

    /// <summary>
    ///  Enumerates all descendant controls of the specified control that are of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the descendant controls to retrieve.</typeparam>
    /// <param name="control">The control to retrieve descendants from.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>An enumerable of descendant controls of the specified type.</returns>
    public static IEnumerable<T> DescendantControls<T>(this Control control, Func<Control, bool>? predicate = default)
        where T : Control
    {
        foreach (Control childControl in control.ChildControls())
        {
            if (childControl is T t && (predicate == null || predicate(t)))
            {
                yield return t;
            }

            foreach (T grandChildControl in childControl.DescendantControls<T>(predicate))
            {
                yield return grandChildControl;
            }
        }
    }

    /// <summary>
    ///  Retrieves the first descendant control that matches the specified predicate.
    /// </summary>
    /// <param name="control">The control to search within.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>The first matching descendant control.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no descendant control is found.</exception>
    public static Control FirstDescendant(this Control control, Func<Control, bool>? predicate = default)
        => control.FirstDescendant<Control>(predicate);

    /// <summary>
    ///  Retrieves the first descendant control of the specified type that matches the specified predicate.
    /// </summary>
    /// <typeparam name="T">The type of the descendant control to retrieve.</typeparam>
    /// <param name="control">The control to search within.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>The first matching descendant control.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no descendant control is found.</exception>
    public static T FirstDescendant<T>(this Control control, Func<T, bool>? predicate = default)
        where T : Control
        => control.FirstDescendantOrDefault(predicate) ?? throw new InvalidOperationException("No descendant control found.");

    /// <summary>
    ///  Retrieves the first descendant control that matches the specified predicate, or null if none is found.
    /// </summary>
    /// <param name="control">The control to search within.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>The first matching descendant control, or null if none is found.</returns>
    public static Control? FirstDescendantOrDefault(this Control control, Func<Control, bool>? predicate = default)
        => control.FirstDescendantOrDefault<Control>(predicate);

    /// <summary>
    ///  Retrieves the first descendant control of the specified type that matches the specified predicate, or null if none is found.
    /// </summary>
    /// <typeparam name="T">The type of the descendant control to retrieve.</typeparam>
    /// <param name="control">The control to search within.</param>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>The first matching descendant control, or null if none is found.</returns>
    public static T? FirstDescendantOrDefault<T>(this Control control, Func<T, bool>? predicate = default)
        where T : Control
    {
        foreach (Control childControl in control.ChildControls())
        {
            if (childControl is T t && (predicate == null || predicate(t)))
            {
                return t;
            }

            T? grandChildControl = childControl.FirstDescendantOrDefault(predicate);

            if (grandChildControl is not null)
            {
                return grandChildControl;
            }
        }

        return null;
    }
}
