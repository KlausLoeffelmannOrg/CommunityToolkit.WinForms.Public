﻿using System.ComponentModel;

namespace CommunityToolkit.WinForms.FluentUI;

public partial class FluentDecoratorPanel : Panel
{
    private int _controlPadding;
    private BorderStyles _borderStyle;
    private int _borderThickness;
    private Orientation _orientation;

    public FluentDecoratorPanel()
    {
        _controlPadding = 10;
        _borderStyle = BorderStyles.Modern;
        _borderThickness = 1;
        _verticalContentAlignment = VerticalContentAlignments.Center;
        _orientation = Orientation.Horizontal;

        // Enable double buffering to reduce flickering
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    /// <summary>
    /// Gets or sets the space between controls laid out in one row.
    /// </summary>
    [Category("Layout")]
    [Description("Specifies the space left between the controls laid out in one row.")]
    [DefaultValue(10)]
    public int ControlPadding
    {
        get => _controlPadding;
        set
        {
            _controlPadding = value;
            PerformLayout();
        }
    }

    /// <summary>
    /// Gets or sets the style of the border.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies the style of the border.")]
    [DefaultValue(BorderStyles.Modern)]
    public new BorderStyles BorderStyle
    {
        get => _borderStyle;
        set
        {
            _borderStyle = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the thickness of the border.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies the thickness of the border.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int BorderThickness
    {
        get => _borderThickness;
        set
        {
            _borderThickness = value;
            PerformLayout();
            Invalidate();
        }
    }

    private VerticalContentAlignments _verticalContentAlignment;

    /// <summary>
    /// Gets or sets the vertical alignment of the content.
    /// </summary>
    [Category("Layout")]
    [Description("Specifies the vertical alignment of the content.")]
    [DefaultValue(VerticalContentAlignments.Center)]
    public VerticalContentAlignments VerticalContentAlignment
    {
        get => _verticalContentAlignment;
        set
        {
            _verticalContentAlignment = value;
            PerformLayout();
        }
    }

    /// <summary>
    /// Gets or sets the orientation of the controls.
    /// </summary>
    [Category("Layout")]
    [Description("Specifies the orientation of the controls.")]
    [DefaultValue(Orientation.Horizontal)]
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;
            PerformLayout();
        }
    }

    protected override void OnPaddingChanged(EventArgs e)
    {
        base.OnPaddingChanged(e);
        PerformLayout();
        Invalidate();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        if (Controls.Count == 0)
            return;

        // Get the first control
        Control firstControl = Controls[0];

        // Calculate the total size of additional controls plus padding
        int totalAdditionalSize = 0;

        for (int i = 1; i < Controls.Count; i++)
        {
            Control ctrl = Controls[i];
            totalAdditionalSize += _orientation == Orientation.Horizontal ? ctrl.Width : ctrl.Height;
        }

        int totalControlPadding = (Controls.Count - 1) * ControlPadding;

        totalAdditionalSize += totalControlPadding;

        // Adjust for border thickness and padding
        int adjustedWidth = Width - (2 * BorderThickness) - Padding.Left - Padding.Right;
        int adjustedHeight = Height - (2 * BorderThickness) - Padding.Top - Padding.Bottom;

        // Adjust first control's size
        int firstControlSize = Math.Max(0, (_orientation == Orientation.Horizontal ? adjustedWidth : adjustedHeight) - totalAdditionalSize);

        // Calculate position based on alignment
        int xPosition = BorderThickness + Padding.Left;
        int yPosition = BorderThickness + Padding.Top + GetVerticalOffset(firstControl.Height, adjustedHeight);

        if (_orientation == Orientation.Vertical)
        {
            yPosition = BorderThickness + Padding.Top;
            xPosition += GetHorizontalOffset(firstControl.Width, adjustedWidth);
        }

        // Set first control position and size
        firstControl.Location = new Point(xPosition, yPosition);
        if (_orientation == Orientation.Horizontal)
        {
            firstControl.Size = new Size(firstControlSize, firstControl.Height);
        }
        else
        {
            firstControl.Size = new Size(firstControl.Width, firstControlSize);
        }

        // Position additional controls
        int currentPosition = _orientation == Orientation.Horizontal 
            ? xPosition + firstControlSize + ControlPadding 
            : yPosition + firstControlSize + ControlPadding;

        for (int i = 1; i < Controls.Count; i++)
        {
            Control ctrl = Controls[i];
            if (_orientation == Orientation.Horizontal)
            {
                ctrl.Location = new Point(currentPosition, yPosition);
                ctrl.Height = firstControl.Height; // Ensure same height
                currentPosition += ctrl.Width + ControlPadding;
            }
            else
            {
                ctrl.Location = new Point(xPosition, currentPosition);
                ctrl.Width = firstControl.Width; // Ensure same width
                currentPosition += ctrl.Height + ControlPadding;
            }
        }

        // Handle 'Fill' alignment
        if (VerticalContentAlignment == VerticalContentAlignments.Fill)
        {
            foreach (Control ctrl in Controls)
            {
                if (_orientation == Orientation.Horizontal)
                {
                    ctrl.Height = adjustedHeight;
                }
                else
                {
                    ctrl.Width = adjustedWidth;
                }
            }
        }
    }

    private int GetVerticalOffset(int contentHeight, int containerHeight) 
        => VerticalContentAlignment switch
            {
                VerticalContentAlignments.Center => (containerHeight - contentHeight) / 2,
                VerticalContentAlignments.Bottom => containerHeight - contentHeight,
                _ => 0, // Top and Fill don't require an offset.
            };

    private int GetHorizontalOffset(int contentWidth, int containerWidth)
    {
        return VerticalContentAlignment switch
        {
            VerticalContentAlignments.Center => (containerWidth - contentWidth) / 2,
            VerticalContentAlignments.Bottom => containerWidth - contentWidth,
            _ => 0, // Top and Fill don't require an offset.
        };
    }

    // Ensure the layout updates when the control is resized
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        PerformLayout();
    }

    // Ensure the layout updates when a control is added or removed
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        PerformLayout();
    }

    protected override void OnControlRemoved(ControlEventArgs e)
    {
        base.OnControlRemoved(e);
        PerformLayout();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (BorderStyle == BorderStyles.None || BorderThickness <= 0)
            return;

        using Pen pen = new(ForeColor, BorderThickness);
        pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;

        Rectangle borderRect = new(
            BorderThickness / 2,
            BorderThickness / 2,
            Width - BorderThickness,
            Height - BorderThickness);

        if (BorderStyle == BorderStyles.Classic)
        {
            e.Graphics.DrawRectangle(pen, borderRect);
        }
        else if (BorderStyle == BorderStyles.Modern)
        {
            int radius = 10; // Adjust the radius for rounded corners
            DrawRoundedRectangle(e.Graphics, pen, borderRect, radius);
        }
    }

    private void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle rect, int radius)
    {
        System.Drawing.Drawing2D.GraphicsPath path = new();

        int diameter = radius * 2;
        Size size = new(diameter, diameter);
        Rectangle arc = new(rect.Location, size);

        // Top-left arc
        path.AddArc(arc, 180, 90);

        // Top-right arc
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);

        // Bottom-right arc
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        // Bottom-left arc
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();

        g.DrawPath(pen, path);
    }

    // Ensure the control repaints when ForeColor changes
    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        Invalidate();
    }
}
