using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;
using GameCore;
using CorePiece = GameCore.Piece;

namespace GameUI.ViewModels
{
    public class BoardViewModel : ViewModelBase
    {
        public const int MaxX = 10; public const int MaxY = 11;
        private List<double>? _gridXs; public List<double>? GridXs { get => _gridXs; private set => SetProperty(ref _gridXs, value); }
        private List<double>? _gridYs; public List<double>? GridYs { get => _gridYs; private set => SetProperty(ref _gridYs, value); }
        private double _lineLumThreshold = 80; public double LineLumThreshold { get => _lineLumThreshold; set { if (SetProperty(ref _lineLumThreshold, value)) UpdateDebugInfo(); } }
        private double _lineFractionThreshold = 0.55; public double LineFractionThreshold { get => _lineFractionThreshold; set { if (SetProperty(ref _lineFractionThreshold, value)) UpdateDebugInfo(); } }
        private bool _showGridLines = true; public bool ShowGridLines { get => _showGridLines; set { if (SetProperty(ref _showGridLines, value)) UpdateDebugInfo(); } }
        private bool _showMarkers = true; public bool ShowMarkers { get => _showMarkers; set { if (SetProperty(ref _showMarkers, value)) UpdateDebugInfo(); } }
        private bool _showDebugPanel = true; public bool ShowDebugPanel { get => _showDebugPanel; set { if (SetProperty(ref _showDebugPanel, value)) UpdateDebugInfo(); } }
        private int _gridYOffsetCells = 1; public int GridYOffsetCells { get => _gridYOffsetCells; set { if (SetProperty(ref _gridYOffsetCells, value)) UpdateDebugInfo(); } }
        private double _pieceScale = 1; public double PieceScale { get => _pieceScale; set { if (SetProperty(ref _pieceScale, value)) UpdateDebugInfo(); } }
        private string _debugInfo = string.Empty; public string DebugInfo { get => _debugInfo; private set => SetProperty(ref _debugInfo, value); }

        public ObservableCollection<PieceViewModel> Pieces { get; } = new();

        public RelayCommand ToggleGridCommand { get; }
        public RelayCommand ToggleMarkersCommand { get; }
        public RelayCommand ToggleDebugCommand { get; }
        public RelayCommand IncreaseLumThresholdCommand { get; }
        public RelayCommand DecreaseLumThresholdCommand { get; }
        public RelayCommand IncreaseFracThresholdCommand { get; }
        public RelayCommand DecreaseFracThresholdCommand { get; }
        public RelayCommand ToggleYOffsetCommand { get; }
        public RelayCommand IncreasePieceScaleCommand { get; }
        public RelayCommand DecreasePieceScaleCommand { get; }
        public RelayCommand ClearSelectionCommand { get; }

        public Board CoreBoard { get; private set; } = new Board();
        private PieceViewModel? _selectedPiece; public PieceViewModel? SelectedPiece { get => _selectedPiece; set { if (SetProperty(ref _selectedPiece, value)) RecomputeLegalMoves(); } }
        private List<(int x,int y)> _legalMoves = new(); public List<(int x,int y)> LegalMoves { get => _legalMoves; private set => SetProperty(ref _legalMoves, value); }

        public BoardViewModel()
        {
            ToggleGridCommand = new RelayCommand(() => ShowGridLines = !ShowGridLines);
            ToggleMarkersCommand = new RelayCommand(() => ShowMarkers = !ShowMarkers);
            ToggleDebugCommand = new RelayCommand(() => ShowDebugPanel = !ShowDebugPanel);
            IncreaseLumThresholdCommand = new RelayCommand(() => { LineLumThreshold = Math.Min(220, LineLumThreshold + 5); });
            DecreaseLumThresholdCommand = new RelayCommand(() => { LineLumThreshold = Math.Max(10, LineLumThreshold - 5); });
            IncreaseFracThresholdCommand = new RelayCommand(() => { LineFractionThreshold = Math.Clamp(LineFractionThreshold + 0.02, 0.30, 0.90); });
            DecreaseFracThresholdCommand = new RelayCommand(() => { LineFractionThreshold = Math.Clamp(LineFractionThreshold - 0.02, 0.30, 0.90); });
            ToggleYOffsetCommand = new RelayCommand(() => { GridYOffsetCells = (GridYOffsetCells == 1) ? 0 : 1; });
            IncreasePieceScaleCommand = new RelayCommand(() => { PieceScale = Math.Min(0.95, PieceScale + 0.02); });
            DecreasePieceScaleCommand = new RelayCommand(() => { PieceScale = Math.Max(0.40, PieceScale - 0.02); });
            ClearSelectionCommand = new RelayCommand(() => { SelectedPiece = null; LegalMoves = new List<(int x,int y)>(); UpdateDebugInfo(); });
            SeedPieces();
            UpdateDebugInfo();
        }

