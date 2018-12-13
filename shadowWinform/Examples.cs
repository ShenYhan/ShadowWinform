using Arction.WinForms.Charting;
using System;
using System.Collections.Generic;
using System.Text;
using PublicColor = System.Drawing.Color;

namespace DemoAppWinForms
{

        public static class ExampleUtils
        {
            static Reverted revertedChart;

            /// <summary>
            /// Alpha channel for transparency.
            /// </summary>
            static int alpha = 180;
            static PublicColor backgroundColor = PublicColor.FromArgb(255, 30, 30, 30);
            static PublicColor titleColor = PublicColor.FromArgb(255, 249, 202, 3);

#if WpfDemoApplication
        public static PublicColor ColorFromArgb(byte a, PublicColor color)
        {
            return PublicColor.FromArgb(a, color.R, color.G, color.B);
        }

        public static PublicColor ColorFromArgb(byte r, byte g, byte b)
        {
            return PublicColor.FromArgb(255, r, g, b);
        }
#endif

            /// <summary>
            /// Set of colors without transparency.
            /// </summary>
            public static List<PublicColor> lcSolidColors = new List<PublicColor>
        {

            PublicColor.FromArgb(187, 28, 0),
            PublicColor.FromArgb(222, 54, 11),
            PublicColor.FromArgb(199, 86, 10),
            PublicColor.FromArgb(255, 160, 0),
            PublicColor.FromArgb(255, 124, 0)

        };

            /// <summary>
            /// Set of colors for series.
            /// </summary>
            public static List<PublicColor> lcGradientColors = new List<PublicColor>
        {
#if WinFormsDemoApplication
            PublicColor.FromArgb(alpha, lcSolidColors[0]),
            PublicColor.FromArgb(alpha, lcSolidColors[1]),
            PublicColor.FromArgb(alpha, lcSolidColors[2]),
            PublicColor.FromArgb(alpha, lcSolidColors[3])
#else
            PublicColor.FromArgb((byte)alpha, lcSolidColors[0].R, lcSolidColors[0].G, lcSolidColors[0].B),
            PublicColor.FromArgb((byte)alpha, lcSolidColors[1].R, lcSolidColors[1].G, lcSolidColors[1].B),
            PublicColor.FromArgb((byte)alpha, lcSolidColors[2].R, lcSolidColors[2].G, lcSolidColors[2].B),
            PublicColor.FromArgb((byte)alpha, lcSolidColors[3].R, lcSolidColors[3].G, lcSolidColors[3].B),
#endif
        };

            /// <summary>
            /// Dispose items in collection before and clear.
            /// </summary>
            /// <typeparam name="T">Collection type</typeparam>
            /// <param name="list">Collection</param>
#if WpfSemibindable
        public static void DisposeAllAndClear<T>(System.Windows.FreezableCollection<T> list) where T : System.Windows.Freezable
#else
            public static void DisposeAllAndClear<T>(List<T> list) where T : IDisposable
#endif
            {
                if (list == null)
                    return;

                foreach (IDisposable item in list)
                {
                    if (item != null)
                        item.Dispose();
                }

                list.Clear();
            }

            /// <summary>
            /// Set predefined dark flat design theme. 
            /// </summary>
            /// <param name="chart">Chart to be redesigned.</param>
            /// <param name="tickmarks">Defines what tickmarks should be shown.</param>
            public static void SetDarkFlatStyle(LightningChartUltimate chart, MinorTicks tickmarks = MinorTicks.None)
            {
                //Get original style theme
                revertedChart = new Reverted(chart);

                //Setup chart background coloring
#if WinFormsDemoApplication
            chart.Background.Color = backgroundColor;
            chart.Background.GradientFill = GradientFill.Solid;
#else
                chart.Background.Color = backgroundColor;
                chart.Background.GradientFill = GradientFill.Solid;

#endif
                chart.ViewXY.GraphBackground.Color = PublicColor.FromArgb(255, 20, 20, 20);
                chart.ViewXY.GraphBackground.GradientFill = GradientFill.Solid;
                chart.Title.Color = titleColor;
                chart.Title.MouseHighlight = MouseOverHighlight.None;

                bool visibleMarksX = false;
                bool visibleMarksY = false;

                if (tickmarks == MinorTicks.Both)
                    visibleMarksX = visibleMarksY = true;
                else if (tickmarks == MinorTicks.OnlyX)
                    visibleMarksX = true;
                else if (tickmarks == MinorTicks.OnlyY)
                    visibleMarksY = true;

                foreach (var yAxis in chart.ViewXY.YAxes)
                {
                    yAxis.Title.Color = titleColor;
                    yAxis.Title.MouseHighlight = MouseOverHighlight.None;
                    yAxis.MajorGrid.Color = PublicColor.FromArgb(35, 255, 255, 255);
                    yAxis.MajorGrid.Pattern = LinePattern.Solid;
                    yAxis.MinorDivTickStyle.Visible = visibleMarksY;
                }

                foreach (var xAxis in chart.ViewXY.XAxes)
                {
                    xAxis.Title.Color = titleColor;
                    xAxis.Title.MouseHighlight = MouseOverHighlight.None;
                    xAxis.MajorGrid.Color = PublicColor.FromArgb(35, 255, 255, 255);
                    xAxis.MajorGrid.Pattern = LinePattern.Solid;
                    xAxis.MinorDivTickStyle.Visible = visibleMarksX;
                }

                if (chart.ViewXY.LegendBoxes != null)
                {
                    foreach (var legend in chart.ViewXY.LegendBoxes)
                        legend.Shadow.Visible = false;
                }
            }
			
