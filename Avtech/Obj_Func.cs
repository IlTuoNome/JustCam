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
using System.Threading;
using System.Xml;
using Avtechlib;
using Avtechlib.Avtechinfo;
using System.IO;
using System.Data;
using System.Windows.Interop;
using Microsoft.Win32;
using System.Runtime.CompilerServices;

namespace Avtech
{
    public partial class MainWindow
    {
        /// <summary>
        /// Istanza per le scansioni e attacco
        /// </summary>
        private Avtechlib.Avtech Avtech = new Avtechlib.Avtech();
        /// <summary>
        /// Istanza per il cambio di colori della UI
        /// </summary>
        internal BrushConverter Brush_Convert = new BrushConverter();
        /// <summary>
        /// Istanza per la lettura e scrittura del file xml
        /// </summary>
        private XmlDocument Xml_Base = new XmlDocument();
        /// <summary>
        /// Istanza per contenere gli indirizzi ip per l'attacco
        /// </summary>
        private List<string> Ips_List = new List<string>();
        /// <summary>
        /// Istanze dei datatable per le UI
        /// </summary>
        private DataTable DataTable_LOGS = new DataTable();
        private DataTable DataTable_IPS = new DataTable();
        private DataTable DataTable_CRED = new DataTable();

        //Istanza per la notifica, consente di capire se la prima schermata è stata chiusa
        internal bool Win_Closed = false;

        //Eventi base
        #region Eventi Base
        /// <summary>
        /// Metodo per l'evento Loaded per caricare/istanziare le informazioni a inizio programma
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Settaggio eventi avtech
            Avtech.Scan_Terminato += Avtech_Scan_Terminato;
            Avtech.Attack_Terminato += Avtech_Attack_Terminato;
            Avtech.Info_Tested += Avtech_Info_Tested;

            //Settaggio della tabella Logs
            DataTable_LOGS.Columns.Add("ID", typeof(int));
            DataTable_LOGS.Columns.Add("Type", typeof(string));
            DataTable_LOGS.Columns.Add("Date", typeof(string));
            DataTable_LOGS.Columns.Add("Location", typeof(string));
            Log_Update();

            //Settaggio della tabella IPs
            DataTable_IPS.Columns.Add("IP Count", typeof(int));
            DataTable_IPS.Columns.Add("IP Port", typeof(string));

            //Settaggio della tabella delle Credenziali
            DataTable_CRED.Columns.Add("Username", typeof(string));
            DataTable_CRED.Columns.Add("Password", typeof(string));
            DataTable_CRED.Columns.Add("IP Count", typeof(int));
            DataTable_CRED.Columns.Add("IP Port", typeof(string));

            //Settaggio cursore threads settings
            Border_Textbox_Thread_Settings.CaretIndex = Border_Textbox_Thread_Settings.Text.Length;

            /*Apertura e lettura del file settings xml
             In caso di errore nel caricamento del file o
             contenuto non valido viene invocato il metodo 
             Xml_File per la ricreazione e lettura del file.
             */
            try
            {
                Xml_Base.Load(AppDomain.CurrentDomain.BaseDirectory + "\\Settings.xml");
                XmlNode Xml_Api_Node = Xml_Base.SelectSingleNode("/Settings/Api");
                XmlNode Xml_Not_Node = Xml_Base.SelectSingleNode("/Settings/Notifications");


                if ((Xml_Api_Node != null && Xml_Api_Node.Attributes.Count == 1) && (Xml_Not_Node != null && Xml_Not_Node.Attributes.Count == 1))
                {
                    switch (Xml_Api_Node.Attributes[0].InnerText)
                    {
                        case "True":
                            Remember_Api = true;
                            Border_Remember_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#2CC49A");
                            Textblock_Api_Scan.FontSize = 10;
                            Canvas.SetLeft(Textblock_Api_Scan, 27);
                            Canvas.SetTop(Textblock_Api_Scan, 5);
                            Textbox_Api_Scan.Text = Xml_Api_Node.InnerText;
                            Textbox_Api_Scan.Visibility = Visibility.Visible;
                            Textbox_Api_Scan.CaretIndex = Textbox_Api_Scan.Text.Length;
                            break;
                    }

                    switch (Xml_Not_Node.Attributes[0].InnerText)
                    {
                        case "True":
                            Notifiche = true;
                            Border_Base_Switch_Grid_Settings.Background = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                            Canvas.SetLeft(Canvas_Switch_Grid_Settings, 37);
                            DropShadow_Border_Grid_Settings.Direction = -180;
                            break;
                    }

                }
                else
                {
                    Xml_File();
                }

            }
            catch (Exception)
            {
                Xml_File();
            }
        }

        /// <summary>
        /// Metodo per la creazione e lettura del file xml in caso di errori
        /// </summary>
        private void Xml_File()
        {
            XmlDocument Xml_Write = new XmlDocument();

            XmlDeclaration xmlDeclaration = Xml_Write.CreateXmlDeclaration("1.0", "UTF-8", null);
            Xml_Write.AppendChild(xmlDeclaration);

            XmlElement xmlElement_Settings = Xml_Write.CreateElement("Settings");
            Xml_Write.AppendChild(xmlElement_Settings);

            XmlElement xmlElement_Api = Xml_Write.CreateElement("Api");
            xmlElement_Settings.AppendChild(xmlElement_Api);

            XmlText xmlText_Api = Xml_Write.CreateTextNode("Null");
            xmlElement_Api.AppendChild(xmlText_Api);

            xmlElement_Api.SetAttribute("Remember", "False");

            XmlElement xmlElement_Notifications = Xml_Write.CreateElement("Notifications");
            xmlElement_Settings.AppendChild(xmlElement_Notifications);

            xmlElement_Notifications.SetAttribute("Remember", "False");

            Xml_Write.Save(AppDomain.CurrentDomain.BaseDirectory + "\\Settings.xml");

            Xml_Base.Load(AppDomain.CurrentDomain.BaseDirectory + "\\Settings.xml");
        }

        //Metodo per far capire alla notifica se la prima schermata è stata chiusa
        private void Window_Closed(object sender, EventArgs e)
        {
            Win_Closed = true;
        }

        //Istanza per comprendere se il programma è in full screen
        private bool Full_Screen = false;

