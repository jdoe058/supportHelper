using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using static supportHelper.PlanFixController;

namespace supportHelper
{
    public class ConnectionModel : INotifyPropertyChanged
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
            ? string.IsNullOrWhiteSpace(Address)
                ? "VGIjMTQ3ODUy"
                : "cmVzdG8jdGI="
            : Crypted_Password));
            set => Crypted_Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private string _Client = string.Empty;
        private string? Crypted_Password;

        public ConnectionModel(List<CustomFieldDatum> customFieldData)
        {
            foreach (var item in customFieldData)
            {
                switch (item?.Field?.Name)
                {
                    case "Клиент": Client = item.StringValue; break;
                    case "Имя": Name = item.StringValue; break;
                    case "Адрес": Address = item.StringValue; break;
                    case "Логин": Login = item.StringValue; break;
                    case "Пароль": Crypted_Password = item.StringValue; break;
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
}