			public static void SetStandardFlatStyle(LightningChartUltimate chart, MinorTicks tickmarks = MinorTicks.None)
        {
            //Get original style theme
            revertedChart = new Reverted(chart);

            //Setup chart background coloring
#if WinFormsDemoApplication
            chart.Background.Color = backgroundColor;
            chart.Background.GradientFill = GradientFill.Solid;
#else
            chart.Background.Color = PublicColor.FromArgb(255, 0, 150, 139);
            chart.Background.GradientFill = GradientFill.Solid;

#endif
            chart.ViewXY.GraphBackground.Color = PublicColor.FromArgb(255, 20, 20, 20);
            chart.ViewXY.GraphBackground.GradientFill = GradientFill.Solid;
            chart.Title.Color = titleColor;
            chart.Title.MouseHighlight = MouseOverHighlight.None;

            bool visibleMarksX = false;
            bool visibleMarksY = false;

            if (tickmarks == MinorTicks.Both)
                visibleMarksX = visibleMarksY = true;
            else if (tickmarks == MinorTicks.OnlyX)
                visibleMarksX = true;
            else if (tickmarks == MinorTicks.OnlyY)
                visibleMarksY = true;

            foreach (var yAxis in chart.ViewXY.YAxes)
            {
                yAxis.Title.Color = titleColor;
                yAxis.Title.MouseHighlight = MouseOverHighlight.None;
                yAxis.MajorGrid.Color = PublicColor.FromArgb(35, 255, 255, 255);
                yAxis.MajorGrid.Pattern = LinePattern.Solid;
                yAxis.MinorDivTickStyle.Visible = visibleMarksY;
            }
        }

            /// <summary>
            /// Revert to default Dark style.
            /// </summary>
            /// <param name="chart">Chart to be reverted.</param>
            public static void CancelDarkFlatStyle(LightningChartUltimate chart)
            {
                if (revertedChart != null)
                {
                    if (chart != null)
                    {
                        chart.BeginUpdate();

#if WinFormsDemoApplication
                    chart.Background.Color = revertedChart.chartColor;
                    chart.Background.GradientFill = revertedChart.gradientChart;
#else
                        chart.Background.Color = revertedChart.chartColor;
                        chart.Background.GradientFill = revertedChart.gradientChart;
#endif
                        chart.ViewXY.GraphBackground.Color = revertedChart.viewColor;
                        chart.ViewXY.GraphBackground.GradientFill = revertedChart.gradientView;

                        foreach (var yAxis in chart.ViewXY.YAxes)
                        {
                            yAxis.MajorGrid.Color = revertedChart.gridColor;
                            yAxis.MajorGrid.Pattern = revertedChart.gridPattern;
                            yAxis.MinorDivTickStyle.Visible = true;
                        }

                        foreach (var xAxis in chart.ViewXY.XAxes)
                        {
                            xAxis.MajorGrid.Color = revertedChart.gridColor;
                            xAxis.MajorGrid.Pattern = revertedChart.gridPattern;
                            xAxis.MinorDivTickStyle.Visible = true;
                        }

                        chart.EndUpdate();
                    }
                }
            }
            class Reverted
            {
                public GradientFill gradientChart;
                public GradientFill gradientView;
                public PublicColor viewColor;
                public PublicColor chartColor;
                public PublicColor gridColor;
                public LinePattern gridPattern;

                public Reverted(LightningChartUltimate chart)
                {
#if WinFormsDemoApplication
                gradientChart = chart.Background.GradientFill;               
                chartColor = chart.Background.Color;
#else
                    chartColor = chart.Background.Color;
                    gradientChart = chart.Background.GradientFill;
#endif
                    gradientView = chart.ViewXY.GraphBackground.GradientFill;
                    viewColor = chart.ViewXY.GraphBackground.Color;
                    gridColor = chart.ViewXY.XAxes[0].MajorGrid.Color;
                    gridPattern = chart.ViewXY.XAxes[0].MajorGrid.Pattern;
                }
            }

            /// <summary>
            /// Defines what tickmarks should be shown.
            /// </summary>
            public enum MinorTicks
            {
                /// <summary>
                /// Hide all.
                /// </summary>
                None = 0,
                /// <summary>
                /// Show X minor tickmarks.
                /// </summary>
                OnlyX,
                /// <summary>
                /// Show Y minor tickmarks.
                /// </summary>
                OnlyY,
                /// <summary>
                /// Show all.
                /// </summary>
                Both
            }
        }


}

