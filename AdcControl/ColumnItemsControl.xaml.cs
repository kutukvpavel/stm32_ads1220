using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Threading;

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

        private bool _DelayRendering = false;
        public bool DelayRendering
        {
            get => _DelayRendering;
            set
            {
                var t = value;
                if (t != _DelayRendering)
                {
                    if (!t)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            InnerItemsControl.Visibility = System.Windows.Visibility.Collapsed;
                            lock (LockObject)
                            {
                                if (DelayedItems.Count == ItemsLimit)
                                {
                                    InnerItemsControl.Items.Clear();
                                }
                                else
                                {
                                    while (InnerItemsControl.Items.Count + DelayedItems.Count > ItemsLimit)
                                    {
                                        InnerItemsControl.Items.RemoveAt(0);
                                    }
                                }
                                foreach (var item in DelayedItems)
                                {
                                    InnerItemsControl.Items.Add(item);
                                }
                                DelayedItems.Clear();
                                _DelayRendering = t;
                            }
                            InnerItemsControl.Visibility = System.Windows.Visibility.Visible;
                        });
                    }
                    else
                    {
                        _DelayRendering = t;
                    }
                }
            }
        }

        public void AddItem(string item)
        {
            if (CheckIfPointShouldBeDropped()) return; //Keep this calculated as eqarly as possible
            AddPointEngine(item);
        }

        public void AddItem(double item)
        {
            if (CheckIfPointShouldBeDropped()) return; //Keep this calculated as eqarly as possible
            AddPointEngine(item.ToString(ItemStringFormat));
        }

        public void Clear()
        {
            Dispatcher.Invoke(() =>
            {
                InnerItemsControl.Items.Clear();
            });
        }

        //Private

        private int AdditionAttempts = 0;
        private object LockObject = new object();
        private List<string> DelayedItems = new List<string>();

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool CheckIfPointShouldBeDropped()
        {
            return (DropItems > 0) && ((++AdditionAttempts % DropItems) != 0);
        }

        private void AddPointEngine(string item)
        {
            lock (LockObject)
            {
                if (DelayRendering)
                {
                    DelayedItems.Add(item);
                    if (DelayedItems.Count > ItemsLimit)
                        DelayedItems.RemoveAt(0);
                }
                else
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        InnerItemsControl.Items.Add(item);
                        if (InnerItemsControl.Items.Count > ItemsLimit)
                            InnerItemsControl.Items.RemoveAt(0);
                    });
                }
            }
        }
    }
}
