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
using System.Windows.Shapes;
using System.Media;
using System.Threading;

namespace Avtech
{
    /// <summary>
    /// Logica di interazione per Notification.xaml
    /// </summary>
    public partial class Notification : Window
    {
        /// <summary>
        /// Istanza per recuperare la schermata principale
        /// </summary>
        private MainWindow Window;

        public Notification(MainWindow Window)
        {
            InitializeComponent();
            this.Window = Window;
            Canvas_Textblock_Time_Notification.Text = DateTime.Now.ToString("HH:mm");
            this.Left = SystemParameters.WorkArea.Right - this.Width - 18;
            this.Top = SystemParameters.WorkArea.Bottom - this.Height - 11;
            SystemSounds.Exclamation.Play();
        }

        private void Canvas_Border_Open_Notification_MouseEnter(object sender, MouseEventArgs e)
        {
            Canvas_Border_Open_Notification.BorderBrush = (Brush)Window.Brush_Convert.ConvertFrom("#8C8C8C");
        }

        private void Canvas_Border_Open_Notification_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas_Border_Open_Notification.BorderBrush = (Brush)Window.Brush_Convert.ConvertFrom("#FF282828");
        }

        private void Open(byte Grid)
        {
            switch (Window.Win_Closed)
            {
                case false:

                    switch (Window.WindowState)
                    {
                        case WindowState.Minimized:
                            Window.WindowState = WindowState.Normal;
                            break;
                    }

                    switch(Grid)
                    {
                        case 0: //Attack
                            switch (Window.Attack_Side_Bar)
                            {
                                case true:
                                    Window.Canvas_Attack_Side_Bar_MouseLeftButtonDown(Window, null);
                                    break;
                            }
                            break;
                        case 1: //Settings
                            switch (Window.Settings_Side_Bar)
                            {
                                case true:
                                    Window.Canvas_Settings_Side_Bar_MouseLeftButtonDown(Window, null);
                                    break;
                            }
                            break;
                    }

                    Window.Show();
                    Window.Activate();
                    break;
            }
            this.Close();
        }
        private void Canvas_Border_Open_Notification_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Open(0);
        }

        private void Canvas_Close_Notification_MouseEnter(object sender, MouseEventArgs e)
        {
            Canvas_Line_A_Close_Notification.Stroke = (Brush)Window.Brush_Convert.ConvertFrom("#ffffffff");
            Canvas_Line_B_Close_Notification.Stroke = (Brush)Window.Brush_Convert.ConvertFrom("#ffffffff");
        }

        private void Canvas_Close_Notification_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas_Line_A_Close_Notification.Stroke = (Brush)Window.Brush_Convert.ConvertFrom("#ff8c8c8c");
            Canvas_Line_B_Close_Notification.Stroke = (Brush)Window.Brush_Convert.ConvertFrom("#ff8c8c8c");
        }

        private void Canvas_Close_Notification_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Canvas_Collapse_Notification_MouseEnter(object sender, MouseEventArgs e)
        {
            Canvas_Line_A_Collapse_Notification.Stroke = (Brush)Window.Brush_Convert.ConvertFrom("#ffffffff");
            Canvas_Line_B_Collapse_Notification.Stroke = (Brush)Window.Brush_Convert.ConvertFrom("#ffffffff");
        }

        private void Canvas_Collapse_Notification_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas_Line_A_Collapse_Notification.Stroke = (Brush)Window.Brush_Convert.ConvertFrom("#ff8c8c8c");
            Canvas_Line_B_Collapse_Notification.Stroke = (Brush)Window.Brush_Convert.ConvertFrom("#ff8c8c8c");
        }

        /// <summary>
        /// Campo per capire se la notifica è ridimensionata
        /// </summary>
        private bool Collapsed = false;
        private void Canvas_Collapse_Notification_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (Collapsed)
            {
                case false:
                    Collapsed = true;
                    Window_Notification.Height = 102;
                    Canvas_Line_A_Collapse_Notification.Y1 = 4;
                    Canvas_Line_A_Collapse_Notification.Y2 = 0;
                    Canvas_Line_B_Collapse_Notification.Y1 = 4;
                    Canvas_Line_B_Collapse_Notification.Y2 = 0;
                    Canvas.SetTop(Canvas_Textblock_Time_Notification, 65);
                    Canvas_Textblock_Message_Notification.Visibility = Visibility.Hidden;
                    break;
                case true:
                    Collapsed = false;
                    Window_Notification.Height = 170;
                    Canvas_Line_A_Collapse_Notification.Y1 = 0;
                    Canvas_Line_A_Collapse_Notification.Y2 = 4;
                    Canvas_Line_B_Collapse_Notification.Y1 = 0;
                    Canvas_Line_B_Collapse_Notification.Y2 = 4;
                    Canvas.SetTop(Canvas_Textblock_Time_Notification, 84);
                    Canvas_Textblock_Message_Notification.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Canvas_Settings_Notification_MouseEnter(object sender, MouseEventArgs e)
        {
            Canvas_Line_A_Settings_Notification.Stroke = (Brush)Window.Brush_Convert.ConvertFrom("#ffffffff");
            Canvas_Line_B_Settings_Notification.Stroke = (Brush)Window.Brush_Convert.ConvertFrom("#ffffffff");
        }

        private void Canvas_Settings_Notification_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas_Line_A_Settings_Notification.Stroke = (Brush)Window.Brush_Convert.ConvertFrom("#ff8c8c8c");
            Canvas_Line_B_Settings_Notification.Stroke = (Brush)Window.Brush_Convert.ConvertFrom("#ff8c8c8c");
        }

        private void Canvas_Settings_Notification_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Open(1);
        }

        private void Window_Notification_MouseEnter(object sender, MouseEventArgs e)
        {
            Canvas_Close_Notification.Visibility = Visibility.Visible;
            Canvas_Settings_Notification.Visibility = Visibility.Visible;
        }

        private void Window_Notification_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas_Close_Notification.Visibility = Visibility.Hidden;
            Canvas_Settings_Notification.Visibility = Visibility.Hidden;
        }

    }
}
