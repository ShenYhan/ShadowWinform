using Arction.WinForms.Charting;
using Arction.WinForms.Charting.Annotations;
using Arction.WinForms.Charting.Axes;
using Arction.WinForms.Charting.SeriesXY;
using Arction.WinForms.Charting.Views.ViewPie3D;
using DemoAppWinForms;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PublicColor = System.Drawing.Color;

namespace shadowWinform
{
    public partial class Form1 : Form
    {
        //用于拖动界面的移动的前期定义
        Point mouseOff;
        bool leftFlag = false;

        //数据更新Data_timer计时器计时、循环显示数据、计数
        private int dataTick = 0;//用于读取数据库中1000条循环数据
        private int TempCount = 0, SiCount = 0, VecCount = 0;
        private Random ran = new Random();

        //铁水监控视频读取的初始化
        private Capture capture_Iron_1;
        private Mat frame_Iron = new Mat();

        //数据库中铁水温度数据、成分数据以及流速数据
        private DataTable IronTemper, IronComponent, IronSpeed;

        //5个lightningChart控件的Chart定义、未初始化
        LightningChartUltimate _chartTempCloud = null, _chartTemp = null, _chartSi = null, _chartVelocity = null, _chartPie3D = null;


        private IntensityGridSeries _intensityGrid = null;
        private AnnotationXY _mouseTrackAnnotation = null;
        private double siga = 0;
        private int nextImg = 1;

        //Trace properties 移动鼠标事件初始化
        private int _traceCellColumnIndex = 0;
        private int _traceCellRowIndex = 0;
        private int _traceNearestDataColumnIndex = 0;
        private int _traceNearestDataRowIndex = 0;
        private float _traceNearestDataColumnCoord = 0;
        private float _traceNearestDataRowCoord = 0;
        //Nearest corner point
        private IntensityPoint _nearestCornerPoint;

        //Mouse coordinates
        private int _mouseX = 0;
        private int _mouseY = 0;

        //三个用于绘制温度云图像的图像数据
        private Image<Bgr, byte> lastCloudImg = new Image<Bgr, byte>(894, 206, new Bgr(0, 255, 0));
        private Image<Bgr, byte> nowlastCloudImg = new Image<Bgr, byte>(894, 206, new Bgr(0, 255, 0));
        private Image<Bgr, byte> ImgChangeOver = new Image<Bgr, byte>(894, 206, new Bgr(0, 255, 0));

        //用于判断是否数据是否读取成功
        bool OutputDataIsReady = false;

        //温度、硅含量、流速的数据、txt格式
        private double[,] TempData = new double[1, 500];
        private double[,] SiData = new double[1, 200];
        private double[,] VecData = new double[1, 120];

        //读取铁水温度数据的路径
        private static String TempPath = Application.StartupPath + "\\IronTempData\\";
        private static String OutputDataPath = Application.StartupPath + "\\data\\";
        private static String[] TempList = { TempPath + "2018-03-27_11_19_47.txt", TempPath + "2018-03-27_11_19_57.txt", TempPath + "2018-03-27_11_20_07.txt", TempPath + "2018-03-27_11_20_17.txt",
            TempPath + "2018-03-27_11_20_27.txt",TempPath + "2018-03-27_11_20_37.txt",TempPath + "2018-03-27_11_20_47.txt", TempPath + "2018-03-27_11_20_57.txt",
            TempPath + "2018-03-27_11_21_07.txt",TempPath + "2018-03-27_11_21_17.txt",TempPath + "2018-03-27_11_21_27.txt",TempPath + "2018-03-27_11_21_37.txt",
            TempPath + "2018-03-27_11_21_47.txt",TempPath + "2018-03-27_11_21_57.txt"};
        private static String IconPath = Application.StartupPath + "\\Icon\\";

        //保存铁水红外数据的一个中间变量
        private double[,] tmpData = new double[206, 894];

        //曲线横轴时间数据的初始化变量
        private double _previousX = 0;

        //电池空间重组中间变量
        private Image<Bgra, byte> tmp;//组合后的用于显示的图片
        private Image<Bgra, byte> battery = new Image<Bgra, byte>(IconPath + "battery.png");//电池外壳
        private Image<Bgra, byte> transparentLine;//用于填补透明外壳的图片（大小为1*200）
        private Image<Bgra, byte> innerLine;//用于填补电池内部的图片（大小为1*200）
        private double X = 0;//接口变量动态变化

        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            IronTemper = IronTemper_Data();
            IronComponent = IronComponent_Data();
            OpenMoltenIron();
            CreateSiChart();
            CreatePie3D();
            CreateTempChart();
            CreateCloudChart();
            CreateVelocityChart();
            GetDataFromFile();
            ApplyFillStyle();
            GetBaseLine();
            Thread initial = new Thread(initial_2Dsurface);
            initial.Start();
        }

        /// <summary>
        /// 获取需要离线显示的曲线数据、温度、硅含量、流速
        /// </summary>
        private void GetDataFromFile()
        {
            readMatrix(TempData, OutputDataPath + "TEMPERATURE.txt");
            readMatrix(SiData, OutputDataPath + "SiData.txt");
            readMatrix(VecData, OutputDataPath + "VecData.txt");
            OutputDataIsReady = true;
        }

        /// <summary>
        /// 数据库中获取铁水温度数据
        /// </summary>
        /// <returns></returns>
        private DataTable IronTemper_Data()
        {
            //创建数据库连接字符串
            string P_Str_ConnectionStr = string.Format(@"Server=yxnat.softdev.top,12306;Database=test1;uid=xiejin;pwd=0000");
            //创建SQL查询字符串
            string P_Str_SqlStr = string.Format("SELECT TOP 1000 铁水温度 FROM v_xtscw  ");
            //创建数据适配器
            SqlDataAdapter P_SqlDataAdapter = new SqlDataAdapter(P_Str_SqlStr, P_Str_ConnectionStr);
            DataTable P_dt = new DataTable();
            P_SqlDataAdapter.Fill(P_dt);
            return P_dt;
        }

