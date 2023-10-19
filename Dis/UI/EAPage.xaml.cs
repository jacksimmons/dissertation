using EA;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace UI;

public partial class EAPage : ContentPage
{
    public ObservableCollection<FoodData> Foods { get; set; } = new();

    [Serializable]
    public class FoodData
    {
        [JsonProperty("fdcId")]
        public int FdcId { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("dataType")]
        public string DataType { get; set; }
        [JsonProperty("publishedDate")]
        public string PublicationDate { get; set; }
        [JsonProperty("brandOwner")]
        public string BrandOwner { get; set; }
        [JsonProperty("ingredients")]
        public string Ingredients { get; set; }

        [JsonProperty("servingSize")]
        public int ServingSize { get; set; }
        [JsonProperty("servingSizeUnit")]
        public string ServiceSizeUnit { get; set; }
        
        [JsonProperty("foodNutrients")]
        public NutrientData[] FoodNutrients { get; set; }

        public class NutrientData
        {
            [JsonProperty("number")]
            public string Number { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("amount")]
            public string Amount { get; set; }
            [JsonProperty("unitName")]
            public string UnitName { get; set; }
            [JsonProperty("derivationCode")]
            public string DerivationCode { get; set; }
            [JsonProperty("derivationDescription")]
            public string DerivationDescription { get; set; }
        }
    }

    public EAPage()
	{
		InitializeComponent();
    }

    private async void FoodSearchBtn_Clicked(object sender, EventArgs e)
    {
        Evolution evo = new Evolution();

        string json = await evo.GetJson(FoodEntry.Text);
        Debug.WriteLine(json);

        JArray jFoods = (JArray)JObject.Parse(json)["foods"];
        List<FoodData> jFoodsObject = jFoods.ToObject<List<FoodData>>();

        Foods.Clear();
        List<string> foodDescriptions = new();
        foreach (FoodData food in jFoodsObject)
        {
            Foods.Add(food);
            foodDescriptions.Add(food.Description);
        }

        FoodOptions.ItemsSource = foodDescriptions;
    }

    private void FoodOptions_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (FoodOptions.SelectedIndex >= Foods.Count || FoodOptions.SelectedIndex < 0) return;

        FoodData food = Foods[FoodOptions.SelectedIndex];

        string servingSize =
            food.ServingSize == 0 ? "" :
            $"\nServing Size: {food.ServingSize}{food.ServiceSizeUnit}";
        string brandOwner =
            food.BrandOwner == null ? "" :
            $"\nBrand Owner: {food.BrandOwner}";
        string ingredients =
            food.Ingredients == null ? "" :
            $"\nIngredients: {food.Ingredients}";

        FoodInfoLabel.Text = $"""
            Desc: {food.Description}
            Date: {food.PublicationDate}
            Data Type: {food.DataType}
            """
            + servingSize
            + brandOwner
            + ingredients
            + $"""

            Nutrients: {food.FoodNutrients}
            """;
    }
}