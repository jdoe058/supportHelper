using supportHelper;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Xml;
using System.Xml.Linq;
using static supportHelper.PlanFixController;

namespace WpfAppTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    public class MainWindowVewModel : INotifyPropertyChanged
    {
        private readonly string AnyDeskPath = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\AnyDesk\AnyDesk.exe");
        private readonly string IikoRMSPath = Environment.ExpandEnvironmentVariables(@"%ProgramW6432%\iiko\iikoRMS");
        private readonly string IikoChainPath = Environment.ExpandEnvironmentVariables(@"%ProgramW6432%\iiko\iikoChain");

        private static readonly HttpClient http = new() { BaseAddress = new Uri("https://zheka003.planfix.ru/rest/") };
        
        static public ObservableCollection<ConnectionModel> ConnectionsList { get; set; } = new();
        private static readonly ICollectionView _collection = CollectionViewSource.GetDefaultView(ConnectionsList);

        #region string : ConnectionFilter = string.Empty
        public string ConnectionFilter
        {
            get => _ConnectionFilter;
            set
            {
                if (Set(ref _ConnectionFilter, value))
                {
                    _collection.Refresh();
                }
            }
        }
        private string _ConnectionFilter = string.Empty;
        #endregion
        #region ConnectionModel? : SelectedConnection
        public ConnectionModel? SelectedConnection
        {
            get => _SelectedConnection;
            set => Set(ref _SelectedConnection, value);
        }
        private ConnectionModel? _SelectedConnection;
        #endregion

        #region RelayCommand : LaunchOfficeCommand
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
                    string launchExec = System.IO.Path.Combine(isChain ? IikoChainPath : IikoRMSPath,
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
                },
                    obj => obj is ConnectionModel s && !string.IsNullOrWhiteSpace(s.Address));
            }
        }
        private RelayCommand? launchOfficeCommand;
        #endregion
        #region RelayCommand : LaunchAnyDeskCommand
        public RelayCommand LaunchAnyDeskCommand
        {
            get
            {
                return launchAnyDeskCommand ??= new RelayCommand(obj =>
                {
                    if (obj is ConnectionModel s)
                    {
                        using Process proc = new();
                        proc.StartInfo.FileName = AnyDeskPath;
                        proc.StartInfo.Arguments = $"{s.Login} --with-password";
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.RedirectStandardInput = true;
                        _ = proc.Start();
                        proc.StandardInput.WriteLine(s.Password);
                    }
                },
                    obj => obj is ConnectionModel s && string.IsNullOrWhiteSpace(s.Address));
            }
        }
        private RelayCommand? launchAnyDeskCommand;
        #endregion
        #region RelayCommand : CallbackCommand
        public RelayCommand CallbackCommand
        {
            get
            {
                return callbackCommand ??= new RelayCommand(obj =>
                {
                    throw new NotImplementedException();
                },
                        obj => false);
            }
        }
        private RelayCommand? callbackCommand;
        #endregion
        #region RelayCommand : CloseApplicationCommand
        public RelayCommand CloseApplicationCommand
        {
            get
            {
                return closeApplicationCommand ??= new RelayCommand(obj =>
                {
                },
                    obj => true);
            }
        }
        private RelayCommand? closeApplicationCommand;

        #endregion

        public MainWindowVewModel()
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "07ba9b03da7930e308d8f67e3e646fd1");
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
            await foreach (DirectoryEntry? i in GetEntryList(http, 608))
                if (i?.CustomFieldData is not null) ConnectionsList.Add(new ConnectionModel(i.CustomFieldData));

        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? PropertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string? PropertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(PropertyName);
            return true;
        }
        #endregion
    }
}
