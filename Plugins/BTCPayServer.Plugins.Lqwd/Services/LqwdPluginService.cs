using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BTCPayServer.Plugins.Lqwd.Data;
using BTCPayServer.Plugins.Lqwd.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using BTCPayServer.Services.Stores;
using BTCPayServer.Services.Wallets;
using BTCPayServer.Payments.Lightning;
using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Data;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Lightning;
using BTCPayServer.Payments;
using BTCPayServer.Configuration;
using BTCPayServer.Services;
using BTCPayServer.Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Linq;
using Newtonsoft.Json.Linq;
using BTCPayServer.Lightning.LND;
using BTCPayServer.Lightning;
using NBitcoin;


namespace BTCPayServer.Plugins.Lqwd.Services;

public class LqwdPluginService
{
    private readonly ILogger<LqwdPluginService> _logger;
    private readonly HttpClient _httpClient;
    private readonly LqwdPluginDbContextFactory _pluginDbContextFactory;
    private readonly StoreRepository _storeRepository;
    private readonly PaymentMethodHandlerDictionary _paymentMethodHandlerDictionary;

    // private readonly BTCPayWalletProvider _walletProvider;
    // private readonly IAuthorizationService _authorizationService;
    // private readonly EventAggregator _eventAggregator;
    // private readonly InvoiceRepository _invoiceRepository;

    private readonly BTCPayServerEnvironment _env;
    private readonly BTCPayNetwork _network;
    private readonly IServiceProvider _serviceProvider;

    private readonly IConfiguration _configuration;
    private readonly IOptions<LightningNetworkOptions> _lightningNetworkOptions;

    private readonly LightningClientFactoryService _lightningClientFactory;


    public LqwdPluginService(HttpClient httpClient, StoreRepository storeRepository
    , BTCPayNetworkProvider btcPayNetworkProvider
    , BTCPayServerEnvironment env
    , LqwdPluginDbContextFactory pluginDbContextFactory
    , IConfiguration configuration
    , LightningClientFactoryService lightningClientFactory
    , ILogger<LqwdPluginService> logger
    , IServiceProvider serviceProvider
    , IOptions<LightningNetworkOptions> lightningNetworkOptions
    , PaymentMethodHandlerDictionary paymentMethodHandlerDictionary)
    {
        _logger = logger;
        _env = env;
        _configuration = configuration;
        _network = btcPayNetworkProvider.BTC;
        _httpClient = httpClient;
        _pluginDbContextFactory = pluginDbContextFactory;
        _lightningClientFactory = lightningClientFactory;
        _storeRepository = storeRepository;
        _paymentMethodHandlerDictionary = paymentMethodHandlerDictionary;
        _serviceProvider = serviceProvider;
        _lightningNetworkOptions = lightningNetworkOptions;
    }

    public string? GetLspsUrlFromEnv()
    {
        var envValue = Environment.GetEnvironmentVariable("BTCPAY_LQWDLSPS");
        if (!string.IsNullOrEmpty(envValue))
        {
            _logger.LogInformation($"[LQWD] Using LSP URL from ENV: {envValue}");
            return envValue.TrimEnd('/');
        }
        return null;
    }

    public async Task<string?> GetActiveLspsUrl()
    {
        // Priority 1: Command-line or environment override
        var overrideUrl = GetLspsUrlFromEnv();
        if (!string.IsNullOrEmpty(overrideUrl))
        {
            return overrideUrl;
        }

        // Fallback: use logic based on network
        var network = _configuration.GetValue<string>("network")?.ToLowerInvariant() ?? "mainnet";

        string activeLspLink = network switch
        {
            "mutinynet" => "https://btcpay-mutinynet.lqwd.tech",
            "regtest" => "http://localhost:9000",
            "testnet" => "https://lsp-testnet.lqwd.tech",
            "mainnet" => "https://btcpay.lqwd.tech",
            _ => throw new Exception($"Unsupported network: {network}")
        };

        _logger.LogInformation($"[LQWD] Active LSPS URL for network '{network}': {activeLspLink}");

        return activeLspLink.TrimEnd('/');
    }



