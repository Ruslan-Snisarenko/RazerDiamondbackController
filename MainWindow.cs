using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Windows.Interop;

namespace RazerDiamondbackController
{
    internal static class Loc
    {
        public static readonly string Lang;

        static Loc()
        {
            try
            {
                string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
                if (code == "ru" || code == "uk")
                {
                    Lang = code;
                }
                else
                {
                    Lang = "en";
                }
            }
            catch
            {
                Lang = "en";
            }
        }

        public static string T(string ru, string en, string uk = null)
        {
            if (Lang == "ru") return ru;
            if (Lang == "uk") return uk ?? ru;
            return en;
        }
    }

    public class MainWindow : Window
    {
        private RazerDevice device;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenu;

        private Ellipse statusIndicator;
        private TextBlock txtStatusText;
        private TextBlock txtSensorName;

        private TextBlock txtDpiValue;
        private Border sliderTrack;
        private Border sliderFill;
        private Border sliderThumb;
        private Canvas sliderCanvas;
        private bool isDraggingSlider = false;

        private Border[] presetChips;
        private int[] presetValues = new int[] { 400, 800, 1200, 1600, 2400, 3200 };

        private Border btnHz125;
        private Border btnHz500;
        private Border btnHz1000;
        private TextBlock txtHz125;
        private TextBlock txtHz500;
        private TextBlock txtHz1000;
        private TextBlock txtPollSub;

        // Lighting Controls
        private Border btnLightOn;
        private Border btnLightSpectrum;
        private Border btnLightOff;
        private TextBlock txtLightOn;
        private TextBlock txtLightSpectrum;
        private TextBlock txtLightOff;
        private Ellipse colorPreviewDot;
        private Border[] colorChips;
        private Color currentColor = Color.FromRgb(28, 168, 222);
        private bool isLightingEnabled = true;
        private bool isSpectrumMode = false;

        // Spectrum Bar Controls
        private Canvas spectrumCanvas;
        private Border spectrumTrack;
        private Border spectrumThumb;
        private bool isDraggingSpectrum = false;

        // Brightness Controls
        private int currentBrightness = 100;
        private Canvas briSliderCanvas;
        private Border briSliderTrack;
        private Border briSliderFill;
        private Border briSliderThumb;
        private TextBlock txtBrightnessValue;
        private bool isDraggingBriSlider = false;

        private Border toastBorder;
        private TextBlock toastText;
        private DispatcherTimer toastTimer;

        private CheckBox chkMinimizeToTray;
        private CheckBox chkAutoStart;

        private int currentDpi = 800;
        private int currentHz = 1000;

        private const int MIN_DPI = 100;
        private const int MAX_DPI = 16000;
        private const string AUTHOR_URL = "https://t.me/Carist";

        // Base64-encoded Telegram plane icon
        private const string TELEGRAM_ICON_BASE64 =
            "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAJ6ElEQVR4nOVbCYxV1Rn+7n0XLCKiNRbBrVRlVXHD" +
            "qlGi1Va0Nbik4oJKI27R2KY1xtQNg0uMW6qptlg3bMcVo6QYjAsYBC2tqBS3UdFxw2VaRFDEmfeeOZPvJ9/8c942" +
            "M0+Y4U+Gt9z7zjnf//3b+c8l2b2hiI1ZMmxckgDI8X1gPr+xKMBA5wG06oUMvZ/tVgIPMgTA0QB2B7AQwIysF4JO" +
            "Hdvhu0MAnApgAoAteP0sAHv2FgWk/FO2twPwawCTAOwl9+bp/0Ex52e9iO0CP/8MwGkAjgIwkNeLct3iQZBVWS9h" +
            "ewcAxwM4GcAecm+r3K/ATRmvZz2Y7QDoUGF7AK8XyHi4vxQ+K34WZz2Q7R0BTCTbIZrH2K5GoUH+k/UQtsM6DwMw" +
            "GcAvAWxWA9sxSfnbJdkGzvZPhO3REbaDsirV8kXHumWATwE0Zhsg230A/IK+fSSA/hG2c7w/J8EtKMXjMbAqFj/e" +
            "AbAy24DY3gnACQBOAjCqDNsGwNY+JzDJQsfSXiKR/v8A3gWwtygxyH/DP9l6YNtAhL9NyHbw7SMA9CvDdiZsB/Od" +
            "BeDPvO9hVngG2qzjK8aMC6gAdZeXvy8FxNjeBcCJ/BtRA9vzQ/0O4J8APmHOf5bMe/ArCf4FAAdFXG5pPRUQY/sH" +
            "AA4n2+P5uRq2A9CZBL5I5vg5gMdoNQbaXpsJPty/G4Ct3NpWA3irHgqIsT1c2B5WA9vPEvSj9GMwQLYwOM6kEgsO" +
            "/HIqeAnH30WuW0BcRiUlWZ3Y7kefPo2sB1+vhu2P6c/3hiJF5jDFrAVwDIAHqAxv9u9zvjeonG+kNC7K3Ev5Psu6" +
            "me2RjOIhmu9cJdvh8zwA9zCwrXCKNZBraUV/d1He0t/bZP4dfv6W45gCtGZoC4BBsm5ge1Oa5GT6Zd8I2wZE2f5Q" +
            "2F4sc+RkfLOQFo5/lxvXwL9O5j+QJojhGylrNxd4ha/FrAtsjxa2Q8VWiW1Twlxhe6VTrIE2yTje2QBu43UDYuBf" +
            "IfOfiFvZWNtyp6gKWEsXqaiAGNuhKvsVffsw+iFk0aXYDsw8SPNdZ368pxABru2s3wG4SYAp+EW0vv8JePs9yH5f" +
            "jm/fNwH4qJwC0gjbu7EeD3X5jyNsG2hlO/z2abId8vaXEbbbNSgj4C8CcE0J8PO5DV7pwKsCxvDVLAd0l3VpMyvD" +
            "9gBhO+y7syrZbmKU/gdTUSW2PXhT/lQAl5cA/xSbm1+JslUs2GlzxAfANoVkEd8bwz7a8eI/ldhu5aKM7dWipKQM" +
            "2zHwYR3XAriQv8k58LMBHEdftrV4sZxvO0gjt10AhLDSn/n1VLKtO7RybIdNxv0AGqy0jLBdjSj4P4VmpdvdtTDe" +
            "zGTQNTJi41vw/RGAofK9uclrXgEHML3EqrRcZJFhMU8CuBvA4zTDWtn2C064uL+wXR0D30DLtLlKKdcUOYyNE90A" +
            "fUwXbaeAGdyKWg/dd1eKXMRSmt/DZXy7VtG4czfjjYJvJfg7AZwullluLgt21i7TEvhNVofrFJgRfAvf++aBaS+k" +
            "mesB3OeA2wTFLoBPGTAnCtsGPsxxK4Bz5f5q54oFQPP/dQpIATzBSS3QFCLmOZgm2ERF7Ml7Wzm4dWaSGsAXxK8V" +
            "fFHA30jwuRrAa+o2DCYvxxYyiea3WrKCLcImNDMNWeEPAF7kbu1MAIM4qVlCrkrw/bidnSCm3nZiy3VczblyrpNT" +
            "TiwADpS9iJ4Iv8rXgi6mGcBvAOwK4PfchSXiEtar0wAX3o8D8FcOehcrQ7/t9JKTGmM2S1hj25Qc3l8G4GKJL9Wa" +
            "vc27M3sA2hBt5kYJOl4qGmpiyTkWwIH0veXOvG0HZkzlOdFkZoZgYueUaEaacrZkH++QCPgc8/80fl9rfLEgGciE" +
            "+30jq9F2neRUwGj0X0DfC4XEKYwTFii1qNDfFzhxUNwU22878FtTUQc48OY6If9fJ5ugzj6+EguAbU1Q76KpvNf8" +
            "bayv4AZmPAe9grV0GrEKqxEC0DNkTGNyCPcGewt43dqGeHKLgO+MFFwKLBsAUeYYyczb3CNlBTWVijiCqesLF/2t" +
            "SgyWs40E07CBeoaRWcFb3REq0Nu7CD6R4Dpcvss5C2hnVWmFQdW8U+m0zGH2GEVXWSED51laH8TvRhD88Aj4Avcc" +
            "9zILdBa8gQWVPdh9v4oxoEMRldYwgWYAc5Hl9PknJGOYIvYFsD3NfqikNzP58PlYAA9Js7MrYlhGyfi2lhD9P48d" +
            "paWdmEgzQB+CmhsZ82CW2UNkV2cB71Nen9VN4FV8E1Tzf4caJeviZOYe86XJYKa4T4kzu3B9jSyuJdLQ6IwUXRMk" +
            "2gT1knZxUvPlRh40mF+raD5XP13AstoONmJ7kWolkTFGRU6B2vUAulMBEPaea/vUfmf4NBslOacwM89Q6j7Pwkv3" +
            "FbVKIg9GbS/fWTs97ALrpgAIWDgWBzNlTnV9fO3sjuG+YhobmMZkLWJzjpAmqIHVJmihHgoo8HUhtW1xoEhzHM0C" +
            "ahz7/wpOU+IltKKxrjdRjcSaoLau1yQ+oV4KSHgsZW0x7TGO4+QLWRtMk7rBUqJZw1gq4RLXgqsksSYonP9H40uK" +
            "7hHT7jy3IPC5vTyD3dcAbvBtKRcs+1JJ88hoNdYQa4ImLgMU66mAIl/nRkrQ/Xg4uoaN10ZhSpuvZqZWPh/IAHmB" +
            "2yqXwjBITqhs/jz3LrrG6I+7KuZvi7jl1EfStyGYqwA8wm6tZgPbBmuhlInVXMdMMrLEo3DG9DCW4BoAQxP0PVlL" +
            "XS0gZbm5OHIaEzo/f3SPrJrZpmys3OIORtQaQqv+XwDOkwCnhzraAlMFvCHnB3VVgI4ViwP9HWAD+AU7wWezFzCB" +
            "jJkFQN4PoJJmSxdbfT30KeHmXlIJZ4ruEx8HdGwrcIydjLn/p9wv2LMCs5gJ7pF06mPDkXS10zmeNWp2rbYHUC8F" +
            "FPj6EoDPnNkljrErmR0ahWGzima22CbSh3WfYPf+EMDfGFMG87fDIgFY03LdFVDkxKvYWDVQ+qDDMj5Ecal7yMFEm" +
            "zAPckP1QAlryDOr/JsBdnMZJ2E86tAEracC1PwauNA+0j67nyb/lGuEejFw1m84gXHic2cN9n5bBtjUgX2LZHToAd" +
            "RTAa0cs4EsL2Mragqf72l2j7CUE7WGGYwNj0aswSwsqaYJWm8FQALdldychAblHeL/tez79aCkiSZ/lvQitUPlJc" +
            "SiipKifpKTTk8tpzsx0QJoOq1hjju88ZgsABbXlwLy7ui7q6IF0NvcZv+WR3pmDS0EvEYehCqsLwWgxtPczljDzdx" +
            "rzKNi+lAZ0+XBqbLzZ+iZUpD1v8pjtnCCtT9L8TtKtOc6SNIL/vN0uadFKkqGni96wmSpsHVjUgAkXdYs3wGIRHeS" +
            "XNiw1wAAAABJRU5ErkJggg==";

