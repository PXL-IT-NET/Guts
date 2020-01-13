﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Guts.Client.Shared.Utility
{
    public class HttpClientToHttpHandlerAdapter : IHttpHandler
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public HttpClientToHttpHandlerAdapter(string apiBaseUrl)
        {
            if (!apiBaseUrl.EndsWith("/"))
            {
                apiBaseUrl += "/";
            }

            _apiBaseUrl = apiBaseUrl;

            _httpClient = new HttpClient { BaseAddress = new Uri(_apiBaseUrl), Timeout = TimeSpan.FromSeconds(6)};
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void UseBearerToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value)
        {
            return await _httpClient.PostAsJsonAsync(requestUri, value);
        }

        public async Task<TResponse> PostAsJsonAsync<TValue, TResponse>(string requestUri, TValue value)
        {
            var response = await _httpClient.PostAsJsonAsync(requestUri, value);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<TResponse>();
            }

            string message = await TryGetErrorMessageFromResponseAsync(response);
            throw new HttpResponseException(_apiBaseUrl + requestUri, response.StatusCode,message);
        }

        public async Task<T> GetAsJsonAsync<T>(string requestUri)
        {
            var response = await _httpClient.GetAsync(requestUri);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<T>();
            }

            string message = await TryGetErrorMessageFromResponseAsync(response);
            throw new HttpResponseException(_apiBaseUrl + requestUri, response.StatusCode, message);
        }

        private static async Task<string> TryGetErrorMessageFromResponseAsync(HttpResponseMessage response)
        {
            try
            {
                var builder = new StringBuilder();
                var messageDictionary = await response.Content.ReadAsAsync<Dictionary<string, string[]>>();
                foreach (var keyValuePair in messageDictionary)
                {
                    foreach (var submessage in keyValuePair.Value)
                    {
                        builder.AppendLine(submessage);
                    }
                }
                return builder.ToString();
            }
            catch (Exception)
            {
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}