    public async Task<List<PubKey>> GetActiveLspPubKeys()
    {
        var network = _configuration.GetValue<string>("network")?.ToLowerInvariant() ?? "mainnet";

        var pubKeys = new List<PubKey>();

        var pubKey = network switch
        {
            "mutinynet" => "0220566172d9e324b41ec6f74ca44d377d3faf72ddb310fd263e6d5bcde4882492", // example
            "regtest" => "0220566172d9e324b41ec6f74ca44d377d3faf72ddb310fd263e6d5bcde4882492",
            "testnet" => "0220566172d9e324b41ec6f74ca44d377d3faf72ddb310fd263e6d5bcde4882492",
            "mainnet" => "02cc611df622184f8c23639cf51b75001c07af0731f5569e87474ba3cc44f079ee",
            _ => throw new Exception($"Unsupported network: {network}")
        };

        pubKeys.Add(new PubKey(pubKey));
        return pubKeys;
    }




    public async Task<string> FetchLiveApiData()
    {
        _logger.LogInformation("FetchLiveApiData Lqwd Plugin");
        //for now, just to make sure we always get the latest value, we just fetch from database everytime a request arives
        //TODO: store in a variable and update only when changed. 
        var url = await GetActiveLspsUrl();
        if (string.IsNullOrEmpty(url))
            return "{}";
        try
        {
            var response = await _httpClient.GetStringAsync($"{url}/api/v1/get_info");
            return response;
        }
        catch
        {
            return "{}"; // Return empty JSON on error
        }
    }

    public async Task<bool> IsLspsConnected(string storeId)
    {
        // Step 1: Get your own node's pubkey
        var myPubkey = await GetNodePubKey(storeId);
        if (string.IsNullOrEmpty(myPubkey))
        {
            Console.WriteLine("Failed to fetch node pubkey");
            return false;
        }

        // Step 2: Get LSP URL
        var lspsUrl = await GetActiveLspsUrl();
        if (string.IsNullOrEmpty(lspsUrl))
        {
            Console.WriteLine("LSP URL is missing");
            return false;
        }

        // Step 3: Make GET request to /api/v1/check_peer?pubkey=<your pubkey>
        try
        {
            var fullUrl = $"{lspsUrl}/api/v1/check_peer?pubkey={myPubkey}";
            Console.WriteLine($"Calling: {fullUrl}");

            var response = await _httpClient.GetAsync(fullUrl);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Dictionary<string, bool>>(responseJson);

            if (result != null && result.TryGetValue("connected", out var isConnected))
            {
                Console.WriteLine($"Connected to LSP? {isConnected}");
                return isConnected;
            }

            Console.WriteLine("Invalid response structure");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking LSP connection: {ex.Message}");
            return false;
        }
    }


    public async Task<string?> GetNodePubKey(string storeId)
    {
        _logger.LogInformation("[LQWD] Starting GetNodePubKey for store: {StoreId}", storeId);

        var lightningClient = await GetMasterLightningClient(storeId);
        if (lightningClient == null)
        {
            _logger.LogWarning("[LQWD] GetMasterLightningClient returned null for store: {StoreId}", storeId);
            return null;
        }

        var url = await GetActiveLspsUrl();
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("[LQWD] GetActiveLspsUrl returned null or empty.");
            return null;
        }

