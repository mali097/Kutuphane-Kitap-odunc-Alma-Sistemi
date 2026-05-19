using LibrarySystem.UI.Models;
using Microsoft.Maui.Controls.Shapes;

namespace LibrarySystem.UI.Helpers;

public static class CategoryUiHelper
{
    public static void FillTwoColumnGrid(
        Grid grid,
        IReadOnlyList<BookCategoryItem> categories,
        Func<BookCategoryItem, Task> onCategoryTapped)
    {
        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnSpacing = 10;
        grid.RowSpacing = 10;

        for (var i = 0; i < categories.Count; i++)
        {
            var row = i / 2;
            var col = i % 2;

            while (grid.RowDefinitions.Count <= row)
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var card = BuildCategoryCard(categories[i], onCategoryTapped);
            Grid.SetRow(card, row);
            Grid.SetColumn(card, col);
            grid.Children.Add(card);
        }
    }

    public static View BuildCategoryCard(
        BookCategoryItem category,
        Func<BookCategoryItem, Task> onCategoryTapped)
    {
        var card = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E8E4DC"),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(10, 8),
            Margin = new Thickness(0, 0, 0, 0)
        };

        var iconBox = new Border
        {
            WidthRequest = 40,
            HeightRequest = 40,
            BackgroundColor = category.IconBackground,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            VerticalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = category.Icon,
                FontSize = 18,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        var nameLabel = new Label
        {
            Text = category.Name,
            FontAttributes = FontAttributes.Bold,
            FontSize = 13,
            TextColor = Color.FromArgb("#1B1530"),
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 2
        };

        var countLabel = new Label
        {
            Text = category.BookCountText,
            FontSize = 11,
            TextColor = Color.FromArgb("#6B6578")
        };

        var textStack = new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children = { nameLabel, countLabel }
        };

        var chevron = new Label
        {
            Text = "›",
            FontSize = 18,
            TextColor = Color.FromArgb("#B0A8C8"),
            VerticalOptions = LayoutOptions.Center
        };

        var layout = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8,
            Children = { iconBox, textStack, chevron }
        };
        Grid.SetColumn(textStack, 1);
        Grid.SetColumn(chevron, 2);

        card.Content = layout;

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await onCategoryTapped(category);
        card.GestureRecognizers.Add(tap);

        return card;
    }
}
