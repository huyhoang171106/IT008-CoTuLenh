using System;

namespace GameUI.ViewModels
{
    public class PieceViewModel : ViewModelBase
    {
        private int _x; private int _y; private string _code = string.Empty; private bool _isRed;
        public int X { get => _x; set => SetProperty(ref _x, value); }
        public int Y { get => _y; set => SetProperty(ref _y, value); }
        public string Code { get => _code; set => SetProperty(ref _code, value); }
        public bool IsRed { get => _isRed; set => SetProperty(ref _isRed, value); }
        public string ImageUri => GetImageUri();
        private string GetImageUri()
        {
            string color = IsRed ? "red" : "blue"; string baseUri = "pack://application:,,,/GameUI;component/Assets/pieces_png/";
            // asset files follow pattern <code>_<color>.png
            return Code switch
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
                "engineer" => baseUri + $"engineer_{color}.png",
                _ => baseUri + $"infantry_{color}.png"
            };
        }
    }
}
