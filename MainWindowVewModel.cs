using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using static supportHelper.PlanFixController;

namespace supportHelper;

public class PasswordConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 1 && values[1] is PasswordBox pw && values[0] is ConnectionModel model) 
        {
            return new Tuple<ConnectionModel, PasswordBox>(model, pw);
        }
        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)    { throw new NotImplementedException(); }
}

public class MainWindowViewModel : BaseModel
{
    static public ObservableCollection<ConnectionModel> ConnectionsList { get; set; } = new();
    private static readonly ICollectionView _collection = CollectionViewSource.GetDefaultView(ConnectionsList);

    public string ConnectionFilter
    {
        get => _ConnectionFilter;
        set { if (Set(ref _ConnectionFilter, value))  _collection.Refresh(); }
    }
    private string _ConnectionFilter = string.Empty;

    public ConnectionModel? SelectedConnection
    {
        get => _SelectedConnection;
        set { Set(ref _SelectedConnection, value); } 
    }
    private ConnectionModel? _SelectedConnection;

    public MainWindowViewModel()
    {
        LoadConnectionsFromPlanfix();

        _collection.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        _collection.SortDescriptions.Add(new SortDescription("Client", ListSortDirection.Ascending));
        _collection.GroupDescriptions.Add(new PropertyGroupDescription("Client"));
        _collection.Filter += obj => obj is not ConnectionModel c
            || c.Name.Contains(ConnectionFilter, StringComparison.CurrentCultureIgnoreCase)
            || c.Client.Contains(ConnectionFilter, StringComparison.CurrentCultureIgnoreCase); ;
    }
    
    static private async void LoadConnectionsFromPlanfix()
    {
        //TO-DO что не так, не пойму что
        await foreach (DirectoryEntry? i in GetEntryList())
            if (i is not null && i.CustomFieldData is not null) 
                ConnectionsList.Add(new ConnectionModel(i));
    }
}
