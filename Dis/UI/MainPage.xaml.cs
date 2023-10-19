using Newtonsoft.Json;

namespace UI;

public partial class MainPage : ContentPage
{
    public MainPage()
	{
		InitializeComponent();

        try
        {
            File.Open("Foods.dat", FileMode.Open);
            EAPageBtn.IsEnabled = true;
        }
        catch (FileNotFoundException)
        {
            MainSubTitle.Text = "Please start by adding some foods.";
            EAPageBtn.IsEnabled = false;
        }
        catch (UnauthorizedAccessException)
        {
            MainSubTitle.Text =
                "Note: Detected that this program doesn't have read permissions in the current folder. " +
                "Saved data will not be accessible.";
            EAPageBtn.IsEnabled = false;
        }
	}

    private async void OnEAPageClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new EAPage());
    }

    private async void OnFoodPageClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new FoodOptions());
    }

    private async void OnFoodPricePageClicked(object sender, EventArgs e)
    {
    }


    private async void OnFoodEnjoymentPageClicked(object sender, EventArgs e)
    {
    }

    private void OnCloseProgram(object sender, EventArgs e)
    {
        Application.Current.Quit();
    }
}