        private uint wmShowMe = 0;

        public MainWindow() : this(false) { }

        public MainWindow(bool startInTray)
        {
            device = new RazerDevice();

            this.Width = 470;
            this.SizeToContent = SizeToContent.Height;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Title = "Razer Diamondback Controller";

            BuildModernUi();
            InitTray();
            LoadSettings();

            IntPtr handle = new WindowInteropHelper(this).EnsureHandle();
            HwndSource source = HwndSource.FromHwnd(handle);
            if (source != null)
            {
                source.AddHook(WndProc);
            }
            wmShowMe = Program.RegisterWindowMessage(Program.ShowWindowMessageName);

            InitializeDeviceAndState();

            this.Closing += (s, e) =>
            {
                if (chkMinimizeToTray != null && chkMinimizeToTray.IsChecked == true)
                {
                    e.Cancel = true;
                    this.Hide();
                    if (trayIcon != null)
                    {
                        trayIcon.ShowBalloonTip(1500, "Razer Diamondback", Loc.T("Приложение работает в трее", "App is running in tray", "Програма працює в треї"), System.Windows.Forms.ToolTipIcon.Info);
                    }
                }
                else
                {
                    if (trayIcon != null) trayIcon.Visible = false;
                    System.Windows.Application.Current.Shutdown();
                }
            };
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (wmShowMe != 0 && (uint)msg == wmShowMe)
            {
                this.Dispatcher.Invoke((Action)(() => RestoreWindow()));
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void InitializeDeviceAndState()
        {
            UpdateSliderVisuals(currentDpi);
            UpdateBrightnessVisuals(currentBrightness);
            UpdateSpectrumThumb(currentColor);
            RefreshFromDevice();
        }

        private static BitmapSource LoadTelegramIcon()
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(TELEGRAM_ICON_BASE64);
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = ms;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
            }
            catch
            {
                return null;
            }
        }

