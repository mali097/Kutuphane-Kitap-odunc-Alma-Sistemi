namespace LibrarySystem.UI.Controls;

public partial class StarRatingView : ContentView
{
    public static readonly BindableProperty RatingProperty =
        BindableProperty.Create(
            nameof(Rating),
            typeof(decimal?),
            typeof(StarRatingView),
            null,
            propertyChanged: (bindable, _, _) => ((StarRatingView)bindable).RenderStars());

    public static readonly BindableProperty IsInteractiveProperty =
        BindableProperty.Create(
            nameof(IsInteractive),
            typeof(bool),
            typeof(StarRatingView),
            false,
            propertyChanged: (bindable, _, _) => ((StarRatingView)bindable).RenderStars());

    public static readonly BindableProperty StarSizeProperty =
        BindableProperty.Create(
            nameof(StarSize),
            typeof(double),
            typeof(StarRatingView),
            20d,
            propertyChanged: (bindable, _, _) => ((StarRatingView)bindable).RenderStars());

    public event EventHandler<decimal>? RatingChanged;

    public StarRatingView()
    {
        InitializeComponent();
        RenderStars();
    }

    public decimal? Rating
    {
        get => (decimal?)GetValue(RatingProperty);
        set => SetValue(RatingProperty, value);
    }

    public bool IsInteractive
    {
        get => (bool)GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    public double StarSize
    {
        get => (double)GetValue(StarSizeProperty);
        set => SetValue(StarSizeProperty, value);
    }

    private void RenderStars()
    {
        StarsContainer.Clear();

        var rating = Rating ?? 0m;
        for (var starIndex = 1; starIndex <= 5; starIndex++)
        {
            StarsContainer.Add(CreateStarView(starIndex, rating));
        }
    }

    private View CreateStarView(int starIndex, decimal currentRating)
    {
        var fill = GetStarFill(starIndex, currentRating);
        var starSize = StarSize;

        var container = new Grid
        {
            WidthRequest = starSize,
            HeightRequest = starSize,
            VerticalOptions = LayoutOptions.Center
        };

        container.Add(new Label
        {
            Text = "☆",
            FontSize = starSize * 0.88,
            TextColor = Color.FromArgb("#CFCFD6"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        });

        if (fill > 0)
        {
            var filledHost = new Grid
            {
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Fill,
                WidthRequest = starSize * (double)fill,
                InputTransparent = true,
                Clip = new Microsoft.Maui.Controls.Shapes.RectangleGeometry
                {
                    Rect = new Rect(0, 0, starSize * (double)fill, starSize)
                }
            };

            filledHost.Add(new Label
            {
                Text = "★",
                FontSize = starSize * 0.88,
                TextColor = Color.FromArgb("#FFB800"),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = starSize,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            });

            container.Add(filledHost);
        }

        if (IsInteractive)
        {
            var tapGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                }
            };

            tapGrid.Add(CreateTapZone(starIndex - 0.5m), 0, 0);
            tapGrid.Add(CreateTapZone(starIndex), 1, 0);
            container.Add(tapGrid);
        }

        return container;
    }

    private View CreateTapZone(decimal score)
    {
        var zone = new BoxView
        {
            Color = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => SetRating(score);
        zone.GestureRecognizers.Add(tap);
        return zone;
    }

    private void SetRating(decimal score)
    {
        if (!IsInteractive)
        {
            return;
        }

        Rating = score;
        RatingChanged?.Invoke(this, score);
    }

    private static decimal GetStarFill(int starIndex, decimal rating)
    {
        if (rating >= starIndex)
        {
            return 1m;
        }

        if (rating >= starIndex - 0.5m)
        {
            return 0.5m;
        }

        return 0m;
    }
}
