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
        private string _lastActionMessage = string.Empty; public string LastActionMessage { get => _lastActionMessage; set { if (SetProperty(ref _lastActionMessage, value)) UpdateDebugInfo(); } }

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
            Pieces.Clear();
            CoreBoard = new Board();
            CoreBoard.ResetToInitialPosition();
            foreach (var kv in CoreBoard.Pieces)
            {
                var boardPos = kv.Key; var piece = kv.Value;
                int x = boardPos.Column; int y = FromBoardRow(boardPos.Row);
                string code = piece.Type switch
                {
                    PieceType.Commander => "commander",
                    PieceType.Headquarter => "headquarter",
                    PieceType.AirForce => "airforce",
                    PieceType.Navy => "navy",
                    PieceType.Rocket => "missile",
                    PieceType.AntiAircraftGun => "antiair",
                    PieceType.Tank => "tank",
                    PieceType.Artillery => "artillery",
                    PieceType.Infantry => "infantry",
                    PieceType.Militia => "militia",
                    PieceType.Engineer => "engineer",
                    _ => "infantry"
                };
                bool isRed = piece.Color == Player.Red;
                Pieces.Add(new PieceViewModel{ X=x, Y=y, Code=code, IsRed=isRed });
            }
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
                LegalMoves = new List<(int x,int y)>(); LastActionMessage = "Không có quân nào được chọn"; UpdateDebugInfo(); return;
            }
            var boardPos = new Position(ToBoardRow(SelectedPiece.Y), SelectedPiece.X);
            var corePiece = CoreBoard.GetPieceAt(boardPos);
            if (corePiece == null)
            {
                LegalMoves = new List<(int x,int y)>(); LastActionMessage = "Không tìm thấy quân trong CoreBoard tại vị trí"; UpdateDebugInfo(); return;
            }
            if (corePiece.Color != CoreBoard.ActivePlayer)
            {
                LegalMoves = new List<(int x,int y)>(); LastActionMessage = "Đang cố chọn quân không tới lượt"; UpdateDebugInfo(); return;
            }
            var moves = CoreBoard.GetLegalMoves(boardPos).Select(p => (p.Column, FromBoardRow(p.Row))).ToList();
            LegalMoves = moves; LastActionMessage = $"Tính {moves.Count} nước đi";
            UpdateDebugInfo();
        }

        public bool TryMoveSelectedTo(int targetX,int targetY)
        {
            if (SelectedPiece == null) { LastActionMessage = "Chưa chọn quân"; return false; }
            if (!LegalMoves.Any(m => m.x==targetX && m.y==targetY)) { LastActionMessage = "Ô được click không nằm trong LegalMoves"; return false; }
            var fromBoard = new Position(ToBoardRow(SelectedPiece.Y), SelectedPiece.X);
            var toBoard = new Position(ToBoardRow(targetY), targetX);
            var captured = CoreBoard.GetPieceAt(toBoard);
            if (!CoreBoard.TryMove(fromBoard, toBoard)) { LastActionMessage = "CoreBoard.TryMove trả về false"; return false; }
            // update viewmodel piece
            // remove captured vm if exists
            if (captured != null)
            {
                var victimVM = Pieces.FirstOrDefault(p => p.X==targetX && p.Y==targetY);
                if (victimVM != null) Pieces.Remove(victimVM);
            }
            SelectedPiece.X = targetX; SelectedPiece.Y = targetY;
            SelectedPiece = null; LegalMoves = new List<(int x,int y)>();
            LastActionMessage = "Di chuyển thành công";
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
            DebugInfo = $"Detect:{status} X:{GridXs?.Count ?? 0} Y:{GridYs?.Count ?? 0}\nLumTh:{LineLumThreshold} FracTh:{LineFractionThreshold:0.00}\nCellW:{cellW:0.0} CellH:{cellH:0.0}\nGridYOffsetCells:{GridYOffsetCells} ({yOffsetPx:0.0}px)\nPieceScale:{PieceScale:0.00}\nActive:{CoreBoard.ActivePlayer}\nLast:{LastActionMessage}\nKeys: G grid, M markers, F3 debug, +/- lum, D frac+, R frac-, Shift+Y offset, Shift+Plus/- piece size";
            if (SelectedPiece != null)
            {
                DebugInfo += $"\nSelected: {SelectedPiece.Code} {(SelectedPiece.IsRed?"Red":"Blue")} @ {SelectedPiece.X},{SelectedPiece.Y} Moves:{LegalMoves.Count}";
            }
        }
    }
}
