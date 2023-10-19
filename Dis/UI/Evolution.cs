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
public class BitField32
{
    // 32 bit integer (32 fields)
    // Undefined behaviour if !(0 <= index <= 31)
    public int Data { get; private set; }

    // Set every bit to 0 by default
    public BitField32(int data = 0) { this.Data = data; }

    public void SetBit(int index, bool value)
    {
        int mask = 1 << index;
        Data = value ? (int)(Data | mask) : (int)(Data & ~mask);
    }

    public bool GetBit(int index)
    {
        int mask = 1 << index;
        return (Data & mask) != 0;
    }
}


public class Portion
{
    public float KCAL;

    /// <summary>
    /// In grams.
    /// </summary>
    public float Protein, Fat, Carbs;

    /// <summary>
    /// Per 100g portion.
    /// </summary>
    public float Saturates;
    public float Sugars;
}


public class Evolution
{
    public Portion ExtractProximates()
    {
        Portion portion = new();


        //https://stackoverflow.com/questions/3507498/reading-csv-files-using-c-sharp
        using (TextFieldParser parser = new("Proximates.csv"))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");

            int rowNum = 0;

            while (!parser.EndOfData)
            {
                // Process row
                if (rowNum < 3)
                    continue;

                string[] fields = parser.ReadFields();
                // Process each col
                for (int i = 0; i < fields.Length; i++)
                {
                    switch (i)
                    {
                        case 9:
                            portion.Protein = float.Parse(fields[i]);
                            break;
                        case 10:
                            portion.Fat = float.Parse(fields[i]);
                            break;
                        case 11:
                            portion.Carbs = float.Parse(fields[i]);
                            break;
                        case 12:
                            portion.KCAL = float.Parse(fields[i]);
                            break;
                    }
                }
                rowNum++;

                break;
            }
        }
        return portion;
    }
}
