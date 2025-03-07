﻿using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CommunityToolkit.WinForms.FluentUI;

public partial class GridView : DataGridView
{
    public event EventHandler? GridViewItemTemplateChanged;
    public event EventHandler? SelectedItemChanged;

    private GridViewItemTemplate? _gridViewItemTemplate;
    private INotifyCollectionChanged? _observableCollection;
    private ICollection? _underlayingList;

    private readonly Color DarkModeBackgroundColor
        = Color.FromArgb(255, 20, 20, 20);

    private readonly Color LightModeBackgroundColor
        = Color.FromArgb(255, 164, 164, 164);

    private object? _selectedItem;
    private Pen _selectionPen;
    private readonly Padding _selectionPadding = new(4, 4, 4, 2);

    private Color ThemedDataGridBackground
        => Application.IsDarkModeEnabled
        ? DarkModeBackgroundColor
        : LightModeBackgroundColor;

    public GridView()
    {
        _selectionPen = new(SystemColors.WindowText, 2);

        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToOrderColumns = false;
        AllowUserToResizeColumns = false;
        AllowUserToResizeRows = false;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        DoubleBuffered = true;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        ColumnHeadersVisible = false;
        RowHeadersVisible = false;
        VirtualMode = true;

        GridViewCell cellTemplate = new()
        {
            ItemTemplateGetter = new(() => GridViewItemTemplate)
        };

        DataGridViewColumn dataRowObjectColumn = new(cellTemplate)
        {
            Name = "DataColumn",
            HeaderText = "DataColumn",
            SortMode = DataGridViewColumnSortMode.NotSortable,
        };

        Columns.Add(dataRowObjectColumn);
    }

    [Bindable(false)]
    [Browsable(true)]
    [AttributeProvider(typeof(IListSource))]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new object? DataContext
    {
        get => base.DataContext;
        set => base.DataContext = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Bindable(true)]
    [Browsable(false)]
    public object? SelectedItem
    {
        get => _selectedItem = base.SelectedRows.Count > 0
            ? ((IList)_underlayingList!)[SelectedRows[0].Index]
            : null;

        set
        {
            if (value == _selectedItem)
            {
                return;
            }

            try
            {
                _selectedItem = value;

                if (value is null)
                {
                    ClearSelection();
                    return;
                }

                foreach (DataGridViewRow row in Rows)
                {
                    if (((IList)_underlayingList!)[row.Index] == value)
                    {
                        ClearSelection();
                        row.Selected = true;
                        FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }
            }
            finally
            {
                OnSelectedItemChanged(EventArgs.Empty);
            }
        }
    }

    protected override void OnSelectionChanged(EventArgs e)
    {
        base.OnSelectionChanged(e);
        OnSelectedItemChanged(EventArgs.Empty);
    }

    protected virtual void OnSelectedItemChanged(EventArgs e)
        => SelectedItemChanged?.Invoke(this, e);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Bindable(false)]
    [Browsable(true)]
    public GridViewItemTemplate? GridViewItemTemplate
    {
        get => _gridViewItemTemplate;
        set
        {
            if (_gridViewItemTemplate == value)
            {
                return;
            }

            _gridViewItemTemplate = value;

            if (_gridViewItemTemplate is not null)
            {
                _gridViewItemTemplate.IsDarkMode = Application.IsDarkModeEnabled;
            }

            OnGridViewItemTemplateChanged();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (IsAncestorSiteInDesignMode)
        {
            return;
        }

        AutoGenerateColumns = false;

        if (DataContext is null)
        {
            _underlayingList = null;
            _observableCollection = null;
            Rows.Clear();
            RowCount = 0;

            return;
        }

        // Let's try to retrieve the Number of rows:
        if (DataContext is INotifyCollectionChanged observableCollection
            and IList list)
        {
            _observableCollection = observableCollection;
            _underlayingList = list;

            Rows.Clear();
            RowCount = list.Count;
        }
    }

    private void OnGridViewItemTemplateChanged()
        => GridViewItemTemplateChanged?.Invoke(this, EventArgs.Empty);

    protected override void OnRowHeightInfoNeeded(DataGridViewRowHeightInfoNeededEventArgs e)
    {
        base.OnRowHeightInfoNeeded(e);
    }

    protected override void OnRowPrePaint(DataGridViewRowPrePaintEventArgs e)
        => e.Graphics.FillRectangle(
            new SolidBrush(ThemedDataGridBackground),
            e.RowBounds);

    protected override void OnRowPostPaint(DataGridViewRowPostPaintEventArgs e)
    {
        if (e.RowIndex == -1)
        {
            return;
        }

        // Get the row
        DataGridViewRow currentRow = Rows[e.RowIndex];

        if (currentRow.Selected)
        {
            e.Graphics.SmoothingMode =
                System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // We need the bounds of the row to paint the selection rectangle:
            Rectangle rowBounds = new Rectangle(
                    x: e.RowBounds.Left,
                    y: e.RowBounds.Top,
                    width: e.RowBounds.Width,
                    height: e.RowBounds.Height)
                // Don't look for it - that's an extension method I wrote :-)
                .Pad(_selectionPadding);

            e.Graphics
                // New in .NET 9: DrawRoundedRectangle!
                .DrawRoundedRectangle(
                    // That pen color is dark-mode aware at this point.
                    pen: _selectionPen,
                    rect: rowBounds,
                    // That's the radius of the rounded corners.
                    radius: new Size(10, 10));
        }
    }

    protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
    {
        base.OnCellValueNeeded(e);

        if (_underlayingList is null)
        {
            return;
        }

        if (_underlayingList is IList list)
        {
            e.Value = list[e.RowIndex];
        }
        else if (_underlayingList is IEnumerable)
        {
            e.Value = _underlayingList
                .Cast<object?>()
                .ElementAt(e.RowIndex);
        }
        else
        {
            throw new InvalidOperationException(
                "DataContext is not a list or an enumerable.");
        }
    }
}
