using System.ComponentModel;
using System.Windows.Controls;

namespace AdcControl
{
    /// <summary>
    /// Логика взаимодействия для ColumnItemsControl.xaml
    /// </summary>
    public partial class ColumnItemsControl : UserControl, INotifyPropertyChanged
    {
        public ColumnItemsControl()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ItemsControl InnerItemsControl 
        {   get
            {
                return ctlItems;
            } 
        }

        private string _Header = "Header";
        public string Header
        {
            get => _Header;
            set
            {
                _Header = value;
                OnPropertyChanged("Header");
            }
        }

        public string ItemStringFormat { get; set; } = "";

        public int ItemsLimit { get; set; } = int.MaxValue;

        public int DropItems { get; set; } = 1;

        public void AddItem(double item)
        {
            if ((DropItems > 0) && ((++AdditionAttempts % DropItems) != 0)) return;
            InnerItemsControl.Items.Add(item.ToString(ItemStringFormat));
            if (InnerItemsControl.Items.Count > ItemsLimit)
                InnerItemsControl.Items.RemoveAt(0);
        }

        //Private

        private int AdditionAttempts = 0;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
