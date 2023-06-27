using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

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
        private static readonly HttpClient http = new() { BaseAddress = new Uri("https://zheka003.planfix.ru/rest/") };

        public Connection? SelectedConnection
        {
            get => _SelectedConnection;
            set => Set(ref _SelectedConnection, value);
        }

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

        static public ObservableCollection<Connection> ConnectionsList { get; set; } = new();
        static private ICollectionView _collection = CollectionViewSource.GetDefaultView(ConnectionsList);
        private Connection? _SelectedConnection;
        private string _ConnectionFilter = string.Empty;


        private RelayCommand? launchOfficeCommand;

        private readonly string IikoRMSPath = Environment.ExpandEnvironmentVariables("%ProgramW6432%\\iiko\\iikoRMS");
        private readonly string IikoChainPath = Environment.ExpandEnvironmentVariables("%ProgramW6432%\\iiko\\iikoChain");
        private readonly string AnyDeskPath = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%\\AnyDesk\\AnyDesk.exe");

        public RelayCommand LaunchOfficeCommand
        {
            get
            {
                return launchOfficeCommand ??
                    (launchOfficeCommand = new RelayCommand(obj =>
                    {
                        if (obj is not Connection server)
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
                    obj => obj is Connection s && !string.IsNullOrWhiteSpace(s.Address)));
            }
        }

        public RelayCommand CallbackCommand
        {
            get
            {
                return callbackCommand ??
                    (callbackCommand = new RelayCommand(obj =>
                        {
                        },
                        obj => false));
            }
        }

        public RelayCommand CloseApplicationCommand
        {
            get 
            {
                return exitApplicationCommand ??
                    (exitApplicationCommand = new RelayCommand(obj =>
                    {
                    },
                    obj => true));
            }
        }

        private RelayCommand? launchAnyDeskCommand;
        private RelayCommand? callbackCommand;
        private RelayCommand? exitApplicationCommand;

        public RelayCommand LaunchAnyDeskCommand
        {
            get
            {
                return launchAnyDeskCommand ??
                    (launchAnyDeskCommand = new RelayCommand(obj => 
                    {
                        if (obj is Connection s) 
                        {
                            using (Process proc = new Process())
                            {
                                proc.StartInfo.FileName = AnyDeskPath;
                                proc.StartInfo.Arguments = $"{s.Login} --with-password";
                                proc.StartInfo.UseShellExecute = false;
                                proc.StartInfo.RedirectStandardInput = true;
                                _ = proc.Start();
                                proc.StandardInput.WriteLine(s.Password);
                            }
                        }
                    },
                    obj => obj is Connection s && string.IsNullOrWhiteSpace(s.Address)));
            }
        }

        public MainWindowVewModel()
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "07ba9b03da7930e308d8f67e3e646fd1");
            LoadConnections();

            _collection.Filter += OnConnectionFilter;
            _collection.GroupDescriptions.Add(new PropertyGroupDescription("Client"));
            _collection.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            _collection.SortDescriptions.Add(new SortDescription("Client", ListSortDirection.Ascending));

        }

        private bool OnConnectionFilter(object obj)
        {

            if (obj is Connection c)
                return c.Name.Contains(ConnectionFilter, StringComparison.CurrentCultureIgnoreCase)
                       || c.Client.Contains(ConnectionFilter, StringComparison.CurrentCultureIgnoreCase);
            return true;

        }


        static private async void LoadConnections()
        {
            await foreach (PlanFix.DirectoryEntry? i in PlanFix.GetEntryList(http, 608))
            {
                if (i?.customFieldData is not null)
                    ConnectionsList.Add(new Connection(i.customFieldData));
            }
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

    public static class PlanFix
    {
        public class Field
        {
            public int id { get; set; } = 0;
            public string name { get; set; } = string.Empty;
            public int type { get; set; } = 0;
            public int objectType { get; set; } = 0;
        }

        public class CustomFieldDatum
        {
            public Field? field { get; set; }
            public string value { get; set; } = string.Empty;
            public string stringValue { get; set; } = string.Empty;
        }

        public class Directory
        {
            public int id { get; set; }
            public string name { get; set; } = string.Empty;
            public List<Field>? fields { get; set; }
        }

        public class DirectoryEntry
        {
            public int key { get; set; }
            public int parentKey { get; set; }
            public string name { get; set; } = string.Empty;
            public List<CustomFieldDatum>? customFieldData { get; set; }
        }

        class DirectoryEntryListResponse
        {
            public string result { get; set; } = string.Empty;
            public List<DirectoryEntry>? directoryEntries { get; set; }
        }

        class DirectoryByIdResponse
        {
            public string result { get; set; } = string.Empty;
            public PlanFix.Directory? directory { get; set; }
        }

        static async IAsyncEnumerable<string?> GetFieldsId(HttpClient http, int id)
        {
            using var resp = await http.GetAsync($"directory/{id}?fields=fields");
            DirectoryByIdResponse? r = await resp.Content.ReadFromJsonAsync<DirectoryByIdResponse>();
            if (r?.directory?.fields is not null)
            {
                foreach (var f in r.directory.fields)
                    yield return f.id.ToString();
            }
            yield return null;
        }

        public static async IAsyncEnumerable<DirectoryEntry?> GetEntryList(HttpClient http, int id)
        {
            string fieds = "name, key, parentKey"; await foreach (var n in GetFieldsId(http, 608)) { fieds += ", " + n; }

            using var response = await http.PostAsJsonAsync($"directory/{id}/entry/list",
                new { offset = 0, pageSize = 100, fields = fieds });

            var data = await response.Content.ReadFromJsonAsync<DirectoryEntryListResponse>();

            if (data?.directoryEntries != null)
                foreach (var i in data.directoryEntries)
                    yield return i;

            yield return null;
        }
    }

    public class Connection : INotifyPropertyChanged
    {
        public string Client
        {
            get => _Client;
            set => Set(ref _Client, value);
        }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;

        public string Password 
        {
            get => Encoding.UTF8.GetString(Convert.FromBase64String(string.IsNullOrWhiteSpace(Crypted_Password)
            ? (string.IsNullOrWhiteSpace(Address)
                ? "VGIjMTQ3ODUy"
                : "cmVzdG8jdGI=")
            : Crypted_Password));
            set => Crypted_Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private string _Client = string.Empty;
        private string? Crypted_Password;

        public Connection(List<PlanFix.CustomFieldDatum> customFieldData)
        {
            foreach (var item in customFieldData)
            {
                switch (item?.field?.name)
                {
                    case "Клиент": Client = item.stringValue; break;
                    case "Имя": Name = item.stringValue; break;
                    case "Адрес": Address = item.stringValue; break;
                    case "Логин": Login = item.stringValue; break;
                    case "Пароль": Crypted_Password = item.stringValue; break;
                }
            }
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

    public class RelayCommand : ICommand
    {
        readonly Action<object?> execute;
        readonly Func<object?, bool>? canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }
        public bool CanExecute(object? parameter)
        {
            return canExecute == null || canExecute(parameter);
        }
        public void Execute(object? parameter)
        {
            execute(parameter);
        }
    }

}
