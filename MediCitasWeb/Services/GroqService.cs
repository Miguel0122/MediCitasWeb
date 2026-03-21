using MediCitasWeb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace MediCitasWeb.Services
{
    public class GroqService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GroqService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> ObtenerRespuestaAsync(string mensajeUsuario, List<ChatMensaje> historial = null)
        {
            try
            {
                var systemPrompt = @"Eres MediBot, asistente virtual de MediCitas. Responde de forma amigable y útil.
Información: Horario L-V 6am-6pm, Sáb 7am-2pm. Especialidades: Medicina General, Pediatría, Cardiología, Odontología.
Para agendar citas, indica que deben ir al menú principal.";

                var mensajes = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                if (historial != null && historial.Any())
                {
                    var ultimos = historial.OrderByDescending(m => m.fecha_envio).Take(10).Reverse();
                    foreach (var msg in ultimos)
                    {
                        mensajes.Add(new
                        {
                            role = msg.remitente == "user" ? "user" : "assistant",
                            content = msg.contenido
                        });
                    }
                }

                mensajes.Add(new { role = "user", content = mensajeUsuario });

                var request = new
                {
                    model = "llama3-70b-8192",
                    messages = mensajes,
                    temperature = 0.7,
                    max_tokens = 500
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Groq API error: {response.StatusCode}");

                var result = JObject.Parse(responseJson);
                return result["choices"][0]["message"]["content"].ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Groq error: {ex.Message}");
                return null;
            }
        }
    }
}