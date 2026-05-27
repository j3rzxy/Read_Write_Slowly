using System.ComponentModel;

namespace Read_Write_Slowly.Models_
{
    /// <summary>
    /// Обёртка над Genre с флагом IsSelected для мультиселекта жанров в форме.
    /// </summary>
    public class SelectableGenre : INotifyPropertyChanged
    {
        public int GenreId { get; set; }
        public string Name { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString() => Name;
    }
}
