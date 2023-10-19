using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EA;
using Microsoft.Maui.Controls.Internals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace UI;

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
