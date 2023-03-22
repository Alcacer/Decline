﻿using MathNet.Numerics;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static Decline_Curve_Analysis.Home;
using static Decline_Curve_Analysis.DataInput;
using System.Windows.Forms.DataVisualization.Charting;

namespace Decline_Curve_Analysis
{
    public partial class Graph : Form
    {
        public Graph()
        {
            InitializeComponent();
        }

        internal static DataTable resultsTable = new DataTable();
        private void Graph_Load(object sender, EventArgs e)
        {
            ChartArea declineChartArea = new ChartArea { Name = "Decline Chart Area" };
            declineChartArea.AxisX.Interval = 1D;
            declineChartArea.AxisX.MajorGrid.LineColor = Color.LightGray;

            //make the the interval of the y-axis to auto adjust to the maximum value of the production data.
            double maxValue = dataTable.AsEnumerable().Select(x => Convert.ToDouble(x[dataTable.Columns[1]])).ToArray().Max();

            if (maxValue > 800)
            {
                declineChartArea.AxisY.Interval = 75D;
            }
            else if (maxValue < 100)
            {
                declineChartArea.AxisY.Interval = 5D;
            }
            else
            {
                declineChartArea.AxisY.Interval = 25D;
            }
            declineChartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            DeclineGraph.ChartAreas.Add(declineChartArea);
            DeclineGraph.Series[0].ChartArea = "Decline Chart Area";
            DeclineGraph.Series[1].ChartArea = "Decline Chart Area";

            TabulizeResultsButton.Enabled = false;
            DeclineGraph.ChartAreas[0].AxisX.Title = dataTable.Columns[0].ToString();
            DeclineGraph.ChartAreas[0].AxisY.Title = dataTable.Columns[1].ToString();

            //For the data points
            DeclineGraph.Series[0].XValueMember = dataTable.Columns[0].ToString();
            DeclineGraph.Series[0].YValueMembers = dataTable.Columns[1].ToString();

            //For the line through the data points
            DeclineGraph.Series[1].XValueMember = dataTable.Columns[0].ToString();
            DeclineGraph.Series[1].YValueMembers = dataTable.Columns[1].ToString();

            DeclineGraph.DataSource = dataTable;
            DeclineGraph.DataBind();
        }
         
        private void CalculateExponentialDecline(DataTable productionData, int forecastDays)
        {
            // Extract the production and time data from the input DataTable
            double[] production = productionData.AsEnumerable().Select(r => Convert.ToDouble(r[productionData.Columns[1]])).ToArray();
            double[] time = productionData.AsEnumerable().Select(r => Convert.ToDouble(r[productionData.Columns[0]])).ToArray();

            // Fit an exponential decline function to the production data using linear regression
            double[] yData = production.Select(r => Math.Log(r)).ToArray();
            var regressionResult = Fit.Line(time, yData);
            double Q0 = Math.Exp(regressionResult.A);
            double D = -regressionResult.B;

            // Calculate the predicted production values for the future time period
            double lastTime = time[time.Length - 1];
            double timeIncrement = lastTime - time[time.Length - 2]; //makes the timeincrement to follow the same order as the data in the table.
            double[] futureTime = Enumerable.Range(1, forecastDays).Select(i => lastTime + (i * timeIncrement)).ToArray();

            double[] futureProduction = futureTime.Select(t => production[production.Length-1] * Math.Exp(-D * (t - lastTime))).ToArray();

            // Show the results
            MessageBox.Show($"Initial production rate (Q0): {Q0}\n\nDecline rate (D): {D}", "Results");

            //This adds the future production data to the graph
            DataRow[] dataRows = new DataRow[forecastDays];
            for (int i = 0; i < forecastDays; i++)
            {
                DataRow row = productionData.NewRow();
                DeclineGraph.Series["DeclineSeries"].Points.AddXY(futureTime[i].ToString(), futureProduction[i]);
                DeclineGraph.Series["DeclineSeries"].Points[productionData.Rows.Count + i].Color = Color.Red;
                DeclineGraph.Series["Line"].Points.AddXY(futureTime[i].ToString(), futureProduction[i]);
                row[0] = futureTime[i];
                row[1] = Math.Round(futureProduction[i], 2, MidpointRounding.AwayFromZero); //rounds the calculated future production data
                dataRows[i] = row;                                                          //to 2 decimal places at most.
            }
            //Add the predicted values to the datatable to display it in the results form
            foreach (DataRow row in dataRows)
            {
                productionData.Rows.Add(row);
            }
            resultsTable = productionData;
        }


        private void RunPredictionButton_Click(object sender, EventArgs e)
        {
            CalculateExponentialDecline(dataTable, 10);
            TabulizeResultsButton.Enabled = true;
            RunPredictionButton.Enabled = false;
            WindowState = FormWindowState.Maximized;
        }

        private string closeReason;
        private void BackButton_Click(object sender, EventArgs e)
        {
            closeReason = "backbutton";
            Close();
        }
        private void Graph_Closed(object sender, FormClosedEventArgs e)
        {
            //Make the whole application close if the X button or Alt + F4 is pressed, but not when the back button is pressed.
            if (closeReason == "backbutton")
            {
                dataForm.Show();
            }
            else
            {
                dataForm.Close();
            }
        }
        private void TabulizeResultsButton_Click(object sender, EventArgs e)
        {
            Results resultForm = new Results();
            resultForm.ShowDialog();
        }
    }
}
