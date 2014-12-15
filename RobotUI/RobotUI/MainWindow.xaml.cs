﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Robot
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        //运行数据对象
        private StaticValue m_staticValue;
        private StatusValue m_statusValue;
        private DoubleAnimation m_daMove;
        private DispatcherTimer m_dispatcherTimer;
        private int m_nextpos = 0;
        private int m_nowpos = 0;
        private Network network;
        public MainWindow()
        {
            InitializeComponent();
            //初始化Encoder
            int res = Encoder.Encoder_StartUp();
            //初始化通讯模块
            network = new Network();
            network.Connect("127.0.0.1", 60000, 1);
            //创建屏幕相关静态数据对象
            m_staticValue = new StaticValue();
            BindCanvas();
            BindConveyor();
            //创建运行状态数据对象
            m_statusValue = new StatusValue();
            BindButton();
        }
        private void BindButton()
        {
            Button_Start.SetBinding(Button.ContentProperty, new Binding("WorkStatus") { Source = m_statusValue, Converter = new WorkStatusToButtonStartContentPathConverter(), Mode = BindingMode.OneWay });
            Button_Start.SetBinding(Button.IsEnabledProperty, new Binding("WorkStatus") { Source = m_statusValue, Converter = new WorkStatusToButtonStartIsEnablePathConverter(), Mode = BindingMode.OneWay });
            Button_Suspend.SetBinding(Button.ContentProperty, new Binding("WorkStatus") { Source = m_statusValue, Converter = new WorkStatusToButtonSuspendContentPathConverter(), Mode = BindingMode.OneWay });
            Button_Suspend.SetBinding(Button.IsEnabledProperty, new Binding("WorkStatus") { Source = m_statusValue, Converter = new WorkStatusToButtonSuspendIsEnablePathConverter(), Mode = BindingMode.OneWay });
        }
        private void BindCanvas()
        {
            MultiBinding MBCanvasX = new MultiBinding() { Converter = new DoubleScreenRateZoomRateConverter() };
            MBCanvasX.Bindings.Add(new Binding("CanvasX") { Source = m_staticValue });
            MBCanvasX.Bindings.Add(new Binding("ScreenRate") { Source = m_staticValue });
            MBCanvasX.Bindings.Add(new Binding("ZoomRate") { Source = m_staticValue });
            canvas2D.SetBinding(Canvas.WidthProperty, MBCanvasX);
            MultiBinding MBCanvasY = new MultiBinding() { Converter = new DoubleScreenRateZoomRateConverter() };
            MBCanvasY.Bindings.Add(new Binding("CanvasY") { Source = m_staticValue });
            MBCanvasY.Bindings.Add(new Binding("ScreenRate") { Source = m_staticValue });
            MBCanvasY.Bindings.Add(new Binding("ZoomRate") { Source = m_staticValue });
            canvas2D.SetBinding(Canvas.HeightProperty, MBCanvasY);
        }
        private void BindConveyor()
        {
            MultiBinding MBStuffConveyorX = new MultiBinding() { Converter = new DoubleScreenRateZoomRateConverter() };
            MBStuffConveyorX.Bindings.Add(new Binding("StuffConveyorLength") { Source = m_staticValue });
            MBStuffConveyorX.Bindings.Add(new Binding("ScreenRate") { Source = m_staticValue });
            MBStuffConveyorX.Bindings.Add(new Binding("ZoomRate") { Source = m_staticValue });
            stuffConveyor.SetBinding(Rectangle.WidthProperty, MBStuffConveyorX);
            MultiBinding MBStuffConveyorY = new MultiBinding() { Converter = new DoubleScreenRateZoomRateConverter() };
            MBStuffConveyorY.Bindings.Add(new Binding("StuffConveyorWidth") { Source = m_staticValue });
            MBStuffConveyorY.Bindings.Add(new Binding("ScreenRate") { Source = m_staticValue });
            MBStuffConveyorY.Bindings.Add(new Binding("ZoomRate") { Source = m_staticValue });
            stuffConveyor.SetBinding(Rectangle.HeightProperty, MBStuffConveyorY);
            MultiBinding MBBoxfConveyorX = new MultiBinding() { Converter = new DoubleScreenRateZoomRateConverter() };
            MBBoxfConveyorX.Bindings.Add(new Binding("BoxConveyorLength") { Source = m_staticValue });
            MBBoxfConveyorX.Bindings.Add(new Binding("ScreenRate") { Source = m_staticValue });
            MBBoxfConveyorX.Bindings.Add(new Binding("ZoomRate") { Source = m_staticValue });
            boxConveyor.SetBinding(Rectangle.WidthProperty, MBBoxfConveyorX);
            MultiBinding MBBoxfConveyorY = new MultiBinding() { Converter = new DoubleScreenRateZoomRateConverter() };
            MBBoxfConveyorY.Bindings.Add(new Binding("BoxConveyorWidth") { Source = m_staticValue });
            MBBoxfConveyorY.Bindings.Add(new Binding("ScreenRate") { Source = m_staticValue });
            MBBoxfConveyorY.Bindings.Add(new Binding("ZoomRate") { Source = m_staticValue });
            boxConveyor.SetBinding(Rectangle.HeightProperty, MBBoxfConveyorY);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            m_daMove = new DoubleAnimation();
            m_daMove.Duration = new Duration(TimeSpan.FromMilliseconds(m_staticValue.RefreshTime));

            m_dispatcherTimer = new DispatcherTimer();
            m_dispatcherTimer.Tick += new EventHandler(OnTimedEvent);
            m_dispatcherTimer.Interval = TimeSpan.FromMilliseconds(m_staticValue.RefreshTime);
            m_dispatcherTimer.Start();
        }
        private void OnTimedEvent(object sender, EventArgs e)
        {
            ///读编码器值
//             int value = 0;
//             Encoder.Encoder_Read(0, ref value);
//             m_nextpos += value;
            //模拟运输传送
            TargetListController.GenerateNewTarget(m_nowpos);
            //扫描并显示新的Target图形
            TargetListController.Draw(DrawTarget);
            //移动Targets
            TargetListController.Move(MoveTarget);
            m_nowpos = m_nextpos;
        }
        private bool MoveTarget(int index)
        {
            TargetUI targetUI;
            TargetListController.GetDisplayTarget(index, out targetUI);
            if ((m_nextpos - targetUI.Target.EncoderValue) * m_staticValue.StuffEncoderRate > m_staticValue.StuffConveyorLength - m_staticValue.TargetRadius //超出画布右侧
                || (m_nextpos - targetUI.Target.EncoderValue) < 0)//超出画布左侧
            {
                canvas2D.Children.Remove(targetUI.UIElement);
                TargetListController.DelDisplayTarget(index);
                return false;
            }
            m_daMove.By = (m_nextpos - m_nowpos) * m_staticValue.StuffEncoderRate / m_staticValue.ScreenRate * m_staticValue.ZoomRate;
            targetUI.TT.BeginAnimation(TranslateTransform.XProperty, m_daMove);
            return true;
        }
        private bool DrawTarget(int index)
        {
            TargetUI targetUI;
            TargetListController.GetNewTarget(index, out targetUI);
            Ellipse eNewTarget = new Ellipse();
            //设置大小
            BindTarget(eNewTarget);
            //设置颜色
            SolidColorBrush mySolidColorBrush = new SolidColorBrush();
            mySolidColorBrush.Color = Color.FromArgb(255, 100, 149, 237);
            eNewTarget.Fill = mySolidColorBrush;
            //设置在画布的位置
            canvas2D.Children.Add(eNewTarget);
            Canvas.SetLeft(eNewTarget, targetUI.Target.PosX / m_staticValue.ScreenRate * m_staticValue.ZoomRate);
            Canvas.SetTop(eNewTarget, targetUI.Target.PosY / m_staticValue.ScreenRate * m_staticValue.ZoomRate + 10);
            //设置呈现变形
            eNewTarget.RenderTransform = targetUI.TT;
            //关联target实例
            targetUI.UIElement = eNewTarget;
            return true;
        }
        private void BindTarget(Ellipse eNewTarget)
        {
            MultiBinding MBTargetR = new MultiBinding() { Converter = new DoubleScreenRateZoomRateConverter() };
            MBTargetR.Bindings.Add(new Binding("TargetRadius") { Source = m_staticValue });
            MBTargetR.Bindings.Add(new Binding("ScreenRate") { Source = m_staticValue });
            MBTargetR.Bindings.Add(new Binding("ZoomRate") { Source = m_staticValue });
            eNewTarget.SetBinding(Ellipse.WidthProperty, MBTargetR);
            eNewTarget.SetBinding(Ellipse.HeightProperty, MBTargetR);
        }
        private void mainWindow_Closed(object sender, EventArgs e)
        {
            /*            EncoderReader.Shutdown();*/
        }
        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            if (m_statusValue.WorkStatus == enWorkStatus.STOPED) m_statusValue.WorkStatus = enWorkStatus.STARTED;
            else if (m_statusValue.WorkStatus == enWorkStatus.STARTED) m_statusValue.WorkStatus = enWorkStatus.STOPED;
        }
        private void Button_Suspend_Click(object sender, RoutedEventArgs e)
        {
            if (m_statusValue.WorkStatus == enWorkStatus.SUSPEND) m_statusValue.WorkStatus = enWorkStatus.STARTED;
            else if (m_statusValue.WorkStatus == enWorkStatus.STARTED) m_statusValue.WorkStatus = enWorkStatus.SUSPEND;
        }
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Target[] targets = new Target[1];
            targets[0].Aangle = 12.1F;
            targets[0].EncoderValue = 213234;
            targets[0].ID = 1;
            targets[0].PosX = 12.4;
            targets[0].PosY = 123.22;
            network.SendTargets(1, targets, 1);
        }
    }
}