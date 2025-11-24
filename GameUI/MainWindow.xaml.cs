using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GameUI.ViewModels;
using GameCore; // for Position reference if needed

namespace GameUI
{
    public partial class MainWindow : Window
    {
        private BoardViewModel VM => (BoardViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new BoardViewModel();
            Loaded += (_, _) => { VM.DetectBoardLines(BoardImage.Source as BitmapSource); RenderAll(); };
            SizeChanged += (_, _) => { VM.DetectBoardLines(BoardImage.Source as BitmapSource); RenderAll(); };
            // Removed HighlightCanvas fallback click handler to avoid intercepting selection
        }

        private void RenderAll()
        {
            DrawDetectedGrid();
            DrawPiecesOnIntersections();
            DrawHighlights();
            UpdateDebugPanel();
        }
        private void DrawHighlights()
        {
            HighlightCanvas.Children.Clear();
            var xs = VM.GridXs; var ys = VM.GridYs; if (xs == null || ys == null) return;
            double avgW = (xs.Last() - xs.First()) / BoardViewModel.MaxX; double avgH = (ys.Last() - ys.First()) / BoardViewModel.MaxY;
            double size = Math.Min(avgW, avgH) * Math.Clamp(VM.PieceScale, 0.4, 0.95);
            // Highlight selected piece
            if (VM.SelectedPiece != null)
            {
                var center = IntersectionCenter(VM.SelectedPiece.X, VM.SelectedPiece.Y);
                if (center != null)
                {
                    var ring = new Ellipse { Width = size, Height = size, Stroke = new SolidColorBrush(Color.FromArgb(220, 255, 255, 0)), StrokeThickness = 4, IsHitTestVisible=false };
                    Canvas.SetLeft(ring, center.Value.X - size/2); Canvas.SetTop(ring, center.Value.Y - size/2);
                    HighlightCanvas.Children.Add(ring);
                }
            }
            // Highlight legal moves
            foreach (var mv in VM.LegalMoves)
            {
                var center = IntersectionCenter(mv.x, mv.y); if (center == null) continue;
                var target = new Ellipse { Width = size*0.6, Height = size*0.6, Fill = new SolidColorBrush(Color.FromArgb(160, 0, 180, 255)), Stroke = new SolidColorBrush(Color.FromArgb(220,0,220,255)), StrokeThickness=2, Tag=mv };
                target.MouseLeftButtonDown += MoveTarget_MouseLeftButtonDown;
                Canvas.SetLeft(target, center.Value.X - target.Width/2); Canvas.SetTop(target, center.Value.Y - target.Height/2);
                HighlightCanvas.Children.Add(target);
            }
        }
        private void MoveTarget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (sender is FrameworkElement fe)
            {
                if (fe.Tag is ValueTuple<int,int> tup)
                {
                    if (VM.TryMoveSelectedTo(tup.Item1, tup.Item2)) { RenderAll(); return; }
                }
                else if (fe.Tag is System.Tuple<int,int> oldTup)
                {
                    if (VM.TryMoveSelectedTo(oldTup.Item1, oldTup.Item2)) { RenderAll(); return; }
                }
            }
        }
        private void Piece_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is PieceViewModel pvm)
            {
                // If a piece is already selected and we click another square containing an enemy piece that is a legal move -> perform capture
                if (VM.SelectedPiece != null && VM.SelectedPiece != pvm)
                {
                    bool isLegalCaptureTarget = VM.LegalMoves.Any(m => m.x == pvm.X && m.y == pvm.Y);
                    if (isLegalCaptureTarget)
                    {
                        if (VM.TryMoveSelectedTo(pvm.X, pvm.Y))
                        {
                            RenderAll();
                            return; // capture completed
                        }
                    }
                }
                // Normal selection / deselection logic
                if (VM.SelectedPiece == pvm)
                {
                    VM.ClearSelectionCommand.Execute(null);
                }
                else
                {
                    VM.SelectedPiece = pvm;
                }
                RenderAll();
            }
        }
        private void DrawDetectedGrid()
        {
            GridOverlayCanvas.Children.Clear();
            var xs = VM.GridXs; var ys = VM.GridYs;
            if ((xs == null || ys == null) || (!VM.ShowGridLines && !VM.ShowMarkers)) return;
            double cellH = (ys.Last() - ys.First()) / BoardViewModel.MaxY;
            double yOffsetPx = VM.GridYOffsetCells * cellH;
            double maxOffset = Math.Max(0, (BoardImage.ActualHeight - ys.Last()) - cellH * 0.5);
            if (yOffsetPx > maxOffset) yOffsetPx = maxOffset;
            double left = xs.First(); double right = xs.Last();
            double top = ys.First() + yOffsetPx; double bottom = ys.Last() + yOffsetPx;
            if (VM.ShowGridLines)
            {
                var border = new Rectangle { Width = right - left, Height = bottom - top, Stroke = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)), StrokeThickness = 2 };
                Canvas.SetLeft(border, left); Canvas.SetTop(border, top); GridOverlayCanvas.Children.Add(border);
                foreach (var x in xs)
                    GridOverlayCanvas.Children.Add(new Line { X1 = x, X2 = x, Y1 = top, Y2 = bottom, Stroke = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), StrokeThickness = 1, SnapsToDevicePixels = true });
                foreach (var y in ys)
                    GridOverlayCanvas.Children.Add(new Line { X1 = left, X2 = right, Y1 = y + yOffsetPx, Y2 = y + yOffsetPx, Stroke = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), StrokeThickness = 1, SnapsToDevicePixels = true });
            }
            if (VM.ShowMarkers)
            {
                double r = 5; var markerBrush = new SolidColorBrush(Color.FromArgb(200, 0, 200, 0));
                for (int gx = 0; gx <= BoardViewModel.MaxX; gx++)
                {
                    for (int gy = 0; gy <= BoardViewModel.MaxY; gy++)
                    {
                        double cx = xs[gx]; double cy = ys[gy] + yOffsetPx;
                        var dot = new Ellipse { Width = r, Height = r, Fill = markerBrush };
                        Canvas.SetLeft(dot, cx - r / 2); Canvas.SetTop(dot, cy - r / 2);
                        GridOverlayCanvas.Children.Add(dot);
                    }
                }
            }
        }

        private Point? IntersectionCenter(int logicalX, int logicalY)
        {
            var xs = VM.GridXs; var ys = VM.GridYs;
            if (xs == null || ys == null) return null;
            if (logicalX < 0 || logicalX > BoardViewModel.MaxX || logicalY < 0 || logicalY > BoardViewModel.MaxY) return null;
            int yIndex = ys.Count - 1 - logicalY; // invert logical Y
            double cellH = (ys.Last() - ys.First()) / BoardViewModel.MaxY;
            double yOffsetPx = VM.GridYOffsetCells * cellH;
            double maxOffset = Math.Max(0, (BoardImage.ActualHeight - ys.Last()) - cellH * 0.5);
            if (yOffsetPx > maxOffset) yOffsetPx = maxOffset;
            return new Point(xs[logicalX], ys[yIndex] + yOffsetPx);
        }

        private void DrawPiecesOnIntersections()
        {
            PieceCanvas.Children.Clear();
            var xs = VM.GridXs; var ys = VM.GridYs;
            if (xs == null || ys == null) return;
            double avgW = (xs.Last() - xs.First()) / BoardViewModel.MaxX;
            double avgH = (ys.Last() - ys.First()) / BoardViewModel.MaxY;
            double size = Math.Min(avgW, avgH) * Math.Clamp(VM.PieceScale, 0.4, 0.95);
            foreach (var piece in VM.Pieces)
            {
                var center = IntersectionCenter(piece.X, piece.Y);
                if (center == null) continue;
                var img = new Image { Width = size, Height = size, Stretch = Stretch.Uniform, SnapsToDevicePixels = true, ToolTip = $"{(piece.IsRed ? "Đỏ" : "Xanh")} {piece.Code}" , Tag=piece};
                img.MouseLeftButtonDown += Piece_MouseLeftButtonDown;
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                img.Source = new BitmapImage(new Uri(piece.ImageUri, UriKind.Absolute));
                Canvas.SetLeft(img, center.Value.X - size / 2); Canvas.SetTop(img, center.Value.Y - size / 2);
                PieceCanvas.Children.Add(img);
            }
        }

        private void UpdateDebugPanel()
        {
            if (DebugPanel == null) return;
            DebugPanel.Visibility = VM.ShowDebugPanel ? Visibility.Visible : Visibility.Collapsed;
            // Text content bound in XAML
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Delegate to commands preserving original shortcuts
            if (e.Key == Key.G) { VM.ToggleGridCommand.Execute(null); RenderAll(); }
            else if (e.Key == Key.M) { VM.ToggleMarkersCommand.Execute(null); RenderAll(); }
            else if (e.Key == Key.F3) { VM.ToggleDebugCommand.Execute(null); UpdateDebugPanel(); }
            else if ((e.Key == Key.OemPlus || e.Key == Key.Add) && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            { VM.IncreasePieceScaleCommand.Execute(null); RenderAll(); return; }
            else if ((e.Key == Key.OemMinus || e.Key == Key.Subtract) && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            { VM.DecreasePieceScaleCommand.Execute(null); RenderAll(); return; }
            else if (e.Key == Key.OemPlus || e.Key == Key.Add)
            { VM.DecreaseLumThresholdCommand.Execute(null); VM.DetectBoardLines(BoardImage.Source as BitmapSource); RenderAll(); }
            else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            { VM.IncreaseLumThresholdCommand.Execute(null); VM.DetectBoardLines(BoardImage.Source as BitmapSource); RenderAll(); }
            else if (e.Key == Key.D)
            { VM.IncreaseFracThresholdCommand.Execute(null); VM.DetectBoardLines(BoardImage.Source as BitmapSource); RenderAll(); }
            else if (e.Key == Key.R)
            { VM.DecreaseFracThresholdCommand.Execute(null); VM.DetectBoardLines(BoardImage.Source as BitmapSource); RenderAll(); }
            else if (e.Key == Key.Y && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            { VM.ToggleYOffsetCommand.Execute(null); RenderAll(); }
        }
    }
}
