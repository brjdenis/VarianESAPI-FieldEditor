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
                datagrid.ElementAt(beamIndex).Datatable[i].Gantry = gantryAngle[i];
            }

            ((MainWindow)Application.Current.MainWindow).RefreshDataGrid(beamIndex);
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // IMport MLCs.
            // The table must be ordered like this.
            // MLC1_0, MLC1_1, MLC1_2 .... MLC2_0, MLC2_1, ...
            // MLC1_0, MLC1_1, MLC1_2 .... MLC2_0, MLC2_1, ...
            // MLC1_0, MLC1_1, MLC1_2 .... MLC2_0, MLC2_1, ...
            // MLC1_0, MLC1_1, MLC1_2 .... MLC2_0, MLC2_1, ...
            var datagrid = ((MainWindow)Application.Current.MainWindow).DataGridBeamList;
            int beamIndex = ((MainWindow)Application.Current.MainWindow).BeamComboBox.SelectedIndex;
            int numberOfLines = this.ImportTextBox.LineCount;
            int numLeaves = datagrid[beamIndex].Datatable[0].MLCPositions.Count;

            for (int line = 0; line < numberOfLines; line++)
            {
                string[] array = this.ImportTextBox.GetLineText(line).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

                if (array.Length != 2 * numLeaves)
                {
                    MessageBox.Show("Line " + line.ToString() + " does not contain " + (numLeaves + 1).ToString() + " columns!", "Error");
                    return;
                }

                for (int j = 0; j < numLeaves; j++)
                {
                    datagrid.ElementAt(beamIndex).Datatable[line].MLCPositions[j].MLC1 = (float)ConvertTextToDouble(array[j]);
                    datagrid.ElementAt(beamIndex).Datatable[line].MLCPositions[j].MLC2 = (float)ConvertTextToDouble(array[numLeaves + j]);
                }
            }
            ((MainWindow)Application.Current.MainWindow).RefreshDataGrid(beamIndex);
            ((MainWindow)Application.Current.MainWindow).PlotMLC();
        }
    }
}
