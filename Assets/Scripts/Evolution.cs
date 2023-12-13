using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http;
using System.Net.Http.Headers;

using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;

namespace EA;

// 32-Bit Bit Field.

// Saves on memory usage for boolean values
// bool = 1 byte, but T/F can be stored in 1 bit.
// + BitField can store data 8x more memory efficiently.
// - Accessing a single value is more computationally expensive.
// + But performing bitwise operations negates this issue.
//public class BitField8
//{
//    // 8 bit integer - 8 fields
//    // Undefined behaviour if !(0 <= index <= 7)
//    public byte Data { get; private set; }

//    // Set every bit to 0 by default
//    public BitField8(byte data = 0) { Data = data; }

//    public void SetBit(int index, bool value)
//    {
//        byte mask = (byte)(1 << index);
//        Data = (byte)(value ? (Data | mask) : (Data & ~mask));
//    }

//    public bool GetBit(int index)
//    {
//        int mask = 1 << index;
//        return (Data & mask) != 0;
//    }
//}


public class Evolution
{
    private bool Vegetarian;
    private bool Pescatarian;
    private bool Vegan;
    private bool NoDairy;


    public Evolution(
        bool vegetarian = false,
        bool pescatarian = false,
        bool vegan = false,
        bool noDairy = false)
    {
        Vegetarian = vegetarian;
        Pescatarian = pescatarian;
        Vegan = vegan;
        NoDairy = noDairy;
    }


    private bool IsFoodGroupAllowed(string foodGroup)
    {
        // Categories
        switch (foodGroup[0])
        {
            case 'M': // Meat
                if (Vegetarian || Pescatarian || Vegan)
                    return false;
                break;
            case 'J': // Fish
                if (Vegetarian || Vegan)
                    return false;
                break;
            case 'C': // Eggs
                if (Vegan)
                    return false;
                break;
            case 'B': // Milk
                if (Vegan || NoDairy)
                    return false;
                break;
            case 'Q': // Alcohol - Excluded
                return false;
        }

        // Unique cases
        switch (foodGroup)
        {
            case "OB": // Animal fats
                if (Vegetarian || Pescatarian || Vegan)
                    return false;
                break;
        }

        return true;
    }


    public List<Portion> ExtractProximates()
    {
        int numMissingDataFields = 0;

        float GetProximate(string field, ref bool error)
        {
            if (field == "Tr" || field == "")
                return 0f;
            
            // Insufficient data; disregard
            if (field == "N")
            {
                numMissingDataFields++;
                error = true;
                return 0f;
            }

            return float.Parse(field);
        }

        //https://stackoverflow.com/questions/3507498/reading-csv-files-using-c-sharp
        using (TextFieldParser parser = new(@"C:\Git\dissertation\Dis\Evolution\Proximates.csv"))
        {
            List<Portion> portions = new();

            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");

            int rowNum = -1;

            while (!parser.EndOfData)
            {
                // Process row
                string[]? fields = parser.ReadFields();
                rowNum++;
                
                if (fields == null || rowNum < 3)
                {
                    continue;
                }

                bool missingData = false;
                bool dietaryConflict = false;

                string name = "Unnamed";
                float protein = 0;
                float fat = 0;
                float carbs = 0;
                float energy = 0;
                float saturates = 0;
                float sugars = 0;
                float trans = 0;

                // Process each col
                for (int i = 0; i < fields.Length; i++)
                {
                    switch (i)
                    {
                        case 1:
                            name = fields[i];
                            break;
                        case 3:
                            // Food groups
                            dietaryConflict = !IsFoodGroupAllowed(fields[i]);
                            break;
                        case 9:
                            protein = GetProximate(fields[i], ref missingData);
                            break;
                        case 10:
                            fat = GetProximate(fields[i], ref missingData);
                            break;
                        case 11:
                            carbs = GetProximate(fields[i], ref missingData);
                            break;
                        case 12:
                            energy = GetProximate(fields[i], ref missingData);
                            break;
                        case 16:
                            sugars = GetProximate(fields[i], ref missingData);
                            break;
                        case 27:
                            saturates = GetProximate(fields[i], ref missingData);
                            break;
                        case 45:
                            trans = GetProximate(fields[i], ref missingData);
                            break;
                        default:
                            continue;
                    }
                }

                if (!missingData && !dietaryConflict)
                {
                    Portion portion = new(name, energy, protein, fat, carbs, saturates, trans, sugars);
                    portions.Add(portion);
                }
            }

            Console.WriteLine(numMissingDataFields + " fields with missing data.");
            return portions;
        }
    }
}
