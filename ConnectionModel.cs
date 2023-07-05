using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static supportHelper.PlanFixController;

namespace supportHelper;

public class ConnectionModel : BaseModel, IEditableObject
{
    public static readonly string csvHeader = "Имя;Клиент;Пароль;Путь"; 

    public string CsvLine => $"{_Name};{_Client};{_Password};{_Login}" + (string.IsNullOrWhiteSpace(_Address)?"":$",{_Address}");

    public string Client  { get => _Client;  set => Set(ref _Client, value);  } private string _Client  = string.Empty;

    public string Name    { get => _Name;    set => Set(ref _Name, value);    } private string _Name    = string.Empty;

    public string? Address { get => _Address; set => Set(ref _Address, value); } private string? _Address;

    public string? Login   { get => _Login;   set => Set(ref _Login, value);   } private string? _Login;


    public string? Password
    {
        get => Encoding.UTF8.GetString(Convert.FromBase64String(string.IsNullOrWhiteSpace(_Password)
            ? string.IsNullOrWhiteSpace(Address)
                ? Properties.Settings.Default.AnyDeskPassword
                : Properties.Settings.Default.IikoPassword
            : _Password));
        set => Set(ref _Password, value is null ? null : Convert.ToBase64String(Encoding.UTF8.GetBytes(value)));
    }
    public string? _Password;

    public readonly int key;
    public readonly int parentKey;
    public readonly int nameId;
    public readonly int clientId;
    public readonly int addressId;
    public readonly int loginId;
    public readonly int passwordId;

    public ConnectionModel(DirectoryEntry directoryEntry)
    {
        key = directoryEntry.Key; 
        parentKey = directoryEntry.ParentKey;

        if (directoryEntry?.CustomFieldData is null) return; 
        
        foreach (var item in directoryEntry.CustomFieldData)
        {
            if (item.Field is null) continue;

            switch (item.Field.Name)
            {
                case "Имя": nameId = item.Field.Id; Name = item.Value; break;
                case "Клиент": clientId = item.Field.Id; Client = item.Value; break;
                //case "Адрес": addressId = item.Field.Id; Address = item.Value; break;
                case "Пароль": passwordId = item.Field.Id; _Password = item.Value; break;
                case "Путь":
                    loginId = item.Field.Id;

                    if (item.Value is not null)
                    {
                        var v = item.Value.Split(',');
                        Login = v[0];
                        if (v.Length > 1)
                            Address = v[1];
                    }
                    break;
            }
        }
    }

    public DirectoryEntry ToDirectoryEntry()
    {
        return new DirectoryEntry
        {
            Key = key,
            ParentKey = parentKey,
            CustomFieldData = new List<CustomFieldDatum> 
            { 
                new CustomFieldDatum { Field = new Field { Id = nameId, Name="Имя"}, Value = Name}, 
                new CustomFieldDatum { Field = new Field { Id = clientId, Name="Клиент"}, Value = Client }, 
                //new CustomFieldDatum { Field = new Field { Id = addressId, Name="Адрес"}, Value = Address }, 
                new CustomFieldDatum { Field = new Field { Id = loginId, Name="Путь"}, Value = Login + (string.IsNullOrWhiteSpace(Address)?"":$",{Address}")}, 
                new CustomFieldDatum { Field = new Field { Id = passwordId, Name="Пароль"}, Value = _Password }, 
            }
        };
    }

    #region IEditableObject implementation
    public bool inEdit;
    ConnectionModel? backupCopy;
    

    public void BeginEdit()
    {
        if (!inEdit)
        { 
            inEdit = true;
            backupCopy = (ConnectionModel)MemberwiseClone();
        }
    }

    public void CancelEdit()
    {
        if (inEdit) 
        { 
            inEdit = false;
            if (backupCopy is not null)
            {
                _Name = backupCopy._Name;
                _Login = backupCopy._Login;  
                _Client = backupCopy._Client;
                _Address = backupCopy._Address;
                _Password = backupCopy._Password; 
            }
        }
    }

    public void EndEdit()
    {
        if (inEdit) 
        { 
            inEdit = false;
            if (backupCopy is not null && (
                backupCopy?._Name != _Name 
                || backupCopy?._Login != _Login 
                || backupCopy?._Client != _Client 
                || backupCopy?._Address != _Address 
                || backupCopy?._Password != _Password))
                UpdateDirectoryEntry(ToDirectoryEntry(),false);
            backupCopy = null;
        }
    }
    #endregion

}