        private void BuildModernUi()
        {
            Grid rootGrid = new Grid { Margin = new Thickness(10) };

            Border mainContainer = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromRgb(16, 17, 22)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(32, 35, 46)),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 20,
                    Opacity = 0.55,
                    ShadowDepth = 5
                }
            };

            StackPanel layout = new StackPanel();

            // --- 0. TITLE BAR ---
            Grid titleBar = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 22, 28)),
                Height = 44
            };
            titleBar.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) this.DragMove(); };

            StackPanel titleLeft = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(18, 0, 0, 0)
            };

            statusIndicator = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Color.FromRgb(28, 168, 222)),
                Margin = new Thickness(0, 0, 9, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            txtStatusText = new TextBlock
            {
                Text = "RAZER DIAMONDBACK",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12.5,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock titleDivider = new TextBlock
            {
                Text = "│",
                Foreground = new SolidColorBrush(Color.FromRgb(48, 52, 66)),
                FontSize = 11,
                Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            txtSensorName = new TextBlock
            {
                Text = "Philips PLN 2034 • 16K DPI",
                Foreground = new SolidColorBrush(Color.FromRgb(120, 128, 146)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };

            titleLeft.Children.Add(statusIndicator);
            titleLeft.Children.Add(txtStatusText);
            titleLeft.Children.Add(titleDivider);
            titleLeft.Children.Add(txtSensorName);
            titleBar.Children.Add(titleLeft);

            StackPanel winControls = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };

            Border btnMin = CreateMinButton("—", () => this.WindowState = WindowState.Minimized);
            Border btnClose = CreateMinButton("✕", () => HandleCloseButton(), true);
            winControls.Children.Add(btnMin);
            winControls.Children.Add(btnClose);
            titleBar.Children.Add(winControls);

            layout.Children.Add(titleBar);

            // --- 1. DPI CARD ---
            Border dpiCard = BuildDpiCard();
            layout.Children.Add(dpiCard);

            // --- 2. POLLING RATE CARD ---
            Border pollCard = BuildPollingRateCard();
            layout.Children.Add(pollCard);

            // --- 3. LIGHTING CARD ---
            Border lightCard = BuildLightingCard();
            layout.Children.Add(lightCard);

            // --- 4. ACTION BUTTONS ---
            Grid actionGrid = new Grid { Margin = new Thickness(18, 6, 18, 4) };
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Apply Button
            Border btnApply = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 168, 222)),
                CornerRadius = new CornerRadius(7),
                Height = 36,
                Cursor = Cursors.Hand
            };
            TextBlock txtApply = new TextBlock
            {
                Text = Loc.T("Применить", "Apply", "Застосувати"),
                Foreground = new SolidColorBrush(Color.FromRgb(13, 14, 18)),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnApply.Child = txtApply;
            btnApply.MouseLeftButtonDown += (s, e) => ApplyHardwareSettings();
            btnApply.MouseEnter += (s, e) => btnApply.Opacity = 0.9;
            btnApply.MouseLeave += (s, e) => btnApply.Opacity = 1.0;
            Grid.SetColumn(btnApply, 0);
            actionGrid.Children.Add(btnApply);

            // Refresh Button
            Border btnRefresh = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(24, 26, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 42, 54)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Height = 36,
                Cursor = Cursors.Hand
            };
            TextBlock txtRefresh = new TextBlock
            {
                Text = Loc.T("Считать с мыши", "Read from mouse", "Зчитати з миші"),
                Foreground = new SolidColorBrush(Color.FromRgb(180, 186, 204)),
                FontWeight = FontWeights.SemiBold,
                FontSize = 11.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnRefresh.Child = txtRefresh;
            btnRefresh.MouseLeftButtonDown += (s, e) => RefreshFromDevice();
            btnRefresh.MouseEnter += (s, e) => btnRefresh.Background = new SolidColorBrush(Color.FromRgb(32, 35, 46));
            btnRefresh.MouseLeave += (s, e) => btnRefresh.Background = new SolidColorBrush(Color.FromRgb(24, 26, 34));
            Grid.SetColumn(btnRefresh, 2);
            actionGrid.Children.Add(btnRefresh);

            layout.Children.Add(actionGrid);

            // --- 5. TOAST NOTIFICATION ---
            toastBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 28, 40)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(28, 168, 222)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Height = 26,
                Margin = new Thickness(18, 3, 18, 3),
                Padding = new Thickness(12, 4, 12, 4),
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            toastText = new TextBlock
            {
                Text = Loc.T("✔ Настройки сохранены", "✔ Settings saved", "✔ Налаштування збережено"),
                Foreground = new SolidColorBrush(Color.FromRgb(28, 168, 222)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            toastBorder.Child = toastText;
            layout.Children.Add(toastBorder);

            toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.0) };
            toastTimer.Tick += (s, e) =>
            {
                toastBorder.Opacity = 0;
                toastTimer.Stop();
            };

            // --- 6. FOOTER OPTIONS & AUTHOR ---
            Grid footerGrid = new Grid
            {
                Margin = new Thickness(20, 4, 20, 14),
                VerticalAlignment = VerticalAlignment.Center
            };
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel footerLeft = new StackPanel();

            chkMinimizeToTray = CreateModernCheckBox(Loc.T("В трей при закрытии", "Minimize to tray on close", "У трей при закритті"), true);
            chkMinimizeToTray.Checked += (s, e) => SaveSettings();
            chkMinimizeToTray.Unchecked += (s, e) => SaveSettings();

            chkAutoStart = CreateModernCheckBox(Loc.T("Автозапуск с Windows", "Start with Windows", "Автозапуск із Windows"), false);
            chkAutoStart.Checked += (s, e) => SetAutoStart(true);
            chkAutoStart.Unchecked += (s, e) => SetAutoStart(false);

            footerLeft.Children.Add(chkMinimizeToTray);
            footerLeft.Children.Add(chkAutoStart);
            Grid.SetColumn(footerLeft, 0);
            footerGrid.Children.Add(footerLeft);

            Border authorBadge = CreateAuthorBadge();
            Grid.SetColumn(authorBadge, 1);
            footerGrid.Children.Add(authorBadge);

            layout.Children.Add(footerGrid);

            mainContainer.Child = layout;
            rootGrid.Children.Add(mainContainer);

            this.Content = rootGrid;
        }

        private Border BuildDpiCard()
        {
            Border card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(22, 23, 30)),
                CornerRadius = new CornerRadius(9),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 37, 48)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(18, 8, 18, 4),
                Padding = new Thickness(15, 10, 15, 10)
            };

            StackPanel stack = new StackPanel();

            // Header: title left, big DPI value right
            Grid header = new Grid();
            TextBlock txtTitle = new TextBlock
            {
                Text = Loc.T("ЧУВСТВИТЕЛЬНОСТЬ", "SENSITIVITY", "ЧУТЛИВІСТЬ"),
                Foreground = new SolidColorBrush(Color.FromRgb(125, 133, 152)),
                FontWeight = FontWeights.Bold,
                FontSize = 10.5,
                VerticalAlignment = VerticalAlignment.Center
            };

            StackPanel valStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            txtDpiValue = new TextBlock
            {
                Text = currentDpi.ToString(),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 23
            };
            TextBlock txtDpiUnit = new TextBlock
            {
                Text = " DPI",
                Foreground = new SolidColorBrush(Color.FromRgb(28, 168, 222)),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(2, 0, 0, 3)
            };
            valStack.Children.Add(txtDpiValue);
            valStack.Children.Add(txtDpiUnit);

            header.Children.Add(txtTitle);
            header.Children.Add(valStack);
            stack.Children.Add(header);

            // Slider
            sliderCanvas = new Canvas
            {
                Height = 20,
                Margin = new Thickness(0, 5, 0, 7),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand
            };

            sliderTrack = new Border
            {
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.FromRgb(36, 39, 50))
            };

            sliderFill = new Border
            {
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.FromRgb(28, 168, 222))
            };

            sliderThumb = new Border
            {
                Width = 14,
                Height = 14,
                CornerRadius = new CornerRadius(7),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(28, 168, 222)),
                BorderThickness = new Thickness(2)
            };

            sliderCanvas.Children.Add(sliderTrack);
            sliderCanvas.Children.Add(sliderFill);
            sliderCanvas.Children.Add(sliderThumb);

            sliderCanvas.SizeChanged += (s, e) => UpdateSliderVisuals(currentDpi);
            sliderCanvas.MouseLeftButtonDown += (s, e) =>
            {
                isDraggingSlider = true;
                sliderCanvas.CaptureMouse();
                HandleSliderInput(e.GetPosition(sliderCanvas).X);
            };
            sliderCanvas.MouseMove += (s, e) =>
            {
                if (isDraggingSlider) HandleSliderInput(e.GetPosition(sliderCanvas).X);
            };
            sliderCanvas.MouseLeftButtonUp += (s, e) =>
            {
                if (isDraggingSlider)
                {
                    isDraggingSlider = false;
                    sliderCanvas.ReleaseMouseCapture();
                    device.SetDpi(currentDpi, currentDpi);
                }
            };
            stack.Children.Add(sliderCanvas);

            // Presets
            WrapPanel presetPanel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0)
            };

            presetChips = new Border[presetValues.Length];
            for (int i = 0; i < presetValues.Length; i++)
            {
                int val = presetValues[i];
                Border chip = CreatePresetChip(val);
                presetChips[i] = chip;
                presetPanel.Children.Add(chip);
            }
            stack.Children.Add(presetPanel);

            card.Child = stack;
            return card;
        }

        private Border BuildPollingRateCard()
        {
            Border card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(22, 23, 30)),
                CornerRadius = new CornerRadius(9),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 37, 48)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(18, 3, 18, 4),
                Padding = new Thickness(15, 10, 15, 10)
            };

            StackPanel stack = new StackPanel();

            Grid header = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            TextBlock txtTitle = new TextBlock
            {
                Text = Loc.T("ЧАСТОТА ОПРОСА (USB)", "POLLING RATE (USB)", "ЧАСТОТА ОПИТУВАННЯ (USB)"),
                Foreground = new SolidColorBrush(Color.FromRgb(125, 133, 152)),
                FontWeight = FontWeights.Bold,
                FontSize = 10.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            txtPollSub = new TextBlock
            {
                Text = currentHz + " " + Loc.T("Гц", "Hz", "Гц"),
                Foreground = new SolidColorBrush(Color.FromRgb(28, 168, 222)),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(txtTitle);
            header.Children.Add(txtPollSub);
            stack.Children.Add(header);

            Border segContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 16, 21)),
                CornerRadius = new CornerRadius(7),
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 33, 44)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(2.5)
            };

            Grid segGrid = new Grid();
            segGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            segGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            segGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            string hzUnit = Loc.T("Гц", "Hz", "Гц");
            btnHz125 = CreateSegmentButton("125 " + hzUnit, 125, out txtHz125);
            btnHz500 = CreateSegmentButton("500 " + hzUnit, 500, out txtHz500);
            btnHz1000 = CreateSegmentButton("1000 " + hzUnit, 1000, out txtHz1000);

            Grid.SetColumn(btnHz125, 0);
            Grid.SetColumn(btnHz500, 1);
            Grid.SetColumn(btnHz1000, 2);

            segGrid.Children.Add(btnHz125);
            segGrid.Children.Add(btnHz500);
            segGrid.Children.Add(btnHz1000);

            segContainer.Child = segGrid;
            stack.Children.Add(segContainer);

            card.Child = stack;
            return card;
        }

        private Border BuildLightingCard()
        {
            Border card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(22, 23, 30)),
                CornerRadius = new CornerRadius(9),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 37, 48)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(18, 3, 18, 4),
                Padding = new Thickness(15, 10, 15, 10)
            };

            StackPanel stack = new StackPanel();

            // Header row
            Grid header = new Grid { Margin = new Thickness(0, 0, 0, 7) };
            StackPanel headerLeft = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock txtTitle = new TextBlock
            {
                Text = Loc.T("ПОДСВЕТКА", "LIGHTING", "ПІДСВІЧУВАННЯ"),
                Foreground = new SolidColorBrush(Color.FromRgb(125, 133, 152)),
                FontWeight = FontWeights.Bold,
                FontSize = 10.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            colorPreviewDot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(currentColor),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            headerLeft.Children.Add(txtTitle);
            headerLeft.Children.Add(colorPreviewDot);

            // On/Off Segmented Toggle
            Border onOffContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 16, 21)),
                CornerRadius = new CornerRadius(6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 33, 44)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            StackPanel onOffStack = new StackPanel { Orientation = Orientation.Horizontal };

            btnLightOn = CreateLightToggleBtn(Loc.T("Статика", "Static", "Статика"), true, out txtLightOn);
            btnLightSpectrum = CreateLightToggleBtn(Loc.T("Спектр", "Spectrum", "Спектр"), false, out txtLightSpectrum);
            btnLightOff = CreateLightToggleBtn(Loc.T("Выкл", "Off", "Вимк"), false, out txtLightOff);

            btnLightOn.MouseLeftButtonDown += (s, e) => SetLightingEnabled(true);
            btnLightSpectrum.MouseLeftButtonDown += (s, e) => ApplySpectrumMode();
            btnLightOff.MouseLeftButtonDown += (s, e) => SetLightingEnabled(false);

            onOffStack.Children.Add(btnLightOn);
            onOffStack.Children.Add(btnLightSpectrum);
            onOffStack.Children.Add(btnLightOff);
            onOffContainer.Child = onOffStack;

            header.Children.Add(headerLeft);
            header.Children.Add(onOffContainer);
            stack.Children.Add(header);

            // --- SPECTRUM GRADIENT BAR ---
            spectrumCanvas = new Canvas
            {
                Height = 18,
                Margin = new Thickness(0, 3, 0, 7),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand
            };

            LinearGradientBrush spectrumBrush = new LinearGradientBrush();
            spectrumBrush.StartPoint = new Point(0, 0.5);
            spectrumBrush.EndPoint = new Point(1, 0.5);
            spectrumBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 0), 0.0));
            spectrumBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 255, 0), 0.167));
            spectrumBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 0), 0.333));
            spectrumBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 255), 0.500));
            spectrumBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 255), 0.667));
            spectrumBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 255), 0.833));
            spectrumBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 0), 1.0));

            spectrumTrack = new Border
            {
                Height = 7,
                CornerRadius = new CornerRadius(3.5),
                Background = spectrumBrush,
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 42, 54)),
                BorderThickness = new Thickness(1)
            };

            spectrumThumb = new Border
            {
                Width = 15,
                Height = 15,
                CornerRadius = new CornerRadius(7.5),
                Background = new SolidColorBrush(currentColor),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 4,
                    ShadowDepth = 1,
                    Opacity = 0.5
                }
            };

            spectrumCanvas.Children.Add(spectrumTrack);
            spectrumCanvas.Children.Add(spectrumThumb);

            spectrumCanvas.SizeChanged += (s, e) => UpdateSpectrumThumb(currentColor);
            spectrumCanvas.MouseLeftButtonDown += (s, e) =>
            {
                isDraggingSpectrum = true;
                spectrumCanvas.CaptureMouse();
                HandleSpectrumInput(e.GetPosition(spectrumCanvas).X);
            };
            spectrumCanvas.MouseMove += (s, e) =>
            {
                if (isDraggingSpectrum) HandleSpectrumInput(e.GetPosition(spectrumCanvas).X);
            };
            spectrumCanvas.MouseLeftButtonUp += (s, e) =>
            {
                if (isDraggingSpectrum)
                {
                    isDraggingSpectrum = false;
                    spectrumCanvas.ReleaseMouseCapture();
                    device.SetStaticColor(currentColor.R, currentColor.G, currentColor.B);
                    SaveSettings();
                }
            };
            stack.Children.Add(spectrumCanvas);

            // --- QUICK SWATCHES & CUSTOM BUTTON ROW ---
            Grid swatchRow = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            swatchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            swatchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel quickPills = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            Color[] quickColors = new Color[]
            {
                Color.FromRgb(255, 0, 0),    // Pure Red (255, 0, 0)
                Color.FromRgb(0, 255, 0),    // Razer Green (0, 255, 0)
                Color.FromRgb(0, 100, 255),  // Deep Blue (0, 100, 255)
                Color.FromRgb(255, 255, 255) // Pure White (255, 255, 255)
            };

            colorChips = new Border[quickColors.Length];
            for (int i = 0; i < quickColors.Length; i++)
            {
                Color qc = quickColors[i];
                Border pill = CreateColorChip(qc);
                colorChips[i] = pill;
                quickPills.Children.Add(pill);
            }
            Grid.SetColumn(quickPills, 0);
            swatchRow.Children.Add(quickPills);

            // Custom Color Picker Button
            Border btnCustomColor = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(24, 26, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(36, 40, 52)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8, 3, 8, 3),
                Cursor = Cursors.Hand
            };
            TextBlock txtCustom = new TextBlock
            {
                Text = Loc.T("🎨 Другой...", "🎨 Custom...", "🎨 Інший..."),
                Foreground = new SolidColorBrush(Color.FromRgb(180, 186, 204)),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold
            };
            btnCustomColor.Child = txtCustom;
            btnCustomColor.MouseLeftButtonDown += (s, e) => PickCustomColor();
            btnCustomColor.MouseEnter += (s, e) => btnCustomColor.Background = new SolidColorBrush(Color.FromRgb(32, 36, 48));
            btnCustomColor.MouseLeave += (s, e) => btnCustomColor.Background = new SolidColorBrush(Color.FromRgb(24, 26, 34));
            Grid.SetColumn(btnCustomColor, 1);
            swatchRow.Children.Add(btnCustomColor);

            stack.Children.Add(swatchRow);

            // Brightness Row
            Grid briGrid = new Grid { Margin = new Thickness(0, 5, 0, 1) };
            briGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            briGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            briGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock txtBriTitle = new TextBlock
            {
                Text = Loc.T("ЯРКОСТЬ", "BRIGHTNESS", "ЯСКРАВІСТЬ"),
                Foreground = new SolidColorBrush(Color.FromRgb(125, 133, 152)),
                FontWeight = FontWeights.Bold,
                FontSize = 10.5,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetColumn(txtBriTitle, 0);
            briGrid.Children.Add(txtBriTitle);

            briSliderCanvas = new Canvas
            {
                Height = 20,
                Margin = new Thickness(0, 0, 0, 0),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand
            };

            briSliderTrack = new Border
            {
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.FromRgb(36, 39, 50))
            };

            briSliderFill = new Border
            {
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.FromRgb(28, 168, 222))
            };

            briSliderThumb = new Border
            {
                Width = 14,
                Height = 14,
                CornerRadius = new CornerRadius(7),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(28, 168, 222)),
                BorderThickness = new Thickness(2)
            };

            briSliderCanvas.Children.Add(briSliderTrack);
            briSliderCanvas.Children.Add(briSliderFill);
            briSliderCanvas.Children.Add(briSliderThumb);

            briSliderCanvas.SizeChanged += (s, e) => UpdateBrightnessVisuals(currentBrightness);
            briSliderCanvas.MouseLeftButtonDown += (s, e) =>
            {
                isDraggingBriSlider = true;
                briSliderCanvas.CaptureMouse();
                HandleBriSliderInput(e.GetPosition(briSliderCanvas).X);
            };
            briSliderCanvas.MouseMove += (s, e) =>
            {
                if (isDraggingBriSlider) HandleBriSliderInput(e.GetPosition(briSliderCanvas).X);
            };
            briSliderCanvas.MouseLeftButtonUp += (s, e) =>
            {
                if (isDraggingBriSlider)
                {
                    isDraggingBriSlider = false;
                    briSliderCanvas.ReleaseMouseCapture();
                    device.SetBrightness(currentBrightness);
                    SaveSettings();
                }
            };
            Grid.SetColumn(briSliderCanvas, 1);
            briGrid.Children.Add(briSliderCanvas);

            txtBrightnessValue = new TextBlock
            {
                Text = currentBrightness + "%",
                Foreground = new SolidColorBrush(Color.FromRgb(28, 168, 222)),
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                Width = 38,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(txtBrightnessValue, 2);
            briGrid.Children.Add(txtBrightnessValue);

            stack.Children.Add(briGrid);

            card.Child = stack;
            return card;
        }

        private static Color GetSpectrumColor(double t)
        {
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            double h = t * 360.0;
            if (h >= 360.0) h = 0.0;
            return HsvToRgb(h, 1.0, 1.0);
        }

        private static Color HsvToRgb(double h, double s, double v)
        {
            int hi = (int)(Math.Floor(h / 60.0)) % 6;
            double f = (h / 60.0) - Math.Floor(h / 60.0);
            byte vVal = (byte)Math.Round(v * 255.0);
            byte p = (byte)Math.Round(v * (1.0 - s) * 255.0);
            byte q = (byte)Math.Round(v * (1.0 - f * s) * 255.0);
            byte t = (byte)Math.Round(v * (1.0 - (1.0 - f) * s) * 255.0);

            switch (hi)
            {
                case 0: return Color.FromRgb(vVal, t, p);
                case 1: return Color.FromRgb(q, vVal, p);
                case 2: return Color.FromRgb(p, vVal, t);
                case 3: return Color.FromRgb(p, q, vVal);
                case 4: return Color.FromRgb(t, p, vVal);
                default: return Color.FromRgb(vVal, p, q);
            }
        }

        private void HandleSpectrumInput(double x)
        {
            double w = spectrumCanvas.ActualWidth;
            if (w <= 0) return;
            double t = x / w;
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            Color c = GetSpectrumColor(t);
            ApplyMouseColor(c);
        }

        private void UpdateSpectrumThumb(Color c)
        {
            if (spectrumThumb != null)
            {
                spectrumThumb.Background = new SolidColorBrush(c);
            }
            if (spectrumCanvas == null) return;
            double w = spectrumCanvas.ActualWidth;
            if (w <= 0) return;

            spectrumTrack.Width = w;
            Canvas.SetLeft(spectrumTrack, 0);
            Canvas.SetTop(spectrumTrack, 5);

            System.Drawing.Color gdi = System.Drawing.Color.FromArgb(c.R, c.G, c.B);
            float sat = gdi.GetSaturation();
            if (sat < 0.15f)
            {
                // Grayscale/White: hide thumb on rainbow spectrum
                spectrumThumb.Visibility = Visibility.Collapsed;
                return;
            }

            spectrumThumb.Visibility = Visibility.Visible;
            double t = gdi.GetHue() / 360.0;
            if (t < 0) t = 0;
            if (t > 1) t = 1;

            double thumbX = t * w - (spectrumThumb.Width / 2);
            if (thumbX < 0) thumbX = 0;
            if (thumbX > w - spectrumThumb.Width) thumbX = w - spectrumThumb.Width;

            Canvas.SetLeft(spectrumThumb, thumbX);
            Canvas.SetTop(spectrumThumb, 1);
        }

        private void HandleBriSliderInput(double x)
        {
            double w = briSliderCanvas.ActualWidth;
            if (w <= 0) return;
            double ratio = x / w;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;
            int val = (int)Math.Round(ratio * 100);
            UpdateBrightness(val, true);
        }

        private void UpdateBrightness(int val, bool applyHardware)
        {
            if (val < 0) val = 0;
            if (val > 100) val = 100;
            currentBrightness = val;
            UpdateBrightnessVisuals(val);
            if (applyHardware)
            {
                device.SetBrightness(val);
                if (val == 0)
                {
                    isLightingEnabled = false;
                    btnLightOff.Background = new SolidColorBrush(Color.FromRgb(200, 40, 50));
                    txtLightOff.Foreground = Brushes.White;
                    txtLightOff.FontWeight = FontWeights.Bold;
                    btnLightOn.Background = Brushes.Transparent;
                    txtLightOn.Foreground = new SolidColorBrush(Color.FromRgb(136, 142, 158));
                    txtLightOn.FontWeight = FontWeights.Normal;
                    btnLightSpectrum.Background = Brushes.Transparent;
                    txtLightSpectrum.Foreground = new SolidColorBrush(Color.FromRgb(136, 142, 158));
                    txtLightSpectrum.FontWeight = FontWeights.Normal;
                    colorPreviewDot.Fill = new SolidColorBrush(Color.FromRgb(36, 38, 46));
                }
                else
                {
                    if (!isLightingEnabled)
                    {
                        isLightingEnabled = true;
                        if (isSpectrumMode)
                        {
                            ApplySpectrumMode();
                        }
                        else
                        {
                            ApplyMouseColor(currentColor);
                        }
                    }
                }
                SaveSettings();
            }
        }

        private void UpdateBrightnessVisuals(int bri)
        {
            if (txtBrightnessValue != null)
            {
                txtBrightnessValue.Text = bri + "%";
            }
            if (briSliderCanvas == null) return;
            double w = briSliderCanvas.ActualWidth;
            if (w <= 0) return;

            double ratio = bri / 100.0;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;

            briSliderTrack.Width = w;
            Canvas.SetLeft(briSliderTrack, 0);
            Canvas.SetTop(briSliderTrack, 8);

            double fillWidth = ratio * w;
            briSliderFill.Width = fillWidth;
            Canvas.SetLeft(briSliderFill, 0);
            Canvas.SetTop(briSliderFill, 8);

            double thumbX = fillWidth - (briSliderThumb.Width / 2);
            if (thumbX < 0) thumbX = 0;
            if (thumbX > w - briSliderThumb.Width) thumbX = w - briSliderThumb.Width;

            Canvas.SetLeft(briSliderThumb, thumbX);
            Canvas.SetTop(briSliderThumb, 3);
        }

        private Border CreateLightToggleBtn(string text, bool isOn, out TextBlock txtOut)
        {
            Border b = new Border
            {
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10, 4, 10, 4),
                Cursor = Cursors.Hand,
                Background = isOn ? new SolidColorBrush(Color.FromRgb(28, 168, 222)) : Brushes.Transparent
            };
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = isOn ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isOn ? new SolidColorBrush(Color.FromRgb(13, 14, 18)) : new SolidColorBrush(Color.FromRgb(136, 142, 158))
            };
            b.Child = tb;
            txtOut = tb;
            return b;
        }

        private Border CreateColorChip(Color c)
        {
            bool isWhite = (c.R == 255 && c.G == 255 && c.B == 255);
            Border chip = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(c),
                BorderBrush = (c == currentColor && isLightingEnabled && !isSpectrumMode)
                    ? (isWhite ? new SolidColorBrush(Color.FromRgb(28, 168, 222)) : Brushes.White)
                    : (isWhite ? new SolidColorBrush(Color.FromRgb(100, 105, 120)) : new SolidColorBrush(Color.FromRgb(36, 40, 52))),
                BorderThickness = (c == currentColor && isLightingEnabled && !isSpectrumMode) ? new Thickness(2.5) : new Thickness(1),
                Margin = new Thickness(0, 0, 9, 0),
                Cursor = Cursors.Hand,
                Tag = c
            };

            chip.MouseLeftButtonDown += (s, e) => ApplyMouseColor(c);
            return chip;
        }

        private void SetLightingEnabled(bool enabled)
        {
            isLightingEnabled = enabled;

            if (enabled)
            {
                isSpectrumMode = false;
                btnLightOn.Background = new SolidColorBrush(Color.FromRgb(28, 168, 222));
                txtLightOn.Foreground = new SolidColorBrush(Color.FromRgb(13, 14, 18));
                txtLightOn.FontWeight = FontWeights.Bold;

                btnLightSpectrum.Background = Brushes.Transparent;
                txtLightSpectrum.Foreground = new SolidColorBrush(Color.FromRgb(136, 142, 158));
                txtLightSpectrum.FontWeight = FontWeights.Normal;

                btnLightOff.Background = Brushes.Transparent;
                txtLightOff.Foreground = new SolidColorBrush(Color.FromRgb(136, 142, 158));
                txtLightOff.FontWeight = FontWeights.Normal;

                colorPreviewDot.Fill = new SolidColorBrush(currentColor);
                if (currentBrightness == 0)
                {
                    currentBrightness = 100;
                    UpdateBrightnessVisuals(100);
                }
                device.SetBrightness(currentBrightness);
                ApplyMouseColor(currentColor);
            }
            else
            {
                isSpectrumMode = false;
                btnLightOff.Background = new SolidColorBrush(Color.FromRgb(200, 40, 50));
                txtLightOff.Foreground = Brushes.White;
                txtLightOff.FontWeight = FontWeights.Bold;

                btnLightOn.Background = Brushes.Transparent;
                txtLightOn.Foreground = new SolidColorBrush(Color.FromRgb(136, 142, 158));
                txtLightOn.FontWeight = FontWeights.Normal;

                btnLightSpectrum.Background = Brushes.Transparent;
                txtLightSpectrum.Foreground = new SolidColorBrush(Color.FromRgb(136, 142, 158));
                txtLightSpectrum.FontWeight = FontWeights.Normal;

                colorPreviewDot.Fill = new SolidColorBrush(Color.FromRgb(36, 38, 46));
                device.SetLightingOff();
                ShowToast(Loc.T("Подсветка выключена", "Lighting turned off", "Підсвічування вимкнено"), false);
            }
            UpdateColorChipsHighlight();
            UpdateSpectrumThumb(currentColor);
        }

        private void ApplySpectrumMode()
        {
            isLightingEnabled = true;
            isSpectrumMode = true;

            btnLightSpectrum.Background = new SolidColorBrush(Color.FromRgb(36, 44, 62));
            txtLightSpectrum.Foreground = new SolidColorBrush(Color.FromRgb(80, 180, 255));
            txtLightSpectrum.FontWeight = FontWeights.Bold;

            btnLightOn.Background = Brushes.Transparent;
            txtLightOn.Foreground = new SolidColorBrush(Color.FromRgb(136, 142, 158));
            txtLightOn.FontWeight = FontWeights.Normal;

            btnLightOff.Background = Brushes.Transparent;
            txtLightOff.Foreground = new SolidColorBrush(Color.FromRgb(136, 142, 158));
            txtLightOff.FontWeight = FontWeights.Normal;

            LinearGradientBrush rainbowBrush = new LinearGradientBrush();
            rainbowBrush.StartPoint = new Point(0, 0);
            rainbowBrush.EndPoint = new Point(1, 1);
            rainbowBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 60, 60), 0.0));
            rainbowBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 230, 60), 0.33));
            rainbowBrush.GradientStops.Add(new GradientStop(Color.FromRgb(60, 255, 120), 0.66));
            rainbowBrush.GradientStops.Add(new GradientStop(Color.FromRgb(60, 180, 255), 1.0));
            colorPreviewDot.Fill = rainbowBrush;

            device.SetSpectrumCycling();
            UpdateColorChipsHighlight();

            if (IsSynapseActive())
            {
                ShowToast(Loc.T("⚠️ Закройте Synapse в трее", "⚠️ Close Synapse in tray", "⚠️ Закрийте Synapse у треї"), true);
            }
            else
            {
                ShowToast(Loc.T("🌈 Спектр (Радуга)", "🌈 Spectrum Cycling", "🌈 Спектр (Веселка)"), false);
            }
        }

        private void ApplyMouseColor(Color c)
        {
            currentColor = c;
            isLightingEnabled = true;
            isSpectrumMode = false;

            btnLightOn.Background = new SolidColorBrush(Color.FromRgb(28, 168, 222));
            txtLightOn.Foreground = new SolidColorBrush(Color.FromRgb(13, 14, 18));
            txtLightOn.FontWeight = FontWeights.Bold;

            btnLightSpectrum.Background = Brushes.Transparent;
            txtLightSpectrum.Foreground = new SolidColorBrush(Color.FromRgb(136, 142, 158));
            txtLightSpectrum.FontWeight = FontWeights.Normal;

            btnLightOff.Background = Brushes.Transparent;
            txtLightOff.Foreground = new SolidColorBrush(Color.FromRgb(136, 142, 158));
            txtLightOff.FontWeight = FontWeights.Normal;

            colorPreviewDot.Fill = new SolidColorBrush(c);

            device.SetStaticColor(c.R, c.G, c.B);
            UpdateColorChipsHighlight();
            UpdateSpectrumThumb(c);

            if (IsSynapseActive())
            {
                ShowToast(Loc.T("⚠️ Закройте Synapse в трее", "⚠️ Close Synapse in tray", "⚠️ Закрийте Synapse у треї"), true);
            }
        }

        private void PickCustomColor()
        {
            using (var cd = new System.Windows.Forms.ColorDialog())
            {
                cd.Color = System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B);
                cd.FullOpen = true;
                if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Color chosen = Color.FromRgb(cd.Color.R, cd.Color.G, cd.Color.B);
                    ApplyMouseColor(chosen);
                }
            }
        }

        private void UpdateColorChipsHighlight()
        {
            if (colorChips == null) return;
            foreach (var chip in colorChips)
            {
                Color c = (Color)chip.Tag;
                bool isWhite = (c.R == 255 && c.G == 255 && c.B == 255);
                if (isLightingEnabled && !isSpectrumMode && c == currentColor)
                {
                    chip.BorderBrush = isWhite ? new SolidColorBrush(Color.FromRgb(28, 168, 222)) : Brushes.White;
                    chip.BorderThickness = new Thickness(2.5);
                }
                else
                {
                    chip.BorderBrush = isWhite ? new SolidColorBrush(Color.FromRgb(100, 105, 120)) : new SolidColorBrush(Color.FromRgb(36, 40, 52));
                    chip.BorderThickness = new Thickness(1);
                }
            }
        }

        private Border CreateAuthorBadge()
        {
            Border badge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 23, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(32, 38, 52)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 5, 9, 5),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = Cursors.Hand,
                ToolTip = Loc.T("Открыть Telegram автора: ", "Open author's Telegram: ", "Відкрити Telegram автора: ") + AUTHOR_URL
            };

            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            Image imgTg = new Image
            {
                Width = 15,
                Height = 15,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Source = LoadTelegramIcon()
            };

            TextBlock tUser = new TextBlock
            {
                Text = "@Carist",
                Foreground = new SolidColorBrush(Color.FromRgb(70, 170, 255)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            sp.Children.Add(imgTg);
            sp.Children.Add(tUser);
            badge.Child = sp;

            badge.MouseLeftButtonDown += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(AUTHOR_URL) { UseShellExecute = true }); } catch { }
            };

            badge.MouseEnter += (s, e) =>
            {
                badge.Background = new SolidColorBrush(Color.FromRgb(26, 32, 44));
                badge.BorderBrush = new SolidColorBrush(Color.FromRgb(70, 170, 255));
            };
            badge.MouseLeave += (s, e) =>
            {
                badge.Background = new SolidColorBrush(Color.FromRgb(20, 23, 30));
                badge.BorderBrush = new SolidColorBrush(Color.FromRgb(32, 38, 52));
            };

            return badge;
        }

        private Border CreatePresetChip(int val)
        {
            Border chip = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(24, 26, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(36, 40, 52)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 5, 12, 5),
                Margin = new Thickness(3, 2, 3, 2),
                Cursor = Cursors.Hand,
                Tag = val
            };
            TextBlock txt = new TextBlock
            {
                Text = val.ToString(),
                Foreground = new SolidColorBrush(Color.FromRgb(170, 176, 195)),
                FontWeight = FontWeights.SemiBold,
                FontSize = 11.5
            };
            chip.Child = txt;

            chip.MouseLeftButtonDown += (s, e) => SetDpiValue(val);
            chip.MouseEnter += (s, e) =>
            {
                if (currentDpi != (int)chip.Tag)
                    chip.Background = new SolidColorBrush(Color.FromRgb(32, 36, 48));
            };
            chip.MouseLeave += (s, e) => UpdateChipsHighlight();

            return chip;
        }

        private Border CreateSegmentButton(string text, int hz, out TextBlock txtOut)
        {
            Border seg = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = Brushes.Transparent,
                Padding = new Thickness(0, 6, 0, 6),
                Cursor = Cursors.Hand,
                Tag = hz
            };
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontSize = 11.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(136, 142, 158))
            };
            seg.Child = tb;
            txtOut = tb;

            int rate = hz;
            seg.MouseLeftButtonDown += (s, e) => SetPollingRateValue(rate);
            return seg;
        }

        private void SetPollingRateValue(int hz)
        {
            currentHz = hz;
            if (txtPollSub != null) txtPollSub.Text = hz + " " + Loc.T("Гц", "Hz", "Гц");
            UpdatePollingRateVisuals();
        }

        private void UpdatePollingRateVisuals()
        {
            UpdateSegmentState(btnHz125, txtHz125, currentHz == 125);
            UpdateSegmentState(btnHz500, txtHz500, currentHz == 500);
            UpdateSegmentState(btnHz1000, txtHz1000, currentHz == 1000);
        }

        private void UpdateSegmentState(Border seg, TextBlock tb, bool isSelected)
        {
            if (seg == null || tb == null) return;
            if (isSelected)
            {
                seg.Background = new SolidColorBrush(Color.FromRgb(28, 168, 222));
                tb.Foreground = new SolidColorBrush(Color.FromRgb(13, 14, 18));
                tb.FontWeight = FontWeights.Bold;
            }
            else
            {
                seg.Background = Brushes.Transparent;
                tb.Foreground = new SolidColorBrush(Color.FromRgb(136, 142, 158));
                tb.FontWeight = FontWeights.Normal;
            }
        }

        private void SetDpiValue(int dpi)
        {
            if (dpi < MIN_DPI) dpi = MIN_DPI;
            if (dpi > MAX_DPI) dpi = MAX_DPI;
            currentDpi = dpi;
            txtDpiValue.Text = dpi.ToString();
            UpdateSliderVisuals(dpi);
            UpdateChipsHighlight();
        }

        private void UpdateChipsHighlight()
        {
            for (int i = 0; i < presetChips.Length; i++)
            {
                Border chip = presetChips[i];
                int val = (int)chip.Tag;
                TextBlock txt = chip.Child as TextBlock;
                if (val == currentDpi)
                {
                    chip.Background = new SolidColorBrush(Color.FromRgb(18, 36, 50));
                    chip.BorderBrush = new SolidColorBrush(Color.FromRgb(28, 168, 222));
                    txt.Foreground = new SolidColorBrush(Color.FromRgb(28, 168, 222));
                    txt.FontWeight = FontWeights.Bold;
                }
                else
                {
                    chip.Background = new SolidColorBrush(Color.FromRgb(24, 26, 34));
                    chip.BorderBrush = new SolidColorBrush(Color.FromRgb(36, 40, 52));
                    txt.Foreground = new SolidColorBrush(Color.FromRgb(170, 176, 195));
                    txt.FontWeight = FontWeights.SemiBold;
                }
            }
        }

        private void HandleSliderInput(double mouseX)
        {
            double w = sliderCanvas.ActualWidth;
            if (w <= 0) return;

            double ratio = mouseX / w;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;

            int dpi = (int)(MIN_DPI + ratio * (MAX_DPI - MIN_DPI));
            dpi = (dpi / 50) * 50;
            if (dpi < MIN_DPI) dpi = MIN_DPI;
            if (dpi > MAX_DPI) dpi = MAX_DPI;

            SetDpiValue(dpi);
        }

        private void UpdateSliderVisuals(int dpi)
        {
            double w = sliderCanvas.ActualWidth;
            if (w <= 0) return;

            double ratio = (double)(dpi - MIN_DPI) / (MAX_DPI - MIN_DPI);
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;

            sliderTrack.Width = w;
            Canvas.SetLeft(sliderTrack, 0);
            Canvas.SetTop(sliderTrack, 8);

            double fillWidth = ratio * w;
            sliderFill.Width = fillWidth;
            Canvas.SetLeft(sliderFill, 0);
            Canvas.SetTop(sliderFill, 8);

            double thumbX = fillWidth - (sliderThumb.Width / 2);
            if (thumbX < 0) thumbX = 0;
            if (thumbX > w - sliderThumb.Width) thumbX = w - sliderThumb.Width;

            Canvas.SetLeft(sliderThumb, thumbX);
            Canvas.SetTop(sliderThumb, 3);
        }

        private Border CreateMinButton(string text, Action onClick, bool isClose = false)
        {
            Border btn = new Border
            {
                Width = 28,
                Height = 26,
                CornerRadius = new CornerRadius(5),
                Background = Brushes.Transparent,
                Margin = new Thickness(2, 0, 2, 0),
                Cursor = Cursors.Hand
            };
            TextBlock tb = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 146, 162)),
                FontSize = isClose ? 11 : 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.Child = tb;
            btn.MouseLeftButtonDown += (s, e) => onClick();
            btn.MouseEnter += (s, e) =>
            {
                if (isClose)
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(210, 45, 45));
                    tb.Foreground = Brushes.White;
                }
                else
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(36, 40, 52));
                    tb.Foreground = Brushes.White;
                }
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.Background = Brushes.Transparent;
                tb.Foreground = new SolidColorBrush(Color.FromRgb(140, 146, 162));
            };
            return btn;
        }

        private CheckBox CreateModernCheckBox(string text, bool isChecked)
        {
            return new CheckBox
            {
                Content = text,
                IsChecked = isChecked,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 148, 166)),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = Cursors.Hand
            };
        }

        private void ApplyHardwareSettings()
        {
            bool okDpi = device.SetDpi(currentDpi, currentDpi);
            bool okHz = device.SetPollingRate(currentHz);

            if (!isLightingEnabled)
            {
                device.SetLightingOff();
            }
            else
            {
                device.SetBrightness(currentBrightness);
                if (isSpectrumMode)
                {
                    device.SetSpectrumCycling();
                }
                else
                {
                    device.SetStaticColor(currentColor.R, currentColor.G, currentColor.B);
                }
            }

            if (IsSynapseActive())
            {
                ShowToast(Loc.T("⚠️ Закройте Synapse в трее", "⚠️ Close Synapse in tray", "⚠️ Закрийте Synapse у треї"), true);
                return;
            }

            if (okDpi && okHz)
            {
                ShowToast(Loc.T("✔ Настройки сохранены", "✔ Settings saved", "✔ Налаштування збережено"), false);
                UpdateTrayTooltip();
            }
            else if (!device.IsConnected)
            {
                ShowToast(Loc.T("✖ Мышь не найдена", "✖ Mouse not found", "✖ Миша не знайдена"), true);
            }
            else
            {
                ShowToast(Loc.T("✖ Ошибка записи", "✖ Write error", "✖ Помилка запису"), true);
            }
        }

        public void RefreshFromDevice()
        {
            if (device.Connect())
            {
                statusIndicator.Fill = new SolidColorBrush(Color.FromRgb(28, 168, 222));
                txtStatusText.Text = "RAZER DIAMONDBACK";
                txtStatusText.Foreground = Brushes.White;
                txtSensorName.Text = "Philips PLN 2034 • 16K DPI";
                txtSensorName.Foreground = new SolidColorBrush(Color.FromRgb(120, 128, 146));

                int dpiX, dpiY;
                if (device.GetDpi(out dpiX, out dpiY))
                {
                    SetDpiValue(dpiX);
                }

                int hz;
                if (device.GetPollingRate(out hz))
                {
                    SetPollingRateValue(hz);
                }

                device.SetBrightness(currentBrightness);
                UpdateTrayTooltip();
                ShowToast(Loc.T("Считано с мыши", "Read from mouse", "Зчитано з миші"), false);
            }
            else
            {
                statusIndicator.Fill = new SolidColorBrush(Color.FromRgb(220, 50, 50));
                txtStatusText.Text = Loc.T("МЫШЬ НЕ НАЙДЕНА", "MOUSE NOT FOUND", "МИША НЕ ЗНАЙДЕНА");
                txtStatusText.Foreground = new SolidColorBrush(Color.FromRgb(240, 90, 90));
                txtSensorName.Text = Loc.T("Проверьте USB", "Check USB cable", "Перевірте USB");
                txtSensorName.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160));
                ShowToast(Loc.T("Мышь не обнаружена", "Mouse not found", "Миша не виявлена"), true);
            }
        }

        private bool IsSynapseActive()
        {
            try
            {
                var procs = Process.GetProcesses();
                foreach (var p in procs)
                {
                    string n = p.ProcessName.ToLowerInvariant();
                    if (n.Contains("rzsynapse") || n == "synapse")
                    {
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private void ShowToast(string message, bool isError)
        {
            toastText.Text = message;
            toastText.Foreground = isError ? new SolidColorBrush(Color.FromRgb(255, 90, 90)) : new SolidColorBrush(Color.FromRgb(28, 168, 222));
            toastBorder.BorderBrush = isError ? new SolidColorBrush(Color.FromRgb(180, 50, 50)) : new SolidColorBrush(Color.FromRgb(28, 168, 222));
            toastBorder.Background = isError ? new SolidColorBrush(Color.FromRgb(36, 18, 20)) : new SolidColorBrush(Color.FromRgb(16, 28, 40));
            toastBorder.Opacity = 1;
            toastTimer.Stop();
            toastTimer.Start();
        }

        private void InitTray()
        {
            trayMenu = new System.Windows.Forms.ContextMenuStrip();
            trayMenu.BackColor = System.Drawing.Color.FromArgb(20, 22, 28);
            trayMenu.ForeColor = System.Drawing.Color.White;
            trayMenu.ShowImageMargin = false;

            var itemOpen = new System.Windows.Forms.ToolStripMenuItem(Loc.T("Панель управления", "Control Panel", "Панель керування"))
            {
                Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold)
            };
            itemOpen.Click += (s, e) => RestoreWindow();
            trayMenu.Items.Add(itemOpen);

            trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var dpiMenu = new System.Windows.Forms.ToolStripMenuItem(Loc.T("Быстрый DPI", "Quick DPI", "Швидкий DPI"));
            foreach (int d in presetValues)
            {
                int val = d;
                var sub = new System.Windows.Forms.ToolStripMenuItem(val + " DPI");
                sub.Click += (s, e) =>
                {
                    SetDpiValue(val);
                    device.SetDpi(val, val);
                    UpdateTrayTooltip();
                    trayIcon.ShowBalloonTip(1500, "Razer Diamondback", Loc.T("Установлен DPI: ", "DPI set to: ", "Встановлено DPI: ") + val, System.Windows.Forms.ToolTipIcon.Info);
                };
                dpiMenu.DropDownItems.Add(sub);
            }
            trayMenu.Items.Add(dpiMenu);

            string hzUnit = Loc.T("Гц", "Hz", "Гц");
            var hzMenu = new System.Windows.Forms.ToolStripMenuItem(Loc.T("Частота опроса", "Polling Rate", "Частота опитування"));
            int[] rates = new int[] { 125, 500, 1000 };
            foreach (int h in rates)
            {
                int rate = h;
                var sub = new System.Windows.Forms.ToolStripMenuItem(rate + " " + hzUnit);
                sub.Click += (s, e) =>
                {
                    SetPollingRateValue(rate);
                    device.SetPollingRate(rate);
                    UpdateTrayTooltip();
                    trayIcon.ShowBalloonTip(1500, "Razer Diamondback", Loc.T("Частота опроса: ", "Polling rate: ", "Частота опитування: ") + rate + " " + hzUnit, System.Windows.Forms.ToolTipIcon.Info);
                };
                hzMenu.DropDownItems.Add(sub);
            }
            trayMenu.Items.Add(hzMenu);

            var briMenu = new System.Windows.Forms.ToolStripMenuItem(Loc.T("Яркость", "Brightness", "Яскравість"));
            var bri100 = new System.Windows.Forms.ToolStripMenuItem(Loc.T("100% (Максимум)", "100% (Maximum)", "100% (Максимум)"));
            bri100.Click += (s, e) => UpdateBrightness(100, true);
            var bri75 = new System.Windows.Forms.ToolStripMenuItem("75%");
            bri75.Click += (s, e) => UpdateBrightness(75, true);
            var bri50 = new System.Windows.Forms.ToolStripMenuItem("50%");
            bri50.Click += (s, e) => UpdateBrightness(50, true);
            var bri25 = new System.Windows.Forms.ToolStripMenuItem("25%");
            bri25.Click += (s, e) => UpdateBrightness(25, true);
            var bri0 = new System.Windows.Forms.ToolStripMenuItem(Loc.T("0% (Выключить)", "0% (Turn Off)", "0% (Вимкнути)"));
            bri0.Click += (s, e) => UpdateBrightness(0, true);
            briMenu.DropDownItems.Add(bri100);
            briMenu.DropDownItems.Add(bri75);
            briMenu.DropDownItems.Add(bri50);
            briMenu.DropDownItems.Add(bri25);
            briMenu.DropDownItems.Add(new System.Windows.Forms.ToolStripSeparator());
            briMenu.DropDownItems.Add(bri0);
            trayMenu.Items.Add(briMenu);

            trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var lightMenu = new System.Windows.Forms.ToolStripMenuItem(Loc.T("Подсветка", "Lighting", "Підсвічування"));
            var itemLightRed = new System.Windows.Forms.ToolStripMenuItem(Loc.T("Красный (Чистый)", "Pure Red", "Червоний (Чистий)"));
            itemLightRed.Click += (s, e) => ApplyMouseColor(Color.FromRgb(255, 0, 0));
            var itemLightGreen = new System.Windows.Forms.ToolStripMenuItem(Loc.T("Зеленый Razer (Чистый)", "Razer Green (Pure)", "Зелений Razer (Чистий)"));
            itemLightGreen.Click += (s, e) => ApplyMouseColor(Color.FromRgb(0, 255, 0));
            var itemLightBlue = new System.Windows.Forms.ToolStripMenuItem(Loc.T("Синий (Глубокий)", "Deep Blue", "Синій (Глибокий)"));
            itemLightBlue.Click += (s, e) => ApplyMouseColor(Color.FromRgb(0, 100, 255));
            var itemLightWhite = new System.Windows.Forms.ToolStripMenuItem(Loc.T("Белый (Чистый)", "Pure White", "Білий (Чистий)"));
            itemLightWhite.Click += (s, e) => ApplyMouseColor(Color.FromRgb(255, 255, 255));
            var itemLightSpectrum = new System.Windows.Forms.ToolStripMenuItem(Loc.T("🌈 Спектр (Радуга)", "🌈 Spectrum (Rainbow)", "🌈 Спектр (Веселка)"));
            itemLightSpectrum.Click += (s, e) => ApplySpectrumMode();
            var itemLightOff = new System.Windows.Forms.ToolStripMenuItem(Loc.T("Выключить подсветку", "Turn Off Lighting", "Вимкнути підсвічування"));
            itemLightOff.Click += (s, e) => SetLightingEnabled(false);

            lightMenu.DropDownItems.Add(itemLightRed);
            lightMenu.DropDownItems.Add(itemLightGreen);
            lightMenu.DropDownItems.Add(itemLightBlue);
            lightMenu.DropDownItems.Add(itemLightWhite);
            lightMenu.DropDownItems.Add(new System.Windows.Forms.ToolStripSeparator());
            lightMenu.DropDownItems.Add(itemLightSpectrum);
            lightMenu.DropDownItems.Add(new System.Windows.Forms.ToolStripSeparator());
            lightMenu.DropDownItems.Add(itemLightOff);
            trayMenu.Items.Add(lightMenu);

            trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var itemExit = new System.Windows.Forms.ToolStripMenuItem(Loc.T("Выход", "Exit", "Вихід"));
            itemExit.Click += (s, e) =>
            {
                trayIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            };
            trayMenu.Items.Add(itemExit);

            trayIcon = new System.Windows.Forms.NotifyIcon();
            trayIcon.Text = "Razer Diamondback Controller";
            trayIcon.ContextMenuStrip = trayMenu;

            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    trayIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    trayIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => RestoreWindow();
        }

        private void RestoreWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
            this.Focus();
        }

        private void HandleCloseButton()
        {
            if (chkMinimizeToTray.IsChecked == true)
            {
                this.Hide();
                trayIcon.ShowBalloonTip(1500, "Razer Diamondback", Loc.T("Приложение работает в трее", "App is running in tray", "Програма працює в треї"), System.Windows.Forms.ToolTipIcon.Info);
            }
            else
            {
                trayIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void UpdateTrayTooltip()
        {
            string hzUnit = Loc.T("Гц", "Hz", "Гц");
            string rateLabel = Loc.T("Частота: ", "Rate: ", "Частота: ");
            string text = "Razer Diamondback\nDPI: " + currentDpi + "\n" + rateLabel + currentHz + " " + hzUnit;
            if (text.Length > 63) text = text.Substring(0, 63);
            trayIcon.Text = text;
        }

        private void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\RazerDiamondbackController"))
                {
                    if (key != null)
                    {
                        object min = key.GetValue("MinimizeToTray");
                        if (min != null) chkMinimizeToTray.IsChecked = ((int)min == 1);
                        object bri = key.GetValue("Brightness");
                        if (bri != null)
                        {
                            UpdateBrightness((int)bri, false);
                        }
                    }
                }
            }
            catch { }

            try
            {
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    if (runKey != null)
                    {
                        chkAutoStart.IsChecked = (runKey.GetValue("RazerDiamondbackController") != null);
                    }
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\RazerDiamondbackController"))
                {
                    if (key != null)
                    {
                        key.SetValue("MinimizeToTray", (chkMinimizeToTray.IsChecked == true) ? 1 : 0);
                        key.SetValue("Brightness", currentBrightness);
                    }
                }
            }
            catch { }
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (runKey != null)
                    {
                        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        if (enable)
                        {
                            runKey.SetValue("RazerDiamondbackController", "\"" + exePath + "\" --tray");
                        }
                        else
                        {
                            runKey.DeleteValue("RazerDiamondbackController", false);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
