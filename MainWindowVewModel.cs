using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Xml;
using System.Xml.Linq;


namespace supportHelper;

public class MainWindowViewModel : BaseModel
{
    #region Command
    public RelayCommand LaunchOfficeCommand
    {
        get
        {
            return launchOfficeCommand ??= new RelayCommand(obj =>
            {
                if (obj is not ConnectionModel server)
                {
                    return;
                }

                if (server.Address is null)
                    return;

                string[] addressPort = server.Address.Split(':');
                //string address = addressPort[0];

                string port = "443";
                string protocol = "https";


                if (addressPort.Length > 1)
                {
                    port = addressPort[1];
                    protocol = "http";
                }

                XmlReader reader;

                try
                {
                    reader = XmlReader.Create($"{protocol}://{server.Address}/resto/get_server_info.jsp?encoding=UTF-8");
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message);
                    return;
                }

                XElement? xml = XDocument.Load(reader)?.Element("r");

                string? fullVersion = xml?.Element("version")?.Value;

                bool isChain = Equals(xml?.Element("edition")?.Value, "chain");
                string launchExec = System.IO.Path.Combine(isChain 
                    ? Properties.Settings.Default.IikoChainPath 
                    : Properties.Settings.Default.IikoRMSPath,
                    fullVersion is null ? "" : fullVersion[..^2], @"BackOffice.exe");

                string type = isChain ? "Chain" : "RMS";

                if (!System.IO.File.Exists(launchExec))
                {
                    if (MessageBox.Show($"Скачать установщик офиса версии {fullVersion}",
                        "Отсутствует офис",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                        ) == MessageBoxResult.Yes)
                    {
                        _ = Process.Start(@".\curl", "-u partners:partners#iiko"
                            + Environment.ExpandEnvironmentVariables($" -o %USERPROFILE%\\Downloads\\{fullVersion}.Setup.{type}.BackOffice.exe")
                            + $"ftp://ftp.iiko.ru/release_iiko/{fullVersion}/Setup/Offline/Setup.{type}.BackOffice.exe");
                    }
                    return;
                }

                DirectoryInfo di = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(
                    Environment.ExpandEnvironmentVariables(@"%AppData%\iiko"), type, addressPort[0], "config"));
                new XDocument(
                    new XElement("config",
                        new XElement("ServersList",
                            new XElement("Protocol", protocol),
                            new XElement("ServerAddr", addressPort[0]),
                            new XElement("ServerSubUrl", "/resto"),
                            new XElement("Port", port),
                            new XElement("IsPresent", false)
                        ),
                        new XElement("Login", server.Login)
                    )
                ).Save(System.IO.Path.Combine(di.FullName, @"backclient.config.xml"));
                _ = Process.Start(launchExec, $"/password={server.Password} /AdditionalTmpFolder={addressPort[0]}");
            }, obj => obj is ConnectionModel s && !string.IsNullOrWhiteSpace(s.Address));
        }
    }
    private RelayCommand? launchOfficeCommand;

    public RelayCommand LaunchAnyDeskCommand
    {
        get
        {
            return launchAnyDeskCommand ??= new RelayCommand(obj =>
            {
                if (obj is ConnectionModel s)
                {
                    using Process proc = new();
                    proc.StartInfo.FileName = Properties.Settings.Default.AnyDeskPath;
                    proc.StartInfo.Arguments = $"{s.Login} --with-password";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardInput = true;
                    _ = proc.Start();
                    proc.StandardInput.WriteLine(s.Password);
                }
            }, obj => obj is ConnectionModel s && string.IsNullOrWhiteSpace(s.Address));
        }
    }
    private RelayCommand? launchAnyDeskCommand;

    public RelayCommand CallbackCommand
    {
        get
        {
            return callbackCommand ??= new RelayCommand(obj =>
            {
                throw new NotImplementedException();
            }, obj => false);
        }
    }
    private RelayCommand? callbackCommand;

    public RelayCommand AddConnectionModelCommand
    {
        get
        {
            return addConnectionModelCommand ??= new RelayCommand(obj =>
            {
                if (obj is ConnectionModel model)
                {
                    PlanFixController.UpdateDirectoryEntry(model.ToDirectoryEntry(), true);
                }
            },
            obj => obj is ConnectionModel model);
        }
    }
    RelayCommand? addConnectionModelCommand;

    public RelayCommand RemoveConnectionModel
    {
        get
        {
            return removeConnectionModel ??= new RelayCommand(obj => 
            { 
                if (obj is ConnectionModel model) 
                    PlanFixController.DeleteDirectoryEntry(model); 
            }, obj => obj is ConnectionModel model);
        }
    }
    RelayCommand? removeConnectionModel;

    public RelayCommand SaveSettingsCommand { get => saveSettingsCommand ??= new RelayCommand(obj => { Properties.Settings.Default.Save(); }); }
    private RelayCommand? saveSettingsCommand;

    public RelayCommand ReloadSettingsCommand { get => reloadSettingsCommand ??= new RelayCommand(obj => { Properties.Settings.Default.Reload(); }); }
    private RelayCommand? reloadSettingsCommand;
    #endregion

    static public ObservableCollection<ConnectionModel> ConnectionsList { get; set; } = new();
    private static readonly ICollectionView _collection = CollectionViewSource.GetDefaultView(ConnectionsList);

    public static string IikoPassword
    {
        get => Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.IikoPassword));
        set => Properties.Settings.Default.IikoPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    public static string AnyDeskPassword
    {
        get => Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.AnyDeskPassword));
        set => Properties.Settings.Default.AnyDeskPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

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
        await foreach (PlanFixController.DirectoryEntry? i in PlanFixController.GetEntryList())
            if (i is not null && i.CustomFieldData is not null) 
                ConnectionsList.Add(new ConnectionModel(i));
    }
}
