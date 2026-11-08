using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FloatDock.Native;
using FloatDock.Services;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;

namespace FloatDock.Views
{
    public partial class ScreenDrawingWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly Stack<StrokeCollection> _undoStack = new();
        private readonly Stack<StrokeCollection> _redoStack = new();

        private enum ShapeMode { None, Line, Arrow, Rectangle, Ellipse, Text }
        private ShapeMode _currentShapeMode = ShapeMode.None;
        private Point? _shapeStartPoint;
        private Stroke? _previewStroke;
        private bool _isPassThrough;

        private Color _activeColor;
        private double _activeThickness;

        public ScreenDrawingWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;

            _activeColor = (Color)ColorConverter.ConvertFromString(_settingsService.CurrentSettings.DefaultDrawingColor);
            _activeThickness = _settingsService.CurrentSettings.DefaultStrokeThickness;
            SliderThickness.Value = _activeThickness;

            UpdateDrawingAttributes();

            Loaded += ScreenDrawingWindow_Loaded;
        }

        private void ScreenDrawingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Position full-screen covering the virtual screen or current monitor
            Left = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
            Top = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
            Width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
            Height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

            SaveUndoState();
        }

        private void UpdateDrawingAttributes()
        {
            var da = new DrawingAttributes
            {
                Color = _activeColor,
                Width = _activeThickness,
                Height = _activeThickness,
                FitToCurve = true,
                IgnorePressure = false,
                StylusTip = StylusTip.Ellipse
            };

            DrawingCanvas.DefaultDrawingAttributes = da;
        }

        #region Tool Modes

        private void BtnPen_Click(object sender, RoutedEventArgs e)
        {
            _currentShapeMode = ShapeMode.None;
            DrawingCanvas.EditingMode = InkCanvasEditingMode.Ink;
            DrawingCanvas.DefaultDrawingAttributes.IsHighlighter = false;
        }

        private void BtnHighlighter_Click(object sender, RoutedEventArgs e)
        {
            _currentShapeMode = ShapeMode.None;
            DrawingCanvas.EditingMode = InkCanvasEditingMode.Ink;
            DrawingCanvas.DefaultDrawingAttributes.IsHighlighter = true;
        }

        private void BtnEraser_Click(object sender, RoutedEventArgs e)
        {
            _currentShapeMode = ShapeMode.None;
            DrawingCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
        }

        private void BtnShapeLine_Click(object sender, RoutedEventArgs e)
        {
            _currentShapeMode = ShapeMode.Line;
            DrawingCanvas.EditingMode = InkCanvasEditingMode.None;
        }

        private void BtnShapeArrow_Click(object sender, RoutedEventArgs e)
        {
            _currentShapeMode = ShapeMode.Arrow;
            DrawingCanvas.EditingMode = InkCanvasEditingMode.None;
        }

        private void BtnShapeRect_Click(object sender, RoutedEventArgs e)
        {
            _currentShapeMode = ShapeMode.Rectangle;
            DrawingCanvas.EditingMode = InkCanvasEditingMode.None;
        }

        private void BtnShapeEllipse_Click(object sender, RoutedEventArgs e)
        {
            _currentShapeMode = ShapeMode.Ellipse;
            DrawingCanvas.EditingMode = InkCanvasEditingMode.None;
        }

        private void BtnText_Click(object sender, RoutedEventArgs e)
        {
            _currentShapeMode = ShapeMode.Text;
            DrawingCanvas.EditingMode = InkCanvasEditingMode.None;
        }

        private void ColorPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                _activeColor = (Color)ColorConverter.ConvertFromString(hex);
                UpdateDrawingAttributes();
            }
        }

        private void SliderThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _activeThickness = e.NewValue;
            UpdateDrawingAttributes();
        }

        #endregion

        #region Shape Drawing on InkCanvas

        private void DrawingCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            Point pos = e.GetPosition(DrawingCanvas);

            if (_currentShapeMode == ShapeMode.Text)
            {
                CreateTextNote(pos);
                return;
            }

            if (_currentShapeMode != ShapeMode.None)
            {
                _shapeStartPoint = pos;
            }
        }

        private void DrawingCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_shapeStartPoint == null || e.LeftButton != MouseButtonState.Pressed) return;

            Point currentPoint = e.GetPosition(DrawingCanvas);

            if (_previewStroke != null)
            {
                DrawingCanvas.Strokes.Remove(_previewStroke);
                _previewStroke = null;
            }

            _previewStroke = GenerateShapeStroke(_shapeStartPoint.Value, currentPoint, _currentShapeMode);
            if (_previewStroke != null)
            {
                DrawingCanvas.Strokes.Add(_previewStroke);
            }
        }

        private void DrawingCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_shapeStartPoint != null)
            {
                Point endPoint = e.GetPosition(DrawingCanvas);
                if (_previewStroke != null)
                {
                    DrawingCanvas.Strokes.Remove(_previewStroke);
                    _previewStroke = null;
                }

                var finalStroke = GenerateShapeStroke(_shapeStartPoint.Value, endPoint, _currentShapeMode);
                if (finalStroke != null)
                {
                    DrawingCanvas.Strokes.Add(finalStroke);
                    SaveUndoState();
                }

                _shapeStartPoint = null;
            }
        }

        private Stroke? GenerateShapeStroke(Point p1, Point p2, ShapeMode mode)
        {
            var pts = new StylusPointCollection();
            var da = DrawingCanvas.DefaultDrawingAttributes.Clone();

            switch (mode)
            {
                case ShapeMode.Line:
                    pts.Add(new StylusPoint(p1.X, p1.Y));
                    pts.Add(new StylusPoint(p2.X, p2.Y));
                    break;

                case ShapeMode.Arrow:
                    pts.Add(new StylusPoint(p1.X, p1.Y));
                    pts.Add(new StylusPoint(p2.X, p2.Y));

                    // Calculate arrowhead points
                    double angle = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                    double arrowLength = Math.Max(16, _activeThickness * 4);
                    double arrowAngle = Math.PI / 6; // 30 degrees

                    Point a1 = new Point(p2.X - arrowLength * Math.Cos(angle - arrowAngle), p2.Y - arrowLength * Math.Sin(angle - arrowAngle));
                    Point a2 = new Point(p2.X - arrowLength * Math.Cos(angle + arrowAngle), p2.Y - arrowLength * Math.Sin(angle + arrowAngle));

                    pts.Add(new StylusPoint(a1.X, a1.Y));
                    pts.Add(new StylusPoint(p2.X, p2.Y));
                    pts.Add(new StylusPoint(a2.X, a2.Y));
                    break;

                case ShapeMode.Rectangle:
                    pts.Add(new StylusPoint(p1.X, p1.Y));
                    pts.Add(new StylusPoint(p2.X, p1.Y));
                    pts.Add(new StylusPoint(p2.X, p2.Y));
                    pts.Add(new StylusPoint(p1.X, p2.Y));
                    pts.Add(new StylusPoint(p1.X, p1.Y));
                    break;

                case ShapeMode.Ellipse:
                    int segments = 40;
                    double rx = Math.Abs(p2.X - p1.X) / 2.0;
                    double ry = Math.Abs(p2.Y - p1.Y) / 2.0;
                    double cx = (p1.X + p2.X) / 2.0;
                    double cy = (p1.Y + p2.Y) / 2.0;

                    for (int i = 0; i <= segments; i++)
                    {
                        double theta = (2 * Math.PI * i) / segments;
                        pts.Add(new StylusPoint(cx + rx * Math.Cos(theta), cy + ry * Math.Sin(theta)));
                    }
                    break;

                default:
                    return null;
            }

            return new Stroke(pts, da);
        }

        private void CreateTextNote(Point pos)
        {
            var textBox = new TextBox
            {
                FontSize = Math.Max(16, _activeThickness * 4),
                Foreground = new SolidColorBrush(_activeColor),
                Background = new SolidColorBrush(Color.FromArgb(180, 20, 20, 20)),
                BorderBrush = new SolidColorBrush(_activeColor),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                MinWidth = 120,
                AcceptsReturn = true
            };

            Canvas.SetLeft(textBox, pos.X);
            Canvas.SetTop(textBox, pos.Y);

            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    DrawingCanvas.Children.Remove(textBox);
                }
            };

            DrawingCanvas.Children.Add(textBox);
            textBox.Focus();
        }

        #endregion

        #region Undo / Redo / Clear

        private void DrawingCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            SaveUndoState();
        }

        private void SaveUndoState()
        {
            _undoStack.Push(DrawingCanvas.Strokes.Clone());
            _redoStack.Clear();
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e) => Undo();
        private void BtnRedo_Click(object sender, RoutedEventArgs e) => Redo();

        private void Undo()
        {
            if (_undoStack.Count > 1)
            {
                _redoStack.Push(_undoStack.Pop());
                DrawingCanvas.Strokes = _undoStack.Peek().Clone();
            }
        }

        private void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var strokes = _redoStack.Pop();
                _undoStack.Push(strokes.Clone());
                DrawingCanvas.Strokes = strokes.Clone();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Strokes.Clear();
            DrawingCanvas.Children.Clear();
            SaveUndoState();
        }

        #endregion

        #region Pass-Through Mode

        private void BtnPassThrough_Click(object sender, RoutedEventArgs e)
        {
            _isPassThrough = !_isPassThrough;
            WindowService.SetClickThrough(this, _isPassThrough);

            if (_isPassThrough)
            {
                TxtPassThrough.Foreground = new SolidColorBrush(Colors.LightGreen);
                PassThroughBanner.Visibility = Visibility.Visible;
                DrawToolbar.Opacity = 0.4;
            }
            else
            {
                TxtPassThrough.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                PassThroughBanner.Visibility = Visibility.Collapsed;
                DrawToolbar.Opacity = 1.0;
            }
        }

        #endregion

        #region Screenshot of Drawings & Export

        private void BtnSaveDrawings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DrawToolbar.Visibility = Visibility.Collapsed;
                PassThroughBanner.Visibility = Visibility.Collapsed;

                // Capture the entire visual desktop with annotations rendered on top
                var desktopBmp = ScreenshotService.CaptureVirtualScreen();

                // Render WPF InkCanvas visuals directly onto bitmap
                var rtb = new RenderTargetBitmap(
                    (int)ActualWidth,
                    (int)ActualHeight,
                    96, 96,
                    PixelFormats.Pbgra32
                );
                rtb.Render(DrawingCanvas);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    using var overlayBmp = new Bitmap(ms);

                    using (var g = Graphics.FromImage(desktopBmp))
                    {
                        g.DrawImage(overlayBmp, 0, 0);
                    }
                }

                // Copy to clipboard & save
                ScreenshotService.CopyToClipboard(desktopBmp);
                string path = ScreenshotService.SaveToDisk(
                    desktopBmp,
                    _settingsService.CurrentSettings.ScreenshotDirectory,
                    _settingsService.CurrentSettings.ScreenshotFormat
                );

                MessageBox.Show(
                    $"Annotated screen copied to clipboard and saved to:\n{path}",
                    "FloatDock Screen Drawing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to save annotated screenshot", ex);
            }
            finally
            {
                DrawToolbar.Visibility = Visibility.Visible;
            }
        }

        #endregion

        private void DrawToolbar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DrawToolbar.CaptureMouse();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Undo();
            }
            else if (e.Key == Key.Y && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Redo();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void DrawingCanvas_DefaultDrawingAttributesReplaced(object sender, DrawingAttributesReplacedEventArgs e)
        {
            // Ensures custom stroke settings persist
        }
    }
}
