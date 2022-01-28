using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace FieldEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ScriptContext scriptcontext;

        public List<DataGridBeam> DataGridBeamList = new List<DataGridBeam>() { };
        public ListCollectionView DataGridBeamCollection { get; set; }

        public PlotModel PlotModelAperture { get; set; }
        public PlotModel PlotModelMeterset { get; set; }
        public PlotModel PlotModelMLCPosition { get; set; }

        public double[] MLCedges;
        public string MLCtype;
        public double LeafLength;
        public List<int> NumControlPoints; // per beam (indexed in the same order as ComboBoxBeam)

        // Y coordinates of leaf edges for various collimators (Agility included)

        public readonly double[] MLC80 = new double[] { -200, -190, -180, -170, -160, -150, -140, -130, -120, -110, -100,
                                                        -90, -80, -70, -60, -50, -40, -30, -20, -10, 0, 10, 20, 30, 40, 50,
                                                        60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200 };

        public readonly double[] MLC120 = new double[] {-200, -190, -180, -170, -160, -150, -140, -130, -120, -110, -100,
                                                        -95,  -90,  -85,  -80,  -75,  -70,  -65,  -60,  -55,  -50,  -45,
                                                        -40,  -35,  -30,  -25,  -20,  -15,  -10,   -5,    0,    5,   10,
                                                         15,   20,   25,   30,   35,   40,   45,   50,   55,   60,   65,
                                                         70,   75,   80,   85,   90,   95,  100,  110,  120,  130,  140,
                                                        150,  160,  170,  180,  190,  200};

        public readonly double[] MLC120HD = new double[] {-110, -105, -100,  -95,  -90,  -85,  -80,  -75,
                                                          -70,  -65,  -60,  -55,  -50,  -45,  -40,  -37.5,
                                                          -35,  -32.5,  -30,  -27.5,  -25,  -22.5,  -20,  -17.5,
                                                          -15,  -12.5,  -10,   -7.5,   -5,   -2.5,    0,    2.5,
                                                            5,    7.5,   10,   12.5,   15,   17.5,   20,   22.5,
                                                            25,   27.5,   30,   32.5,   35,   37.5,   40,   45,
                                                            50,   55,   60,   65,   70,   75,   80,   85,
                                                            90,   95,  100,  105,  110 };
        // Halcyon collimators are stacked together!
        public readonly double[] MLCHalcyon = new double[] { -140, -130, -120, -110, -100, -90, -80, -70, -60, -50, -40, -30, -20,
                                                              -10, 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140,
                                                              -145, -135, -125, -115, -105, -95, -85, -75, -65, -55, -45, -35, -25,
                                                              -15, -5, 5, 15, 25, 35, 45, 55, 65, 75, 85, 95, 105, 115, 125, 135, 145};
        // Agility collimator (Elekta):
        public readonly double[] MLCAgility = new double[] { -200, -195, -190, -185, -180, -175, -170, -165, -160, -155, -150, -145, -140,
                                                             -135, -130, -125, -120, -115, -110, -105, -100, -95, -90, -85, -80, -75, -70,
                                                             -65, -60, -55, -50, -45, -40, -35, -30, -25, -20, -15, -10, -5, 0, 5, 10, 15,
                                                             20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110,
                                                             115, 120, 125, 130, 135, 140, 145, 150, 155, 160, 165, 170, 175, 180, 185, 190, 195, 200 };

        public List<RectangleAnnotation> MLC1Annotations = new List<RectangleAnnotation>() { };
        public List<RectangleAnnotation> MLC2Annotations = new List<RectangleAnnotation>() { };

        public List<int> SelectedControlPoints = new List<int>() { };
        public List<int> SelectedMLCs = new List<int>() { };

        public bool FinishedSelectingMLCs = true;
        public bool FinishedSelectingControlPoints = true;

        public MainWindow(ScriptContext scriptcontext)
        {
            this.scriptcontext = scriptcontext;

            InitializeComponent();

            CollectControlPointsData();
            DetectMLC();

            FillBeamComboBox();

            FillSelectionComboBoxes();

            FillMetersetPlotTypeComboBox();

            // MLC plot
            this.PlotModelAperture = CreatePlotModelAperture();
            this.PlotAperture.Model = this.PlotModelAperture;
            SetController(this.PlotAperture);

            // meterset plot
            this.PlotModelMeterset = CreatePlotModelMeterset();
            this.PlotMeterset.Model = this.PlotModelMeterset;
            SetController(this.PlotMeterset);

            // MLC position plot
            this.PlotModelMLCPosition = CreatePlotModelMLCPosition();
            this.PlotMLCPosition.Model = this.PlotModelMLCPosition;
            SetController(this.PlotMLCPosition);

            this.SizeChanged += LayoutUpdate;

            this.BeamComboBox.SelectedIndex = 0;
            AddMLCToPlot();
            this.DataGridControlPoints.SelectedIndex = 0;
        }

        private Beam FindFirstDynamicField()
        {
            Beam foundBeam = null;
            foreach (var beam in scriptcontext.ExternalPlanSetup.Beams)
            {
                if (!beam.IsSetupField & beam.MLC != null & beam.MLCPlanType != MLCPlanType.Static)
                {
                    foundBeam = beam;
                    break;
                }
            }
            return foundBeam;
        }


        private void DetectMLC()
        {
            string MLCmodel = FindFirstDynamicField().MLC.Model;

            if (MLCmodel == "Varian High Definition 120")
            {
                this.MLCedges = MLC120HD;
                this.MLCtype = "120HD";
                this.LeafLength = 185;
            }
            else if (MLCmodel == "Millennium 120")
            {
                this.MLCedges = MLC120;
                this.MLCtype = "120";
                this.LeafLength = 190;
            }
            else if (MLCmodel == "SX2")
            {
                this.MLCedges = MLCHalcyon;
                this.MLCtype = "Halcyon";
                this.LeafLength = 280;
            }
            else if (MLCmodel == "Agility")
            {
                this.MLCedges = MLCAgility;
                this.MLCtype = "Agility";
                this.LeafLength = 400;
            }
            else if (MLCmodel == "Millennium 80")
            {
                this.MLCedges = MLC80;
                this.MLCtype = "80";
                this.LeafLength = 190;
            }
        }


        private void LayoutUpdate(object sender, EventArgs e)
        {
            this.PlotAperture.Width = this.PlotApertureTab.ActualWidth;
            this.PlotAperture.Height = this.PlotApertureTab.ActualHeight;

            this.PlotMeterset.Width = this.PlotAperture.Width;
            this.PlotMeterset.Height = this.PlotAperture.Height;

            this.PlotMLCPosition.Width = this.PlotAperture.Width;
            this.PlotMLCPosition.Height = this.PlotAperture.Height;
        }


        public class DataGridBeam
        {
            public string BeamId { get; set; }
            public List<DataGridControlPoint> Datatable { get; set; }
        }

        
        public class DataGridControlPoint
        {
            public int Index { get; set; }
            public double Gantry { get; set; }
            public double Collimator { get; set; }
            public double MetersetWeight { get; set; }
            public List<DataGridControlPointMLC> MLCPositions { get; set; }
            public List<DataGridControlPointJaw> JawPositions { get; set; }
        }

        public class DataGridControlPointMLC
        {
            public int Num { get; set; }
            public float MLC1 { get; set; }
            public float MLC2 { get; set; }
        }

        public class DataGridControlPointJaw
        {
            public double JawX1 { get; set; }
            public double JawX2 { get; set; }
            public double JawY1 { get; set; }
            public double JawY2 { get; set; }
        }

        private PlotModel CreatePlotModelAperture()
        {
            var plotModel = new PlotModel { PlotType = PlotType.Cartesian };

            LinearAxis axisX = new LinearAxis { Position = AxisPosition.Bottom, Minimum = -200, Maximum = 200 };
            LinearAxis axisY = new LinearAxis { Position = AxisPosition.Left, Minimum = -200, Maximum = 200 };

            plotModel.Axes.Add(axisX);
            plotModel.Axes.Add(axisY);
            SetPlotModelColors(plotModel);
            SetAxisColors(axisX);
            SetAxisColors(axisY);
            return plotModel;
        }

        private PlotModel CreatePlotModelMeterset()
        {
            var plotModel = new PlotModel { };

            LinearAxis axisX = new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Key = "X" };
            LinearAxis axisY = new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Key = "Y" };

            plotModel.Axes.Add(axisX);
            plotModel.Axes.Add(axisY);
            SetPlotModelColors(plotModel);
            SetAxisColors(axisX);
            SetAxisColors(axisY);
            return plotModel;
        }

        private PlotModel CreatePlotModelMLCPosition()
        {
            var plotModel = new PlotModel { };

            LinearAxis axisX = new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Key = "X" };
            LinearAxis axisY = new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Key = "Y" };

            plotModel.Axes.Add(axisX);
            plotModel.Axes.Add(axisY);
            SetPlotModelColors(plotModel);
            SetAxisColors(axisX);
            SetAxisColors(axisY);
            return plotModel;
        }

        private void SetAxisColors(LinearAxis axis)
        {
            axis.AxislineColor = OxyColors.White;
            axis.AxislineColor = OxyColors.White;
            axis.TextColor = OxyColors.White;
            axis.TextColor = OxyColors.White;
            axis.TicklineColor = OxyColors.White;
            axis.TicklineColor = OxyColors.White;
        }

        private void SetPlotModelColors(PlotModel plotModel)
        {
            plotModel.Background = OxyColors.Black;
            plotModel.TextColor = OxyColors.White;
            plotModel.SelectionColor = OxyColors.White;
            plotModel.PlotAreaBorderColor = OxyColors.White;
        }

        private void SetController(OxyPlot.Wpf.PlotView plotview)
        {
            var myController = new PlotController();
            plotview.Controller = myController;

            myController.UnbindMouseWheel();

            myController.UnbindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.Control);
            myController.UnbindMouseDown(OxyMouseButton.Right, OxyModifierKeys.None);
            myController.UnbindMouseDown(OxyMouseButton.Right, OxyModifierKeys.None, 2);
            myController.UnbindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.None);
            myController.UnbindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.None, 2);

            myController.BindMouseWheel(OxyModifierKeys.Control, OxyPlot.PlotCommands.ZoomWheelFine);
            myController.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.None, OxyPlot.PlotCommands.ZoomRectangle);
            myController.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.None, 2, OxyPlot.PlotCommands.ResetAt);
            myController.BindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.None, OxyPlot.PlotCommands.PanAt);
        }

        private void PlotAperture_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                int beamIndex = this.BeamComboBox.SelectedIndex;

                if (e.Delta < 0)
                {
                    if (this.DataGridControlPoints.SelectedIndex < this.NumControlPoints[beamIndex])
                    {
                        this.DataGridControlPoints.SelectedIndex += 1;
                    }
                }
                else if (e.Delta > 0)
                {
                    if (this.DataGridControlPoints.SelectedIndex > 0)
                    {
                        this.DataGridControlPoints.SelectedIndex -= 1;
                    }
                }
            }
        }


        private void AddMLCToPlot()
        {
            // create rectangle, when plot is update, just change the positions (faster)
            var mlcPos = this.DataGridBeamList[0].Datatable[0].MLCPositions;
            var jawPos = this.DataGridBeamList[0].Datatable[0].JawPositions;
            int numLeaves = mlcPos.Count;

            for (int i = 0; i < numLeaves; i++)
            {
                var mlc = mlcPos[i];

                float mlc1 = mlc.MLC1;
                float mlc2 = mlc.MLC2;

                OxyColor mlcColor = OxyColors.Blue;
                OxyColor mlcFillcolor = OxyColors.Transparent;
                int mlcStroke = 1;

                double edgeMin = MLCedges[i];
                double edgeMax = MLCedges[i + 1];

                if (this.MLCtype == "Halcyon" & i > 27)
                {
                    edgeMin = MLCedges[i + 1];
                    edgeMax = MLCedges[i + 1 + 1];
                }

                RectangleAnnotation rectangleAnnot1 = new RectangleAnnotation
                {
                    MinimumX = mlc1 - this.LeafLength,
                    MaximumX = mlc1,
                    MinimumY = edgeMin,
                    MaximumY = edgeMax,
                    Fill = mlcFillcolor,
                    Stroke = mlcColor,
                    StrokeThickness = mlcStroke,
                    Text = i.ToString(),
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
                    TextPosition = new OxyPlot.DataPoint(mlc1 - this.LeafLength, (edgeMin + edgeMax) / 2.0),
                    FontSize = 10,
                    Tag = "MLC1-" + i.ToString()
                };

                RectangleAnnotation rectangleAnnot2 = new RectangleAnnotation
                {
                    MinimumX = mlc2,
                    MaximumX = mlc2 + this.LeafLength,
                    MinimumY = edgeMin,
                    MaximumY = edgeMax,
                    Fill = mlcFillcolor,
                    Stroke = mlcColor,
                    StrokeThickness = mlcStroke,
                    Text = i.ToString(),
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Left,
                    TextPosition = new OxyPlot.DataPoint(mlc2 + this.LeafLength, (edgeMin + edgeMax) / 2.0),
                    FontSize = 10,
                    Tag = "MLC2-" + i.ToString()
                };

                this.PlotModelAperture.Annotations.Add(rectangleAnnot1);
                this.PlotModelAperture.Annotations.Add(rectangleAnnot2);
              
            }
            RectangleAnnotation rectangleAnnotJaws = new RectangleAnnotation
            {
                MinimumX = jawPos[0].JawX1,
                MaximumX = jawPos[0].JawX2,
                MinimumY = jawPos[0].JawY1,
                MaximumY = jawPos[0].JawY2,
                Fill = OxyColors.Transparent,
                Stroke = OxyColors.Green,
                StrokeThickness = 1,
                Tag = "Jaws-0"
            };
            this.PlotModelAperture.Annotations.Add(rectangleAnnotJaws);

            // Add some text for jaws
            var textAnnotationJawX1 = new TextAnnotation
            {
                Text = "X1",
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Middle,
                Stroke = OxyColors.Transparent,
                TextPosition = new DataPoint(jawPos[0].JawX1, (jawPos[0].JawY1+ jawPos[0].JawY2)/2.0),
                Tag = "TextJaws-0"
            };
            var textAnnotationJawX2 = new TextAnnotation
            {
                Text = "X2",
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Middle,
                Stroke = OxyColors.Transparent,
                TextPosition = new DataPoint(jawPos[0].JawX2, (jawPos[0].JawY1 + jawPos[0].JawY2) / 2.0),
                Tag = "TextJaws-1"
            };
            var textAnnotationJawY1 = new TextAnnotation
            {
                Text = "Y1",
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Middle,
                Stroke = OxyColors.Transparent,
                TextPosition = new DataPoint((jawPos[0].JawX1 + jawPos[0].JawX2) / 2.0, jawPos[0].JawY1),
                Tag = "TextJaws-2"
            };
            var textAnnotationJawY2 = new TextAnnotation
            {
                Text = "Y2",
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Middle,
                Stroke = OxyColors.Transparent,
                TextPosition = new DataPoint((jawPos[0].JawX1 + jawPos[0].JawX2) / 2.0, jawPos[0].JawY2),
                Tag = "TextJaws-3"
            };

            this.PlotModelAperture.Annotations.Add(textAnnotationJawX1);
            this.PlotModelAperture.Annotations.Add(textAnnotationJawX2);
            this.PlotModelAperture.Annotations.Add(textAnnotationJawY1);
            this.PlotModelAperture.Annotations.Add(textAnnotationJawY2);

            this.PlotModelAperture.InvalidatePlot(false);
        }

        public void PlotMLC()
        {
            if (this.DataGridControlPoints.SelectedIndex != -1)
            {
                //RemoveAnnotations(this.PlotModelAperture);

                var selectedIndex = this.DataGridControlPoints.SelectedIndex;

                var mlcPos = this.DataGridBeamList[this.BeamComboBox.SelectedIndex].Datatable[selectedIndex].MLCPositions;
                var jawPos = this.DataGridBeamList[this.BeamComboBox.SelectedIndex].Datatable[selectedIndex].JawPositions;

                List<int> selectedMLC = this.SelectedMLCs;

                int numLeaves = mlcPos.Count;

                // do this loop only once (faster?):
                // I am starting to learn how bad a programmer i am...
                RectangleAnnotation[] mlc1Annot = new RectangleAnnotation[numLeaves]; // should be ordered by leaf number
                RectangleAnnotation[] mlc2Annot = new RectangleAnnotation[numLeaves];
                RectangleAnnotation[] jawsAnnot = new RectangleAnnotation[numLeaves];
                TextAnnotation[] jawsTextAnnot = new TextAnnotation[4];

                foreach (var a in this.PlotModelAperture.Annotations)
                {
                    string[] tagArray = a.Tag.ToString().Split('-');
                    int ind = Int32.Parse(tagArray[1]);
                    string tagName = tagArray[0];
                    
                    if (tagName == "MLC1")
                    {
                        mlc1Annot[ind] = (RectangleAnnotation)a;
                    }
                    else if (tagName == "MLC2")
                    {
                        mlc2Annot[ind] = (RectangleAnnotation)a;
                    }
                    else if (tagName == "Jaws")
                    {
                        jawsAnnot[ind] = (RectangleAnnotation)a;
                    }
                    else if (tagName == "TextJaws")
                    {
                        jawsTextAnnot[ind] = (TextAnnotation)a;
                    }
                }

                for (int i = 0; i < numLeaves; i++)
                {
                    var mlc = mlcPos[i];

                    float mlc1 = mlc.MLC1;
                    float mlc2 = mlc.MLC2;

                    OxyColor mlcColor = OxyColors.Blue;
                    OxyColor mlcFillcolor = OxyColors.Transparent;
                    int mlcStroke = 1;

                    if (selectedMLC.Contains(i))
                    {
                        mlcColor = OxyColors.Red;
                        mlcStroke = 2;
                    }

                    double edgeMin = MLCedges[i];
                    double edgeMax = MLCedges[i + 1];

                    if (this.MLCtype == "Halcyon" & i > 27)
                    {
                        edgeMin = MLCedges[i + 1];
                        edgeMax = MLCedges[i + 1 + 1];
                    }

                    mlc1Annot[i].MinimumX = mlc1 - this.LeafLength;
                    mlc1Annot[i].MaximumX = mlc1;
                    mlc1Annot[i].MinimumY = edgeMin;
                    mlc1Annot[i].MaximumY = edgeMax;
                    mlc1Annot[i].StrokeThickness = mlcStroke;
                    mlc1Annot[i].Stroke = mlcColor;
                    mlc1Annot[i].TextPosition = new OxyPlot.DataPoint(mlc1 - this.LeafLength, (edgeMin + edgeMax) / 2.0);

                    mlc2Annot[i].MinimumX = mlc2;
                    mlc2Annot[i].MaximumX = mlc2 + this.LeafLength;
                    mlc2Annot[i].MinimumY = edgeMin;
                    mlc2Annot[i].MaximumY = edgeMax;
                    mlc2Annot[i].StrokeThickness = mlcStroke;
                    mlc2Annot[i].Stroke = mlcColor;
                    mlc2Annot[i].TextPosition = new OxyPlot.DataPoint(mlc2 + this.LeafLength, (edgeMin + edgeMax) / 2.0);
                }

                jawsAnnot[0].MinimumX = jawPos[0].JawX1;
                jawsAnnot[0].MaximumX = jawPos[0].JawX2;
                jawsAnnot[0].MinimumY = jawPos[0].JawY1;
                jawsAnnot[0].MaximumY = jawPos[0].JawY2;

                jawsTextAnnot[0].TextPosition = new DataPoint(jawPos[0].JawX1, (jawPos[0].JawY1 + jawPos[0].JawY2) / 2.0);
                jawsTextAnnot[1].TextPosition = new DataPoint(jawPos[0].JawX2, (jawPos[0].JawY1 + jawPos[0].JawY2) / 2.0);
                jawsTextAnnot[2].TextPosition = new DataPoint((jawPos[0].JawX1 + jawPos[0].JawX2) / 2.0, jawPos[0].JawY1);
                jawsTextAnnot[3].TextPosition = new DataPoint((jawPos[0].JawX1 + jawPos[0].JawX2) / 2.0, jawPos[0].JawY2);

                this.PlotModelAperture.InvalidatePlot(true);
            }
            PlotMLCPositionGraph();
        }

        private void PlotMetersetWeight()
        {
            int beamindex = this.BeamComboBox.SelectedIndex;
            if (beamindex == -1) return;
            
            string plotType = this.MetersetPlotType.SelectedItem.ToString();

            this.PlotModelMeterset.Series.Clear();

            string title = "Mw";
            string titleAxisX = "Control point";
            string titleAxisY = "MetersetWeight";

            if (plotType == "dMw")
            {
                title = "MetersetWeight difference";
                titleAxisX = "Control point interval";
                titleAxisY = "MetersetWeight difference";
            }
            else if (plotType == "dMw / dGantry")
            {
                title = "dMw / dGantry";
                titleAxisX = "Control point interval";
                titleAxisY = "MetersetWeight difference / Gantry angle difference [1/deg]";
            }

            LineSeries mwSeries =  new LineSeries
            {
                Title = title,
                Color = OxyColors.White,
                TrackerFormatString = "{0}\n{1}: {2:0.##}\n{3}: {4:0.#######}",
                CanTrackerInterpolatePoints = false
            };

            var calculatedData = CalculateMetersetPlotData(plotType, beamindex);

            List<double> dataX = calculatedData.Item1;
            List<double> dataY = calculatedData.Item2;
            double maxX = calculatedData.Item4;
            double maxY = calculatedData.Item6;

            for (int i = 0; i < dataX.Count; i++)
            {
                mwSeries.Points.Add(new DataPoint(dataX[i], dataY[i]));
            }

            this.PlotModelMeterset.Series.Add(mwSeries);
            var axisX = this.PlotModelMeterset.GetAxis("X");
            var axisY = this.PlotModelMeterset.GetAxis("Y");
            axisX.Maximum = maxX;
            axisX.Title = titleAxisX;
            axisY.Maximum = maxY * 1.05;
            axisY.Title = titleAxisY;

            // Re-read selected control point with highliting
            List<int> selectedControlPointsIndices = GetSelectedControlPoints();

            if (this.FinishedSelectingControlPoints & selectedControlPointsIndices.Count > 0)
            {

                ScatterSeries mwSeriesHighlight = new ScatterSeries
                {
                    MarkerStroke = OxyColors.Yellow,
                    MarkerFill = OxyColors.Yellow,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 3
                };

                if (plotType == "dMw" | plotType == "dMw / dGantry")
                {
                    // Remove zero control point
                    selectedControlPointsIndices.Remove(0);

                    if (selectedControlPointsIndices.Count > 0)
                    {
                        foreach (int i in selectedControlPointsIndices)
                        {
                            mwSeriesHighlight.Points.Add(new ScatterPoint(dataX[i - 1], dataY[i - 1]));
                        }
                    }
                }
                else
                {
                    foreach (int i in selectedControlPointsIndices)
                    {
                        mwSeriesHighlight.Points.Add(new ScatterPoint(dataX[i], dataY[i]));
                    }
                }
                this.PlotModelMeterset.Series.Add(mwSeriesHighlight);
            }
            this.PlotModelMeterset.InvalidatePlot(true);
        }

        private Tuple<List<double>, List<double>, double, double, double, double> CalculateMetersetPlotData(string plotType, int beamindex)
        {
            List<double> dataY = new List<double>() { };
            List<double> dataX = new List<double>() { };

            var dataTable = this.DataGridBeamList[beamindex].Datatable;
            double minX = 0;
            double maxX = 0;
            double minY = 0;
            double maxY = 0;

            if (plotType == "Mw")
            {
                for (int i = 0; i < dataTable.Count; i++)
                {
                    dataX.Add(i);
                    dataY.Add(dataTable[i].MetersetWeight);
                }
                maxX = dataTable.Count;
                maxY = 1.0;
            }
            else if (plotType == "dMw")
            {
                for (int i = 1; i < dataTable.Count; i++)
                {
                    double dy = dataTable[i].MetersetWeight - dataTable[i - 1].MetersetWeight;
                    dataX.Add(i);
                    dataY.Add(dy);
                    if (dy > maxY)
                    {
                        maxY = dy;
                    }
                }
                maxX = dataTable.Count;
            }
            else if (plotType == "dMw / dGantry")
            {
                for (int i = 1; i < dataTable.Count; i++)
                {
                    double angleDiff = 180.0 - Math.Abs(Math.Abs(dataTable[i].Gantry - dataTable[i - 1].Gantry) - 180.0);
                    double dy = (dataTable[i].MetersetWeight - dataTable[i - 1].MetersetWeight) / angleDiff;
                    dataX.Add(i);
                    dataY.Add(dy);

                    if (dy > maxY)
                    {
                        maxY = dy;
                    }
                }
                maxX = dataTable.Count;
            }
            return Tuple.Create(dataX, dataY, minX, maxX, minY, maxY);
        }

        private void PlotMLCPositionGraph()
        {
            int beamindex = this.BeamComboBox.SelectedIndex;
            int leafIndex = this.DataGridMLC.SelectedIndex;
            if (beamindex == -1 | leafIndex == -1) return;

            this.PlotModelMLCPosition.Series.Clear();
            this.PlotModelMLCPosition.Legends.Clear();

            string titleAxisX = "Control point";
            string titleAxisY = "MLC position";

            LineSeries mlc1Series = new LineSeries
            {
                Title = "MLC1",
                Color = OxyColors.Red,
                TrackerFormatString = "{0}\n{1}: {2:0.##}\n{3}: {4:0.#######}",
                CanTrackerInterpolatePoints = false
            };
            LineSeries mlc2Series = new LineSeries
            {
                Title = "MLC2",
                Color = OxyColors.Blue,
                TrackerFormatString = "{0}\n{1}: {2:0.##}\n{3}: {4:0.#######}",
                CanTrackerInterpolatePoints = false
            };

            var calculatedData = CalculateMLCPositionData(beamindex, leafIndex);

            List<double> dataX = calculatedData.Item1;
            List<float> dataY1 = calculatedData.Item2;
            List<float> dataY2 = calculatedData.Item3;
            double minX = calculatedData.Item4;
            double maxX = calculatedData.Item5;
            double minY = calculatedData.Item6;
            double maxY = calculatedData.Item7;

            for (int i = 0; i < dataX.Count; i++)
            {
                mlc1Series.Points.Add(new DataPoint(dataX[i], dataY1[i]));
                mlc2Series.Points.Add(new DataPoint(dataX[i], dataY2[i]));
            }

            this.PlotModelMLCPosition.Series.Add(mlc1Series);
            this.PlotModelMLCPosition.Series.Add(mlc2Series);

            var axisX = this.PlotModelMLCPosition.GetAxis("X");
            var axisY = this.PlotModelMLCPosition.GetAxis("Y");
            axisX.Minimum = minX;
            axisX.Maximum = maxX;
            axisX.Title = titleAxisX;
            //axisY.Minimum = minY * 0.95;
            //axisY.Maximum = maxY * 1.05;
            axisY.Minimum = -400;
            axisY.Maximum = 400;
            axisY.Title = titleAxisY;

            // Re-read selected control point with highliting
            List<int> selectedControlPointsIndices = GetSelectedControlPoints();

            if (this.FinishedSelectingControlPoints & selectedControlPointsIndices.Count > 0)
            {
                ScatterSeries mwSeriesHighlight1 = new ScatterSeries
                {
                    MarkerStroke = OxyColors.Yellow,
                    MarkerFill = OxyColors.Yellow,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 3
                };
                ScatterSeries mwSeriesHighlight2 = new ScatterSeries
                {
                    MarkerStroke = OxyColors.Yellow,
                    MarkerFill = OxyColors.Yellow,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 3
                };

                foreach (int i in selectedControlPointsIndices)
                {
                    mwSeriesHighlight1.Points.Add(new ScatterPoint(i, dataY1[i]));
                    mwSeriesHighlight2.Points.Add(new ScatterPoint(i, dataY2[i]));
                }
                
                this.PlotModelMLCPosition.Series.Add(mwSeriesHighlight1);
                this.PlotModelMLCPosition.Series.Add(mwSeriesHighlight2);
            }

            this.PlotModelMLCPosition.Legends.Add(new Legend()
            {
                LegendTitle = "Legend",
                LegendPosition = LegendPosition.RightTop,
            });

            this.PlotModelMLCPosition.InvalidatePlot(true);
        }


        private Tuple<List<double>, List<float>, List<float>, double, double, double, double> CalculateMLCPositionData(int beamindex, int leafIndex)
        {
            List<double> dataX = new List<double>() { };
            List<float> dataY1 = new List<float>() { };
            List<float> dataY2 = new List<float>() { };

            var dataTable = this.DataGridBeamList[beamindex].Datatable;
            double minX = 0;
            double maxX = 0;
            double minY = float.MaxValue;
            double maxY = 0;

            for (int i = 0; i < dataTable.Count; i++)
            {
                dataX.Add(i);
                float mlc1 = dataTable[i].MLCPositions[leafIndex].MLC1;
                float mlc2 = dataTable[i].MLCPositions[leafIndex].MLC2;
                dataY1.Add(mlc1);
                dataY2.Add(mlc2);

                if (mlc1 > maxY)
                {
                    maxY = mlc1;
                }
                if (mlc2 > maxY)
                {
                    maxY = mlc2;
                }

                if (mlc1 < minY)
                {
                    minY = mlc1;
                }
                if (mlc2 < minY)
                {
                    minY = mlc2;
                }
            }

            maxX = dataTable.Count;

            return Tuple.Create(dataX, dataY1, dataY2, minX, maxX, minY, maxY);
        }


        private List<int> GetSelectedControlPoints()
        {
            List<int> selected = new List<int>() { };

            if (this.DataGridControlPoints.SelectedItems.Count > 0)
            {
                var selectedItems = this.DataGridControlPoints.SelectedItems;
                List<DataGridControlPoint> items = selectedItems.Cast<DataGridControlPoint>().ToList();

                foreach (var item in items)
                {
                    selected.Add(item.Index);
                }
            }
            return selected;
        }

        private void SetControlPointsSelection()
        {
            if (this.DataGridControlPoints.Items.Count > 0)
            {
                this.FinishedSelectingControlPoints = false;
                for (int i = 0; i < this.SelectedControlPoints.Count; i++)
                {
                    if (this.SelectedControlPoints[i] < this.DataGridControlPoints.Items.Count)
                    {
                        object item = this.DataGridControlPoints.Items[this.SelectedControlPoints[i]];
                        this.DataGridControlPoints.SelectedItems.Add(item);
                    }
                }
                this.FinishedSelectingControlPoints = true;
            }
        }

        private List<int> GetSelectedMLC()
        {
            List<int> selected = new List<int>() { };

            if (this.DataGridMLC.SelectedItems.Count > 0)
            {
                var selectedItems = this.DataGridMLC.SelectedItems;
                List<DataGridControlPointMLC> items = selectedItems.Cast<DataGridControlPointMLC>().ToList();

                foreach (var item in items)
                {
                    selected.Add(item.Num);
                }
            }
            return selected;
        }

        private void SetMLCSelection()
        {
            if (this.DataGridMLC.Items.Count > 0)
            {
                this.FinishedSelectingMLCs = false;
                for (int i = 0; i < this.SelectedMLCs.Count; i++)
                {
                    object item = this.DataGridMLC.Items[this.SelectedMLCs[i]];
                    this.DataGridMLC.SelectedItems.Add(item);
                }
                this.FinishedSelectingMLCs = true;
            }
        }


        private void DataGridControlPoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.FinishedSelectingControlPoints)
            {
                if (this.DataGridControlPoints.SelectedIndex != -1)
                {
                    this.SelectedControlPoints = GetSelectedControlPoints();
                }
                SetControlPointsSelection();
                PlotMLC();
                PlotMetersetWeight();
            }
        }

        private void DataGridMLC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.FinishedSelectingMLCs)
            {
                if (this.DataGridMLC.SelectedIndex != -1)
                {
                    this.SelectedMLCs = GetSelectedMLC();
                }
                SetMLCSelection();
                PlotMLC();
            }
        }


        private void DataGridMLC_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            PlotMLC();
        }

        private void DataGridJaws_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            PlotMLC();
        }

        private void BeamComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //this.SelectedMLCs = new List<int>() { };
            PlotMetersetWeight();
        }

        private void DataGridControlPoints_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            PlotMetersetWeight();
        }


        private void MetersetPlotType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlotMetersetWeight();
        }

        private void CollectControlPointsData()
        {
            ExternalPlanSetup plan = this.scriptcontext.ExternalPlanSetup;

            List<DataGridBeam> datagridbeamList = new List<DataGridBeam>() { };
            List<int> numCP = new List<int>() { };

            foreach (var beam in plan.Beams)
            {
                if (!beam.IsSetupField & beam.MLC != null & beam.MLCPlanType != MLCPlanType.Static)
                {
                    List<DataGridControlPoint> items = new List<DataGridControlPoint>() { };

                    numCP.Add(beam.ControlPoints.Count);

                    foreach (var cp in beam.ControlPoints)
                    {
                        int index = cp.Index;

                        List<DataGridControlPointMLC> mlc = new List<DataGridControlPointMLC>() { };

                        for (int i = 0; i < cp.LeafPositions.GetLength(1); i++)
                        {
                            DataGridControlPointMLC mlcpos = new DataGridControlPointMLC()
                            {
                                Num = i,
                                MLC1 = cp.LeafPositions[0, i],
                                MLC2 = cp.LeafPositions[1, i],
                            };
                            mlc.Add(mlcpos);
                        }

                        VRect<double> jawpos = cp.JawPositions;
                        DataGridControlPointJaw jaws = new DataGridControlPointJaw()
                        {
                            JawX1 = jawpos.X1,
                            JawX2 = jawpos.X2,
                            JawY1 = jawpos.Y1,
                            JawY2 = jawpos.Y2
                        };

                        DataGridControlPoint item = new DataGridControlPoint()
                        {
                            Index = index,
                            Gantry = cp.GantryAngle,
                            Collimator = cp.CollimatorAngle,
                            MetersetWeight = cp.MetersetWeight,
                            MLCPositions = mlc,
                            JawPositions = new List<DataGridControlPointJaw>() { jaws }
                        };
                        items.Add(item);
                    }

                    DataGridBeam datagridbeam = new DataGridBeam()
                    {
                        BeamId = beam.Id,
                        Datatable = items
                    };
                    datagridbeamList.Add(datagridbeam);
                }
            }
            this.DataGridBeamList = datagridbeamList;
            this.NumControlPoints = numCP;
        }

       
        private void FillBeamComboBox()
        {
            ListCollectionView collectionView = new ListCollectionView(this.DataGridBeamList);
            this.DataGridBeamCollection = collectionView;
            this.BeamComboBox.ItemsSource = this.DataGridBeamCollection;
            //this.BeamComboBox.SelectedIndex = 0;
        }

        private void FillSelectionComboBoxes()
        {
            this.ComboBoxSelectionParameter.ItemsSource = new List<string>() { "Meterset", "MLC1", "MLC2", "JawX1", "JawX2", "JawY1", "JawY2" };
            this.ComboBoxSelectionAction.ItemsSource = new List<string>() { "Add", "Set to" };
        }

        private void FillMetersetPlotTypeComboBox()
        {
            this.MetersetPlotType.ItemsSource = new List<string>() { "Mw", "dMw", "dMw / dGantry"};
            this.MetersetPlotType.SelectedIndex = 0;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            int ind = txt.CaretIndex;
            txt.Text = txt.Text.Replace(",", ".");
            txt.CaretIndex = ind;
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

        private void DataGridMLC_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var uiElement = e.OriginalSource as UIElement;
            if (e.Key == Key.Enter && uiElement != null)
            {
                this.DataGridMLC.CommitEdit();
                e.Handled = true;

                // Don't know why, but enter has to be pressed twice. Will fix when i figure it out...
            }
        }

        private void DataGridControlPoints_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var uiElement = e.OriginalSource as UIElement;
            if (e.Key == Key.Enter && uiElement != null)
            {
                this.DataGridControlPoints.CommitEdit();
                e.Handled = true;

                // Don't know why, but enter has to be pressed twice. Will fix when i figure it out...
            }
        }


        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // Get selected control points and/or leaves and do the math.
            if (this.ComboBoxSelectionParameter.SelectedIndex == -1 || this.ComboBoxSelectionAction.SelectedIndex == -1)
            {
                MessageBox.Show("Parameter and/or action not selected.", "Error");
                return;
            }

            double textboxValue = ConvertTextToDouble(this.TextBoxSelectionValue.Text);
            
            if (Double.IsNaN(textboxValue))
            {
                MessageBox.Show("Value is not a valid number.", "Error");
                return;
            }
            string comboboxParameter = this.ComboBoxSelectionParameter.SelectedItem.ToString();
            string comboboxAction = this.ComboBoxSelectionAction.SelectedItem.ToString();

            this.SelectedControlPoints = GetSelectedControlPoints();
            this.SelectedMLCs = GetSelectedMLC();
            
            int beamIndex = this.BeamComboBox.SelectedIndex;

            if (comboboxParameter == "Meterset")
            {
                if (comboboxAction == "Add")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        this.DataGridBeamList[beamIndex].Datatable[cpIndex].MetersetWeight += textboxValue;
                    }
                }
                else if (comboboxAction == "Set to")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        this.DataGridBeamList[beamIndex].Datatable[cpIndex].MetersetWeight = textboxValue;
                    }
                }
            }

            else if (comboboxParameter == "MLC1")
            {
                if (comboboxAction == "Add")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        foreach (var leafNum in this.SelectedMLCs)
                        {
                            float old = this.DataGridBeamList[beamIndex].Datatable[cpIndex].MLCPositions[leafNum].MLC1;
                            float opposite = this.DataGridBeamList[beamIndex].Datatable[cpIndex].MLCPositions[leafNum].MLC2;
                            if (old + (float)textboxValue <= opposite)
                            {
                                this.DataGridBeamList[beamIndex].Datatable[cpIndex].MLCPositions[leafNum].MLC1 += (float)textboxValue;
                            }
                            else
                            {
                                this.DataGridBeamList[beamIndex].Datatable[cpIndex].MLCPositions[leafNum].MLC1 = this.DataGridBeamList[beamIndex].Datatable[cpIndex].MLCPositions[leafNum].MLC2;
                            }
                        }
                    }
                }
                else if (comboboxAction == "Set to")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        foreach (var leafNum in this.SelectedMLCs)
                        {
                            this.DataGridBeamList[beamIndex].Datatable[cpIndex].MLCPositions[leafNum].MLC1 = (float)textboxValue;
                        }
                    }
                }
            }

            else if (comboboxParameter == "MLC2")
            {
                if (comboboxAction == "Add")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        foreach (var leafNum in this.SelectedMLCs)
                        {
                            float old = this.DataGridBeamList[beamIndex].Datatable[cpIndex].MLCPositions[leafNum].MLC2;
                            float opposite = this.DataGridBeamList[beamIndex].Datatable[cpIndex].MLCPositions[leafNum].MLC1;
                            if (old + (float)textboxValue >= opposite)
                            {
                                this.DataGridBeamList[beamIndex].Datatable[cpIndex].MLCPositions[leafNum].MLC2 += (float)textboxValue;
                            }
                            else
                            {
                                this.DataGridBeamList[beamIndex].Datatable[cpIndex].MLCPositions[leafNum].MLC2 = this.DataGridBeamList[beamIndex].Datatable[cpIndex].MLCPositions[leafNum].MLC1;
                            }
                        }

                    }
                }
                else if (comboboxAction == "Set to")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        foreach (var leafNum in this.SelectedMLCs)
                        {
                            this.DataGridBeamList[beamIndex].Datatable[cpIndex].MLCPositions[leafNum].MLC2 = (float)textboxValue;
                        }
                    }
                }
            }

            else if (comboboxParameter == "JawX1")
            {
                if (comboboxAction == "Add")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        double old = this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawX1;
                        double opposite = this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawX2;
                        if (old + textboxValue <= opposite)
                        {
                            this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawX1 += textboxValue;
                        }
                        else
                        {
                            this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawX1 = this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawX2;
                        }
                    }
                }
                else if (comboboxAction == "Set to")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawX1 = textboxValue;
                    }
                }
            }

            else if (comboboxParameter == "JawX2")
            {
                if (comboboxAction == "Add")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        double old = this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawX2;
                        double opposite = this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawX1;
                        if (old + (float)textboxValue >= opposite)
                        {
                            this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawX2 += textboxValue;
                        }
                        else
                        {
                            this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawX2 = this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawX1;
                        }
                    }
                }
                else if (comboboxAction == "Set to")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawX2 = textboxValue;
                    }
                }
            }

            else if (comboboxParameter == "JawY1")
            {
                if (comboboxAction == "Add")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        double old = this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawY1;
                        double opposite = this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawY2;
                        if (old + textboxValue <= opposite)
                        {
                            this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawY1 += textboxValue;
                        }
                        else
                        {
                            this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawY1 = this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawY2;
                        }
                    }
                }
                else if (comboboxAction == "Set to")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawY1 = textboxValue;
                    }
                }
            }

            else if (comboboxParameter == "JawY2")
            {
                if (comboboxAction == "Add")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        double old = this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawY2;
                        double opposite = this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawY1;
                        if (old + textboxValue >= opposite)
                        {
                            this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawY2 += textboxValue;
                        }
                        else
                        {
                            this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawY2 = this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawY1;
                        }
                    }
                }
                else if (comboboxAction == "Set to")
                {
                    foreach (var cpIndex in this.SelectedControlPoints)
                    {
                        this.DataGridBeamList[beamIndex].Datatable[cpIndex].JawPositions[0].JawY2 = textboxValue;
                    }
                }
            }

            RefreshDataGrid(beamIndex);
            PlotMLC();
        }

        public void RefreshDataGrid(int beamIndex)
        {
            // this is the stupidest way of refreshing the tables and plots
            this.BeamComboBox.SelectedIndex = -1;
            this.BeamComboBox.SelectedIndex = beamIndex;
            SetControlPointsSelection();
            SetMLCSelection();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // A fresh new plan is created. Beams are inserted with internal methods.
            MessageBoxResult dialogResult = MessageBox.Show("A new plan will be created and fields will be added with internal methods.\n" +
                "Four types of dynamic fields are recognized: VMAT, Conformal Arc (+dynamic MLC), Step and shoot IMRT, Sliding window IMRT", "Message", MessageBoxButton.OKCancel);

            switch (dialogResult)
            {
                case MessageBoxResult.OK:
                    // Add a waiting window here
                    this.Cursor = Cursors.Wait;
                    var waitWindow = new WaitingWindow();
                    waitWindow.Show();

                    try
                    {
                        CreatePlanAndFields();
                    }
                    catch (Exception exc)
                    {
                        waitWindow.Close();
                        this.Cursor = null;
                        MessageBox.Show("Something went wrong.\n\n" + exc.ToString(), "Error");
                    }
                    waitWindow.Close();
                    this.Cursor = null;

                    break;
                case MessageBoxResult.Cancel:
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult dialogResult = MessageBox.Show("The plan will be copied, and fields in the copied plan will be modified in-place.\n" +
                "The original plan will be marked as modified by this script as well. If the original plan is " +
                "approved, you will not be able to save changes into the database!", "Message", MessageBoxButton.OKCancel);
            
            switch (dialogResult)
            {
                case MessageBoxResult.OK:
                    // Add a waiting window here
                    this.Cursor = Cursors.Wait;
                    var waitWindow = new WaitingWindow();
                    waitWindow.Show();

                    try
                    {
                        CopyPlanAndModifyFields();
                    }
                    catch (Exception exc)
                    {
                        waitWindow.Close();
                        this.Cursor = null;
                        MessageBox.Show("Something went wrong.\n\n" + exc.ToString(), "Error");
                    }
                    waitWindow.Close();
                    this.Cursor = null;

                    break;
                case MessageBoxResult.Cancel:
                    break;
            }
        }

        private void CopyPlanAndModifyFields()
        {
            // copy plan and modify jaw/MLC positions.
            // The original plan will also have a "modified by script" status.

            List<DataGridBeam> data = this.DataGridBeamList;

            ExternalPlanSetup newPlan = (ExternalPlanSetup)this.scriptcontext.Course.CopyPlanSetup(this.scriptcontext.ExternalPlanSetup);

            foreach (var beamdata in data)
            {
                string beamId = beamdata.BeamId;

                Beam existingBeam = newPlan.Beams.First(b => b.Id == beamId);

                BeamParameters existingParameters = existingBeam.GetEditableParameters();

                IEnumerable<ControlPointParameters> cpParameters = existingParameters.ControlPoints;

                foreach (var cp in cpParameters)
                {
                    var dataGridData = beamdata.Datatable.First(u => u.Index == cp.Index);
                    var dataGridcpMLCPositions = dataGridData.MLCPositions;
                    var dataGridcpJawPositions = dataGridData.JawPositions[0];

                    int numLeaves = cp.LeafPositions.GetLength(1);

                    float[,] mlcpos = new float[2, numLeaves];

                    for (int i = 0; i < dataGridcpMLCPositions.Count; i++)
                    {
                        mlcpos[0, i] = dataGridcpMLCPositions[i].MLC1;
                        mlcpos[1, i] = dataGridcpMLCPositions[i].MLC2;
                    }

                    // watch it: x1, y1, x2, y2
                    cp.JawPositions = new VRect<double>(
                        dataGridcpJawPositions.JawX1,
                        dataGridcpJawPositions.JawY1,
                        dataGridcpJawPositions.JawX2,
                        dataGridcpJawPositions.JawY2
                    );

                    cp.LeafPositions = mlcpos;
                }
                existingBeam.ApplyParameters(existingParameters);
            }
        }


        private void CreatePlanAndFields()
        {
            // Create a new plan and add appropriate fields
            List<string> fieldTypes = DetermineFieldType();
            foreach (var type in fieldTypes)
            {
                if (type == "UnknownType")
                {
                    MessageBox.Show("Unknown field type.", "Error");
                    return;
                }
            }

            List<DataGridBeam> data = this.DataGridBeamList;

            ExternalPlanSetup newPlan = CreateNewPlan();

            for (int i = 0; i < data.Count; i++)
            {
                if (fieldTypes[i] == "SlidingWindowIMRT")
                {
                    AddIMRTField(newPlan, data[i], "SlidingWindow");
                }
                else if (fieldTypes[i] == "StepAndShootIMRT")
                {
                    AddIMRTField(newPlan, data[i], "StepAndShoot");
                }
                else if (fieldTypes[i] == "VMAT")
                {
                    AddVMATField(newPlan, data[i], "VMAT");
                }
                else if (fieldTypes[i] == "ConformalArc")
                {
                    AddVMATField(newPlan, data[i], "ConformalArc");
                }
            }
        }


        public void AddIMRTField(ExternalPlanSetup newPlan, DataGridBeam beamData, string imrtType)
        {
            string beamId = beamData.BeamId;
            Beam existingBeam = this.scriptcontext.ExternalPlanSetup.Beams.First(b => b.Id == beamId);

            string energyModeId;
            string primaryFluenceMode;
            string energy = existingBeam.EnergyModeDisplayName;

            if (energy == "6X-FFF")
            {
                energyModeId = "6X";
                primaryFluenceMode = "FFF";
            }
            else if (energy == "10X-FFF")
            {
                energyModeId = "10X";
                primaryFluenceMode = "FFF";
            }
            else
            {
                energyModeId = energy;
                primaryFluenceMode = "";
            }


            // Take from existing beam:
            ExternalBeamMachineParameters machineParameters = new ExternalBeamMachineParameters(
                existingBeam.TreatmentUnit.Id,
                energyModeId,
                existingBeam.DoseRate,
                existingBeam.Technique.Id,
                primaryFluenceMode
            );

            // Extract metersetweights
            List<double> metersetWeights = new List<double>() { };

            for (int i = 0; i < beamData.Datatable.Count; i++)
            {
                metersetWeights.Add(beamData.Datatable[i].MetersetWeight);
            }

            // Extract some other things:
            double collAngle = beamData.Datatable[0].Collimator;
            double gantryAngle = beamData.Datatable[0].Gantry;
            double couchAngle = existingBeam.ControlPoints[0].PatientSupportAngle;
            VVector isocenter = existingBeam.IsocenterPosition;

            Beam newBeam = (Beam)null;

            if (imrtType == "SlidingWindow")
            {
                newBeam = newPlan.AddSlidingWindowBeam(machineParameters, metersetWeights, collAngle, gantryAngle, couchAngle, isocenter);
            }
            else if (imrtType == "StepAndShoot")
            {
                newBeam = newPlan.AddMultipleStaticSegmentBeam(machineParameters, metersetWeights, collAngle, gantryAngle, couchAngle, isocenter);
            }
            
            BeamParameters beamParameters = newBeam.GetEditableParameters();
            
            IEnumerable<ControlPointParameters> cpParameters = beamParameters.ControlPoints;

            foreach (var cp in cpParameters)
            {
                var dataGridData = beamData.Datatable.First(u => u.Index == cp.Index);
                var dataGridcpMLCPositions = dataGridData.MLCPositions;
                var dataGridcpJawPositions = dataGridData.JawPositions[0];

                int numLeaves = cp.LeafPositions.GetLength(1);

                float[,] mlcpos = new float[2, numLeaves];

                for (int i = 0; i < dataGridcpMLCPositions.Count; i++)
                {
                    mlcpos[0, i] = dataGridcpMLCPositions[i].MLC1;
                    mlcpos[1, i] = dataGridcpMLCPositions[i].MLC2;
                }

                // watch it: x1, y1, x2, y2
                cp.JawPositions = new VRect<double>(
                    dataGridcpJawPositions.JawX1,
                    dataGridcpJawPositions.JawY1,
                    dataGridcpJawPositions.JawX2,
                    dataGridcpJawPositions.JawY2
                );

                cp.LeafPositions = mlcpos;
            }
            newBeam.ApplyParameters(beamParameters);
        }

        public void AddVMATField(ExternalPlanSetup newPlan, DataGridBeam beamData, string fieldType)
        {
            string beamId = beamData.BeamId;
            Beam existingBeam = this.scriptcontext.ExternalPlanSetup.Beams.First(b => b.Id == beamId);

            string energyModeId;
            string primaryFluenceMode;
            string energy = existingBeam.EnergyModeDisplayName;

            if (energy == "6X-FFF")
            {
                energyModeId = "6X";
                primaryFluenceMode = "FFF";
            }
            else if (energy == "10X-FFF")
            {
                energyModeId = "10X";
                primaryFluenceMode = "FFF";
            }
            else
            {
                energyModeId = energy;
                primaryFluenceMode = "";
            }

            // Take from existing beam:
            ExternalBeamMachineParameters machineParameters = new ExternalBeamMachineParameters(
                existingBeam.TreatmentUnit.Id,
                energyModeId,
                existingBeam.DoseRate,
                existingBeam.Technique.Id,
                primaryFluenceMode
            );

            // Extract metersetweights
            List<double> metersetWeights = new List<double>() { };

            for (int i = 0; i < beamData.Datatable.Count; i++)
            {
                metersetWeights.Add(beamData.Datatable[i].MetersetWeight);
            }

            // Extract some other things:
            double collAngle = beamData.Datatable[0].Collimator;
            double gantryAngle = beamData.Datatable[0].Gantry;
            double couchAngle = existingBeam.ControlPoints[0].PatientSupportAngle;
            VVector isocenter = existingBeam.IsocenterPosition;
            double gantryStop = beamData.Datatable.Last().Gantry;
            GantryDirection gantryDirection = existingBeam.GantryDirection; // read from plan, even though this can be independently set

            Beam newBeam = (Beam)null;

            if (fieldType == "VMAT")
            {
                newBeam = newPlan.AddVMATBeam(machineParameters, metersetWeights, collAngle, gantryAngle, gantryStop, gantryDirection, couchAngle, isocenter);
            }
            else if (fieldType == "ConformalArc")
            {
                newBeam = newPlan.AddConformalArcBeam(machineParameters, collAngle, metersetWeights.Count, gantryAngle, gantryStop, gantryDirection, couchAngle, isocenter);
            }

            BeamParameters beamParameters = newBeam.GetEditableParameters();

            IEnumerable<ControlPointParameters> cpParameters = beamParameters.ControlPoints;

            foreach (var cp in cpParameters)
            {
                var dataGridData = beamData.Datatable.First(u => u.Index == cp.Index);
                var dataGridcpMLCPositions = dataGridData.MLCPositions;
                var dataGridcpJawPositions = dataGridData.JawPositions[0];

                int numLeaves = cp.LeafPositions.GetLength(1);

                float[,] mlcpos = new float[2, numLeaves];

                for (int i = 0; i < dataGridcpMLCPositions.Count; i++)
                {
                    mlcpos[0, i] = dataGridcpMLCPositions[i].MLC1;
                    mlcpos[1, i] = dataGridcpMLCPositions[i].MLC2;
                }

                // watch it: x1, y1, x2, y2
                cp.JawPositions = new VRect<double>(
                    dataGridcpJawPositions.JawX1,
                    dataGridcpJawPositions.JawY1,
                    dataGridcpJawPositions.JawX2,
                    dataGridcpJawPositions.JawY2
                );

                cp.LeafPositions = mlcpos;
            }
            newBeam.ApplyParameters(beamParameters);
        }

        public List<string> DetermineFieldType()
        {
            List<string> fieldTypes = new List<string>() { };

            List<DataGridBeam> data = this.DataGridBeamList;
            
            foreach (var beamdata in data)
            {
                var datatable = beamdata.Datatable;

                List<double> metersetWeights = new List<double>() { };
                for (int i = 0; i < beamdata.Datatable.Count; ++i)
                {
                    metersetWeights.Add(beamdata.Datatable[i].MetersetWeight);
                }

                int numPoints = metersetWeights.Count;

                List<double> gantries = new List<double>() { };
                foreach (var g in datatable)
                {
                    gantries.Add(g.Gantry);
                }

                if (gantries.Distinct().ToList().Count > 1)
                {
                    // Check if dmw/dgantry is constant => Conformal arc beam or so called constant dose rate VMAT
                    // If dmw/dgantry is arbitrary (up to a point) consider it as VMAT
                    List<double> coeff = new List<double>() { };

                    for (int i = 1; i < metersetWeights.Count; i++)
                    {
                        double angleDiff = 180.0 - Math.Abs(Math.Abs(gantries[i] - gantries[i - 1]) - 180.0);
                        double dd = (metersetWeights[i] - metersetWeights[i - 1]) / angleDiff;
                        coeff.Add(dd);
                    }
                    double coeffMax = coeff.Max();
                    double coeffMin = coeff.Min();

                    if (coeffMax / coeffMin < 1.04)
                    {
                        fieldTypes.Add("ConformalArc");
                    }
                    else
                    {
                        fieldTypes.Add("VMAT");
                    }
                }
                else
                {
                    // Static beam (IMRT only)
                    // Step and shoot IMRT if mw follows this pattern: 0, A, A, B, B, C, C, ... , 1; where A, B, C etc are all unique.
                    // Sliding window IMRT if mw follows this pattern: 0, A, B, C, D, ... , 1; where A, B, C etc are all unique.

                    if (metersetWeights.First() == 0 & Math.Abs(metersetWeights.Last() - 1) < 1e-3)
                    {
                        if (metersetWeights.GetRange(1, numPoints - 2).Distinct().ToList().Count == numPoints - 2)
                        {
                            fieldTypes.Add("SlidingWindowIMRT");
                        }
                        else if ((numPoints - 2) % 2 == 0)
                        {
                            double sum = 0;
                            for (int i = 1; i < numPoints - 1; i++)
                            {
                                sum += metersetWeights[i] * Math.Pow(-1, i);
                            }

                            if (Math.Abs(sum) < 1e-3)
                            {
                                fieldTypes.Add("StepAndShootIMRT");
                            }
                            else
                            {
                                fieldTypes.Add("UnknownType");
                            }
                        }
                        else
                        {
                            fieldTypes.Add("UnknownType");
                        }
                    }
                    else
                    {
                        fieldTypes.Add("UnknownType");
                    }
                }
            }
            return fieldTypes;
        }


        public ExternalPlanSetup CreateNewPlan()
        {
            ExternalPlanSetup newPlan = this.scriptcontext.Course.AddExternalPlanSetup(this.scriptcontext.StructureSet);

            int fractions = (int)this.scriptcontext.ExternalPlanSetup.NumberOfFractions;
            DoseValue dosePerFraction = this.scriptcontext.ExternalPlanSetup.DosePerFraction;
            double treatPercentage = this.scriptcontext.ExternalPlanSetup.TreatmentPercentage;
            newPlan.SetPrescription(fractions, dosePerFraction, treatPercentage);

            double normalization = this.scriptcontext.ExternalPlanSetup.PlanNormalizationValue;
            if (!Double.IsNaN(normalization))
            {
                newPlan.PlanNormalizationValue = normalization;
            }
            else
            {
                newPlan.PlanNormalizationValue = 100;
            }
            return newPlan;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            // Delete selected control points from datagrid and refresh
            int beamIndex = this.BeamComboBox.SelectedIndex;

            if (this.DataGridControlPoints.SelectedIndex == -1)
            {
                MessageBox.Show("No control point selected.", "Error");
                return;
            }

            // Filter original list by creating a new one without selected control points
            List<DataGridControlPoint> newList = new List<DataGridControlPoint>() { };
            for (int i = 0; i < this.DataGridBeamList[beamIndex].Datatable.Count; i++)
            {
                if (!this.SelectedControlPoints.Contains(this.DataGridBeamList[beamIndex].Datatable[i].Index))
                {
                    newList.Add(this.DataGridBeamList[beamIndex].Datatable[i]);
                }
            }
            this.DataGridBeamList[beamIndex].Datatable = newList;
            
            // Re-index the datagrid!
            for (int i = 0; i < this.DataGridBeamList[beamIndex].Datatable.Count; i++)
            {
                this.DataGridBeamList[beamIndex].Datatable[i].Index = i;
            }

            this.SelectedControlPoints = new List<int>() { 0 };
            RefreshDataGrid(beamIndex);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            // Add control point at the selected index.
            // Parameters are interpolated between previous and next control point (for starters)
            int beamIndex = this.BeamComboBox.SelectedIndex;

            int cpPosition = this.DataGridControlPoints.SelectedIndex;
            if (cpPosition == -1)
            {
                MessageBox.Show("Select an existing point. A new one will be added after it.", "Error");
                return;
            }

            if (cpPosition >= this.DataGridControlPoints.Items.Count - 1)
            {
                MessageBox.Show("Selected row must be followed by at least one control point.", "Error");
                return;
            }

            DataGridControlPoint previousCP = this.DataGridBeamList[beamIndex].Datatable[cpPosition];
            DataGridControlPoint nextCP = this.DataGridBeamList[beamIndex].Datatable[cpPosition + 1];

            List<DataGridControlPointMLC> previousMLC = previousCP.MLCPositions;
            List<DataGridControlPointMLC> nextMLC = nextCP.MLCPositions;
            List<DataGridControlPointMLC> newMLC = new List<DataGridControlPointMLC>() { };

            for (int i = 0; i < previousMLC.Count; i++)
            {
                DataGridControlPointMLC newMLCitem = new DataGridControlPointMLC()
                {
                    Num = i,
                    MLC1 = (float)((previousMLC[i].MLC1 + nextMLC[i].MLC1) / 2.0),
                    MLC2 = (float)((previousMLC[i].MLC2 + nextMLC[i].MLC2) / 2.0)
                };
                newMLC.Add(newMLCitem);
            }

            DataGridControlPointJaw previousJaw = previousCP.JawPositions[0];
            DataGridControlPointJaw nextJaw = nextCP.JawPositions[0];
            List<DataGridControlPointJaw> newJaw = new List<DataGridControlPointJaw>() { };

            newJaw.Add(new DataGridControlPointJaw()
            {
                JawX1 = (previousJaw.JawX1 + nextJaw.JawX1) / 2.0,
                JawX2 = (previousJaw.JawX2 + nextJaw.JawX2) / 2.0,
                JawY1 = (previousJaw.JawY1 + nextJaw.JawY1) / 2.0,
                JawY2 = (previousJaw.JawY2 + nextJaw.JawY2) / 2.0
            });

            DataGridControlPoint newCP = new DataGridControlPoint()
            {
                Index = cpPosition + 1, // will be redefined later
                Collimator = previousCP.Collimator,
                Gantry = (previousCP.Gantry + nextCP.Gantry) / 2.0,
                MetersetWeight = (previousCP.MetersetWeight + nextCP.MetersetWeight) / 2.0,
                MLCPositions = newMLC,
                JawPositions = newJaw
            };

            this.DataGridBeamList[beamIndex].Datatable.Insert(cpPosition + 1, newCP);

            // Re-index the datagrid!
            for (int i = 0; i < this.DataGridBeamList[beamIndex].Datatable.Count; i++)
            {
                this.DataGridBeamList[beamIndex].Datatable[i].Index = i;
            }

            this.SelectedControlPoints = new List<int>() { 0 };
            RefreshDataGrid(beamIndex);
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            HelpWindow helpwindow = new HelpWindow();
            helpwindow.Owner = this;
            helpwindow.Show();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            ImportWindow importWindow = new ImportWindow();
            importWindow.Owner = this;
            importWindow.Show();
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            // Copy MLC positions to clipboard
            // MLC1_0, MLC1_1, MLC1_2 .... MLC2_0, MLC2_1, ...
            // MLC1_0, MLC1_1, MLC1_2 .... MLC2_0, MLC2_1, ...
            // MLC1_0, MLC1_1, MLC1_2 .... MLC2_0, MLC2_1, ...
            // MLC1_0, MLC1_1, MLC1_2 .... MLC2_0, MLC2_1, ...
            int beamIndex = this.BeamComboBox.SelectedIndex;
            string clipboardText = "";

            for (int i = 0; i < this.DataGridBeamList[beamIndex].Datatable.Count; i++)
            {
                var mlcPos = this.DataGridBeamList[beamIndex].Datatable[i].MLCPositions;
                var lineTextMLC1 = "";
                var lineTextMLC2 = "";
                for (int j = 0; j < mlcPos.Count; j++)
                {
                    lineTextMLC1 += mlcPos[j].MLC1.ToString() + " ";
                    lineTextMLC2 += mlcPos[j].MLC2.ToString() + " ";
                }
                clipboardText += lineTextMLC1 + lineTextMLC2 + "\n";
            }
            Clipboard.SetData(DataFormats.Text, clipboardText);
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            // Copy jaw positions to clipboard
            int beamIndex = this.BeamComboBox.SelectedIndex;
            string clipboardText = "";

            for (int i = 0; i < this.DataGridBeamList[beamIndex].Datatable.Count; i++)
            {
                var jawPos = this.DataGridBeamList[beamIndex].Datatable[i].JawPositions[0];

                clipboardText += jawPos.JawX1.ToString() + " " + jawPos.JawX2.ToString() + " " + jawPos.JawY1.ToString() + " " + jawPos.JawY2.ToString() + "\n";
            }
            Clipboard.SetData(DataFormats.Text, clipboardText);
        }
    }
}