        /// <summary>
        /// 数据库中获取铁水硅含量以及钛含量数据
        /// </summary>
        /// <returns></returns>
        private DataTable IronComponent_Data()
        {
            //创建数据库连接字符串
            string P_Str_ConnectionStr = string.Format(@"Server=yxnat.softdev.top,12306;Database=test1;uid=xiejin;pwd=0000");
            //创建SQL查询字符串
            string P_Str_SqlStr = string.Format("SELECT TOP 1000 SI,TI FROM v_tscf_g ");
            //创建数据适配器
            SqlDataAdapter P_SqlDataAdapter = new SqlDataAdapter(P_Str_SqlStr, P_Str_ConnectionStr);
            DataTable P_dt = new DataTable();
            P_SqlDataAdapter.Fill(P_dt);
            return P_dt;
        }

        /// <summary>
        /// 从txt中读取文本数据
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="filename"></param>
        private void readMatrix(double[,] temp, string filename)
        {
            StreamReader sr;
            sr = new StreamReader(filename);
            String line;
            int i = 0;
            while ((line = sr.ReadLine()) != null)
            {

                string[] s1 = line.Trim().Split(',');
                for (int j = 0; j < s1.Length; j++)
                {
                    temp[i, j] = Convert.ToDouble(s1[j]);
                }
                i++;
            }
            sr.Close();
        }

