using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Labb1_NLPQnA
{
    class Program
    {
        private static string qnaEndpoint;
        private static string qnaKey;
        private static string translatorEndpoint;
        private static string translatorKey;
        private static string translatorRegion;

        static async Task Main(string[] args)
        {
            // Laddar konfiguration från appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // Hämtar inställningar
            qnaEndpoint = config["QnA:Endpoint"];
            qnaKey = config["QnA:Key"];
            translatorEndpoint = config["Translator:Endpoint"];
            translatorKey = config["Translator:Key"];
            translatorRegion = config["Translator:Region"];

            Console.WriteLine("Ask a question about AI:");

            string userQuestion;

            while (true)
            {
                // Läser in användarens fråga
                userQuestion = Console.ReadLine();

                // Kontrollerar om användaren vill avsluta programmet
                if (userQuestion.ToLower() == "quit")
                {
                    break;
                }

                // Hämtar svar från QnA-tjänsten
                var answer = await GetAnswerFromQ(userQuestion);

                // Visar svaret
                Console.WriteLine($"Answer: {answer}");

                // Frågar användaren om de vill översätta svaret
                Console.WriteLine("Do you want to translate this answer? (type 'translate' to translate or 'ask' to ask another question, 'quit' to end):");
                string action = Console.ReadLine();

                //Om använderen väljer translate 
                if (action.ToLower() == "translate")
                {
                    Console.WriteLine("Which language do you want to translate to? (type 'fr' for French, 'es' for Spanish or 'de' for German):");
                    string languageChoice = Console.ReadLine();

                    string translatedAnswer = languageChoice.ToLower() switch
                    {
                        "fr" => await TranslateText(answer, "fr"),
                        "es" => await TranslateText(answer, "es"),
                        "de" => await TranslateText(answer, "de"),
                        _ => "Invalid language choice."
                    };

                    // Skriver ut översättningen beroende på vilket språk användaren valt
                    Console.WriteLine($"Translated Answer: {translatedAnswer}");

                    // Vänta på att användaren trycker på "Enter" för att fortsätta
                    Console.WriteLine("Press Enter to ask another question...");
                    Console.ReadLine(); // Väntar på att användaren trycker Enter
                    Console.WriteLine("Ask a new question about AI:");

                }
                // Om användaren väljer ask
                else if (action.ToLower() == "ask")
                {
                    // Programmet fortsätter
                    continue;
                }
                // Om användaren väljer quit
                else if (action.ToLower() == "quit")
                {
                    Console.WriteLine("Program ended!");
                    // Programmet avslutas
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid Option. Please, Choose 'translate', 'ask' or quit'.");
                }
            }

            static async Task<string> GetAnswerFromQ(string question)
            {
                // Skapar en HTTP-klient
                using (HttpClient client = new HttpClient())
                {
                    // Lägger till en HTTP-header för att autentisera begäran med en API-nyckel
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", qnaKey);
                    // Specificerar att klienten förväntar sig ett svar i JSON-format från servern
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var requestBody = new
                    {
                        question = question
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(qnaEndpoint, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(jsonResponse);
                        return data.answers[0].answer;
                    }
                    else
                    {
                        return "Error: " + response.ReasonPhrase;
                    }
                }
            }

            static async Task<string> TranslateText(string text, string language)
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", translatorKey);
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", translatorRegion);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Skapar request body i korrekt format
                    var requestBody = new[]
                    {
                    new { Text = text }
                };

                    var content = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");

                    // Skickar begäran
                    var response = await client.PostAsync($"{translatorEndpoint}&to={language}", content);

                    // Hanterar svaret
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(jsonResponse);
                        return data[0].translations[0].text;
                    }
                    else
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Translation failed. Status code: {response.StatusCode}, Error: {errorResponse}");
                        return "Translation Error";
                    }
                }
            }
        }
    }
}