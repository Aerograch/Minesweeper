using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Сапер
{
    public partial class Info : Window
    {
        public Info()
        {
            InitializeComponent();
            MainLabel.Content = "Игра \"Сапер\" v1.0\n" +
                                "ЛКМ по клетке чтобы открыть клетку.\n" +
                                "ПКМ по клетке чтобы пометить её флагом\n" +
                                "Победа наступает когда останется 40 клеток\n" +
                                "Поражение наступает если открыть клетку с миной";
        }
    }
}
