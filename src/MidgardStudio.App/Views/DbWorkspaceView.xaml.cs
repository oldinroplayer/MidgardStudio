using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MidgardStudio.App.ViewModels;

namespace MidgardStudio.App.Views;

public partial class DbWorkspaceView : UserControl
{
    private GridViewColumn? _keyColumn, _nameColumn;

    public DbWorkspaceView()
    {
        InitializeComponent();
        MasterList.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(OnHeaderClick));
        MasterList.SelectionChanged += (_, _) =>
        {
            if (MasterList.SelectedItem is { } item) MasterList.ScrollIntoView(item);
        };
        DataContextChanged += (_, _) => BuildColumns();
    }

    /// <summary>Builds the master-list columns to match the schema: origin badge (always, left), icon
    /// (items only), the key column (headed by the key's label), and a separate Name column only when the
    /// display field differs from the key (so string-keyed DBs show one column, not a duplicate).</summary>
    private void BuildColumns()
    {
        if (DataContext is not DbWorkspaceViewModel vm) return;

        MasterGrid.Columns.Clear();
        _keyColumn = _nameColumn = null;

        MasterGrid.Columns.Add(new GridViewColumn
        {
            Header = string.Empty,
            Width = 70,
            CellTemplate = (DataTemplate)Resources["OriginBadgeCellTemplate"],
        });

        if (vm.ShowIconColumn)
            MasterGrid.Columns.Add(new GridViewColumn
            {
                Header = string.Empty,
                Width = 30,
                CellTemplate = (DataTemplate)Resources["IconCellTemplate"],
            });

        _keyColumn = new GridViewColumn
        {
            Header = vm.KeyColumnHeader,
            Width = vm.ShowNameColumn ? 70 : 236,
            DisplayMemberBinding = new Binding("KeyText"),
        };
        MasterGrid.Columns.Add(_keyColumn);

        if (vm.ShowNameColumn)
        {
            _nameColumn = new GridViewColumn
            {
                Header = vm.NameColumnHeader,
                Width = 166,
                DisplayMemberBinding = new Binding("Name"),
            };
            MasterGrid.Columns.Add(_nameColumn);
        }
    }

    private void OnHeaderClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader header) return;
        if (DataContext is not DbWorkspaceViewModel vm || vm.List is not { } list) return;

        string? key = header.Column == _keyColumn ? "Id" : header.Column == _nameColumn ? "Name" : null;
        if (key is null) return;

        list.ToggleSort(key);
        if (_keyColumn is not null) _keyColumn.Header = vm.KeyColumnHeader + list.SortGlyph("Id");
        if (_nameColumn is not null) _nameColumn.Header = vm.NameColumnHeader + list.SortGlyph("Name");
    }
}
