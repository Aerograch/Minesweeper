using System.IO;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Сапер
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool gameStarted = false;
        private Cell[][] cells;
        public static Dictionary<string, BitmapImage> sourceImages;
        private System.Windows.Threading.DispatcherTimer gameTimer;
        public static int flagsPlaced = 0;
        public static int cellsLeft = 20 * 20;
        private Info infoWindow;
        private Author authorWindow;

        public const int bombAmount = 40;
        public MainWindow()
        {
            InitializeComponent();

            // Populate field with rows and columns
            for (int i = 0; i < 20; i++)
            {
                FieldGrid.RowDefinitions.Add(new RowDefinition());
                FieldGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            // Set bomb string
            SetBombString(bombAmount);

            // Precook images for optimisation
            sourceImages = new Dictionary<string, BitmapImage>();
            string[] files = Directory.GetFiles("Images", "*.png", SearchOption.TopDirectoryOnly);
            foreach (string filePath in files)
            {
                string imgName = filePath.Substring(7, filePath.Length-4-7);
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(System.IO.Path.GetFullPath(filePath));
                img.EndInit();
                sourceImages[imgName] = img;
            }

            // Generate blank field
            cells = new Cell[20][];
            PopulateGrid();

            // Initialise game timer for future
            gameTimer = new System.Windows.Threading.DispatcherTimer();
            gameTimer.Tick += IncrementGameTimer;
            gameTimer.Interval = new TimeSpan(0, 0, 1);

            // Init of additional windows
            infoWindow = new Info();
            infoWindow.Closed += OnInfoWindowClose;

            authorWindow = new Author();
            authorWindow.Closed += OnAuthorWindowClose;

        }

        private void ResetButtonOnCluck(object sender, RoutedEventArgs e)
        {
            GameReset();
        }

        private void CellOnRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as Cell).Flag();
            SetBombString(bombAmount - flagsPlaced);
        }

        private void CellOnLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!gameStarted)
                StartGame(sender as Cell);
            else
            {
                bool blowedUp = (sender as Cell).ProcessLeftClick();
                if (blowedUp) EnableOrDisableField(false);
            }
            if (cellsLeft == bombAmount)
            {
                ResetButton.Content = "You\nwin!";
                EnableOrDisableField(false);
                gameTimer.Stop();
            }
        }

        /// <summary>
        /// Resets whole game to initial state
        /// </summary>
        private void GameReset()
        {
            BombCountLabel.Content = "000";
            TimeElapsedLabel.Content = "000";
            flagsPlaced = 0;
            gameStarted = false;
            ResetButton.Content = "Reset";
            gameTimer.Stop();
            EnableOrDisableField(true);
            PopulateGrid();
        }

        /// <summary>
        /// Populates field with empty cells
        /// </summary>
        private void PopulateGrid()
        {
            for (int x = 0; x < 20; x++)
            {
                cells[x] = new Cell[20];
                for (int y = 0; y < 20; y++)
                {
                    Cell cell = new Cell(x, y, cells);
                    cell.PreviewMouseRightButtonUp += CellOnRightButtonUp;
                    cell.PreviewMouseLeftButtonUp += CellOnLeftButtonUp;
                    FieldGrid.Children.Add(cell);
                }
            }
        }

        /// <summary>
        /// Places mines randomly on a field. Does not guarantee a determenistic field
        /// </summary>
        private void GenerateField()
        {
            Random random = new Random();
            for (int i = 0; i < bombAmount; i++)
            {
                int x, y;
                do
                {
                    x = random.Next(0, 20);
                    y = random.Next(0, 20);
                }
                while (cells[x][y].IsBomb);
                cells[x][y].IsBomb = true;
            }
        }

        /// <summary>
        /// Starts game after first click on a cell
        /// </summary>
        /// <param name="startCell"></param>
        private void StartGame(Cell startCell)
        {
            // Set start cell as a bomb so GenerateField does not generate bomb on it
            startCell.IsBomb = true;
            GenerateField();
            startCell.IsBomb = false;
            startCell.ProcessLeftClick();
            gameStarted = true;
            gameTimer.Start();
        }

        private void IncrementGameTimer(Object source, EventArgs e)
        {
            string timeString = TimeElapsedLabel.Content.ToString().TrimStart("0").ToString();
            int time = timeString != "" ? int.Parse(timeString) : 0;
            time++;
            timeString = time.ToString();
            timeString = timeString.Substring(Math.Max(timeString.Length - 3, 0), Math.Min(timeString.Length, 3));
            while (timeString.Length < 3)
            {
                timeString = "0" + timeString;
            }
            TimeElapsedLabel.Content = timeString;
        }

        /// <summary>
        /// Sets bomb string in the correct format
        /// </summary>
        /// <param name="amount"></param>
        private void SetBombString(int amount)
        {
            string bombString = amount.ToString();
            bombString = bombString.Substring(Math.Max(bombString.Length - 3, 0), Math.Min(bombString.Length, 3));
            while (bombString.Length < 3)
            {
                bombString = "0" + bombString;
            }
            BombCountLabel.Content = bombString;
        }

        /// <summary>
        /// Enables or disables every cell in a field
        /// </summary>
        /// <param name="enable">Enables if true, disables if false</param>
        private void EnableOrDisableField(bool enable)
        {
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    cells[i][j].IsEnabled = enable;
                }
            }
        }

        private void InfoOptionOnClick(object sender, RoutedEventArgs e)
        {
            infoWindow.Show();
        }

        private void AuthorOptionOnClick(object sender, RoutedEventArgs e)
        {
            authorWindow.Show();
        }

        private void OnInfoWindowClose(object sender, EventArgs e)
        {
            infoWindow.Closed -= OnInfoWindowClose;
            infoWindow = new Info();
            infoWindow.Closed += OnInfoWindowClose;
        }

        private void OnAuthorWindowClose(object sender, EventArgs e)
        {
            authorWindow.Closed -= OnAuthorWindowClose;
            authorWindow = new Author();
            authorWindow.Closed += OnAuthorWindowClose;
        }
    }

    public class Cell : Button
    {
        private bool isRevealed = false;
        private int? amountOfBombsAround = null;
        private int x;
        private int y;
        private bool isFlagged = false;
        private Cell[][] cells;

        public Cell(int x, int y, Cell[][] cells) : base()
        {
            this.x = x;
            this.y = y;
            this.cells = cells;
            BorderThickness = new Thickness(0);
            Grid.SetColumn(this, x);
            Grid.SetRow(this, y);
            cells[x][y] = this;
            Content = x.ToString() + y.ToString();
            Image childImage = new Image();
            childImage.Source = MainWindow.sourceImages["EmptyUP"];
            Content = childImage;
        }

        public bool IsBomb { get; set; } = false;
        public bool IsFlagged { get { return isFlagged; } }
        /// <summary>
        /// Returns -1 if is a bomb
        /// </summary>
        public int? AmountOfBombsAround
        {
            get 
            { 
                return amountOfBombsAround != null ? amountOfBombsAround : CalculateBombsAround(); 
            } 
        }
        public int X { get { return x; } }
        public int Y { get { return y; } }

        /// <summary>
        /// Processes all the neccesary conditions that occur on left click
        /// </summary>
        /// <returns>True if is a bomb</returns>
        public bool ProcessLeftClick()
        {
            if (IsBomb && isRevealed)
                return true;

            if (isRevealed) return RevealNeighbours();

            if (amountOfBombsAround == 0)
            {
                RevealEmptyRecursive();
            }
            else
            {
                foreach(Cell cell in GetDirectNeighbours())
                {
                    if (cell.AmountOfBombsAround == 0)
                    {
                        cell.RevealEmptyRecursive();
                        break;
                    }
                }
            }

            return Reveal();
        }

        /// <summary>
        /// Reveals tile and assigns it corresponding image
        /// </summary>
        /// <returns>True if is a bomb</returns>
        private bool Reveal()
        {
            if (isRevealed) return IsBomb;
            isRevealed = true;

            if (IsBomb)
            {
                Content = new Image()
                {
                    Source = MainWindow.sourceImages["Bomb"]
                };
                return true;
            }

            int amount = (int)AmountOfBombsAround;
            string imgName = amount != 0 ? amount.ToString() : "EmptyDN";
            Content = new Image()
            {
                Source = MainWindow.sourceImages[imgName]
            };

            MainWindow.cellsLeft--;

            return false;
        }

        /// <summary>
        /// Reveals neiggbours if amount of flags equals cell's number
        /// </summary>
        private bool RevealNeighbours()
        {
            int amount = 0;

            foreach (Cell cell in GetIndirectNeighbours())
            {
                if (cell.IsFlagged) amount++;
            }

            bool blowedUp = false;

            if (amount == AmountOfBombsAround)
            {
                foreach(Cell cell in GetIndirectNeighbours())
                {
                    if (!cell.IsFlagged && !cell.isRevealed)
                    {
                        if (amountOfBombsAround == 0)
                        {
                            RevealEmptyRecursive();
                        }
                        else
                        {
                            foreach (Cell cell1 in GetDirectNeighbours())
                            {
                                if (cell1.AmountOfBombsAround == 0)
                                {
                                    cell1.RevealEmptyRecursive();
                                    break;
                                }
                            }
                        }
                        blowedUp = blowedUp | cell.Reveal();
                    }
                }
            }
            return blowedUp;
        }

        /// <summary>
        /// Recursive function that reveals all connected empty shells
        /// </summary>
        private void RevealEmptyRecursive()
        {
            if (isRevealed) return;
            Reveal();
            foreach(Cell cell in GetIndirectNeighbours())
            {
                if (cell.AmountOfBombsAround == 0)
                {
                    cell.RevealEmptyRecursive();
                }
                else if (!cell.IsBomb)
                {
                    cell.Reveal();
                }
            }
        }

        /// <summary>
        /// Toggles flag state if applicable
        /// </summary>
        public void Flag()
        {
            if (isRevealed) return;
            if (!isFlagged && MainWindow.flagsPlaced < MainWindow.bombAmount)
            {
                isFlagged = true;
                MainWindow.flagsPlaced++;
            }
            else
            {
                if (!isFlagged) return;
                isFlagged = false;
                MainWindow.flagsPlaced--;
            }
            if (isFlagged)
            {
                Content = new Image()
                {
                    Source = MainWindow.sourceImages["Flag"]
                };
            }
            else
            {
                Content = new Image()
                {
                    Source = MainWindow.sourceImages["EmptyUP"]
                };
            }
        }


        /// <summary>
        /// Searches bombs around current cell
        /// </summary>
        /// <returns></returns>
        private int CalculateBombsAround()
        {
            if (IsBomb)
            {
                amountOfBombsAround = -1;
                return -1;
            }
            int amount = 0;

            foreach(Cell cell in GetIndirectNeighbours())
            {
                if (cell.IsBomb) amount++;
            }

            amountOfBombsAround = amount;
            return amount;
        }

        /// <summary>
        /// Gets all direct neighbours
        /// Table of neghours, 1 = included 0 = not included
        /// 0 1 0
        /// 1 0 1
        /// 0 1 0
        /// </summary>
        /// <returns></returns>
        private List<Cell> GetDirectNeighbours()
        {
            List<Cell> output = new List<Cell>();
            if (x > 0)
                output.Add(cells[x - 1][y]);
            if (y > 0)
                output.Add(cells[x][y - 1]);
            if (x < cells.Length - 1)
                output.Add(cells[x + 1][y]);
            if (y < cells[x].Length - 1)
                output.Add(cells[x][y + 1]);

            return output;
        }

        /// <summary>
        /// Gets all direct and inderect neighbours
        /// Table of neghours, 1 = included, 0 = not included
        /// 1 1 1
        /// 1 0 1
        /// 1 1 1
        /// </summary>
        /// <returns></returns>
        private List<Cell> GetIndirectNeighbours()
        {
            List<Cell> output = new List<Cell>();
            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (i < 0 || i > cells.Length - 1 || j < 0 || j > cells[i].Length - 1 || (i == x && j == y)) continue;
                    output.Add(cells[i][j]);
                }
            }

            return output;
        }
    }
}