        /// <summary>
        /// 自定义退出按钮，关闭主界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #region 拖动主界面事件的编写
        private void shadowpanel11_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseOff = new Point(-e.X, -e.Y);
                leftFlag = true;
            }
        }

        private void shadowpanel11_MouseMove(object sender, MouseEventArgs e)
        {
            if (leftFlag)
            {
                Point mouseSet = Control.MousePosition;
                mouseSet.Offset(mouseOff.X, mouseOff.Y);
                this.Location = mouseSet;
            }
        }

        private void shadowpanel11_MouseUp(object sender, MouseEventArgs e)
        {
            if (leftFlag)
            {
                leftFlag = false;
            }
        }
        #endregion


        #region 计时初始化
        int CurHour = 0, CurMin = 0, CurSec = 0, count_cycle = 3;
        #endregion

        private void timer1_Tick(object sender, EventArgs e)
        {


            #region 显示系统时间与已运行时间
            CurSec++;
            if (CurSec >= 60 * 20)
            {
                CurMin++;
                CurSec = 0;
            }
            if (CurMin >= 60)
            {
                CurHour++;
                CurMin -= 60;
            }
            if (CurHour >= 24)
            {
                CurHour -= 24;
            }
            textBox9.Text = DateTime.Now.ToString("t");
            textBox10.Text = string.Format("{0}:{1}:{2}", CurHour, CurMin, CurSec / 20);
            #endregion

            #region 用于产生过渡图像
            siga += 0.1;
            if (siga >= 2)
            {
                nextImg += 1;
                if (nextImg > 13)
                    nextImg = 0;
                lastCloudImg = nowlastCloudImg;
                nowlastCloudImg = ArraytoImg(ImgCut(readTempData(TempList[nextImg])));
                siga = 0;
            }
            GetTempImg(lastCloudImg, nowlastCloudImg, siga);
            #endregion

            #region 电池控件动态展示效果
            SetOutputImg(0.6 + 0.4 * Math.Sin(X));
            X += 0.05;
            #endregion
        }

        /// <summary>
        /// 温度云图界面的初始化与鼠标移动事件的初始化
        /// </summary>
        private void initial_2Dsurface()
        {
            initialCreateImg();
            mouseTraceLabel();
        }

        /// <summary>
        /// 显示标签的绑定移动事件
        /// </summary>
        private void mouseTraceLabel()
        {
            _chartTempCloud.BeginUpdate();
            _intensityGrid.MouseTraceCellChanged += new IntensitySeriesBase.MouseTraceCellChangedHandler(_intensityGrid_MouseTraceCellChanged);
            _chartTempCloud.EndUpdate();
        }

        /// <summary>
        /// 显示铁水视频
        /// </summary>
        private void OpenMoltenIron()
        {
            capture_Iron_1 = new Capture("irw1.AVI"); //打开视频、摄像头 
            //int VideoH = (int)CvInvoke.cveVideoCaptureGet(capture_Iron_1, CapProp.FrameHeight);
            //VideoW = (int)CvInvoke.cveVideoCaptureGet(capture_Iron_1, CapProp.FrameWidth);
            //VideoFps = (int)CvInvoke.cveVideoCaptureGet(capture_Iron_1, CapProp.Fps);
            capture_Iron_1.ImageGrabbed += ProcessFrame_Iron;
            capture_Iron_1.Start();
        }

        /// <summary>
        /// 铁水视频播放的绑定触发事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessFrame_Iron(object sender, EventArgs e)
        {
            Mat frame_CLL = new Mat();
            capture_Iron_1.Retrieve(frame_CLL, 0);
            System.Threading.Thread.Sleep((int)(800.0 / 15 - 5));
            imageBox1.Image = frame_CLL;

        }

        /// <summary>
        /// 温度云图的具体初始化细节
        /// </summary>
        private void CreateCloudChart()
        {
            _chartTempCloud = new LightningChartUltimate();

            // Disable rendering, strongly recommended before updating chart properties.
            _chartTempCloud.BeginUpdate();

            _chartTempCloud.ActiveView = ActiveView.ViewXY;
            _chartTempCloud.Parent = lightningChartUltimate4;
            _chartTempCloud.Name = "出铁口铁水温度分布云图";
            _chartTempCloud.Title.Text = "出铁口铁水温度分布云图";
            _chartTempCloud.Dock = DockStyle.Fill;

            // Setup x-axis.
            _chartTempCloud.ViewXY.XAxes[0].ValueType = AxisValueType.Number;
            _chartTempCloud.ViewXY.XAxes[0].ScrollMode = XAxisScrollMode.None;
            _chartTempCloud.ViewXY.XAxes[0].SetRange(0, 100);
            _chartTempCloud.ViewXY.XAxes[0].Visible = false;

            // Setup y-axis.
            _chartTempCloud.ViewXY.YAxes[0].SetRange(0, 100);
            _chartTempCloud.ViewXY.YAxes[0].Visible = false;

            // Setup custom style.
            ExampleUtils.SetDarkFlatStyle(_chartTempCloud);

            // Setup legend box.
            _chartTempCloud.ViewXY.LegendBoxes[0].Layout = LegendBoxLayout.Vertical;
            _chartTempCloud.ViewXY.LegendBoxes[0].Offset = new PointIntXY(-15, -70);

            // Prepare intensity series for data.
            _intensityGrid = new IntensityGridSeries(_chartTempCloud.ViewXY, _chartTempCloud.ViewXY.XAxes[0], _chartTempCloud.ViewXY.YAxes[0]);
            _intensityGrid.ContourLineType = ContourLineTypeXY.None;
            _intensityGrid.Optimization = IntensitySeriesOptimization.DynamicData;
            _intensityGrid.LegendBoxUnits = "°C";
            _intensityGrid.LegendBoxValuesFormat = "0";
            _intensityGrid.Title.Text = "Heat map";
            _intensityGrid.MouseInteraction = false;


            _chartTempCloud.ViewXY.IntensityGridSeries.Add(_intensityGrid);

            //Create mouse tracking point label
            _mouseTrackAnnotation = new AnnotationXY(_chartTempCloud.ViewXY, _chartTempCloud.ViewXY.XAxes[0], _chartTempCloud.ViewXY.YAxes[0]);
            _mouseTrackAnnotation.Fill.Color = Color.FromArgb(0, 30, 30, 30);
            _mouseTrackAnnotation.Fill.GradientFill = GradientFill.Solid;
            _mouseTrackAnnotation.BorderLineStyle.Color = Color.FromArgb(10, Color.White);
            //_mouseTrackAnnotation.Style = AnnotationStyle.RoundedRectangle;


            _mouseTrackAnnotation.Shadow.Visible = false;
            //_mouseTrackAnnotation.TargetCoordinateSystem = AnnotationTargetCoordinates.AxisValues;
            //annotation.Visible = false; //Don't show before the data point has been found
            _mouseTrackAnnotation.TextStyle.Color = Color.White;
            _mouseTrackAnnotation.TextStyle.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
            _mouseTrackAnnotation.Style = AnnotationStyle.Rectangle;

            _mouseTrackAnnotation.LocationCoordinateSystem = CoordinateSystem.RelativeCoordinatesToTarget;
            _mouseTrackAnnotation.LocationRelativeOffset.SetValues(10, 10);
            _mouseTrackAnnotation.Sizing = AnnotationXYSizing.Automatic;
            _mouseTrackAnnotation.Anchor.SetValues(0, 0); //Anchor to Top-left
            _mouseTrackAnnotation.Visible = false;
            _mouseTrackAnnotation.MouseInteraction = false;
            _chartTempCloud.ViewXY.Annotations.Add(_mouseTrackAnnotation);

            _intensityGrid.MouseInteraction = true;

            // Allow chart rendering.
            _chartTempCloud.EndUpdate();

        }

        /// <summary>
        /// 温度云图的调色板设定
        /// </summary>
        private void ApplyFillStyle()
        {
            _chartTempCloud.BeginUpdate();

            _intensityGrid.Fill = IntensityFillStyle.Paletted;
            _intensityGrid.ValueRangePalette = CreatePalette(_intensityGrid, 800, 1600);
            //将调色板隐藏
            _chartTempCloud.ViewXY.LegendBoxes[0].Visible = false;
            _intensityGrid.Visible = true;
            _intensityGrid.ContourLineType = ContourLineTypeXY.None;

            _chartTempCloud.EndUpdate();
        }

        /// <summary>
        /// 设置调色板的颜色区间
        /// </summary>
        /// <param name="ownerSeries"></param>
        /// <param name="valueMin"></param>
        /// <param name="valueMax"></param>
        /// <returns></returns>
        private ValueRangePalette CreatePalette(IntensityGridSeries ownerSeries, double valueMin, double valueMax)
        {
            ValueRangePalette palette = new ValueRangePalette(ownerSeries);
            palette.Steps.Clear();
            ExampleUtils.DisposeAllAndClear(palette.Steps);
            double valueStep = (valueMax - valueMin) / 20.0;
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(0, 17, 190), valueMin));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(0, 17, 190), valueMin + valueStep * 2));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(0, 151, 255), valueMin + valueStep * 3));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(0, 221, 151), valueMin + valueStep * 4));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(34, 236, 85), valueMin + valueStep * 5));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(84, 255, 51), valueMin + valueStep * 6));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(135, 255, 17), valueMin + valueStep * 7));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(169, 255, 0), valueMin + valueStep * 8));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(204, 255, 0), valueMin + valueStep * 9));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(255, 255, 0), valueMin + valueStep * 10));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(255, 204, 0), valueMin + valueStep * 11));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(255, 170, 0), valueMin + valueStep * 12));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(255, 136, 0), valueMin + valueStep * 13));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(255, 92, 0), valueMin + valueStep * 14));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(255, 51, 0), valueMin + valueStep * 15));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(255, 36, 0), valueMin + valueStep * 16));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(255, 56, 56), valueMin + valueStep * 17));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(255, 115, 171), valueMin + valueStep * 18));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(255, 170, 238), valueMin + valueStep * 19));
            palette.Steps.Add(new PaletteStep(palette, Color.FromArgb(255, 220, 255), valueMin + valueStep * 20));
            palette.Type = PaletteType.Gradient;
            palette.MinValue = valueMin;

            return palette;
        }

        /// <summary>
        /// 温度云图上鼠标移动事件触发时应执行的操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mouseX"></param>
        /// <param name="mouseY"></param>
        /// <param name="newCellColumn"></param>
        /// <param name="newCellRow"></param>
        /// <param name="nearestDataColumnIndex"></param>
        /// <param name="nearestDataRowIndex"></param>
        /// <param name="nearestCornerPoint"></param>
        /// <param name="nearestDataColumnCoord"></param>
        /// <param name="nearestDataRowCoord"></param>
        private void _intensityGrid_MouseTraceCellChanged(IntensitySeriesBase sender, int mouseX, int mouseY,
            int newCellColumn, int newCellRow, int nearestDataColumnIndex, int nearestDataRowIndex,
            IntensityPoint nearestCornerPoint,
            float nearestDataColumnCoord, float nearestDataRowCoord)
        {
            _traceCellColumnIndex = newCellColumn;
            _traceCellRowIndex = newCellRow;
            _traceNearestDataColumnIndex = nearestDataColumnIndex;
            _traceNearestDataRowIndex = nearestDataRowIndex;
            _traceNearestDataColumnCoord = nearestDataColumnCoord;
            _traceNearestDataRowCoord = nearestDataRowCoord;
            _nearestCornerPoint = nearestCornerPoint;
            _mouseX = mouseX;
            _mouseY = mouseY;
            UpdateTraceResultNormal();
        }

        /// <summary>
        /// 更新云图跟踪标签
        /// </summary>
        private void UpdateTraceResultNormal()
        {

            _chartTempCloud.BeginUpdate();

            //Move tracing point 
            double xGridStep = (_intensityGrid.RangeMaxX - _intensityGrid.RangeMinX) / (double)(_intensityGrid.SizeX - 1);
            double yGridStep = (_intensityGrid.RangeMaxY - _intensityGrid.RangeMinY) / (double)(_intensityGrid.SizeY - 1);

            double trackX = _intensityGrid.RangeMinX + xGridStep * (double)_traceCellColumnIndex;
            double trackY = _intensityGrid.RangeMinY + yGridStep * (double)_traceCellRowIndex;
            trackX = _nearestCornerPoint.X;
            trackY = _nearestCornerPoint.Y;

            //Show label at position of tracing point
            _mouseTrackAnnotation.Visible = true;
            _mouseTrackAnnotation.TargetAxisValues.X = trackX;
            _mouseTrackAnnotation.TargetAxisValues.Y = trackY;


            double pixelTemp = 0;
            if (_nearestCornerPoint.Value <= 127)
            {
                pixelTemp = _nearestCornerPoint.Value;
            }
            else
            {
                pixelTemp = _nearestCornerPoint.Value;
            }
            if (pixelTemp > 1520)
            {
                _mouseTrackAnnotation.Text = string.Format("温度:{0} \n 铁水区 ", pixelTemp.ToString("0.0"));
            }
            else if (pixelTemp > 1450)
            {
                _mouseTrackAnnotation.Text = string.Format("温度:{0} \n 炉渣区 ", pixelTemp.ToString("0.0"));
            }
            else if (pixelTemp > 1400)
            {
                _mouseTrackAnnotation.Text = string.Format("温度:{0} \n 粉尘区 ", pixelTemp.ToString("0.0"));
            }

            _chartTempCloud.EndUpdate();
        }

        /// <summary>
        /// 读取铁水温度数据
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private double[,] readTempData(string filename)
        {

            //Bitmap bitmap = null;
            string nextLine;
            double[,] intArray = new double[768, 1024];
            int row = 0, col = 0, n = 0;
            try
            {

                StreamReader SR = File.OpenText(filename);

                while ((nextLine = SR.ReadLine()) != null)
                {
                    string[] ss = nextLine.Split(' ');
                    col = ss.Length;
                    for (int i = 0; i < 1024; i++)
                    {
                        intArray[n, i] = Convert.ToDouble(ss[i]);
                    }
                    row++;
                    n++;
                }
                row = 0;
                return intArray;
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// 温度云图初始运行数据给定
        /// </summary>
        private void initialCreateImg()
        {
            lastCloudImg = ArraytoImg(ImgCut(readTempData(TempList[0])));
            nowlastCloudImg = ArraytoImg(ImgCut(readTempData(TempList[1])));
            siga = 0;
            nextImg = 1;
            GetTempImg(lastCloudImg, nowlastCloudImg, siga);
        }

        /// <summary>
        /// 温度数据数组转温度云图、数组转图像
        /// </summary>
        /// <param name="intArray"></param>
        /// <returns></returns>
        private Image<Bgr, byte> ArraytoImg(double[,] intArray)
        {
            Image<Bgr, byte> dst = new Image<Bgr, byte>(894, 206, new Bgr(0, 255, 0));
            double a, b, c, temp;
            for (int i = 0; i < 206; i++)
            {
                for (int j = 0; j < 894; j++)
                {
                    a = dst.Data[i, j, 0];
                    b = dst.Data[i, j, 1];
                    c = dst.Data[i, j, 2];
                    temp = intArray[i, j];
                    dst.Data[i, j, 0] = (byte)temp;
                    dst.Data[i, j, 1] = (byte)temp;
                    dst.Data[i, j, 2] = (byte)temp;
                }
            }
            //demo
            return dst;
        }


        /// <summary>
        /// 第二个定时器、用于更新textbox显示的数据以及更新三个曲线的数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Data_timer_Tick(object sender, EventArgs e)
        {
            double RandKey = (double)ran.Next(0, 99) / 100;
            if (dataTick++ == 998) dataTick = 0;

            #region 更新三个曲线的数据
            if (OutputDataIsReady)
            {
                if (TempCount >= 500)
                {
                    TempCount = 0;
                }
                if (SiCount >= 200)
                {
                    SiCount = 0;
                }
                if (VecCount >= 110)
                {
                    VecCount = 0;
                }

                IronTempDetectLineUpdate(TempData[0, TempCount]);
                SipredLineUpdate(SiData[0, SiCount]);
                IronVelocityDetectLineUpdate(VecData[0, VecCount]);
                TempCount += 1;
                SiCount += 1;
                VecCount += 1;
            }
            #endregion

            //Convert.ToInt32(IronTemper.Rows[dataTick][0].ToString()); string转int类型
            //热电偶温度
            textBox1.Text = IronTemper.Rows[dataTick][0].ToString() + "°C";
            //红外温度
            textBox2.Text = ((int)TempData[0, TempCount]).ToString() + "°C";
            //预测硅含量
            textBox3.Text = ((double)((int)(SiData[0, SiCount] * 1000)) / 1000).ToString();
            //预测钛含量
            textBox4.Text = IronComponent.Rows[dataTick][1].ToString();
            //硅含量可信度
            textBox5.Text = ran.Next(84, 91).ToString() + "%";
            //实际硅含量
            textBox6.Text = (Convert.ToDouble(IronComponent.Rows[dataTick][0].ToString()) + 0.027).ToString();
            //铁水流速
            textBox11.Text = ((double)((int)(VecData[0, VecCount] * 100)) / 100).ToString() + "M/S";
        }

        /// <summary>
        /// 获取中间过渡云图
        /// </summary>
        /// <param name="ImgPre"></param>
        /// <param name="ImgAfter"></param>
        /// <param name="siga"></param>
        private void GetTempImg(Image<Bgr, byte> ImgPre, Image<Bgr, byte> ImgAfter, double siga)
        {

            CvInvoke.AddWeighted(ImgPre, siga, ImgAfter, 1 - siga, 0, ImgChangeOver);
            CreateIntensitySeriesData(ImgChangeOver);
        }


        /// <summary>
        /// 图像的裁剪、ROI的选定
        /// </summary>
        /// <param name="imgCut"></param>
        /// <returns></returns>
        private double[,] ImgCut(double[,] imgCut)
        {
            for (int i = 0; i < 768; i++)
            {
                for (int j = 0; j < 1024; j++)
                {
                    if ((i >= 242) && (i <= 447) && (j >= 131))
                    {
                        imgCut[i - 242, j - 131] = imgCut[i, j];
                    }
                }
            }
            return imgCut;
        }

        /// <summary>
        /// 将图像数据转换为lightningChart中显示数据
        /// </summary>
        /// <param name="imageframe"></param>
        private void CreateIntensitySeriesData(Image<Bgr, byte> imageframe)
        {
            _chartTempCloud.BeginUpdate();
            Bitmap img = new Bitmap(imageframe.Bitmap);
            BitmapAntialiasOptions options = new BitmapAntialiasOptions();
            if (_intensityGrid != null)
            {
                if (_intensityGrid.SetHeightDataFromBitmap(_chartTempCloud.ViewXY.XAxes[0].Minimum, _chartTempCloud.ViewXY.XAxes[0].Maximum,
                    _chartTempCloud.ViewXY.YAxes[0].Minimum, _chartTempCloud.ViewXY.YAxes[0].Maximum, 800, 1600,
                    img, options) == false)
                {

                }
            }
            _chartTempCloud.EndUpdate();
        }

        /// <summary>
        /// 初始化温度曲线
        /// </summary>
        private void CreateTempChart()
        {
            //Create new _chartTemp
            _chartTemp = new LightningChartUltimate();
            _chartTemp.BeginUpdate();
            _chartTemp.ViewXY.DropOldSeriesData = true;

            _chartTemp.Parent = lightningChartUltimate1;
            _chartTemp.Name = "出铁口铁水温度曲线";
            //_chartTemp.Title.Font.;
            _chartTemp.Title.Text = "出铁口铁水温度曲线";
            _chartTemp.Title.Color = PublicColor.FromArgb(255, Color.Yellow);
            _chartTemp.Title.Font = new Font("黑体", 10, FontStyle.Bold);
            //_chartTemp.Title.Visible = false;
            _chartTemp.Dock = DockStyle.Fill;
            _chartTemp.Background.Color = PublicColor.FromArgb(255, Color.Gray);
            _chartTemp.Background.GradientFill = GradientFill.Solid;

            AxisX xAxis = _chartTemp.ViewXY.XAxes[0];
            xAxis.ValueType = AxisValueType.DateTime;
            xAxis.Title.Text = "Time";
            xAxis.AutoFormatLabels = false;

            //xAxis.LabelsTimeFormat = "dd/MM/yyyy\nHH:mm.ss";
            xAxis.LabelsTimeFormat = "HH:mm.ss";
            xAxis.LabelsAngle = 0;
            xAxis.ScrollMode = XAxisScrollMode.Scrolling;
            xAxis.Title.Visible = false;
            xAxis.Visible = false;

            //Convert DateTime values to axis values
            DateTime now = DateTime.Now;
            double minX = xAxis.DateTimeToAxisValue(now);
            double maxX = xAxis.DateTimeToAxisValue(now) + 100;
            xAxis.SetRange(minX, maxX);

            //Configure y-axis
            AxisY yAxis = _chartTemp.ViewXY.YAxes[0];
            yAxis.Title.Text = "Temperature / °C";
            yAxis.Title.Visible = false;
            yAxis.SetRange(1500, 1600);
            yAxis.LabelsFont = new Font("黑体", 6);
            yAxis.LabelsColor = PublicColor.FromArgb(255, Color.Yellow);

            //Configure and add series to the chart
            PointLineSeries series = new PointLineSeries(_chartTemp.ViewXY, xAxis, yAxis);
            series.LineStyle.Color = Color.Yellow;
            series.MouseInteraction = false;

            _chartTemp.ViewXY.PointLineSeries.Add(series);

            //Don't show legendbox
            _chartTemp.ViewXY.LegendBoxes[0].Visible = false;

            //Allow chart rendering
            _chartTemp.EndUpdate();


        }

        /// <summary>
        /// 初始化硅含量曲线
        /// </summary>
        private void CreateSiChart()
        {
            //Create new _chartTemp

            _chartSi = new LightningChartUltimate();
            //Disable rendering, strongly recommended before updating chart properties
            _chartSi.BeginUpdate();

            //Reduce memory usage and increase performance. Destroys out-scrolled data. 
            _chartSi.ViewXY.DropOldSeriesData = true;

            _chartSi.Parent = lightningChartUltimate2;
            _chartSi.Name = "Si prediction";
            _chartSi.Title.Text = "硅含量预测曲线";
            _chartSi.Title.Color = PublicColor.FromArgb(255, Color.Yellow);
            _chartSi.Title.Font = new Font("黑体", 10, FontStyle.Bold);
            _chartSi.Dock = DockStyle.Fill;

            // Setup custom style.
            //ExampleUtils.SetStandardFlatStyle(_chartSi);
            _chartSi.Background.Color = PublicColor.FromArgb(255, Color.Gray);
            _chartSi.Background.GradientFill = GradientFill.Solid;
            //Configure x-axis
            AxisX xAxis = _chartSi.ViewXY.XAxes[0];
            xAxis.ValueType = AxisValueType.DateTime;
            xAxis.Title.Text = "Time";
            xAxis.AutoFormatLabels = false;
            //xAxis.LabelsTimeFormat = "dd/MM/yyyy\nHH:mm.ss";
            xAxis.LabelsTimeFormat = "HH:mm.ss";
            xAxis.LabelsAngle = 0;
            xAxis.ScrollMode = XAxisScrollMode.Scrolling;
            xAxis.Visible = false;
            xAxis.Title.Visible = false;

            //Convert DateTime values to axis values
            DateTime now = DateTime.Now;
            double minX = xAxis.DateTimeToAxisValue(now);
            double maxX = xAxis.DateTimeToAxisValue(now) + 100;
            xAxis.SetRange(minX, maxX);

            //Configure y-axis
            AxisY yAxis = _chartSi.ViewXY.YAxes[0];
            yAxis.Title.Text = "Si Predition";
            yAxis.SetRange(0, 1);
            yAxis.LabelsFont = new Font("黑体", 6);
            yAxis.LabelsColor = PublicColor.FromArgb(255, Color.Yellow);
            yAxis.Title.Visible = false;

            //Configure and add series to the chart
            PointLineSeries series = new PointLineSeries(_chartSi.ViewXY, xAxis, yAxis);
            series.LineStyle.Color = Color.Yellow;
            series.MouseInteraction = false;
            _chartSi.ViewXY.PointLineSeries.Add(series);

            //Don't show legendbox
            _chartSi.ViewXY.LegendBoxes[0].Visible = false;

            //Allow chart rendering
            _chartSi.EndUpdate();

        }

        /// <summary>
        /// 初始化速度曲线
        /// </summary>
        private void CreateVelocityChart()
        {
            //Create new _chartTemp

            _chartVelocity = new LightningChartUltimate();
            //Disable rendering, strongly recommended before updating chart properties
            _chartVelocity.BeginUpdate();

            //Reduce memory usage and increase performance. Destroys out-scrolled data. 
            _chartVelocity.ViewXY.DropOldSeriesData = true;

            _chartVelocity.Parent = lightningChartUltimate3;
            _chartVelocity.Name = "Si prediction";
            _chartVelocity.Title.Text = "铁水实时流速曲线";
            _chartVelocity.Title.Color = PublicColor.FromArgb(255, Color.Yellow);
            _chartVelocity.Title.Font = new Font("黑体", 10, FontStyle.Bold);
            _chartVelocity.Dock = DockStyle.Fill;

            // Setup custom style.
            //ExampleUtils.SetStandardFlatStyle(_chartSi);
            _chartVelocity.Background.Color = PublicColor.FromArgb(255, Color.Gray);
            _chartVelocity.Background.GradientFill = GradientFill.Solid;
            //Configure x-axis
            AxisX xAxis = _chartVelocity.ViewXY.XAxes[0];
            xAxis.ValueType = AxisValueType.DateTime;
            xAxis.Title.Text = "Time";
            xAxis.AutoFormatLabels = false;
            //xAxis.LabelsTimeFormat = "dd/MM/yyyy\nHH:mm.ss";
            xAxis.LabelsTimeFormat = "HH:mm.ss";
            xAxis.LabelsAngle = 0;
            xAxis.ScrollMode = XAxisScrollMode.Scrolling;
            xAxis.LabelsFont = new Font("黑体", 6);
            xAxis.LabelsColor = PublicColor.FromArgb(255, Color.Yellow);
            xAxis.Visible = false;
            xAxis.Title.Visible = false;

            //Convert DateTime values to axis values
            DateTime now = DateTime.Now;
            double minX = xAxis.DateTimeToAxisValue(now);
            double maxX = xAxis.DateTimeToAxisValue(now) + 200;
            xAxis.SetRange(minX, maxX);

            //Configure y-axis
            AxisY yAxis = _chartVelocity.ViewXY.YAxes[0];
            yAxis.Title.Text = "Si Predition";
            yAxis.SetRange(4.5, 8);
            yAxis.LabelsFont = new Font("黑体", 6);
            yAxis.LabelsColor = PublicColor.FromArgb(255, Color.Yellow);
            yAxis.Title.Visible = false;

            //Configure and add series to the chart
            PointLineSeries series = new PointLineSeries(_chartVelocity.ViewXY, xAxis, yAxis);
            series.LineStyle.Color = Color.Yellow;
            series.MouseInteraction = false;
            _chartVelocity.ViewXY.PointLineSeries.Add(series);

            //Don't show legendbox
            _chartVelocity.ViewXY.LegendBoxes[0].Visible = false;

            //Allow chart rendering
            _chartVelocity.EndUpdate();

        }


        /// <summary>
        /// 温度曲线更新
        /// </summary>
        /// <param name="Nowtemp"></param>
        private void IronTempDetectLineUpdate(double Nowtemp)
        {
            //**************************************************
            //***********************温度数据折线图*************
            if (_chartTemp == null)
                return;

            //Disable updates, to prevent several extra refreshes
            _chartTemp.BeginUpdate();

            //Array for 1 point
            SeriesPoint[] points = new SeriesPoint[1];

            //Convert 'Now' to X value 
            _previousX = _chartTemp.ViewXY.XAxes[0].DateTimeToAxisValue(DateTime.Now);

            //Store the X value
            points[0].X = _previousX;

            //Randomize and store Y value 
            points[0].Y = Nowtemp;
            //points[0].Y = ;

            //Add the new point into end of first PointLineSeries
            _chartTemp.ViewXY.PointLineSeries[0].AddPoints(points, false);

            //_chartTemp.Visible = false;

            //Set real-time monitoring scroll position, to latest X point. 
            //ScrollPosition indicates the position where monitoring is currently progressing. 
            _chartTemp.ViewXY.XAxes[0].ScrollPosition = _previousX;

            //Allow updates again, and update
            _chartTemp.EndUpdate();
        }

        /// <summary>
        /// 硅含量数据更新
        /// </summary>
        /// <param name="Sipred"></param>
        private void SipredLineUpdate(double Sipred)
        {
            //**************************************************
            //***********************Si预测数据折线图*************
            if (_chartSi == null)
                return;

            //Disable updates, to prevent several extra refreshes
            _chartSi.BeginUpdate();

            //Array for 1 point
            SeriesPoint[] points = new SeriesPoint[1];

            //Convert 'Now' to X value 
            _previousX = _chartSi.ViewXY.XAxes[0].DateTimeToAxisValue(DateTime.Now);

            //Store the X value
            points[0].X = _previousX;

            //Randomize and store Y value 
            points[0].Y = Sipred;

            //Add the new point into end of first PointLineSeries
            _chartSi.ViewXY.PointLineSeries[0].AddPoints(points, false);

            //Set real-time monitoring scroll position, to latest X point. 
            //ScrollPosition indicates the position where monitoring is currently progressing. 
            _chartSi.ViewXY.XAxes[0].ScrollPosition = _previousX;

            //Allow updates again, and update
            _chartSi.EndUpdate();
        }

        /// <summary>
        /// 流速曲线数据更新
        /// </summary>
        /// <param name="NowVec"></param>
        private void IronVelocityDetectLineUpdate(double NowVec)
        {
            //**************************************************
            //***********************流速数据折线图*************
            if (_chartVelocity == null)
                return;

            //Disable updates, to prevent several extra refreshes
            _chartVelocity.BeginUpdate();

            //Array for 1 point
            SeriesPoint[] points = new SeriesPoint[1];

            //Convert 'Now' to X value 
            _previousX = _chartVelocity.ViewXY.XAxes[0].DateTimeToAxisValue(DateTime.Now);

            //Store the X value
            points[0].X = _previousX;

            //Randomize and store Y value 
            points[0].Y = NowVec;
            //points[0].Y = ;

            //Add the new point into end of first PointLineSeries
            _chartVelocity.ViewXY.PointLineSeries[0].AddPoints(points, false);

            //_chartTemp.Visible = false;

            //Set real-time monitoring scroll position, to latest X point. 
            //ScrollPosition indicates the position where monitoring is currently progressing. 
            _chartVelocity.ViewXY.XAxes[0].ScrollPosition = _previousX;

            //Allow updates again, and update
            _chartVelocity.EndUpdate();
        }

        /// <summary>
        /// 创建三维饼图
        /// </summary>
        private void CreatePie3D()
        {
            _chartPie3D = new LightningChartUltimate();

            // Disable rendering, strongly recommended before updating chart properties.
            _chartPie3D.BeginUpdate();

            // Change active view to Pie3D view.
            _chartPie3D.ActiveView = ActiveView.ViewPie3D;

            _chartPie3D.Parent = panel1;
            _chartPie3D.Dock = DockStyle.Fill;
            _chartPie3D.Name = "Pie3D chart";
            _chartPie3D.Title.Text = "预警模块";

            // Configure background.
            _chartPie3D.Background.GradientFill = GradientFill.Radial;
            _chartPie3D.Background.GradientColor = Color.Black;

            // Configure 3D pie view.
            _chartPie3D.ViewPie3D.Style = PieStyle3D.Pie;
            _chartPie3D.ViewPie3D.Rounding = 40; // Set pie rounding.


            // Configure legend.
            _chartPie3D.ViewPie3D.LegendBox3DPie.Layout = LegendBoxLayout.HorizontalRowSpan;
            _chartPie3D.ViewPie3D.LegendBox3DPie.Position = LegendBoxPosition.BottomCenter;
            _chartPie3D.ViewPie3D.LegendBox3DPie.Visible = false;

            // Add pie slice data.
            // By using TRUE as a last parameter, the slice will be automatically added to chart.ViewPie3D.Values collection.
            PieSlice slice1 = new PieSlice("铁水温度", Color.FromArgb(150, 255, 255, 0), 8, _chartPie3D.ViewPie3D, true);
            slice1.Title.Font = new Font("黑体", 10);
            PieSlice slice2 = new PieSlice("铁水硅含量", Color.FromArgb(150, 0, 0, 255), 6, _chartPie3D.ViewPie3D, true);
            slice2.Title.Font = new Font("黑体", 10);
            PieSlice slice3 = new PieSlice("铁水流速", Color.FromArgb(150, 0, 255, 255), 5, _chartPie3D.ViewPie3D, true);
            slice3.Title.Font = new Font("黑体", 10);
            PieSlice slice4 = new PieSlice("铁水出铁量", Color.FromArgb(150, 255, 0, 255), 2, _chartPie3D.ViewPie3D, true);
            slice4.Title.Font = new Font("黑体", 10); //slice1.Title.Color = PublicColor.FromArgb(255, Color.Black);

            _chartPie3D.EndUpdate();

        }
        /// <summary>
        /// 基础图片加载及用于填补的transparentLine与innerLine初始化
        /// </summary>
        private void GetBaseLine()
        {
            Image<Bgra, byte> tmpImg = new Image<Bgra, byte>(IconPath + "transparentShell.png");
            transparentLine = new Image<Bgra, byte>(tmpImg.Width, 1);
            for (int i = 0; i < tmpImg.Width; i++)
            {
                transparentLine.Data[0, i, 0] = tmpImg.Data[150, i, 0];
                transparentLine.Data[0, i, 1] = tmpImg.Data[150, i, 1];
                transparentLine.Data[0, i, 2] = tmpImg.Data[150, i, 2];
                transparentLine.Data[0, i, 3] = tmpImg.Data[150, i, 3];
            }
            Image<Bgra, byte> tmp_inner = new Image<Bgra, byte>(IconPath + "WithInnerFlow.png");
            innerLine = new Image<Bgra, byte>(tmp_inner.Width, 1);
            for (int i = 0; i < tmp_inner.Width; i++)
            {
                innerLine.Data[0, i, 0] = tmp_inner.Data[150, i, 0];
                innerLine.Data[0, i, 1] = tmp_inner.Data[150, i, 1];
                innerLine.Data[0, i, 2] = tmp_inner.Data[150, i, 2];
                innerLine.Data[0, i, 3] = tmp_inner.Data[150, i, 3];
            }
        }


        /// <summary>
        /// 电池控件输出图片接口函数，por为铁渣比
        /// </summary>
        /// <param name="por"></param>
        private void SetOutputImg(double por)
        {
            tmp = new Image<Bgra, byte>(battery.Width, battery.Height);//初始化输出图像
            int vertTranslation = (int)((326 - 135) * (1 - por));//由渣铁比换算到电池内部上底面偏移像素值
            if (por == 0 || por > 1 || por < 0)
                imageBox2.Image = battery;
            else
            {
                #region 输出图像重组及显示
                //图片大小为401*200，从0至89为电池外壳原图像像素
                for (int i = 0; i < 90; i++)
                {
                    for (int j = 0; j < 200; j++)
                    {
                        tmp.Data[i, j, 0] = battery.Data[i, j, 0];
                        tmp.Data[i, j, 1] = battery.Data[i, j, 1];
                        tmp.Data[i, j, 2] = battery.Data[i, j, 2];
                        tmp.Data[i, j, 3] = battery.Data[i, j, 3];
                    }
                }
                //从90至90+vertTranslation-1为插入的透明外壳像素
                for (int i = 0; i < vertTranslation; i++)
                {
                    for (int j = 0; j < 200; j++)
                    {
                        tmp.Data[90 + i, j, 0] = transparentLine.Data[0, j, 0];
                        tmp.Data[90 + i, j, 1] = transparentLine.Data[0, j, 1];
                        tmp.Data[90 + i, j, 2] = transparentLine.Data[0, j, 2];
                        tmp.Data[90 + i, j, 3] = transparentLine.Data[0, j, 3];
                    }
                }
                //从90+vertTranslation至90+vertTranslation+45为电池内部上底面原图像
                for (int i = 0; i < 46; i++)
                {
                    for (int j = 0; j < 200; j++)
                    {
                        tmp.Data[90 + vertTranslation + i, j, 0] = battery.Data[90 + i, j, 0];
                        tmp.Data[90 + vertTranslation + i, j, 1] = battery.Data[90 + i, j, 1];
                        tmp.Data[90 + vertTranslation + i, j, 2] = battery.Data[90 + i, j, 2];
                        tmp.Data[90 + vertTranslation + i, j, 3] = battery.Data[90 + i, j, 3];
                    }
                }
                //从90+vertTranslation+46至400为电池外壳原图像像素
                for (int i = 136 + vertTranslation; i < battery.Height; i++)
                {
                    for (int j = 0; j < 200; j++)
                    {
                        tmp.Data[i, j, 0] = battery.Data[i, j, 0];
                        tmp.Data[i, j, 1] = battery.Data[i, j, 1];
                        tmp.Data[i, j, 2] = battery.Data[i, j, 2];
                        tmp.Data[i, j, 3] = battery.Data[i, j, 3];
                    }
                }
                imageBox2.Image = null;
                imageBox2.Image = tmp;
                #endregion
            }
        }
    }
}