        //Metodo per impostare il monitor in full screen
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.F11:
                    switch(Full_Screen)
                    {
                        case false:
                            Full_Screen = true;
                            Visibility = Visibility.Collapsed;
                            WindowStyle = WindowStyle.None;
                            WindowState = WindowState.Maximized;
                            ResizeMode = ResizeMode.NoResize;
                            Visibility = Visibility.Visible;
                            break;
                        case true:
                            Full_Screen = false;
                            Visibility = Visibility.Collapsed;
                            WindowStyle = WindowStyle.SingleBorderWindow;
                            WindowState = WindowState.Normal;
                            ResizeMode = ResizeMode.CanResize;
                            Visibility = Visibility.Visible;
                            break;
                    }
                    break;
            }
        }

        #endregion

        //Eventi del cambio grafica della home
        #region Eventi Home

        #region Scan
        private void Canvas_A_Home_Scan_MouseEnter(object sender, MouseEventArgs e)
        {
            Canvas_A_Home_Scan.Width = 400;
            Canvas_A_Home_Scan.Height = 400;
            Canvas_A_Home_Scan.Margin = new Thickness(15, 135, 420, 514);

            Border_Home_Scan.Width = 400;
            Border_Home_Scan.Height = 400;
            Border_Home_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#ff585858");

            Textblock_Home_Scan.FontSize = 60;
            Canvas.SetLeft(Textblock_Home_Scan, 138);
            Canvas.SetTop(Textblock_Home_Scan, 267);

            Canvas_B_Home_Scan.Width = 172;
            Canvas_B_Home_Scan.Height = 168;
            Canvas.SetLeft(Canvas_B_Home_Scan, 114);
            Canvas.SetTop(Canvas_B_Home_Scan, 108);

            Ellipse_Home_Scan.Width = 134;
            Ellipse_Home_Scan.Height = 135;
            Ellipse_Home_Scan.StrokeThickness = 8;

            Line_Home_Scan.X2 = 56;
            Line_Home_Scan.Y2 = 56;
            Canvas.SetLeft(Line_Home_Scan, 115.5);
            Canvas.SetTop(Line_Home_Scan, 112.5);
            Line_Home_Scan.StrokeThickness = 8;
        }

        private void Canvas_A_Home_Scan_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas_A_Home_Scan.Width = 318;
            Canvas_A_Home_Scan.Height = 318;
            Canvas_A_Home_Scan.Margin = new Thickness(74, 107, 443, 454);

            Border_Home_Scan.Width = 318;
            Border_Home_Scan.Height = 318;
            Border_Home_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#cc585858");

            Textblock_Home_Scan.FontSize = 30;
            Canvas.SetLeft(Textblock_Home_Scan, 128);
            Canvas.SetTop(Textblock_Home_Scan, 232);

            Canvas_B_Home_Scan.Width = 94;
            Canvas_B_Home_Scan.Height = 92;
            Canvas.SetLeft(Canvas_B_Home_Scan, 111);
            Canvas.SetTop(Canvas_B_Home_Scan, 112);

            Ellipse_Home_Scan.Width = 74;
            Ellipse_Home_Scan.Height = 74;
            Ellipse_Home_Scan.StrokeThickness = 3;

            Line_Home_Scan.X2 = 31;
            Line_Home_Scan.Y2 = 31;
            Canvas.SetLeft(Line_Home_Scan, 64);
            Canvas.SetTop(Line_Home_Scan, 61);
            Line_Home_Scan.StrokeThickness = 3;
        }

        private void Canvas_A_Home_Scan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Home_Close();
            Canvas_Scan_Side_Bar_MouseLeftButtonDown(this, e);
        }

        #endregion

        #region Attack
        private void Canvas_A_Home_Attack_MouseEnter(object sender, MouseEventArgs e)
        {
            Canvas_A_Home_Attack.Width = 400;
            Canvas_A_Home_Attack.Height = 400;
            Canvas_A_Home_Attack.Margin = new Thickness(425, 135, 10, 514);

            Border_Home_Attack.Width = 400;
            Border_Home_Attack.Height = 400;
            Border_Home_Attack.Background = (Brush)Brush_Convert.ConvertFrom("#ff585858");

            Textblock_Home_Attack.FontSize = 60;
            Canvas.SetLeft(Textblock_Home_Attack, 116);
            Canvas.SetTop(Textblock_Home_Attack, 282);

            Canvas_B_Home_Attack.Width = 172;
            Canvas_B_Home_Attack.Height = 172;
            Canvas.SetLeft(Canvas_B_Home_Attack, 113.9697265625);
            Canvas.SetTop(Canvas_B_Home_Attack, 114.22412109375);

            Ellipse_Home_Attack.Width = 171.99972534179688;
            Ellipse_Home_Attack.Height = 172;
            Canvas.SetLeft(Ellipse_Home_Attack, 0.000244140625);
            Ellipse_Home_Attack.StrokeThickness = 8;

            Line_A_Home_Attack.X2 = 44;
            Canvas.SetLeft(Line_A_Home_Attack, 122);
            Canvas.SetTop(Line_A_Home_Attack, 86);
            Line_A_Home_Attack.StrokeThickness = 8;

            Line_B_Home_Attack.Y1 = 44;
            Canvas.SetLeft(Line_B_Home_Attack, 86);
            Canvas.SetTop(Line_B_Home_Attack, 5);
            Line_B_Home_Attack.StrokeThickness = 8;

            Line_C_Home_Attack.Y1 = 44;
            Canvas.SetLeft(Line_C_Home_Attack, 86);
            Canvas.SetTop(Line_C_Home_Attack, 122);
            Line_C_Home_Attack.StrokeThickness = 8;

            Line_D_Home_Attack.X2 = 44;
            Canvas.SetLeft(Line_D_Home_Attack, 5);
            Canvas.SetTop(Line_D_Home_Attack, 86);
            Line_D_Home_Attack.StrokeThickness = 8;
        }

        private void Canvas_A_Home_Attack_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas_A_Home_Attack.Width = 318;
            Canvas_A_Home_Attack.Height = 318;
            Canvas_A_Home_Attack.Margin = new Thickness(440, 107, 77, 454);

            Border_Home_Attack.Width = 318;
            Border_Home_Attack.Height = 318;
            Border_Home_Attack.Background = (Brush)Brush_Convert.ConvertFrom("#cc585858");

            Textblock_Home_Attack.FontSize = 30;
            Canvas.SetLeft(Textblock_Home_Attack, 117);
            Canvas.SetTop(Textblock_Home_Attack, 233);

            Canvas_B_Home_Attack.Width = 95;
            Canvas_B_Home_Attack.Height = 95;
            Canvas.SetLeft(Canvas_B_Home_Attack, 111);
            Canvas.SetTop(Canvas_B_Home_Attack, 110.45556640625);

            Ellipse_Home_Attack.Width = 95.83617401123047;
            Ellipse_Home_Attack.Height = 95.83617401123047;
            Canvas.SetLeft(Ellipse_Home_Attack, 0);
            Ellipse_Home_Attack.StrokeThickness = 3;

            Line_A_Home_Attack.X2 = 30;
            Canvas.SetLeft(Line_A_Home_Attack, 64);
            Canvas.SetTop(Line_A_Home_Attack, 48);
            Line_A_Home_Attack.StrokeThickness = 3;

            Line_B_Home_Attack.Y1 = 30;
            Canvas.SetLeft(Line_B_Home_Attack, 48);
            Canvas.SetTop(Line_B_Home_Attack, 2);
            Line_B_Home_Attack.StrokeThickness = 3;

            Line_C_Home_Attack.Y1 = 30;
            Canvas.SetLeft(Line_C_Home_Attack, 48);
            Canvas.SetTop(Line_C_Home_Attack, 64);
            Line_C_Home_Attack.StrokeThickness = 3;

            Line_D_Home_Attack.X2 = 30;
            Canvas.SetLeft(Line_D_Home_Attack, 2);
            Canvas.SetTop(Line_D_Home_Attack, 48);
            Line_D_Home_Attack.StrokeThickness = 3;
        }

        private void Canvas_A_Home_Attack_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Home_Close();
            Canvas_Attack_Side_Bar_MouseLeftButtonDown(this, e);
        }

        #endregion

        #region Log
        private void Canvas_A_Home_Log_MouseEnter(object sender, MouseEventArgs e)
        {
            Canvas_A_Home_Log.Width = 400;
            Canvas_A_Home_Log.Height = 400;
            Canvas_A_Home_Log.Margin = new Thickness(15, 545, 420, 104);

            Border_A_Home_Log.Width = 400;
            Border_A_Home_Log.Height = 400;
            Border_A_Home_Log.Background = (Brush)Brush_Convert.ConvertFrom("#ff585858");

            Textblock_Home_Log.FontSize = 60;
            Canvas.SetLeft(Textblock_Home_Log, 151);
            Canvas.SetTop(Textblock_Home_Log, 282);

            Canvas_B_Home_Log.Width = 139;
            Canvas_B_Home_Log.Height = 174;
            Canvas.SetLeft(Canvas_B_Home_Log, 130.7711181640625);
            Canvas.SetTop(Canvas_B_Home_Log, 107);

            Border_B_Home_Log.Width = 142;
            Border_B_Home_Log.Height = 174.8551483154297;
            Border_B_Home_Log.BorderThickness = new Thickness(8);
            Border_B_Home_Log.CornerRadius = new CornerRadius(15);

            Line_A_Home_Log.X2 = 97;
            Canvas.SetLeft(Line_A_Home_Log, 22);
            Canvas.SetTop(Line_A_Home_Log, 52);
            Line_A_Home_Log.StrokeThickness = 8;

            Line_B_Home_Log.X2 = 97;
            Canvas.SetLeft(Line_B_Home_Log, 22);
            Canvas.SetTop(Line_B_Home_Log, 88.6630859375);
            Line_B_Home_Log.StrokeThickness = 8;

            Line_C_Home_Log.X2 = 97;
            Canvas.SetLeft(Line_C_Home_Log, 22);
            Canvas.SetTop(Line_C_Home_Log, 126);
            Line_C_Home_Log.StrokeThickness = 8;
        }

        private void Canvas_A_Home_Log_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas_A_Home_Log.Width = 318;
            Canvas_A_Home_Log.Height = 318;
            Canvas_A_Home_Log.Margin = new Thickness(74, 475, 443, 86);

            Border_A_Home_Log.Width = 318;
            Border_A_Home_Log.Height = 318;
            Border_A_Home_Log.Background = (Brush)Brush_Convert.ConvertFrom("#cc585858");

            Textblock_Home_Log.FontSize = 30;
            Canvas.SetLeft(Textblock_Home_Log, 134);
            Canvas.SetTop(Textblock_Home_Log, 233);

            Canvas_B_Home_Log.Width = 77;
            Canvas_B_Home_Log.Height = 97;
            Canvas.SetLeft(Canvas_B_Home_Log, 120);
            Canvas.SetTop(Canvas_B_Home_Log, 110);

            Border_B_Home_Log.Width = 77.46868896484375;
            Border_B_Home_Log.Height = 97.45178985595703;
            Border_B_Home_Log.BorderThickness = new Thickness(3);
            Border_B_Home_Log.CornerRadius = new CornerRadius(10);

            Line_A_Home_Log.X2 = 42;
            Canvas.SetLeft(Line_A_Home_Log, 17.51953125);
            Canvas.SetTop(Line_A_Home_Log, 28.195068359375);
            Line_A_Home_Log.StrokeThickness = 3;

            Line_B_Home_Log.X2 = 42;
            Canvas.SetLeft(Line_B_Home_Log, 17.51953125);
            Canvas.SetTop(Line_B_Home_Log, 48.7255859375);
            Line_B_Home_Log.StrokeThickness = 3;

            Line_C_Home_Log.X2 = 42;
            Canvas.SetLeft(Line_C_Home_Log, 17.51953125);
            Canvas.SetTop(Line_C_Home_Log, 68.0244140625);
            Line_C_Home_Log.StrokeThickness = 3;
        }

        private void Canvas_A_Home_Log_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Home_Close();
            Canvas_Log_Side_Bar_MouseLeftButtonDown(this, e);
        }
        
        #endregion

        #region Settings
        private void Canvas_A_Home_Settings_MouseEnter(object sender, MouseEventArgs e)
        {
            Canvas_A_Home_Settings.Width = 400;
            Canvas_A_Home_Settings.Height = 400;
            Canvas_A_Home_Settings.Margin = new Thickness(425, 545, 10, 104);

            Border_Home_Settings.Width = 400;
            Border_Home_Settings.Height = 400;
            Border_Home_Settings.Background = (Brush)Brush_Convert.ConvertFrom("#ff585858");

            Tetxtblock_Home_Settings.FontSize = 60;
            Canvas.SetLeft(Tetxtblock_Home_Settings, 93);
            Canvas.SetTop(Tetxtblock_Home_Settings, 281);

            Canvas_B_Home_Settings.Width = 172;
            Canvas_B_Home_Settings.Height = 171;
            Canvas.SetLeft(Canvas_B_Home_Settings, 113.969970703125);
            Canvas.SetTop(Canvas_B_Home_Settings, 114.37060546875);

            Path_A_Home_Settings.Width = 172;
            Path_A_Home_Settings.Height = 171;
            Canvas.SetLeft(Path_A_Home_Settings, 0);
            Path_A_Home_Settings.StrokeThickness = 5;

            Path_B_Home_Settings.Width = 100;
            Path_B_Home_Settings.Height = 99;
            Canvas.SetLeft(Path_B_Home_Settings, 35.5037841796875);
            Canvas.SetTop(Path_B_Home_Settings, 36.45703125);
            Path_B_Home_Settings.StrokeThickness = 5;
        }

        private void Canvas_A_Home_Settings_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas_A_Home_Settings.Width = 318;
            Canvas_A_Home_Settings.Height = 318;
            Canvas_A_Home_Settings.Margin = new Thickness(440, 475, 77, 86);

            Border_Home_Settings.Width = 318;
            Border_Home_Settings.Height = 318;
            Border_Home_Settings.Background = (Brush)Brush_Convert.ConvertFrom("#cc585858");

            Tetxtblock_Home_Settings.FontSize = 30;
            Canvas.SetLeft(Tetxtblock_Home_Settings, 106);
            Canvas.SetTop(Tetxtblock_Home_Settings, 233);

            Canvas_B_Home_Settings.Width = 95;
            Canvas_B_Home_Settings.Height = 95;
            Canvas.SetLeft(Canvas_B_Home_Settings, 111);
            Canvas.SetTop(Canvas_B_Home_Settings, 111.29296875);

            Path_A_Home_Settings.Width = 95;
            Path_A_Home_Settings.Height = 95;
            Canvas.SetLeft(Path_A_Home_Settings, 0.000244140625);
            Path_A_Home_Settings.StrokeThickness = 3;

            Path_B_Home_Settings.Width = 52;
            Path_B_Home_Settings.Height = 52;
            Canvas.SetLeft(Path_B_Home_Settings, 21.503662109375);
            Canvas.SetTop(Path_B_Home_Settings, 21.456787109375);
            Path_B_Home_Settings.StrokeThickness = 3;
        }

        private void Canvas_A_Home_Settings_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Home_Close();
            Canvas_Settings_Side_Bar_MouseLeftButtonDown(this, e);
        }
        #endregion

        /// <summary>
        /// Metodo per la chiusura della home non ridondante
        /// </summary>
        private void Home_Close()
        {
            ScrollViewer_Grid_Home.Visibility = Visibility.Hidden;
            Grid_Home.Visibility = Visibility.Hidden;
            Canvas_Side_Bar.Visibility = Visibility.Visible;
        }
        #endregion

        //Eventi del cambio grafica/funzionamento della side bar
        #region Eventi Side Bar

        /// <summary>
        /// Campi bool, indicano lo stato degli eventi MouseEnter/Leave/Click dei vari elementi della side bar
        /// True = bottone allo stato di default con gli eventi attivi
        /// False = bottone cliccato con gli eventi disattivati
        /// </summary>
        private bool Scan_Side_Bar = true;
        internal bool Attack_Side_Bar = true;
        private bool Log_Side_Bar = true;
        internal bool Settings_Side_Bar = true;

        /// <summary>
        /// Campi bool indicano quale grid è aperta per scan e attack
        /// True = grid di scansione/attacco
        /// False = grid di risultati scansione/attacco
        /// </summary>
        private bool Grid_Results_Scan_Pass = true;
        private bool Grid_Result_Attack_Pass = true;

        #region Scan
        private void Canvas_Scan_Side_Bar_MouseEnter(object sender, MouseEventArgs e)
        {
            Border_Scan_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#ff585858");
        }

        private void Canvas_Scan_Side_Bar_MouseLeave(object sender, MouseEventArgs e)
        {
            Border_Scan_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#cc585858");
        }

        private void Canvas_Scan_Side_Bar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {   
            switch(Grid_Results_Scan_Pass)
            {
                case true:
                    ScrollViewer_Grid_Scan.Visibility = Visibility.Visible;
                    Grid_Scan.Visibility = Visibility.Visible;
                    break;
                case false:
                    ScrollViewer_Grid_Results_Scan.Visibility = Visibility;
                    Grid_Results_Scan.Visibility = Visibility.Visible;
                    break;
            }

            Border_Scan_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#FF2CC49A");
            Ellipse_Scan_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Line_Scan_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Textblock_Scan_Side_Bar.Foreground = (Brush)Brush_Convert.ConvertFrom("#FF585858");

            Event_Active();
            Scan_Side_Bar = false;
            Canvas_Scan_Side_Bar.MouseEnter -= Canvas_Scan_Side_Bar_MouseEnter;
            Canvas_Scan_Side_Bar.MouseLeave -= Canvas_Scan_Side_Bar_MouseLeave;
            Canvas_Scan_Side_Bar.MouseLeftButtonDown -= Canvas_Scan_Side_Bar_MouseLeftButtonDown;
        }
        #endregion

        #region Attack
        private void Canvas_Attack_Side_Bar_MouseEnter(object sender, MouseEventArgs e)
        {
            Border_Attack_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#ff585858");
        }

        private void Canvas_Attack_Side_Bar_MouseLeave(object sender, MouseEventArgs e)
        {
            Border_Attack_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#cc585858");
        }

        internal void Canvas_Attack_Side_Bar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch(Grid_Result_Attack_Pass)
            {
                case true:
                    ScrollViewer_Grid_Attack.Visibility = Visibility.Visible;
                    Grid_Attack.Visibility = Visibility.Visible;
                    break;
                case false:
                    ScrollViewer_Grid_Results_Attack.Visibility = Visibility.Visible;
                    Grid_Results_Attack.Visibility = Visibility.Visible;
                    break;
            }

            Border_Attack_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#FF2CC49A");
            Ellipse_Attack_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Line_B_Attack_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Line_C_Attack_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Line_D_Attack_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Line_A_Attack_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Textblock_Attack_Side_Bar.Foreground = (Brush)Brush_Convert.ConvertFrom("#FF585858");

            Event_Active();
            Attack_Side_Bar = false;
            Canvas_Attack_Side_Bar.MouseEnter -= Canvas_Attack_Side_Bar_MouseEnter;
            Canvas_Attack_Side_Bar.MouseLeave -= Canvas_Attack_Side_Bar_MouseLeave;
            Canvas_Attack_Side_Bar.MouseLeftButtonDown -= Canvas_Attack_Side_Bar_MouseLeftButtonDown;
        }
        #endregion

        #region Log
        private void Canvas_Log_Side_Bar_MouseEnter(object sender, MouseEventArgs e)
        {
            Border_Log_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#ff585858");
        }

        private void Canvas_Log_Side_Bar_MouseLeave(object sender, MouseEventArgs e)
        {
            Border_Log_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#cc585858");
        }

        private void Canvas_Log_Side_Bar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer_Grid_Log.Visibility = Visibility.Visible;
            Grid_Log.Visibility = Visibility.Visible;
            Border_Log_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#FF2CC49A");
            Border_B_Log_Side_Bar.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Line_A_Log_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Line_B_Log_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Line_C_Log_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Textblock_Log_Side_Bar.Foreground = (Brush)Brush_Convert.ConvertFrom("#FF585858");

            Event_Active();
            Log_Side_Bar = false;
            Canvas_Log_Side_Bar.MouseEnter -= Canvas_Log_Side_Bar_MouseEnter;
            Canvas_Log_Side_Bar.MouseLeave -= Canvas_Log_Side_Bar_MouseLeave;
            Canvas_Log_Side_Bar.MouseLeftButtonDown -= Canvas_Log_Side_Bar_MouseLeftButtonDown;
        }
        #endregion

        #region Settings
        private void Canvas_Settings_Side_Bar_MouseEnter(object sender, MouseEventArgs e)
        {
            Border_Settings_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#ff585858");
        }

        private void Canvas_Settings_Side_Bar_MouseLeave(object sender, MouseEventArgs e)
        {
            Border_Settings_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#cc585858");
        }

        internal void Canvas_Settings_Side_Bar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer_Grid_Settings.Visibility = Visibility.Visible;
            Grid_Settings.Visibility = Visibility.Visible;
            Border_Settings_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#FF2CC49A");
            Path_A_Settings_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Path_B_Settings_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#FF585858");
            Textblock_Settings_Side_Bar.Foreground = (Brush)Brush_Convert.ConvertFrom("#FF585858");

            Event_Active();
            Settings_Side_Bar = false;
            Canvas_Settings_Side_Bar.MouseEnter -= Canvas_Settings_Side_Bar_MouseEnter;
            Canvas_Settings_Side_Bar.MouseLeave -= Canvas_Settings_Side_Bar_MouseLeave;
            Canvas_Settings_Side_Bar.MouseLeftButtonDown -= Canvas_Settings_Side_Bar_MouseLeftButtonDown;
        }
        #endregion

        /// <summary>
        /// Metodo che si occupa di reimpostare l'ultimo bottone cliccato allo stato di default al click del nuovo buttone
        /// </summary>
        private void Event_Active()
        {
            switch(Scan_Side_Bar)
            {
                case false:
                    Scan_Side_Bar = true;

                    switch (Grid_Results_Scan_Pass)
                    {
                        case true:
                            ScrollViewer_Grid_Scan.Visibility = Visibility.Hidden;
                            Grid_Scan.Visibility = Visibility.Hidden;
                            break;
                        case false:
                            ScrollViewer_Grid_Results_Scan.Visibility = Visibility.Hidden;
                            Grid_Results_Scan.Visibility = Visibility.Hidden;
                            break;
                    }

                    Border_Scan_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#cc585858");
                    Ellipse_Scan_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Line_Scan_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Textblock_Scan_Side_Bar.Foreground = (Brush)Brush_Convert.ConvertFrom("#ffffffff");

                    Canvas_Scan_Side_Bar.MouseEnter += Canvas_Scan_Side_Bar_MouseEnter;
                    Canvas_Scan_Side_Bar.MouseLeave += Canvas_Scan_Side_Bar_MouseLeave;
                    Canvas_Scan_Side_Bar.MouseLeftButtonDown += Canvas_Scan_Side_Bar_MouseLeftButtonDown;
                    break;
            }

            switch(Attack_Side_Bar)
            {
                case false:
                    Attack_Side_Bar = true;

                    switch (Grid_Result_Attack_Pass)
                    {
                        case true:
                            ScrollViewer_Grid_Attack.Visibility = Visibility.Hidden;
                            Grid_Attack.Visibility = Visibility.Hidden;
                            break;
                        case false:
                            ScrollViewer_Grid_Results_Attack.Visibility = Visibility.Hidden;
                            Grid_Results_Attack.Visibility = Visibility.Hidden;
                            break;
                    }

                    Border_Attack_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#cc585858");
                    Ellipse_Attack_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Line_B_Attack_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Line_C_Attack_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Line_D_Attack_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Line_A_Attack_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Textblock_Attack_Side_Bar.Foreground = (Brush)Brush_Convert.ConvertFrom("#ffffffff");

                    Canvas_Attack_Side_Bar.MouseEnter += Canvas_Attack_Side_Bar_MouseEnter;
                    Canvas_Attack_Side_Bar.MouseLeave += Canvas_Attack_Side_Bar_MouseLeave;
                    Canvas_Attack_Side_Bar.MouseLeftButtonDown += Canvas_Attack_Side_Bar_MouseLeftButtonDown;
                    break;
            }

            switch(Log_Side_Bar)
            {
                case false:
                    Log_Side_Bar = true;

                    ScrollViewer_Grid_Log.Visibility = Visibility.Hidden;
                    Grid_Log.Visibility = Visibility.Hidden;
                    Border_Log_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#cc585858");
                    Border_B_Log_Side_Bar.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Line_A_Log_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Line_B_Log_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Line_C_Log_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Textblock_Log_Side_Bar.Foreground = (Brush)Brush_Convert.ConvertFrom("#ffffffff");


                    Canvas_Log_Side_Bar.MouseEnter += Canvas_Log_Side_Bar_MouseEnter;
                    Canvas_Log_Side_Bar.MouseLeave += Canvas_Log_Side_Bar_MouseLeave;
                    Canvas_Log_Side_Bar.MouseLeftButtonDown += Canvas_Log_Side_Bar_MouseLeftButtonDown;
                    break;
            }

            switch(Settings_Side_Bar)
            {
                case false:
                    Settings_Side_Bar = true;

                    ScrollViewer_Grid_Settings.Visibility = Visibility.Hidden;
                    Grid_Settings.Visibility = Visibility.Hidden;
                    Border_Settings_Side_Bar.Background = (Brush)Brush_Convert.ConvertFrom("#cc585858");
                    Path_A_Settings_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Path_B_Settings_Side_Bar.Stroke = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Textblock_Settings_Side_Bar.Foreground = (Brush)Brush_Convert.ConvertFrom("#ffffffff");

                    Canvas_Settings_Side_Bar.MouseEnter += Canvas_Settings_Side_Bar_MouseEnter;
                    Canvas_Settings_Side_Bar.MouseLeave += Canvas_Settings_Side_Bar_MouseLeave;
                    Canvas_Settings_Side_Bar.MouseLeftButtonDown += Canvas_Settings_Side_Bar_MouseLeftButtonDown;
                    break;
            }
        }

        #endregion

        //Eventi del cambio grafica/funzionamento della schermata settings
        #region Eventi Settings
        /// <summary>
        /// Campo bool per l'invio di notifiche
        /// True = notifiche permesse
        /// False = notifiche non permesse
        /// </summary>
        private bool Notifiche = false;

        private void Canvas_Thread_Settings_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border_Thread_Settings.Background = (Brush)Brush_Convert.ConvertFrom("#ff464646");
            Border_Thread_Settings.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
            Border_Textbox_Thread_Settings.IsEnabled = true;
            Border_Textbox_Thread_Settings.Focus();
        }

        private void Canvas_Base_Switch_Grid_Settings_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (Notifiche)
            {
                case true:
                    Xml_Base.DocumentElement.LastChild.Attributes[0].InnerText = "False";
                    Notifiche = false;
                    Border_Base_Switch_Grid_Settings.Background = (Brush)Brush_Convert.ConvertFrom("#ff464646");
                    Canvas.SetLeft(Canvas_Switch_Grid_Settings, 3);
                    DropShadow_Border_Grid_Settings.Direction = 0;
                    break;

                case false:
                    Xml_Base.DocumentElement.LastChild.Attributes[0].InnerText = "True";
                    Notifiche = true;
                    Border_Base_Switch_Grid_Settings.Background = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");
                    Canvas.SetLeft(Canvas_Switch_Grid_Settings, 37);
                    DropShadow_Border_Grid_Settings.Direction = -180;
                    break;
            }

            Xml_Base.Save(AppDomain.CurrentDomain.BaseDirectory + "\\Settings.xml");

        }

        private void Textblock_Url_Info_Setting_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        private void Texblock_Last_Info_Settings_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        #endregion

        //Eventi del cambio grafica/funzionamento della schermata scan
        #region Eventi Scan

        /// <summary>
        /// Campi bool, indicano lo stato dell'evento Click dei vari elementi di scan
        /// True = bottone allo stato di default con gli eventi attivi
        /// False = bottone cliccatto con gli eventi disattivati
        /// </summary>
        private bool Api_Scan = true;
        private bool Country_Scan = true;
        private bool Town_Scan = true;
        private bool Pages_Scan = true;

        private void Canvas_Api_Scan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Textblock_Api_Scan.FontSize = 10;
            Canvas.SetLeft(Textblock_Api_Scan, 27);
            Canvas.SetTop(Textblock_Api_Scan, 5);

            Border_Api_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#ff464646");
            Border_Api_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");

            Textbox_Api_Scan.IsEnabled = true;
            Textbox_Api_Scan.Focus();

            Event_Scan_Active();
            Api_Scan = false;
            Canvas_Api_Scan.MouseLeftButtonDown -= Canvas_Api_Scan_MouseLeftButtonDown;
        }

        private void Canvas_Country_Scan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Textblock_Country_Scan.FontSize = 10;
            Canvas.SetLeft(Textblock_Country_Scan, 27);
            Canvas.SetTop(Textblock_Country_Scan, 5);

            Border_Country_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#ff464646");
            Border_Country_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");

            Textbox_Country_Scan.IsEnabled = true;
            Textbox_Country_Scan.Focus();

            Event_Scan_Active();
            Country_Scan = false;
            Canvas_Country_Scan.MouseLeftButtonDown -= Canvas_Country_Scan_MouseLeftButtonDown;
        }

        private void Canvas_Town_Scan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Textblock_Town_Scan.FontSize = 10;
            Canvas.SetLeft(Textblock_Town_Scan, 27);
            Canvas.SetTop(Textblock_Town_Scan, 5);

            Border_Town_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#ff464646");
            Border_Town_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");

            Textbox_Town_Scan.IsEnabled = true;
            Textbox_Town_Scan.Focus();

            Event_Scan_Active();
            Town_Scan = false;
            Canvas_Town_Scan.MouseLeftButtonDown -= Canvas_Town_Scan_MouseLeftButtonDown;
        }

        private void Canvas_Pages_Scan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Textblock_Pages_Scan.FontSize = 10;
            Canvas.SetLeft(Textblock_Pages_Scan, 27);
            Canvas.SetTop(Textblock_Pages_Scan, 5);

            Border_Pages_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#ff464646");
            Border_Pages_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#ff2cc49a");

            Textbox_Pages_Scan.IsEnabled = true;
            Textbox_Pages_Scan.Focus();

            Event_Scan_Active();
            Pages_Scan = false;
            Canvas_Pages_Scan.MouseLeftButtonDown -= Canvas_Pages_Scan_MouseLeftButtonDown;
        }

        private async void Canvas_Start_Scan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Event_Scan_Active();
            Border_Start_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#2CC49A");
            Canvas_Start_Scan.IsEnabled = false;

            try
            {
                await Avtech.Scan_Async(Textbox_Api_Scan.Text, Textbox_Country_Scan.Text, Textbox_Town_Scan.Text, Convert.ToByte(Textbox_Pages_Scan.Text));
            }
            catch (ArgumentException Error)
            {
                switch (Error.ParamName)
                {
                    case "Shodan_Api":
                        Border_Api_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#FFCE3939");
                        break;
                    case "Paese":
                        Border_Country_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#FFCE3939");
                        break;
                    case "Citta":
                        Border_Town_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#FFCE3939");
                        break;
                }
            }catch(FormatException)
            {
                Border_Pages_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#FFCE3939");
            }
            catch(OverflowException)
            {
                Border_Pages_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#FFCE3939");
                Textbox_Pages_Scan.Text = "255";
                Textbox_Pages_Scan.CaretIndex = Textbox_Pages_Scan.Text.Length;
            }
            catch (Exception Errors)
            {
                MessageBox.Show("Generic error: " + Errors.Message + ", please try again.","Error",MessageBoxButton.OK,MessageBoxImage.Warning);
            }

            Border_Start_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#CC2CC49A");
            Canvas_Start_Scan.IsEnabled = true;
        }

        private void Avtech_Scan_Terminato(object sender, Ip_Info e)
        {
            if (e.Ip_Trovati > 0)
            {
                int Num_File = 0;
                while (true)
                {
                    string Directory = AppDomain.CurrentDomain.BaseDirectory + $"\\Results\\Scan\\Scan[{Num_File}].txt";
                    if (!File.Exists(Directory))
                    {
                        StreamWriter StreamWriter_IPS = new StreamWriter(Directory);
                        DataTable_IPS.Rows.Clear();
                        int Ips_Count = 0;
                        foreach (string Ips in e.Ip_Port_Show)
                        {
                            StreamWriter_IPS.WriteLine(Ips);
                            DataTable_IPS.Rows.Add(Ips_Count, Ips);
                            Ips_Count++;
                        }
                        StreamWriter_IPS.Close();
                        Dispatcher.Invoke(new Action(() =>
                        {
                            Grid_IPS_Results_Scan.Items.Refresh();
                            Grid_IPS_Results_Scan.ItemsSource = DataTable_IPS.DefaultView;
                            Log_Update();
                            Textblock_ScanResults_Scan.Visibility = Visibility.Visible;
                            switch(Scan_Side_Bar)
                            {
                                case false:
                                    ScrollViewer_Grid_Scan.Visibility = Visibility.Hidden;
                                    Grid_Scan.Visibility = Visibility.Hidden;

                                    ScrollViewer_Grid_Results_Scan.Visibility = Visibility.Visible;
                                    Grid_Results_Scan.Visibility = Visibility.Visible;
                                    break;
                            }
                            Grid_Results_Scan_Pass = false;
                        }));
                        break;
                    }
                    Num_File++;
                }
            }
        }

        private void Textblock_Run_Scan_Results_Scan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer_Grid_Results_Scan.Visibility = Visibility.Hidden;
            Grid_Results_Scan.Visibility = Visibility.Hidden;

            ScrollViewer_Grid_Scan.Visibility = Visibility.Visible;
            Grid_Scan.Visibility = Visibility.Visible;
            Grid_Results_Scan_Pass = true;
        }

        private void Textblock_Run_Results_Results_Scan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer_Grid_Scan.Visibility = Visibility.Hidden;
            Grid_Scan.Visibility = Visibility.Hidden;

            ScrollViewer_Grid_Results_Scan.Visibility = Visibility.Visible;
            Grid_Results_Scan.Visibility = Visibility.Visible;
            Grid_Results_Scan_Pass = false;
        }

        /// <summary>
        /// Metodo che si occupa di reimpostare l'ultimo bottone cliccato allo stato di default al click del nuovo buttone
        /// </summary>
        private void Event_Scan_Active()
        {
            switch(Api_Scan)
            {
                case false:
                    Api_Scan = true;

                    Border_Api_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#ff333333");
                    Border_Api_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#ff707070");
                    Textbox_Api_Scan.IsEnabled = false;

                    Canvas_Api_Scan.MouseLeftButtonDown += Canvas_Api_Scan_MouseLeftButtonDown;
                    break;
            }

            switch(Country_Scan)
            {
                case false:
                    Country_Scan = true;

                    Border_Country_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#ff333333");
                    Border_Country_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#ff707070");
                    Textbox_Country_Scan.IsEnabled = false;

                    Canvas_Country_Scan.MouseLeftButtonDown += Canvas_Country_Scan_MouseLeftButtonDown;
                    break;
            }

            switch(Town_Scan)
            {
                case false:
                    Town_Scan = true;

                    Border_Town_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#ff333333");
                    Border_Town_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#ff707070");
                    Textbox_Town_Scan.IsEnabled = false;

                    Canvas_Town_Scan.MouseLeftButtonDown += Canvas_Town_Scan_MouseLeftButtonDown;
                    break;
            }

            switch(Pages_Scan)
            {
                case false:
                    Pages_Scan = true;

                    Border_Pages_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#ff333333");
                    Border_Pages_Scan.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#ff707070");
                    Textbox_Pages_Scan.IsEnabled = false;

                    Canvas_Pages_Scan.MouseLeftButtonDown += Canvas_Pages_Scan_MouseLeftButtonDown;
                    break;
            }
        }

        /// <summary>
        /// Campo bool per ricordare l'api inserita
        /// True = api salvata, al prossimo riavvio verrà visualizzata
        /// False = api non salvata
        /// </summary>
        private bool Remember_Api = false;

        private void Canvas_Remember_Scan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (Remember_Api)
            {
                case true:
                    Xml_Base.DocumentElement.FirstChild.Attributes[0].InnerText = "False";
                    Xml_Base.DocumentElement.FirstChild.InnerText = "Null";
                    Remember_Api = false;
                    Border_Remember_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#585858");
                    break;
                case false:
                    Xml_Base.DocumentElement.FirstChild.Attributes[0].InnerText = "True";
                    Xml_Base.DocumentElement.FirstChild.InnerText = Textbox_Api_Scan.Text;
                    Remember_Api = true;
                    Border_Remember_Scan.Background = (Brush)Brush_Convert.ConvertFrom("#2CC49A");
                    break;
            }

            Xml_Base.Save(AppDomain.CurrentDomain.BaseDirectory + "\\Settings.xml");
        }
        #endregion

        //Eventi del cambio grafica/funzionamento della schermata attack
        #region Eventi Attack

        private void Canvas_Browser_Attack_MouseEnter(object sender, MouseEventArgs e)
        {
            Canvas_Border_Browser_Attack.Background = (Brush)Brush_Convert.ConvertFrom("#FF585858");
        }

        private void Canvas_Browser_Attack_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas_Border_Browser_Attack.Background = (Brush)Brush_Convert.ConvertFrom("#CC585858");
        }

        private void Canvas_Browser_Attack_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog Open_File = new OpenFileDialog();
            Open_File.Filter = "Text files (*.txt)|*.txt";
            switch (Open_File.ShowDialog().Value)
            {
                case true:
                    Start_Active(Open_File.FileName);
                    break;
            }
        }

        private void Canvas_Drag_Attack_Drop(object sender, DragEventArgs e)
        {
            string[] File_Path = (string[])e.Data.GetData(DataFormats.FileDrop);

            switch(System.IO.Path.GetExtension(File_Path[0]))
            {
                case ".txt":
                    Start_Active(File_Path[0]);
                    break;
            }
        }

        /// <summary>
        /// Metodo per l'attivazione del bottone start non ridondante
        /// </summary>
        private void Start_Active(string Path)
        {
            if (Ips_List.Count > 0)
            {
                Ips_List.Clear();
            }

            foreach (string Ip in File.ReadLines(Path))
            {
                switch (string.IsNullOrWhiteSpace(Ip))
                {
                    case false:
                        Ips_List.Add(Ip);
                        break;
                }
            }

            if (Ips_List.Count > 0 && !Canvas_Start_Attack.IsEnabled)
            {
                Canvas_Start_Attack.IsEnabled = true;
                Canvas_Border_Start_Attack.Background = (Brush)Brush_Convert.ConvertFrom("#CC2CC49A");
                Textblock_Start_Attack.Foreground = (Brush)Brush_Convert.ConvertFrom("#FFFFFF");
            }
            else if (Ips_List.Count == 0 && Canvas_Start_Attack.IsEnabled)
            {
                Canvas_Start_Attack.IsEnabled = false;
                Canvas_Border_Start_Attack.Background = (Brush)Brush_Convert.ConvertFrom("#802cc49a");
                Textblock_Start_Attack.Foreground = (Brush)Brush_Convert.ConvertFrom("#802cc49a");
            }
        }

        private void Canvas_Start_Attack_MouseEnter(object sender, MouseEventArgs e)
        {
            Canvas_Border_Start_Attack.Background = (Brush)Brush_Convert.ConvertFrom("#FF2CC49A");
        }

        private void Canvas_Start_Attack_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas_Border_Start_Attack.Background = (Brush)Brush_Convert.ConvertFrom("#CC2CC49A");
        }

        private async void Canvas_Start_Attack_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Canvas_Start_Attack.MouseLeftButtonDown -= Canvas_Start_Attack_MouseLeftButtonDown;
                Textblock_Start_Attack.Text = "Results";
                Canvas_Start_Attack.MouseLeftButtonDown += Canvas_Results_Attack_MouseLeftButtonDown;
                Canvas_Drag_Attack.Visibility = Visibility.Hidden;
                Textblock_Or_Attack.Visibility = Visibility.Hidden;
                Canvas_Browser_Attack.Visibility = Visibility.Hidden;

                Textblock_Other_Progressbar_Attack.Visibility = Visibility.Visible;
                Textblock_Percentage_Progressbar_Attack.Visibility = Visibility.Visible;
                Border_Progressbar_Attack.Visibility = Visibility.Visible;

                byte Threads = Convert.ToByte(Border_Textbox_Thread_Settings.Text);

                await Avtech.Attack_Async(Ips_List.ToArray(), Threads);
            }
            catch(OverflowException)
            {
                Canvas_Settings_Side_Bar_MouseLeftButtonDown(this, e);
                Border_Thread_Settings.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#FFCE3939");
                Border_Textbox_Thread_Settings.Text = "255";
                Border_Textbox_Thread_Settings.IsEnabled = false;
                Border_Textbox_Thread_Settings.CaretIndex = Border_Textbox_Thread_Settings.Text.Length;
            }
            catch (FormatException)
            {
                Canvas_Settings_Side_Bar_MouseLeftButtonDown(this, e);
                Border_Thread_Settings.BorderBrush = (Brush)Brush_Convert.ConvertFrom("#FFCE3939");
                Border_Textbox_Thread_Settings.IsEnabled = false;
            }
            catch (Exception Errors)
            {
                MessageBox.Show("Generic error: " + Errors.Message + ", please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            Canvas_Start_Attack.MouseLeftButtonDown -= Canvas_Results_Attack_MouseLeftButtonDown;
            Textblock_Start_Attack.Text = "Attack";
            Canvas_Start_Attack.MouseLeftButtonDown += Canvas_Start_Attack_MouseLeftButtonDown;
            Textblock_Other_Progressbar_Attack.Visibility = Visibility.Hidden;
            Textblock_Percentage_Progressbar_Attack.Visibility = Visibility.Hidden;
            Border_Progressbar_Attack.Visibility = Visibility.Hidden;

            Canvas_Drag_Attack.Visibility = Visibility.Visible;
            Textblock_Or_Attack.Visibility = Visibility.Visible;
            Canvas_Browser_Attack.Visibility = Visibility.Visible;
            Canvas_Start_Attack.IsEnabled = true;
        }

        private void Canvas_Results_Attack_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Canvas_Start_Attack.IsEnabled = false;
            Avtech.Stop_Attack();
            //Apertura schermata risultati
        }

        private void Avtech_Attack_Terminato(object sender, Attack_Info e)
        {
            if (e.Credenziali_Trovate > 0)
            {
                int Num_File = 0;
                while (true)
                {
                    string Directory = AppDomain.CurrentDomain.BaseDirectory + $"\\Results\\Attack\\Attack[{Num_File}].txt";
                    if (!File.Exists(Directory))
                    {
                        StreamWriter StreamWriter_CRED = new StreamWriter(Directory);

                        DataTable_CRED.Rows.Clear();
                        int Ip_Count = 0;
                        foreach (string Cred in e.Ip_Port_Cred_Show)
                        {
                            StreamWriter_CRED.WriteLine(Cred);
                            string[] Cred_Split = Cred.Split('|');
                            string[] User_Pass = Cred_Split[1].Split(':');
                            DataTable_CRED.Rows.Add(User_Pass[0], User_Pass[1], Ip_Count, Cred_Split[0]);
                            Ip_Count++;
                        }
                        StreamWriter_CRED.Close();
                        Dispatcher.Invoke(new Action(() =>
                        {
                            Grid_CRED_Results_Scan.Items.Refresh();
                            Grid_CRED_Results_Scan.ItemsSource = DataTable_CRED.DefaultView;
                            Log_Update();
                            Textblock_AttackResults_Attack.Visibility = Visibility.Visible;
                            switch(Attack_Side_Bar)
                            {
                                case false:
                                    ScrollViewer_Grid_Attack.Visibility = Visibility.Hidden;
                                    Grid_Attack.Visibility = Visibility.Hidden;

                                    ScrollViewer_Grid_Results_Attack.Visibility = Visibility.Visible;
                                    Grid_Results_Attack.Visibility = Visibility.Visible;
                                    break;
                            }
                            Grid_Result_Attack_Pass = false;

                            if (!this.IsActive && Notifiche || this.IsActive && Attack_Side_Bar && Notifiche)
                            {
                                Notification notification = new Notification(this);
                                notification.Show();
                            }
                        }));
                        break;
                    }
                    Num_File++;
                }
            }
        }

        private void Textblock_Run_Attack_Results_Attack_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer_Grid_Results_Attack.Visibility = Visibility.Hidden;
            Grid_Results_Attack.Visibility = Visibility.Hidden;

            ScrollViewer_Grid_Attack.Visibility = Visibility.Visible;
            Grid_Attack.Visibility = Visibility.Visible;
            Grid_Result_Attack_Pass = true;
        }

        private void Textblock_Run_Results_Attack_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer_Grid_Attack.Visibility = Visibility.Hidden;
            Grid_Attack.Visibility = Visibility.Hidden;

            ScrollViewer_Grid_Results_Attack.Visibility = Visibility.Visible;
            Grid_Results_Attack.Visibility = Visibility.Visible;
            Grid_Result_Attack_Pass = false;
        }

        private void Avtech_Info_Tested(object sender, Test_Info e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                double Percentage = (double)e.Percentuale / 100;
                Border_Progressbar_Green_Attack.Offset = Percentage;
                Border_Progressbar_Gray_Attack.Offset = Percentage;
                Textblock_Percentage_Progressbar_Attack.Text = e.Percentuale + "%";
                Textblock_Other_Progressbar_Attack.Text = e.Ips_Testati_Totali + "/" + e.Ips_Totali;
            }));
        }

        #endregion

        //Eventi del cambio grafica/funzionamento della schermata log
        #region Eventi Log
        private void DataGrid_Log_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch(e.ClickCount)
            {
                case 2:
                    DataRowView Data_Log = (DataRowView)DataGrid_Log.SelectedItem;
                    if(Data_Log != null)
                    {
                        switch (File.Exists(Data_Log["Location"].ToString()))
                        {
                            case true:
                                System.Diagnostics.Process.Start(Data_Log["Location"].ToString());
                                break;
                            case false:
                                Log_Update();
                                break;
                        }

                    }
                    break;
            }
        }

        /// <summary>
        /// Metodo per aggiornare la tabella log
        /// </summary>
        private void Log_Update()
        {
            if(DataTable_LOGS.Rows.Count > 0)
            {
                DataTable_LOGS.Rows.Clear();
            }

            int ID = 0;
            switch(Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Results\\Scan"))
            {
                case true:
                    foreach(string Files in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\Results\\Scan"))
                    {
                        DataTable_LOGS.Rows.Add(ID, "Scan", File.GetCreationTime(Files).ToString("dd/MM/yyyy"), Files);
                        ID++;
                    }
                    break;
                case false:
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Results\\Scan");
                    break;
            }

            switch (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Results\\Attack"))
            {
                case true:
                    foreach (string Files in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\Results\\Attack"))
                    {
                        DataTable_LOGS.Rows.Add(ID, "Attack", File.GetCreationTime(Files).ToString("dd/MM/yyyy"), Files);
                        ID++;
                    }
                    break;
                case false:
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Results\\Attack");
                    break;
            }

            if(DataTable_LOGS.Rows.Count > 0)
            {
                DataGrid_Log.Items.Refresh();
                DataGrid_Log.ItemsSource = DataTable_LOGS.DefaultView;

                switch (Canvas_B_Null_Log.Visibility)
                {
                    case Visibility.Visible:
                        DataGrid_Log.Visibility = Visibility.Visible;

                        Canvas_B_Null_Log.Visibility = Visibility.Hidden;
                        Canvas_Line_Null_Log.Visibility = Visibility.Hidden;
                        Canvas_Textblock_A_Null_Log.Visibility = Visibility.Hidden;
                        Canvas_Textblock_B_Null_Log.Visibility = Visibility.Hidden;
                        break;
                }
            }
            else
            {
                switch (DataGrid_Log.Visibility)
                {
                    case Visibility.Visible:
                        DataGrid_Log.Visibility = Visibility.Hidden;

                        Canvas_B_Null_Log.Visibility = Visibility.Visible;
                        Canvas_Line_Null_Log.Visibility = Visibility.Visible;
                        Canvas_Textblock_A_Null_Log.Visibility = Visibility.Visible;
                        Canvas_Textblock_B_Null_Log.Visibility = Visibility.Visible;
                        break;
                }
            }
        }
        #endregion

    }
}
