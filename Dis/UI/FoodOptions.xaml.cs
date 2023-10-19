using EA;
using Microsoft.Maui.Controls.Internals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace UI;


public partial class FoodOptions : ContentPage
{
    public List<FoodData> FoodSearchResults { get; set; } = new();
    public List<FoodData> SelectedFoods { get; set; } = new();


    public FoodOptions()
	{
        InitializeComponent();

        BindingContext = this;
    }


    private async void FoodSearchBtn_Clicked(object sender, EventArgs e)
    {
        Evolution evo = new();

        string json = await evo.GetJson(FoodEntry.Text);
        Debug.WriteLine(json);

        JArray jFoods = (JArray)JObject.Parse(json)["foods"];
        List<FoodData> jFoodsObject = jFoods.ToObject<List<FoodData>>();

        foreach (FoodData food in jFoodsObject)
        {
            FoodSearchResults.Add(food);
        }
        FoodPicker.ItemsSource = FoodSearchResults.Select(o => o.Description).ToList();
    }

    private void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (FoodPicker.SelectedIndex >= FoodSearchResults.Count || FoodPicker.SelectedIndex < 0) return;

        FoodData food = FoodSearchResults[FoodPicker.SelectedIndex];

        string servingSize =
            food.ServingSize == 0 ? "" :
            $"\nServing Size: {food.ServingSize}{food.ServiceSizeUnit}";
        string brandOwner =
            food.BrandOwner == null ? "" :
            $"\nBrand Owner: {food.BrandOwner}";
        string ingredients =
            food.Ingredients == null ? "" :
            $"\nIngredients: {food.Ingredients}";

        string energyVal = "";

        foreach (var nutrient in food.FoodNutrients)
        {
            switch (nutrient.Name)
            {
                case "Energy":
                    energyVal = nutrient.Number;
                    break;
                case "Protein":
            }
        }

        string energy =
            energyVal == "" ? "" :
            $"\nCalories: {energyVal}";

        FoodInfoLabel.Text = $"""
            Desc: {food.Description}
            Date: {food.PublicationDate}
            Data Type: {food.DataType}
            """
            + servingSize
            + brandOwner
            + ingredients
            + $"""

            Calories: {food.FoodNutrients}
            """;

        SelectedFoods.Add(food);
        FoodList.ItemsSource = SelectedFoods.Select(o => o.Description).ToList();
    }
}