using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GameUI
{
    public partial class MainWindow : Window
    {
        // Logical coordinates: X 0..10 (11 vertical lines), Y 0..11 (12 horizontal lines)
        private const int MaxX = 10;
        private const int MaxY = 11;

        // Precise line detection fields
        private List<double>? _gridXs; // 11 vertical line x centers (X=0..10)
        private List<double>? _gridYs; // 12 horizontal line y centers (Y=0..11, top-origin list)
        private double _lineLumThreshold = 80; // luminance below this counts as dark (black line)
        private double _lineFractionThreshold = 0.55; // fraction of dark pixels per scan column/row
        private bool _showGridLines = true;
        private bool _showMarkers = true;
        private bool _showDebugPanel = true; // toggle with F3

        private readonly List<(int X,int Y,string Code,bool Red)> _pieces = new();
        private int _gridYOffsetCells = 1; // shift grid downward by 1 logical cell for display
        private double _pieceScale = 1; // configurable scale relative to cell size (0..1)

        public MainWindow()
        {
            InitializeComponent();
            SeedPieces();
            Loaded += (_, _) => { DetectBoardLines(); RenderAll(); };
            SizeChanged += (_, _) => { DetectBoardLines(); RenderAll(); };
        }

        private void SeedPieces()
        {
            _pieces.Clear();
            // Red (bottom)
            Add(6,0,"commander",true);
            Add(1,1,"navy",true); Add(4,1,"airforce",true); Add(5,1,"headquarter",true); Add(7,1,"headquarter",true); Add(8,1,"airforce",true);
            Add(3,2,"antiair",true); Add(6,2,"missile",true); Add(9,2,"antiair",true);
            Add(2,3,"navy",true); Add(4,3,"artillery",true); Add(5,3,"tank",true); Add(7,3,"tank",true); Add(8,3,"artillery",true);
            Add(2,4,"infantry",true); Add(6,4,"militia",true); Add(10,4,"infantry",true);
            // Blue (top)
            Add(2,7,"infantry",false); Add(6,7,"militia",false); Add(10,7,"infantry",false);
            Add(2,8,"navy",false); Add(4,8,"artillery",false); Add(5,8,"tank",false); Add(7,8,"tank",false); Add(8,8,"artillery",false);
            Add(3,9,"antiair",false); Add(6,9,"missile",false); Add(9,9,"antiair",false);
            Add(1,10,"navy",false); Add(4,10,"airforce",false); Add(5,10,"headquarter",false); Add(7,10,"headquarter",false); Add(8,10,"airforce",false);
            Add(6,11,"commander",false);
            void Add(int x,int y,string code,bool red) => _pieces.Add((x,y,code,red));
        }

        private void RenderAll()
        {
            DrawDetectedGrid();
            DrawPiecesOnIntersections();
            UpdateDebugPanel();
        }

        private void DetectBoardLines()
        {
            _gridXs = null; _gridYs = null;
            if (BoardImage.Source is not BitmapSource bmp) return;
            int w = bmp.PixelWidth; int h = bmp.PixelHeight;
            int stride = (w * 4 + 3) / 4 * 4;
            byte[] pixels = new byte[h * stride];
            bmp.CopyPixels(pixels, stride, 0);
            bool IsDark(int idx)
            {
                byte b = pixels[idx]; byte g = pixels[idx + 1]; byte r = pixels[idx + 2]; byte a = pixels[idx + 3];
                if (a < 20) return false;
                int lum = (r * 2126 + g * 7152 + b * 722) / 10000;
                return lum < _lineLumThreshold;
            }
            double ColumnDarkFrac(int x)
            {
                int dark = 0, total = 0; int step = Math.Max(1, h / 1500);
                for (int y = 0; y < h; y += step)
                {
                    int idx = y * stride + x * 4; if (IsDark(idx)) dark++; total++;
                }
                return total == 0 ? 0 : dark / (double)total;
            }
            double RowDarkFrac(int y)
            {
                int dark = 0, total = 0; int step = Math.Max(1, w / 1500); int baseIdx = y * stride;
                for (int x = 0; x < w; x += step)
                {
                    int idx = baseIdx + x * 4; if (IsDark(idx)) dark++; total++;
                }
                return total == 0 ? 0 : dark / (double)total;
            }
            var vSegs = new List<(int s,int e)>(); bool inLine=false; int start=0;
            for (int x=0;x<w;x++)
            {
                bool line = ColumnDarkFrac(x) >= _lineFractionThreshold;
                if (line && !inLine) { inLine=true; start=x; }
                else if (!line && inLine) { inLine=false; vSegs.Add((start,x-1)); }
            }
            if (inLine) vSegs.Add((start,w-1));
            var hSegs = new List<(int s,int e)>(); inLine=false; start=0;
            for (int y=0;y<h;y++)
            {
                bool line = RowDarkFrac(y) >= _lineFractionThreshold;
                if (line && !inLine) { inLine=true; start=y; }
                else if (!line && inLine) { inLine=false; hSegs.Add((start,y-1)); }
            }
            if (inLine) hSegs.Add((start,h-1));
            // Merge close segments (line thickness)
            List<(int s,int e)> Merge(List<(int s,int e)> segs)
            {
                var merged=new List<(int s,int e)>();
                foreach(var seg in segs)
                {
                    if (merged.Count==0){ merged.Add(seg); continue; }
                    var last = merged[^1];
                    if (seg.s - last.e <=2) merged[^1]=(last.s, seg.e); else merged.Add(seg);
                }
                return merged;
            }
            var mergedV = Merge(vSegs); var mergedH = Merge(hSegs);
            var vCenters = mergedV.Select(seg => (seg.s + seg.e)/2.0).ToList();
            var hCenters = mergedH.Select(seg => (seg.s + seg.e)/2.0).ToList();
            vCenters.Sort(); hCenters.Sort();
            // Filter to expected counts (11 vertical, 12 horizontal) by selecting best contiguous run
            _gridXs = SelectBestRun(vCenters, MaxX+1);
            _gridYs = SelectBestRun(hCenters, MaxY+1);
        }

        private List<double>? SelectBestRun(List<double> centers, int expected)
        {
            if (centers.Count == expected) return centers;
            if (centers.Count < expected) return null;
            double bestVar = double.MaxValue; int bestStart = 0;
            for (int s=0; s+expected<=centers.Count; s++)
            {
                var slice = centers.GetRange(s, expected);
                var diffs = new List<double>(); for(int i=1;i<slice.Count;i++) diffs.Add(slice[i]-slice[i-1]);
                double avg = diffs.Average(); double var = diffs.Select(d => (d-avg)*(d-avg)).Average();
                if (var < bestVar) { bestVar=var; bestStart=s; }
            }
            return centers.GetRange(bestStart, expected);
        }

        private void DrawDetectedGrid()
        {
            GridOverlayCanvas.Children.Clear();
            if ((_gridXs == null || _gridYs == null) || (!_showGridLines && !_showMarkers)) return;
            double cellH = (_gridYs.Last() - _gridYs.First()) / MaxY;
            double yOffsetPx = _gridYOffsetCells * cellH;
            // Clamp offset if it would push bottom outside image
            double maxOffset = Math.Max(0, (BoardImage.ActualHeight - _gridYs.Last()) - cellH*0.5);
            if (yOffsetPx > maxOffset) yOffsetPx = maxOffset;
            double left = _gridXs.First(); double right = _gridXs.Last();
            double top = _gridYs.First() + yOffsetPx; double bottom = _gridYs.Last() + yOffsetPx;
            if (_showGridLines)
            {
                var border = new Rectangle { Width = right - left, Height = bottom - top, Stroke = new SolidColorBrush(Color.FromArgb(220,255,255,255)), StrokeThickness=2 };
                Canvas.SetLeft(border,left); Canvas.SetTop(border,top); GridOverlayCanvas.Children.Add(border);
                foreach (var x in _gridXs)
                    GridOverlayCanvas.Children.Add(new Line { X1=x, X2=x, Y1=top, Y2=bottom, Stroke=new SolidColorBrush(Color.FromArgb(160,255,255,255)), StrokeThickness=1, SnapsToDevicePixels=true });
                foreach (var y in _gridYs)
                    GridOverlayCanvas.Children.Add(new Line { X1=left, X2=right, Y1=y + yOffsetPx, Y2=y + yOffsetPx, Stroke=new SolidColorBrush(Color.FromArgb(160,255,255,255)), StrokeThickness=1, SnapsToDevicePixels=true });
            }
            if (_showMarkers)
            {
                double r = 5; var markerBrush = new SolidColorBrush(Color.FromArgb(200,0,200,0));
                for(int gx=0; gx<=MaxX; gx++)
                {
                    for(int gy=0; gy<=MaxY; gy++)
                    {
                        double cx = _gridXs[gx]; double cy = _gridYs[gy] + yOffsetPx;
                        var dot = new Ellipse { Width=r, Height=r, Fill=markerBrush };
                        Canvas.SetLeft(dot, cx - r/2); Canvas.SetTop(dot, cy - r/2);
                        GridOverlayCanvas.Children.Add(dot);
                    }
                }
            }
        }

        private Point? IntersectionCenter(int logicalX, int logicalY)
        {
            if (_gridXs == null || _gridYs == null) return null;
            if (logicalX <0 || logicalX>MaxX || logicalY<0 || logicalY>MaxY) return null;
            int yIndex = _gridYs.Count - 1 - logicalY; // invert logical Y
            double cellH = (_gridYs.Last() - _gridYs.First()) / MaxY;
            double yOffsetPx = _gridYOffsetCells * cellH;
            double maxOffset = Math.Max(0, (BoardImage.ActualHeight - _gridYs.Last()) - cellH*0.5);
            if (yOffsetPx > maxOffset) yOffsetPx = maxOffset;
            return new Point(_gridXs[logicalX], _gridYs[yIndex] + yOffsetPx);
        }

        private void DrawPiecesOnIntersections()
        {
            PieceCanvas.Children.Clear();
            if (_gridXs == null || _gridYs == null) return;
            double avgW = (_gridXs.Last() - _gridXs.First()) / MaxX;
            double avgH = (_gridYs.Last() - _gridYs.First()) / MaxY;
            double size = Math.Min(avgW, avgH) * Math.Clamp(_pieceScale, 0.4, 0.95);
            foreach (var (x,y,code,red) in _pieces)
            {
                var center = IntersectionCenter(x,y);
                if (center == null) continue;
                var img = new Image { Width=size, Height=size, Stretch=Stretch.Uniform, SnapsToDevicePixels=true, ToolTip=$"{(red?"Đỏ":"Xanh")} {code}" };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                img.Source = new BitmapImage(new Uri(PieceAsset(code, red), UriKind.Absolute));
                Canvas.SetLeft(img, center.Value.X - size/2); Canvas.SetTop(img, center.Value.Y - size/2);
                PieceCanvas.Children.Add(img);
            }
        }

        private string PieceAsset(string code, bool red)
        {
            string color = red ? "red" : "blue"; string baseUri = "pack://application:,,,/GameUI;component/Assets/pieces_png/";
            return code switch
            {
                "commander" => baseUri + $"commander_{color}.png",
                "headquarter" => baseUri + $"headquarter_{color}.png",
                "airforce" => baseUri + $"airforce_{color}.png",
                "navy" => baseUri + $"navy_{color}.png",
                "missile" => baseUri + $"missile_{color}.png",
                "antiair" => baseUri + $"antiair_{color}.png",
                "tank" => baseUri + $"tank_{color}.png",
                "artillery" => baseUri + $"artillery_{color}.png",
                "infantry" => baseUri + $"infantry_{color}.png",
                "militia" => baseUri + $"militia_{color}.png",
                _ => baseUri + $"infantry_{color}.png"
            };
        }

        private void UpdateDebugPanel()
        {
            if (DebugText == null) return;
            if (_showDebugPanel)
            {
                DebugPanel.Visibility = Visibility.Visible;
                string status = (_gridXs!=null && _gridYs!=null)?"OK":"FAILED";
                double cellW = _gridXs!=null? (_gridXs.Last()-_gridXs.First())/MaxX:0;
                double cellH = _gridYs!=null? (_gridYs.Last()-_gridYs.First())/MaxY:0;
                double yOffsetPx = _gridYOffsetCells * cellH;
                DebugText.Text = $"Detect:{status} X:{_gridXs?.Count ?? 0} Y:{_gridYs?.Count ?? 0}\nLumTh:{_lineLumThreshold} FracTh:{_lineFractionThreshold:0.00}\nCellW:{cellW:0.0} CellH:{cellH:0.0}\nGridYOffsetCells:{_gridYOffsetCells} ({yOffsetPx:0.0}px)\nPieceScale:{_pieceScale:0.00}\nKeys: G grid, M markers, F3 debug, +/- lum, D frac+, R frac-, Shift+Y offset, Shift+Plus/- piece size";
            }
            else DebugPanel.Visibility = Visibility.Collapsed;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.G) { _showGridLines = !_showGridLines; RenderAll(); }
            else if (e.Key == Key.M) { _showMarkers = !_showMarkers; RenderAll(); }
            else if (e.Key == Key.F3) { _showDebugPanel = !_showDebugPanel; UpdateDebugPanel(); }
            else if (e.Key == Key.OemPlus || e.Key == Key.Add)
            {
                _lineLumThreshold = Math.Max(10, _lineLumThreshold - 5); DetectBoardLines(); RenderAll();
            }
            else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                _lineLumThreshold = Math.Min(220, _lineLumThreshold + 5); DetectBoardLines(); RenderAll();
            }
            else if (e.Key == Key.D)
            {
                _lineFractionThreshold = Math.Clamp(_lineFractionThreshold + 0.02, 0.30, 0.90); DetectBoardLines(); RenderAll();
            }
            else if (e.Key == Key.R)
            {
                _lineFractionThreshold = Math.Clamp(_lineFractionThreshold - 0.02, 0.30, 0.90); DetectBoardLines(); RenderAll();
            }
            else if (e.Key == Key.Y && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                _gridYOffsetCells = (_gridYOffsetCells == 1) ? 0 : 1; RenderAll();
            }
            else if ((e.Key == Key.OemPlus || e.Key == Key.Add) && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                _pieceScale = Math.Min(0.95, _pieceScale + 0.02); RenderAll(); return;
            }
            else if ((e.Key == Key.OemMinus || e.Key == Key.Subtract) && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                _pieceScale = Math.Max(0.40, _pieceScale - 0.02); RenderAll(); return;
            }
        }
    }
}
