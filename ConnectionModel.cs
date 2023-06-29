using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static supportHelper.PlanFixController;

namespace supportHelper;

public class ConnectionModel : BaseModel, IEditableObject
{
    public string Client  { get => _Client;  set => Set(ref _Client, value);  } private string _Client  = string.Empty;

    public string Name    { get => _Name;    set => Set(ref _Name, value);    } private string _Name    = string.Empty;

    public string? Address { get => _Address; set => Set(ref _Address, value); } private string? _Address;

    public string? Login   { get => _Login;   set => Set(ref _Login, value);   } private string? _Login;


    public string? Password
    {
        get => Encoding.UTF8.GetString(Convert.FromBase64String(string.IsNullOrWhiteSpace(_Password)
            ? string.IsNullOrWhiteSpace(Address)
                ? "VGIjMTQ3ODUy"
                : "cmVzdG8jdGI="
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
            
            switch (item?.Field?.Name)
            {
                case "Клиент": Client    = item.Value is null ? "" : item.Value; clientId   = item.Field.Id; break;
                case "Имя":    Name      = item.Value is null ? "" : item.Value; nameId     = item.Field.Id; break;
                case "Адрес":  Address   = item.Value; addressId  = item.Field.Id; break;
                case "Логин":  Login     = item.Value; loginId    = item.Field.Id; break;
                case "Пароль": _Password = item.Value; passwordId = item.Field.Id; break;
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
                new CustomFieldDatum { Field = new Field { Id = addressId, Name="Адрес"}, Value = Address }, 
                new CustomFieldDatum { Field = new Field { Id = loginId, Name="Логин"}, Value = Login }, 
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
            backupCopy = null;
            UpdateDirectoryEntry(608,ToDirectoryEntry());
        }
    }
    #endregion

}
