using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Read_Write_Slowly.ViewModels_
{
    // Класс обязательно должен быть public и abstract (чтобы его нельзя было создать напрямую, а только наследоваться)
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        // Событие, на которое подписывается WPF-интерфейс для отслеживания изменений
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод, который уведомляет интерфейс об изменении свойства
        // Атрибут [CallerMemberName] автоматически подставляет имя свойства, которое вызвало метод
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