        private void SeedPieces()
        {
            Pieces.Clear(); CoreBoard = new Board(); // re-init board
            void Add(int vx,int vy,string code,bool red)
            {
                var vm = new PieceViewModel{ X=vx, Y=vy, Code=code, IsRed=red }; Pieces.Add(vm);
                var piece = CreateCorePiece(code, red ? Player.Red : Player.Blue);
                var boardRow = ToBoardRow(vy); var boardCol = vx; CoreBoard.PlacePiece(new Position(boardRow, boardCol), piece);
            }
            // Red (bottom)
            Add(6,0,"commander",true);
            Add(3,4,"engineer",true); Add(8,4,"engineer",true); // newly added engineers
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
            Add(3,7,"engineer",false); Add(8,7,"engineer",false); // newly added engineers
        }
        private CorePiece CreateCorePiece(string code, Player color) => code switch
        {
            "commander" => new Commander(color),
            "headquarter" => new Headquarter(color),
            "airforce" => new AirForce(color),
            "navy" => new Navy(color),
            "missile" => new Rocket(color),
            "antiair" => new AntiAircraftGun(color),
            "tank" => new Tank(color),
            "artillery" => new Artillery(color),
            "infantry" => new Infantry(color),
            "militia" => new Militia(color),
            "engineer" => new Engineer(color),
            _ => new Infantry(color)
        };
        private int ToBoardRow(int viewY) => MaxY - viewY;
        private int FromBoardRow(int boardRow) => MaxY - boardRow;

        private void RecomputeLegalMoves()
        {
            if (SelectedPiece == null)
            {
                LegalMoves = new List<(int x,int y)>(); UpdateDebugInfo(); return;
            }
            var boardPos = new Position(ToBoardRow(SelectedPiece.Y), SelectedPiece.X);
            var corePiece = CoreBoard.GetPieceAt(boardPos);
            if (corePiece == null || corePiece.Color != CoreBoard.ActivePlayer)
            {
                LegalMoves = new List<(int x,int y)>(); UpdateDebugInfo(); return;
            }
            var moves = CoreBoard.GetLegalMoves(boardPos).Select(p => (p.Column, FromBoardRow(p.Row))).ToList();
            LegalMoves = moves;
            UpdateDebugInfo();
        }

        public bool TryMoveSelectedTo(int targetX,int targetY)
        {
            if (SelectedPiece == null) return false;
            if (!LegalMoves.Any(m => m.x==targetX && m.y==targetY)) return false;
            var fromBoard = new Position(ToBoardRow(SelectedPiece.Y), SelectedPiece.X);
            var toBoard = new Position(ToBoardRow(targetY), targetX);
            var captured = CoreBoard.GetPieceAt(toBoard);
            if (!CoreBoard.TryMove(fromBoard, toBoard)) return false;
            // update viewmodel piece
            // remove captured vm if exists
            if (captured != null)
            {
                var victimVM = Pieces.FirstOrDefault(p => p.X==targetX && p.Y==targetY);
                if (victimVM != null) Pieces.Remove(victimVM);
            }
            SelectedPiece.X = targetX; SelectedPiece.Y = targetY;
            SelectedPiece = null; LegalMoves = new List<(int x,int y)>();
            UpdateDebugInfo();
            return true;
        }

        public void DetectBoardLines(BitmapSource? bmp)
        {
            GridXs = null; GridYs = null;
            if (bmp == null) { UpdateDebugInfo(); return; }
            int w = bmp.PixelWidth; int h = bmp.PixelHeight;
            int stride = (w * 4 + 3) / 4 * 4;
            byte[] pixels = new byte[h * stride];
            bmp.CopyPixels(pixels, stride, 0);
            bool IsDark(int idx)
            {
                byte b = pixels[idx]; byte g = pixels[idx + 1]; byte r = pixels[idx + 2]; byte a = pixels[idx + 3];
                if (a < 20) return false;
                int lum = (r * 2126 + g * 7152 + b * 722) / 10000;
                return lum < LineLumThreshold;
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
                bool line = ColumnDarkFrac(x) >= LineFractionThreshold;
                if (line && !inLine) { inLine=true; start=x; }
                else if (!line && inLine) { inLine=false; vSegs.Add((start,x-1)); }
            }
            if (inLine) vSegs.Add((start,w-1));
            var hSegs = new List<(int s,int e)>(); inLine=false; start=0;
            for (int y=0;y<h;y++)
            {
                bool line = RowDarkFrac(y) >= LineFractionThreshold;
                if (line && !inLine) { inLine=true; start=y; }
                else if (!line && inLine) { inLine=false; hSegs.Add((start,y-1)); }
            }
            if (inLine) hSegs.Add((start,h-1));
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
            GridXs = SelectBestRun(vCenters, MaxX+1);
            GridYs = SelectBestRun(hCenters, MaxY+1);
            UpdateDebugInfo();
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

        public void UpdateDebugInfo()
        {
            string status = (GridXs!=null && GridYs!=null)?"OK":"FAILED";
            double cellW = GridXs!=null? (GridXs.Last()-GridXs.First())/MaxX:0;
            double cellH = GridYs!=null? (GridYs.Last()-GridYs.First())/MaxY:0;
            double yOffsetPx = GridYOffsetCells * cellH;
            DebugInfo = $"Detect:{status} X:{GridXs?.Count ?? 0} Y:{GridYs?.Count ?? 0}\nLumTh:{LineLumThreshold} FracTh:{LineFractionThreshold:0.00}\nCellW:{cellW:0.0} CellH:{cellH:0.0}\nGridYOffsetCells:{GridYOffsetCells} ({yOffsetPx:0.0}px)\nPieceScale:{PieceScale:0.00}\nKeys: G grid, M markers, F3 debug, +/- lum, D frac+, R frac-, Shift+Y offset, Shift+Plus/- piece size";
            if (SelectedPiece != null)
            {
                DebugInfo += $"\nSelected: {SelectedPiece.Code} {(SelectedPiece.IsRed?"Red":"Blue")} @ {SelectedPiece.X},{SelectedPiece.Y} Moves:{LegalMoves.Count}";
            }
        }
    }
}
