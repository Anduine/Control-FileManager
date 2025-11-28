using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace ControlFileManager.UI.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void Raise([CallerMemberName] string? propName = null)
        { 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
