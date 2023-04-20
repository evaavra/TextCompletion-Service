using ConsoleApp1;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.VisualBasic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq.Expressions;
using System.Reflection.PortableExecutable;
using System.Security;
using System.Text;
using System.Text.Json;
using static iTextSharp.text.pdf.qrcode.Version;

using static System.Net.Mime.MediaTypeNames;

using static System.Net.WebRequestMethods;

using System.util;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Net.Http.Headers;

class PDFReader
{
    static void Main(string[] args)
    {
        string filePath = @"C:\Users\eva.avramidou\Desktop\cli\content\assets\a.useful\ITIL_Foundation_Manual_-_ITIL_4_Edition_(English).pdf";

        // Create a reader for the PDF file
        PdfReader reader = new PdfReader(filePath);
        string textall = string.Empty;
        // Extract the text from each page of the PDF file
        for (int page = 1; page <= reader.NumberOfPages; page++)
        {
            textall += PdfTextExtractor.GetTextFromPage(reader, page);
        }

        // Close the reader
        reader.Close();
        //Console.WriteLine(text);
        //string[] texts = textall.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        List<string> texts = new List<string>();
        int start = 0;
        for (int i = 0; i < textall.Length; i++)
        {
            if (i > 0 && i % 1000 == 0)
            {
                texts.Add(textall.Substring(start, 1000));
                start = 1000 + start;
            }
            if (textall.Length - start < 1000)
            {
                texts.Add(textall.Substring(start, textall.Length - start));
                break;
            }
        }

        var textWithoutBlankSpaces = texts[171].Replace("\n", "\\n").Replace("\u00AE", "\\u00AE");
        //Console.WriteLine(textWithoutBlankSpaces);
        var response = WordEmbeddings(textWithoutBlankSpaces).Result;

        //foreach (var embd in response.data.FirstOrDefault().embedding)
        //{
        //    Console.WriteLine(embd);
        //}

        UploadVectorsToPinecone(1, response.data.FirstOrDefault().embedding);

        Console.ReadLine();

    }

    public static async Task<Root> WordEmbeddings(string text)
    {
        try
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("authorization", "Bearer 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");
            client.Timeout = TimeSpan.FromMinutes(3);

            var content = new StringContent("{\"model\": \"text-embedding-ada-002\", \"input\": \"" + text + "\"}",
                Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync("https://api.openai.com/v1/embeddings", content).Result;
            string responseString = response.Content.ReadAsStringAsync().Result;
            var responseObject = JsonSerializer.Deserialize<Root>(responseString); //(Root)JsonConvert.DeserializeObject(responseString);

            return responseObject;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
        }
        return null;

    }

    public static async void UploadVectorsToPinecone(int i, List<double> emb)
    {
        var client = new HttpClient();
        var vectorObject = new VectorObject()
        {
            Values = emb,
            Id = i
        };
        var vectorList = new VectorsList();
        vectorList.Vectors.Add(vectorObject);

        //var vectorListString = vectorList.Vectors.JsonSerializer.

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://axeloschatbot-56a1186.svc.us-east4-gcp.pinecone.io/vectors/upsert"),
            Headers =
            {
                { "accept", "application/json" },
                { "Api-Key", "f909df8a-53aa-49b7-a25b-f428a9528215" },
            },
            Content = new StringContent()
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json")
                }
            }
        };
        using (var response = await client.SendAsync(request))
        {
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine(body);
        }
    }
}