        try
        {
            _logger.LogInformation("[LQWD] Creating invoice to extract pubkey");
            var invoice = await lightningClient.CreateInvoice(new CreateInvoiceParams(
                amount: 1,
                description: "get pubkey",
                expiry: TimeSpan.FromSeconds(60)
            ));

            var bolt11 = invoice.BOLT11;
            _logger.LogInformation("[LQWD] Created invoice: {Bolt11}", bolt11);

            var requestBody = JsonConvert.SerializeObject(new { invoice = bolt11 });
            _logger.LogDebug("[LQWD] Sending decode request to LSPS: {Url}/api/v1/decode, body: {Body}", url, requestBody);

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{url}/api/v1/decode", content);

            _logger.LogInformation("[LQWD] Decode API status code: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("[LQWD] Decode API response: {ResponseJson}", responseJson);

            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseJson);

            if (result == null)
            {
                _logger.LogWarning("[LQWD] Decode API response is null after deserialization.");
                return null;
            }

            if (result.TryGetValue("payee_pubkey", out var pubkey))
            {
                _logger.LogInformation("[LQWD] Extracted pubkey from LSPS: {PubKey}", pubkey);
                return pubkey;
            }
            else
            {
                _logger.LogWarning("[LQWD] 'payee_pubkey' not found in LSPS decode response.");
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LQWD] Exception occurred while getting pubkey for store: {StoreId}", storeId);
            return null;
        }
    }






    public async Task<ILightningClient?> GetMasterLightningClient(string storeId)
    {
        var store = await _storeRepository.FindStore(storeId);
        if (store is null)
        {
            return null;
        }

        var pmi = PaymentTypes.LN.GetPaymentMethodId(_network.CryptoCode);
        var lightningConnectionString = store.GetPaymentMethodConfig<LightningPaymentMethodConfig>(pmi, _paymentMethodHandlerDictionary)
            ?.CreateLightningClient(_network,
                _lightningNetworkOptions.Value, _serviceProvider.GetService<LightningClientFactoryService>());

        return lightningConnectionString;
    }

    public async Task<List<string>> ConnectToActiveLspsUris(string storeId, CancellationToken cancellationToken = default)
    {
        var responses = new List<string>();
        var url = await GetActiveLspsUrl();
        if (string.IsNullOrEmpty(url))
        {
            responses.Add("LSPS URL not found.");
            return responses;
        }

        string response;
        try
        {
            response = await _httpClient.GetStringAsync($"{url}/api/v1/get_info", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch LSPS info");
            responses.Add("Failed to fetch LSPS info.");
            return responses;
        }

        JObject parsed;
        try
        {
            parsed = JObject.Parse(response);
        }
        catch
        {
            responses.Add("Invalid JSON returned from LSPS");
            return responses;
        }

        var uris = parsed["uris"]?.ToObject<string[]>();
        if (uris is null || uris.Length == 0)
        {
            responses.Add("No URIs found in LSPS info.");
            return responses;
        }

        var lightningClient = await GetMasterLightningClient(storeId);
        if (lightningClient is null)
        {
            responses.Add("LND client not available for store.");
            return responses;
        }

        foreach (var uri in uris)
        {
            if (!NodeInfo.TryParse(uri, out var nodeInfo))
            {
                responses.Add($"Invalid node URI format: {uri}");
                continue;
            }
            var result = await lightningClient.ConnectTo(nodeInfo, cancellationToken);
            switch (result)
            {
                case ConnectionResult.Ok:
                    responses.Add($"Connected to {uri}");
                    break;
                case ConnectionResult.CouldNotConnect:
                    responses.Add($"Could not connect to {uri}");
                    break;
                default:
                    responses.Add($"Unknown connection result for {uri}");
                    break;
            }
        }

        return responses;
    }

    public async Task ReconnectToHardcodedLsps()
    {
        var stores = await _storeRepository.GetStores(); // IEnumerable<StoreData>

        foreach (var store in stores)
        {
            var storeId = store.Id;
            ConnectToActiveLspsUris(storeId);
        }
    }


    public async Task<string> GetLndInfoAsJson(string storeId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetLndInfoAsJson Lqwd Plugin");
        var lightningClient = await GetMasterLightningClient(storeId);
        if (lightningClient is null)
        {
            return JsonConvert.SerializeObject(new { error = "LND client not available for store." });
        }

        var info = await lightningClient.GetInfo(cancellationToken);

        var result = new LightningNodeInformationData
        {
            BlockHeight = info.BlockHeight,
            NodeURIs = info.NodeInfoList.ToArray(),
            Alias = info.Alias,
            Color = info.Color,
            Version = info.Version,
            PeersCount = info.PeersCount,
            ActiveChannelsCount = info.ActiveChannelsCount,
            InactiveChannelsCount = info.InactiveChannelsCount,
            PendingChannelsCount = info.PendingChannelsCount
        };

        return JsonConvert.SerializeObject(result, Formatting.Indented); // ← formatted JSON
    }

    public async Task<LightningChannel[]> GetChannels(string storeId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetChannelsAsJson Lqwd Plugin");

        var lightningClient = await GetMasterLightningClient(storeId);
        if (lightningClient is null)
        {
            return null;
        }

        var channels = await lightningClient.ListChannels(cancellationToken);

        return channels;
    }

    public async Task<string> TestData(string storeId)
    {
        _logger.LogInformation("TestData Lqwd Plugin");
        var store = await _storeRepository.FindStore(storeId);
        if (store is null)
        {
            return "";
        }

        return store.StoreName;
    }


    public async Task<OrderData?> CreateOrder(string storeId, OrderRequest orderRequest)
    {
        _logger.LogInformation("Creating new order with input: {@OrderRequest}", orderRequest);
        var url = await GetActiveLspsUrl();
        if (string.IsNullOrEmpty(url))
            return null;

        try
        {
            var requestBody = JsonConvert.SerializeObject(orderRequest);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{url}/api/v1/create_order", content);

            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("LQWD API returned error {StatusCode}: {Response}", response.StatusCode, responseString);
                return null; // or throw new Exception("...")
            }

            _logger.LogDebug("LQWD API response: {ResponseString}", responseString);

            var responseData = JsonConvert.DeserializeObject<CreateOrderResponse>(responseString);
            if (responseData == null)
            {
                _logger.LogWarning("Deserialization returned null for response: {responseString}", responseString);
                return null;
            }

            var orderData = new OrderData
            {
                OrderId = responseData.OrderId,
                StoreId = storeId, // Fill in as needed
                LspBalanceSat = responseData.LspBalanceSat,
                ClientBalanceSat = responseData.ClientBalanceSat,
                RequiredChannelConfirmations = responseData.RequiredChannelConfirmations,
                FundingConfirmsWithinBlocks = responseData.FundingConfirmsWithinBlocks,
                ChannelExpiryBlocks = responseData.ChannelExpiryBlocks,
                Token = responseData.Token,
                RefundOnchainAddress = orderRequest.RefundOnchainAddress,
                AnnounceChannel = responseData.AnnounceChannel,
                CreatedAt = responseData.CreatedAt,
                ExpiresAt = responseData.ExpiresAt,
                OrderState = responseData.OrderState,
                Payment = new BTCPayServer.Plugins.Lqwd.Data.PaymentData
                {
                    OrderId = responseData.OrderId,
                    Bolt11 = new Bolt11PaymentData
                    {
                        State = responseData.Payment?.Bolt11?.State,
                        ExpiresAt = DateTimeOffset.Parse(responseData.Payment?.Bolt11?.ExpiresAt ?? responseData.ExpiresAt.ToString()),
                        FeeTotalSat = long.Parse(responseData.Payment?.Bolt11?.FeeTotalSat ?? "0"),
                        OrderTotalSat = long.Parse(responseData.Payment?.Bolt11?.OrderTotalSat ?? "0"),
                        Invoice = responseData.Payment?.Bolt11?.Invoice
                    },
                    Onchain = new OnchainPaymentData
                    {
                        State = responseData.Payment?.Onchain?.State,
                        ExpiresAt = DateTimeOffset.Parse(responseData.Payment?.Onchain?.ExpiresAt ?? responseData.ExpiresAt.ToString()),
                        FeeTotalSat = long.Parse(responseData.Payment?.Onchain?.FeeTotalSat ?? "0"),
                        OrderTotalSat = long.Parse(responseData.Payment?.Onchain?.OrderTotalSat ?? "0"),
                        OnchainAddress = responseData.Payment?.Onchain?.OnchainAddress,
                        MinOnchainPaymentConfirmations = responseData.Payment?.Onchain?.MinOnchainPaymentConfirmations ?? 0,
                        MinFeeFor0Conf = responseData.Payment?.Onchain?.MinFeeFor0Conf ?? 0
                    }
                },
                Channel = new ChannelData
                {
                    FundedAt = responseData.Channel?.FundedAt ?? throw new JsonSerializationException("FundedAt missing in Channel data"),
                    FundingOutpoint = responseData.Channel?.FundingOutpoint ?? throw new JsonSerializationException("FundingOutpoint missing in Channel data"),
                    ExpiresAt = string.IsNullOrEmpty(responseData.Channel?.ExpiresAt) ? null : DateTimeOffset.Parse(responseData.Channel.ExpiresAt)
                }
            };

            await using var context = _pluginDbContextFactory.CreateContext();
            context.Orders.Add(orderData);
            await context.SaveChangesAsync();

            _logger.LogInformation("Order created successfully: {OrderId}", orderData.OrderId);

            return orderData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for public_key: {PublicKey}", orderRequest.PublicKey);
            return null; // or rethrow if you want
        }
    }



    public async Task<List<OrderData>> GetOrders(string storeId)
    {
        await using var context = _pluginDbContextFactory.CreateContext();

        return await context.Orders
            .Where(o => o.StoreId == storeId)
            .ToListAsync();
    }


    public async Task<OrderData?> FetchOrderStatus(string orderId)
    {
        _logger.LogInformation("Fetching order status for order ID: {OrderId}", orderId);
        var url = await GetActiveLspsUrl();
        if (string.IsNullOrEmpty(url))
            return null;

        try
        {
            _logger.LogInformation($"{url}/api/v1/get_order?order_id={orderId}");
            var response = await _httpClient.GetAsync($"{url}/api/v1/get_order?order_id={orderId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LQWD API returned non-success status {StatusCode} for order {OrderId}", response.StatusCode, orderId);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("LQWD get_order response: {Response}", responseContent);
            _logger.LogInformation("LQWD get_order response: {Response}", responseContent);

            // Deserialize API response (full structure expected, like CreateOrderResponse)
            var latestData = JsonConvert.DeserializeObject<CreateOrderResponse>(responseContent);
            if (latestData == null)
            {
                _logger.LogWarning("Deserialization returned null for order ID: {OrderId}", orderId);
                _logger.LogInformation("Deserialization returned null for order ID: {OrderId}", orderId);
                return null;
            }

            _logger.LogInformation($"Deserialized OrderId: {latestData.OrderId}");
            await LogAllOrdersAsync();

            await using var context = _pluginDbContextFactory.CreateContext();


            var existingOrder = await context.Orders
                .Include(o => o.Payment)
                .Include(o => o.Channel)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (existingOrder != null)
            {
                // Update relevant fields
                _logger.LogInformation("existingOrder: {existingOrder}", existingOrder);
                existingOrder.OrderState = latestData.OrderState;

                if (latestData.Payment?.Bolt11 != null)
                {
                    existingOrder.Payment ??= new BTCPayServer.Plugins.Lqwd.Data.PaymentData();
                    existingOrder.Payment.Bolt11 ??= new Bolt11PaymentData();
                    existingOrder.Payment.Bolt11.State = latestData.Payment.Bolt11.State;
                    existingOrder.Payment.Bolt11.ExpiresAt = DateTimeOffset.Parse(latestData.Payment.Bolt11.ExpiresAt);
                    existingOrder.Payment.Bolt11.FeeTotalSat = long.Parse(latestData.Payment.Bolt11.FeeTotalSat ?? "0");
                    existingOrder.Payment.Bolt11.OrderTotalSat = long.Parse(latestData.Payment.Bolt11.OrderTotalSat ?? "0");
                    existingOrder.Payment.Bolt11.Invoice = latestData.Payment.Bolt11.Invoice;
                }

                if (latestData.Payment?.Onchain != null)
                {
                    existingOrder.Payment ??= new BTCPayServer.Plugins.Lqwd.Data.PaymentData();
                    existingOrder.Payment.Onchain ??= new OnchainPaymentData();
                    existingOrder.Payment.Onchain.State = latestData.Payment.Onchain.State;
                    existingOrder.Payment.Onchain.ExpiresAt = DateTimeOffset.Parse(latestData.Payment.Onchain.ExpiresAt);
                    existingOrder.Payment.Onchain.FeeTotalSat = long.Parse(latestData.Payment.Onchain.FeeTotalSat ?? "0");
                    existingOrder.Payment.Onchain.OrderTotalSat = long.Parse(latestData.Payment.Onchain.OrderTotalSat ?? "0");
                    existingOrder.Payment.Onchain.OnchainAddress = latestData.Payment.Onchain.OnchainAddress;
                    existingOrder.Payment.Onchain.MinOnchainPaymentConfirmations = latestData.Payment.Onchain.MinOnchainPaymentConfirmations;
                    existingOrder.Payment.Onchain.MinFeeFor0Conf = latestData.Payment.Onchain.MinFeeFor0Conf;
                }

                if (latestData.Channel != null)
                {
                    existingOrder.Channel ??= new ChannelData();
                    existingOrder.Channel.FundedAt = latestData.Channel.FundedAt;
                    existingOrder.Channel.FundingOutpoint = latestData.Channel.FundingOutpoint;
                    existingOrder.Channel.ExpiresAt = string.IsNullOrEmpty(latestData.Channel.ExpiresAt)
                        ? null
                        : DateTimeOffset.Parse(latestData.Channel.ExpiresAt);
                }

                await context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} updated with latest state: {OrderState}", orderId, existingOrder.OrderState);
            }

            return existingOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching or updating order status for order ID: {OrderId}", orderId);
            return null;
        }
    }

    public async Task LogAllOrdersAsync()
    {
        await using var context = _pluginDbContextFactory.CreateContext();
        var orders = await context.Orders.ToListAsync();

        foreach (var order in orders)
        {
            Console.WriteLine($"OrderId: {order.OrderId}, State: {order.OrderState}, CreatedAt: {order.CreatedAt}");
        }

        if (!orders.Any())
        {
            Console.WriteLine("No orders found in the database.");
        }
    }

    public async Task<(List<PluginSettings> Settings, string ActiveLsps)> GetStoreSettingsWithDefaults(string storeId)
    {
        await using var context = _pluginDbContextFactory.CreateContext();
        var settings = await context.Settings
            .Where(s => s.StoreId == storeId)
            .ToListAsync();

        // Seed defaults if missing
        if (!settings.Any(s => s.Key == "active_lsps"))
        {
            var defaultLsps = new List<PluginSettings>
        {
            new() { Id = Guid.NewGuid().ToString(), StoreId = storeId, Key = "lsps_mutinynet", Value = "https://btcpay-mutinynet.lqwd.tech" },
            new() { Id = Guid.NewGuid().ToString(), StoreId = storeId, Key = "lsps_mainnet", Value = "https://btcpay.lqwd.tech" },
            new() { Id = Guid.NewGuid().ToString(), StoreId = storeId, Key = "active_lsps", Value = "lsps_mutinynet" }
        };

            context.Settings.AddRange(defaultLsps);
            await context.SaveChangesAsync();

            settings.AddRange(defaultLsps);
        }

        var activeLsps = settings.FirstOrDefault(s => s.Key == "active_lsps")?.Value;
        var filteredSettings = settings.Where(s => s.Key.StartsWith("lsps_")).ToList();

        return (filteredSettings, activeLsps!);
    }

    public async Task SetActiveLsps(string storeId, string newKey)
    {
        await using var context = _pluginDbContextFactory.CreateContext();
        var activeSetting = await context.Settings
            .FirstOrDefaultAsync(s => s.StoreId == storeId && s.Key == "active_lsps");

        if (activeSetting is null)
        {
            activeSetting = new PluginSettings
            {
                Id = Guid.NewGuid().ToString(),
                StoreId = storeId,
                Key = "active_lsps",
                Value = newKey
            };
            context.Settings.Add(activeSetting);
        }
        else
        {
            activeSetting.Value = newKey;
            context.Settings.Update(activeSetting);
        }

        await context.SaveChangesAsync();
    }

    public async Task<bool> DoesSettingExist(string storeId, string key)
    {
        await using var context = _pluginDbContextFactory.CreateContext();
        return await context.Settings.AnyAsync(s => s.StoreId == storeId && s.Key == key);
    }

    public async Task AddSetting(string storeId, string key, string value)
    {
        await using var context = _pluginDbContextFactory.CreateContext();
        var setting = new PluginSettings
        {
            Id = Guid.NewGuid().ToString(),
            StoreId = storeId,
            Key = key,
            Value = value
        };
        context.Settings.Add(setting);
        await context.SaveChangesAsync();
    }

    public async Task RemoveSetting(string storeId, string key)
    {
        await using var context = _pluginDbContextFactory.CreateContext();
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.StoreId == storeId && s.Key == key);

        if (setting != null)
        {
            context.Settings.Remove(setting);
            await context.SaveChangesAsync();
        }
    }




    public async Task AddTestDataRecord()
    {
        await using var context = _pluginDbContextFactory.CreateContext();

        await context.PluginRecords.AddAsync(new PluginData { Timestamp = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();
    }

    public async Task<List<PluginData>> Get()
    {
        await using var context = _pluginDbContextFactory.CreateContext();

        return await context.PluginRecords.ToListAsync();
    }
}

