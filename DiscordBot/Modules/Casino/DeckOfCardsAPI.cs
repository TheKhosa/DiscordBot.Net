using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Casino
{
    /// <summary>
    /// Wrapper for DeckOfCardsAPI.com
    /// </summary>
    public class DeckOfCardsAPI
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://deckofcardsapi.com/api/deck";

        public DeckOfCardsAPI(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Create a new shuffled deck
        /// </summary>
        public async Task<string?> CreateNewDeckAsync(int deckCount = 1)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{BaseUrl}/new/shuffle/?deck_count={deckCount}");
                var json = JsonDocument.Parse(response);
                return json.RootElement.GetProperty("deck_id").GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating deck: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Shuffle an existing deck
        /// </summary>
        public async Task<bool> ShuffleDeckAsync(string deckId)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{BaseUrl}/{deckId}/shuffle/");
                var json = JsonDocument.Parse(response);
                return json.RootElement.GetProperty("success").GetBoolean();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error shuffling deck: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Draw cards from a deck
        /// </summary>
        public async Task<List<Card>?> DrawCardsAsync(string deckId, int count = 1)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{BaseUrl}/{deckId}/draw/?count={count}");
                var json = JsonDocument.Parse(response);
                var cards = new List<Card>();

                foreach (var cardElement in json.RootElement.GetProperty("cards").EnumerateArray())
                {
                    cards.Add(new Card
                    {
                        Code = cardElement.GetProperty("code").GetString() ?? "",
                        Value = cardElement.GetProperty("value").GetString() ?? "",
                        Suit = cardElement.GetProperty("suit").GetString() ?? "",
                        Image = cardElement.GetProperty("image").GetString() ?? ""
                    });
                }

                return cards;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error drawing cards: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get remaining cards in deck
        /// </summary>
        public async Task<int> GetRemainingCardsAsync(string deckId)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{BaseUrl}/{deckId}/");
                var json = JsonDocument.Parse(response);
                return json.RootElement.GetProperty("remaining").GetInt32();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Return cards to deck
        /// </summary>
        public async Task<bool> ReturnCardsToDeckAsync(string deckId, List<string> cardCodes)
        {
            try
            {
                var cards = string.Join(",", cardCodes);
                var response = await _httpClient.GetStringAsync($"{BaseUrl}/{deckId}/return/?cards={cards}");
                var json = JsonDocument.Parse(response);
                return json.RootElement.GetProperty("success").GetBoolean();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get numeric value of a card
        /// </summary>
        public static int GetCardValue(Card card, bool aceAsEleven = true)
        {
            return card.Value switch
            {
                "ACE" => aceAsEleven ? 11 : 1,
                "KING" or "QUEEN" or "JACK" => 10,
                _ => int.TryParse(card.Value, out int val) ? val : 0
            };
        }

        /// <summary>
        /// Get card emoji representation
        /// </summary>
        public static string GetCardEmoji(Card card)
        {
            var suitEmoji = card.Suit switch
            {
                "HEARTS" => "♥️",
                "DIAMONDS" => "♦️",
                "CLUBS" => "♣️",
                "SPADES" => "♠️",
                _ => ""
            };

            return $"{card.Value}{suitEmoji}";
        }
    }

    public class Card
    {
        public string Code { get; set; } = "";
        public string Value { get; set; } = "";
        public string Suit { get; set; } = "";
        public string Image { get; set; } = "";

        public override string ToString()
        {
            return DeckOfCardsAPI.GetCardEmoji(this);
        }
    }
}
