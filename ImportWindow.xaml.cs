using System;
using System.Collections.Generic;
using System.Linq;
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

namespace FieldEditor
{
    /// <summary>
    /// Interaction logic for ImportWindow.xaml
    /// </summary>
    public partial class ImportWindow : Window
    {
        public ImportWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Import control point data (meterset, gantry angle)
            // Check data, three columns are needed.
            int numberOfLines = this.ImportTextBox.LineCount;
            List<double> gantryAngle = new List<double>() { };
            List<double> meterset = new List<double>() { };

            var datagrid = ((MainWindow)Application.Current.MainWindow).DataGridBeamList;
            int beamIndex = ((MainWindow)Application.Current.MainWindow).BeamComboBox.SelectedIndex;

            for (int line = 0; line < numberOfLines; line++)
            {
                string[] array = this.ImportTextBox.GetLineText(line).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (array.Length != 3)
                {
                    MessageBox.Show("Line " + line.ToString() + " does not contain three columns!", "Error");
                    return;
                }
                gantryAngle.Add(ConvertTextToDouble(array[1]));
                meterset.Add(ConvertTextToDouble(array[2]));
            }

            for (int i = 0; i < datagrid.ElementAt(beamIndex).Datatable.Count(); i++)
            {
                datagrid.ElementAt(beamIndex).Datatable[i].MetersetWeight = meterset[i];
            }

            ((MainWindow)Application.Current.MainWindow).RefreshDataGrid(beamIndex);
        }


        private double ConvertTextToDouble(string text)
        {
            if (Double.TryParse(text, out double result))
            {
                return result;
            }
            else
            {
                return Double.NaN;
            }
        }
    }
}
