using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace csNavplan
{
    /// <summary>
    /// Interaction logic for Colors.xaml
    /// </summary>
    public partial class ColorsDlg : Window
    {
        public ColorsDlg()
        {
            InitializeComponent();

            listBox1.ItemsSource = typeof(Brushes)
                .GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Select(x => new
                {
                    Name = x.Name,
                    Brush = x.GetValue(null, null)
                });
        }
    }
}
